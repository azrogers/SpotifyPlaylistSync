using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FuzzySharp;
using Soulseek;
using TagLib;

namespace SpotifyPlaylistDownloader
{
	internal class SoulseekTrackProvider : ITrackProvider
	{
		private static readonly HashSet<string> ExtensionsToDownload = new HashSet<string>()
		{
			"mp3", "ogg", "oga", "aac", "wav", "flac", "m4a", "wma", "aiff"
		};

		private SoulseekClient _client;
		private PlaylistItem _item;
		private SoulseekFile? _foundFile = null;
		private string? _downloadedFilename = null;

		public SoulseekTrackProvider(SoulseekClient client, PlaylistItem item)
		{
			_client = client;
			_item = item;
		}

		public bool IsResolved() => _foundFile != null;

		public PlaylistItemInfo? GetInfo()
		{
			return new PlaylistItemInfo()
			{
				Filename = _downloadedFilename ?? "",
				Title = $"{string.Join(", ", _item.Artists)} - {_item.Title}",
				Length = _item.LengthMs / 1000
			};
		}

		public async Task<bool> Resolve(string outputDir)
		{
			_foundFile = await FindTrack();
			if(_foundFile != null)
			{
				Console.WriteLine($"Found {string.Join(", ", _item.Artists)} - {_item.Title} from user {_foundFile.Username}");
				
				var ext = string.IsNullOrWhiteSpace(_foundFile.File.Extension) ? Path.GetExtension(_foundFile.File.Filename).Substring(1) : _foundFile.File.Extension;
				var outputFilename = Path.Combine(outputDir, Util.MakeValidFileName($"{string.Join(", ", _item.Artists)} - {_item.Title}.{ext}"));

				var progress = DownloadTracker.Start(_foundFile.File.Filename);
				var options = new TransferOptions(
					disposeOutputStreamOnCompletion: true,
					stateChanged: progress.OnStateChanged,
					progressUpdated: progress.OnProgressUpdated);

				Console.WriteLine($"Downloading {string.Join(", ", _item.Artists)} - {_item.Title}");
				var transfer = await _client.DownloadAsync(
					username: _foundFile.Username,
					remoteFilename: _foundFile.File.Filename,
					outputStreamFactory: () => Task.FromResult(new FileStream(outputFilename, FileMode.Create) as Stream),
					size: _foundFile.File.Size,
					options: options);

				DownloadTracker.Stop(_foundFile.File.Filename);

				if(transfer.State.HasFlag(TransferStates.Succeeded))
				{
					try
					{
						using (var tagFile = TagLib.File.Create(outputFilename))
						{
							tagFile.Tag.Album = _item.Album;
							tagFile.Tag.Title = _item.Title;
							tagFile.Tag.Track = (uint)_item.TrackNumber;
							tagFile.Tag.Performers = _item.Artists;
							tagFile.Save();
						}
					}
					catch(CorruptFileException e)
					{

					}
				}

				_downloadedFilename = Path.GetFileName(outputFilename);
				Console.WriteLine($"Downloaded {_downloadedFilename}");
			}

			return _foundFile != null;
		}

		private async Task<SoulseekFile?> FindTrack()
		{
			var file = await FindTrack($"{string.Join(' ', _item.Artists)} {_item.Title}");
			if (file != null)
			{
				return file;
			}

			file = await FindTrack($"{string.Join(' ', _item.Artists)} {_item.TitleSanitized}");
			if (file != null)
			{
				return file;
			}

			file = await FindTrack($"{string.Join(' ', _item.Artists)} {_item.Album}");
			if(file != null)
			{
				return file;
			}

			file = await FindTrack($"{_item.Artists.First()} {_item.Title}");
			return file;
		}

		private async Task<SoulseekFile?> FindTrack(string query)
		{
			var responses = await _client.SearchAsync(SearchQuery.FromText(query));

			var candidates = new List<(Soulseek.SearchResponse, Soulseek.File)>();
			foreach(var res in responses.Responses)
			{
				foreach(var f in res.Files)
				{
					if(MatchFilename(f.Filename))
					{
						var ext = string.IsNullOrWhiteSpace(f.Extension) ? Util.SubstringSafe(Path.GetExtension(f.Filename), 1) : f.Extension;
						if(ExtensionsToDownload.Contains(ext.ToLower()))
						{
							candidates.Add((res, f));
						}
					}
				}
			}

			if(!candidates.Any())
			{
				return null;
			}

			var maxSpeed = candidates.Max(c => c.Item1.UploadSpeed);
			var maxBitrate = candidates.Max(c => c.Item2.BitRate);

			var lenSeconds = _item.LengthMs / 1000;

			var item = candidates
				.OrderBy(c => Math.Abs((c.Item2.Length ?? lenSeconds) - lenSeconds))
				.ThenBy(c => (c.Item1.HasFreeUploadSlot && c.Item1.QueueLength == 0) ? -1000 : c.Item1.QueueLength)
				.ThenByDescending(c => c.Item1.UploadSpeed)
				.ThenByDescending(c => c.Item2.BitRate)
				.FirstOrDefault();

			return new SoulseekFile(item.Item2, item.Item1.Username);
		}


		private bool MatchFilename(string filename)
		{
			if(Fuzz.PartialRatio($"{_item.TrackNumber} {_item.Title}".ToLower(), filename.ToLower()) > 80)
			{
				return true;
			}

			if(Fuzz.PartialRatio(_item.Title.ToLower(), filename.ToLower()) > 80)
			{
				return true;
			}

			if(Fuzz.PartialRatio($"{string.Join(", ", _item.Artists)} - {_item.Title}", filename.ToLower()) > 80)
			{
				return true;
			}

			if(Fuzz.PartialRatio($"{string.Join(", ", _item.Artists)} - {_item.Album} - {_item.Title}", filename.ToLower()) > 90)
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

	/*internal static class FilenameTokenizer
	{
		public static IEnumerable<Token> Tokenize(string filename)
		{
			var reader = new StringReader(Path.GetFileNameWithoutExtension(filename));

			int ch = -1;
			var tokenContents = new StringBuilder();
			var currentToken = TokenType.Invalid;
			while((ch = reader.Read()) != -1)
			{
				var c = (char)ch;
				if(char.IsNumber(c))
				{
					if(currentToken == TokenType.Number || currentToken == TokenType.String)
					{
						tokenContents.Append(c);
					}
					else
					{
						if (currentToken != TokenType.Invalid)
						{
							yield return new Token(currentToken, tokenContents.ToString());
						}

						currentToken = TokenType.Number;
						tokenContents = new StringBuilder(c.ToString());
					}
				}
				else if(char.IsPunctuation(c))
				{

				}
			}
		}

		public struct Token
		{
			public Token(TokenType token, string contents)
			{

			}
		}

		public enum TokenType
		{
			Invalid,
			String,
			Number,
			Punctuation,
			Whitespace
		}
	}*/
}
