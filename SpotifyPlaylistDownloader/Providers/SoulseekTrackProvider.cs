using ByteSizeLib;
using FuzzySharp;
using Soulseek;
using System.Diagnostics;
using TagLib;

namespace SpotifyPlaylistDownloader.Providers
{
    internal class SoulseekTrackProvider : ITrackProvider
    {
        private static readonly HashSet<string> ExtensionsToDownload = new HashSet<string>()
        {
            "mp3", "ogg", "oga", "aac", "wav", "flac", "m4a", "wma", "aiff"
        };

        private PlaylistItem _item;
        private string _downloadedFilename;

        public SoulseekTrackProvider(PlaylistItem item, string downloadedFilename)
        {
            _item = item;
            _downloadedFilename = downloadedFilename;
        }

        public PlaylistItemInfo? GetInfo()
        {
            return new PlaylistItemInfo()
            {
                Filename = _downloadedFilename ?? "",
                Title = $"{string.Join(", ", _item.Artists)} - {_item.Title}",
                Length = _item.LengthMs / 1000
            };
        }

        public static async Task<ResolveProviderResult> Resolve(PlaylistItem item, Context context)
        {
            var queries = FindTrack(item, context);
			var stopwatch = new Stopwatch();
            // don't keep trying to download the same file from the same user
            var filesChecked = new HashSet<string>();

			foreach (var query in queries)
            {
                var files = await query;
                foreach(var foundFile in files)
				{
                    var fileId = $"{foundFile.Username}:{foundFile.File.Filename}";
                    if(filesChecked.Contains(fileId))
                    {
                        continue;
                    }

                    filesChecked.Add(fileId);

					Logger.Write($"Found {string.Join(", ", item.Artists)} - {item.Title} from user {foundFile.Username}");

					// sometimes extension is unset from soulseek despite the filename having an extension
					var ext = foundFile.File.GetExtension();
                    var tempOutputFilename = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "." + ext);

					// start tracking the download
					var progress = DownloadTracker.Start(foundFile.File.Filename, item);
					var options = new TransferOptions(
						disposeOutputStreamOnCompletion: true,
						stateChanged: progress.OnStateChanged,
						progressUpdated: progress.OnProgressUpdated);

					Logger.Write($"Downloading {string.Join(", ", item.Artists)} - {item.Title}");

                    // calculate timeouts 
                    var timeoutSize = ByteSize.FromBytes(foundFile.File.Size).MegaBytes * context.Config.DownloadDurationTimeoutByMegabyte;
                    var timeout = Math.Min(timeoutSize, context.Config.DownloadDurationTimeout); 

					var downloadCancellationToken = new CancellationTokenSource();
                    stopwatch.Restart();

                    Transfer? transfer = null;
					var downloadedFilename = Util.MakeValidFileName($"{string.Join(", ", item.Artists)} - {item.Title}");

                    // start the download
                    var task = context.Client.DownloadAsync(
                        username: foundFile.Username,
                        remoteFilename: foundFile.File.Filename,
                        outputStreamFactory: () => Task.FromResult(new FileStream(tempOutputFilename, FileMode.Create) as Stream),
                        size: foundFile.File.Size,
                        cancellationToken: downloadCancellationToken.Token,
                        options: options);

                    // don't await the download - then we're reliant on it listening to its cancellation token (unreliable ime)
                    // instead, await a task that polls its completion, that we can cancel more easily
                    await Task.Run(() =>
                    {
                        while (!task.IsCompleted && !downloadCancellationToken.IsCancellationRequested)
                        {
                            var timeElapsed = stopwatch.Elapsed.TotalSeconds;
                            if (progress.BytesTransferred == 0 && timeElapsed > context.Config.DownloadInitiationTimeout)
                            {
                                Logger.Write($"Download for {downloadedFilename} cancelled - hit download initiation timeout");
                                downloadCancellationToken.Cancel();
                                break;
                            }
                            else if (timeElapsed > timeout)
                            {
                                Logger.Write($"Download for {downloadedFilename} cancelled - hit download timeout");
                                downloadCancellationToken.Cancel();
                                break;
                            }

                            Thread.Sleep(100);
                        }
                    });

                    try
					{
						transfer = task.IsCompleted ? task.Result : null;
					}
                    catch(Exception e)
                    {
                        Logger.Write($"{downloadedFilename} hit exception: {e.Message}");
                    }

					DownloadTracker.Stop(foundFile.File.Filename);

					if (transfer == null || !transfer.State.HasFlag(TransferStates.Succeeded))
					{
                        // try another file
                        Logger.Write($"Failed to download {downloadedFilename}");
                        continue;
					}

					Logger.Write($"Downloaded {downloadedFilename}");

                    var fileType = FileTypeDetector.Detect(tempOutputFilename);
                    if(fileType == FileTypeDetector.FileType.Unknown)
                    {
                        Logger.Write($"File {downloadedFilename} is of an unknown type - deleting and trying another");
                        System.IO.File.Delete(tempOutputFilename);
                        continue;
                    }

                    var newExt = fileType.ToString().ToLower();
                    if(newExt != ext)
                    {
                        Logger.Write($"{downloadedFilename} claimed to be {ext} but detected as a {newExt} file");
                    }

					var outputFilename = Path.Combine(context.Config.OutputDirectory ?? "", $"{downloadedFilename}.{newExt}");
                    System.IO.File.Move(tempOutputFilename, outputFilename);

					try
					{
						using (var tagFile = TagLib.File.Create(outputFilename))
						{
							tagFile.Tag.Album = item.Album;
							tagFile.Tag.Title = item.Title;
							tagFile.Tag.Track = (uint)item.TrackNumber;
							tagFile.Tag.Performers = item.Artists;
							tagFile.Save();
						}
					}
					catch (CorruptFileException e)
					{

					}

					Logger.Write($"Wrote tags for {downloadedFilename}");

					return new ResolveProviderResult(true, new SoulseekTrackProvider(item, $"{downloadedFilename}.{newExt}"));
				}
            }

