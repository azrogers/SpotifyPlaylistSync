using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuzzySharp;
using Soulseek;
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
        private string _downloadedFilename = null;

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
            var foundFile = await FindTrack(item, context);
            if (foundFile == null)
            {
                return new ResolveProviderResult(false, null);
            }

            Logger.Write($"Found {string.Join(", ", item.Artists)} - {item.Title} from user {foundFile.Username}");

            // sometimes extension is unset from soulseek despite the filename having an extension
            var ext = string.IsNullOrWhiteSpace(foundFile.File.Extension) ? Path.GetExtension(foundFile.File.Filename).Substring(1) : foundFile.File.Extension;
            var outputFilename = Path.Combine(context.Config.OutputDirectory ?? "", Util.MakeValidFileName($"{string.Join(", ", item.Artists)} - {item.Title}.{ext}"));

            // start tracking the download
            var progress = DownloadTracker.Start(foundFile.File.Filename, item);
            var options = new TransferOptions(
                disposeOutputStreamOnCompletion: true,
                stateChanged: progress.OnStateChanged,
                progressUpdated: progress.OnProgressUpdated);

            Logger.Write($"Downloading {string.Join(", ", item.Artists)} - {item.Title}");
            var transfer = await context.Client.DownloadAsync(
                username: foundFile.Username,
                remoteFilename: foundFile.File.Filename,
                outputStreamFactory: () => Task.FromResult(new FileStream(outputFilename, FileMode.Create) as Stream),
                size: foundFile.File.Size,
                options: options);

            DownloadTracker.Stop(foundFile.File.Filename);
            var downloadedFilename = Path.GetFileName(outputFilename);

            if (!transfer.State.HasFlag(TransferStates.Succeeded))
            {
                return new ResolveProviderResult(false, null);
			}

			Logger.Write($"Downloaded {downloadedFilename}");

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

            return new ResolveProviderResult(true, new SoulseekTrackProvider(item, downloadedFilename));
        }

        private static async Task<SoulseekFile?> FindTrack(PlaylistItem item, Context context)
        {
            // attempt various search queries
            var file = await FindTrack(item, context, $"{string.Join(' ', item.Artists)} {item.Title}");
            if (file != null)
            {
                return file;
            }

            file = await FindTrack(item, context, $"{string.Join(' ', item.Artists)} {item.TitleSanitized}");
            if (file != null)
            {
                return file;
            }

            file = await FindTrack(item, context, $"{string.Join(' ', item.Artists)} {item.Album}");
            if (file != null)
            {
                return file;
            }

            file = await FindTrack(item, context, $"{item.Artists.First()} {item.Title}");
            return file;
        }

        private static async Task<SoulseekFile?> FindTrack(PlaylistItem item, Context context, string query)
        {
            var responses = await context.Client.SearchAsync(SearchQuery.FromText(query));

            var candidates = new List<(SearchResponse, Soulseek.File)>();
            foreach (var res in responses.Responses)
            {
                foreach (var f in res.Files)
                {
                    // if the filename's a match and it's got the right extension, give it a try
                    if (MatchFilename(context, item, f.Filename))
                    {
                        var ext = string.IsNullOrWhiteSpace(f.Extension) ? Util.SubstringSafe(Path.GetExtension(f.Filename), 1) : f.Extension;
                        if (ExtensionsToDownload.Contains(ext.ToLower()))
                        {
                            candidates.Add((res, f));
                        }
                    }
                }
            }

            if (!candidates.Any())
            {
                return null;
            }

            var lenSeconds = item.LengthMs / 1000;
    
            // order first by the closest match by length,
            // then by whether or not they have a free slot/would put us in a queue
            // then by the top upload speed
            // then by the highest bitrate available
            var foundItem = candidates
                .OrderBy(c => Math.Abs((c.Item2.Length ?? lenSeconds) - lenSeconds))
                .ThenBy(c => c.Item1.HasFreeUploadSlot && c.Item1.QueueLength == 0 ? -1000 : c.Item1.QueueLength)
                .ThenByDescending(c => c.Item1.UploadSpeed)
                .ThenByDescending(c => c.Item2.BitRate)
                .FirstOrDefault();

            return new SoulseekFile(foundItem.Item2, foundItem.Item1.Username);
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
