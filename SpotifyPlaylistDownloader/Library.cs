﻿using FuzzySharp;
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
		private static readonly HashSet<string> _extensions = new HashSet<string>()
		{
			".mp3", ".ogg", ".flac", ".wav", ".wma", ".m4a", ".mp4", ".aac",
			".aiff", ".3gp", ".oga"
		};

		private List<LibraryFile> _files = new List<LibraryFile>();
		private Config _config;
		private Dictionary<string, LibraryFile> _cachedFiles = new Dictionary<string, LibraryFile>();

		public LibraryFile? Find(PlaylistItem item)
		{
			// try to find an exact match for this file
			var exactMatch = _files.FirstOrDefault(f =>
				string.Equals(f.Title, item.Title, StringComparison.CurrentCultureIgnoreCase) &&
				string.Equals(f.Album, item.Album, StringComparison.CurrentCultureIgnoreCase) &&
				f.Artists.Any(a => item.Artists.Contains(a)));

			if(exactMatch != default(LibraryFile))
			{
				return exactMatch;
			}

			// do a fuzzy text match
			var comp = $"{string.Join(", ", item.Artists)} - {item.Title}";
			var compSingle = $"{item.Artists.FirstOrDefault()} - {item.Title}";

			return _files
				.Where(f => !string.IsNullOrWhiteSpace(f.Title) && f.Artists.Length > 0)
				.SelectMany(f =>
				{
					return new[] {
						(f, Fuzz.PartialRatio($"{string.Join(", ", f.Artists)} - {f.Title}", comp), _config.PartialRatioDetectionThreshold),
						(f, Fuzz.Ratio($"{string.Join(", ", f.Artists)} - {f.TitleSanitized}", comp), _config.LibraryDetectionThreshold),
						(f, Fuzz.Ratio($"{f.Artists.FirstOrDefault()} - {f.TitleSanitized}", compSingle), _config.LibraryDetectionThreshold)
					};
				})
				.Where(f => f.Item2 > f.Item3)
				.OrderBy(f => Math.Abs(f.f.Length - (item.LengthMs / 1000)))
				.ThenByDescending(f => f.Item2)
				.Select(f => f.f)
				.FirstOrDefault();
		}

		public Library(Config config, params string[] libraryPaths)
		{
			_config = config;
			Logger.Write("Assembling existing music library");

			if(System.IO.File.Exists(".cache.json"))
			{
				var cachedFiles = JsonConvert.DeserializeObject<List<LibraryFile>>(System.IO.File.ReadAllText(".cache.json")) ?? new List<LibraryFile>();
				foreach(var f in cachedFiles)
				{
					_cachedFiles[f.Filename] = f;
				}

				Logger.Write($"Loaded {_cachedFiles.Count} files from cache");
			}

			foreach(var dir in libraryPaths)
			{
				ParseDirectory(dir);
			}

			Logger.Write($"Found {_files.Count} tracks in existing music library");

			// write cache
			System.IO.File.WriteAllText(".cache.json", JsonConvert.SerializeObject(_files));
		}

		private void ParseDirectory(string directory)
		{
			foreach(var d in Directory.EnumerateDirectories(directory))
			{
				// recurse
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

						// add info from tags to library
						_files.Add(new LibraryFile()
						{
							Filename = fullFilename,
							Artists = file.Tag.Performers.Any() ? file.Tag.Performers : file.Tag.AlbumArtists,
							Album = file.Tag.Album,
							Title = file.Tag.Title,
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
			public string TitleSanitized => Util.SanitizeTrackTitle(Title ?? "");
			public int Length;
		}
	}
}