            return new ResolveProviderResult(false, null);
        }

        /// <summary>
        /// Runs multiple queries to find the track.
        /// </summary>
        /// <returns>An iterator of tasks that each return the results of a single query. Each iteration will perform the next query.</returns>
        private static IEnumerable<Task<IEnumerable<SoulseekFile>>> FindTrack(PlaylistItem item, Context context)
        {
            // attempt various search queries
            yield return RunTrackQuery(item, context, $"{string.Join(' ', item.Artists)} {item.Title}");
            yield return RunTrackQuery(item, context, $"{string.Join(' ', item.Artists)} {item.TitleSanitized}");
            yield return RunTrackQuery(item, context, $"{string.Join(' ', item.Artists)} {item.Album}");
            yield return RunTrackQuery(item, context, $"{item.Artists.First()} {item.Title}");
        }

        /// <summary>
        /// Executes the given search query and returns all qualified candidates for this track that were found.
        /// </summary>
        private static async Task<IEnumerable<SoulseekFile>> RunTrackQuery(PlaylistItem item, Context context, string query)
        {
            var responses = await context.Client.SearchAsync(SearchQuery.FromText(query));

            var maxFileSize = ByteSize.FromKiloBytes((item.LengthMs / 1000) * context.Config.MaxKbsPerSecond);

            var candidates = new List<(SearchResponse Response, Soulseek.File File)>();
            foreach (var res in responses.Responses)
            {
                foreach (var f in res.Files)
                {
                    // if the filename's a match and it's got the right extension, give it a try
                    if (MatchFilename(context, item, f.Filename))
                    {
                        var ext = f.GetExtension();
                        if(ext == "mp3" && f.BitDepth < context.Config.MinMp3Bitrate)
                        {
                            continue;
                        }

                        if(ByteSize.FromBytes(f.Size) > maxFileSize)
                        {
                            continue;
                        }

                        if (ExtensionsToDownload.Contains(ext.ToLower()))
                        {
                            candidates.Add((res, f));
                        }
                    }
                }
            }

            if (!candidates.Any())
            {
                return new SoulseekFile[0];
            }

            var lenSeconds = item.LengthMs / 1000;

            // order first by the closest match by length,
            // then by whether or not they have a free slot/would put us in a queue
            // then by prefered or avoided formats
            // then by the top upload speed
            // then by the highest bitrate available
            var search = candidates
                .OrderBy(c => Math.Abs((c.File.Length ?? lenSeconds) - lenSeconds))
                .ThenBy(c => c.Response.HasFreeUploadSlot && c.Response.QueueLength == 0 ? -1000 : c.Response.QueueLength)
                .ThenBy(c =>
                {
                    var ext = c.File.GetExtension();

                    if(context.Config.PreferFormats.Contains(ext))
                    {
                        return 1;
                    }
                    else if(context.Config.AvoidFormats.Contains(ext))
                    {
                        return -1;
                    }

                    return 0;
				})
                .ThenByDescending(c => c.Response.UploadSpeed)
                .ThenByDescending(c => c.File.BitRate);

            return search.Select(c => new SoulseekFile(c.File, c.Response.Username));
        }


        private static bool MatchFilename(Context context, PlaylistItem item, string filename)
        {
            if (Fuzz.PartialRatio($"{item.TrackNumber} {item.Title}".ToLower(), filename.ToLower()) > context.Config.SoulseekTrackMatchThreshold)
            {
                return true;
            }

            if (Fuzz.PartialRatio(item.Title.ToLower(), filename.ToLower()) > context.Config.SoulseekTrackMatchThreshold)
            {
                return true;
            }

            if (Fuzz.PartialRatio($"{string.Join(", ", item.Artists)} - {item.Title}", filename.ToLower()) > context.Config.SoulseekTrackMatchThreshold)
            {
                return true;
            }

            if (Fuzz.PartialRatio($"{string.Join(", ", item.Artists)} - {item.Album} - {item.Title}", filename.ToLower()) > context.Config.SoulseekTrackAlbumMatchThreshold)
            {
                return true;
            }

            return false;
        }

        private class SoulseekFile
        {
            public string Username => _username;
            public Soulseek.File File => _file;

            private Soulseek.File _file;
            private string _username;

            public SoulseekFile(Soulseek.File file, string username)
            {
                _file = file;
                _username = username;
            }
        }
    }
}
