using FuzzySharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TagLib;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace SpotifyPlaylistDownloader
{
	internal class Library
	{
		private static readonly Regex _sanitizeTitleRegex = new Regex(@"\((feat|ft|featuring).+?\)");
		private static readonly HashSet<string> _extensions = new HashSet<string>()
		{
			".mp3", ".ogg", ".flac", ".wav", ".wma", ".m4a", ".mp4", ".aac",
			".aiff", ".3gp", ".oga"
		};

		private List<LibraryFile> _files = new List<LibraryFile>();
		private Dictionary<string, LibraryFile> _cachedFiles = new Dictionary<string, LibraryFile>();

		public LibraryFile? Find(PlaylistItem item)
		{
			var exactMatch = _files.FirstOrDefault(f =>
				string.Equals(f.Title, item.Title, StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(f.Album, item.Album, StringComparison.CurrentCultureIgnoreCase) &&
				f.Artists.Any(a => item.Artists.Contains(a)));

			if(exactMatch != default(LibraryFile))
			{
				return exactMatch;
			}

			var comp = $"{string.Join(", ", item.Artists)} - {item.Title}";
			var compSingle = $"{item.Artists.FirstOrDefault()} - {item.Title}";

			return _files.SelectMany(f =>
				{
					return new[] {
						(f, Fuzz.Ratio($"{string.Join(", ", f.Artists)} - {f.Title}", comp)),
						(f, Fuzz.Ratio($"{string.Join(", ", f.Artists)} - {f.TitleSanitized}", comp)),
						(f, Fuzz.Ratio($"{f.Artists.FirstOrDefault()} - {f.TitleSanitized}", compSingle))
					};
				})
				.Where(f => f.Item2 > 70)
				.OrderBy(f => Math.Abs(f.f.Length - (item.LengthMs / 1000)))
				.ThenByDescending(f => f.Item2)
				.Select(f => f.f)
				.FirstOrDefault();
		}

		public Library(params string[] libraryPaths)
		{
			Console.WriteLine("Assembling existing music library");

			if(System.IO.File.Exists(".cache.json"))
			{
				var cachedFiles = JsonConvert.DeserializeObject<List<LibraryFile>>(System.IO.File.ReadAllText(".cache.json")) ?? new List<LibraryFile>();
				foreach(var f in cachedFiles)
				{
					_cachedFiles[f.Filename] = f;
				}

				Console.WriteLine($"Loaded {_cachedFiles.Count} files from cache");
			}

			foreach(var dir in libraryPaths)
			{
				ParseDirectory(dir);
			}

			Console.WriteLine($"Found {_files.Count} tracks in existing music library");

			// write cache
			System.IO.File.WriteAllText(".cache.json", JsonConvert.SerializeObject(_files));
		}

		private void ParseDirectory(string directory)
		{
			foreach(var d in Directory.EnumerateDirectories(directory))
			{
				ParseDirectory(d);
			}

			foreach(var f in Directory.EnumerateFiles(directory))
			{
				var ext = Path.GetExtension(f.ToLower());
				if(!_extensions.Contains(ext))
				{
					continue;
				}

				var fullFilename = Path.GetFullPath(f);
				if(_cachedFiles.ContainsKey(fullFilename))
				{
					_files.Add(_cachedFiles[fullFilename]);
					continue;
				}

				try
				{
					using (var file = TagLib.File.Create(f))
					{
						if(file == null)
						{
							continue;
						}

						_files.Add(new LibraryFile()
						{
							Filename = fullFilename,
							Artists = file.Tag.Performers.Any() ? file.Tag.Performers : file.Tag.AlbumArtists,
							Album = file.Tag.Album,
							Title = file.Tag.Title,
							TitleSanitized = _sanitizeTitleRegex.Replace(file.Tag.Title ?? "", "").Trim(),
							Length = (int)file.Properties.Duration.TotalSeconds
						});
					}
				} 
				catch(CorruptFileException e)
				{
					continue;
				}
			}
		}

		public class LibraryFile
		{
			public string Filename;
			public string[] Artists;
			public string Album;
			public string Title;
			public string TitleSanitized;
			public int Length;
		}
	}
}
