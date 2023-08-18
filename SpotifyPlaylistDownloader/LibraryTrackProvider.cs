using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class LibraryTrackProvider : ITrackProvider
	{
		private Library.LibraryFile _file;

		public LibraryTrackProvider(Library.LibraryFile file)
		{
			_file = file;
		}

		public bool IsResolved() => true;

		public Task<bool> Resolve(string outputDir) => Task.FromResult(true);

		public PlaylistItemInfo GetInfo()
		{
			return new PlaylistItemInfo() { Filename = _file.Filename, Length = _file.Length, Title = _file.Title };
		}
	}
}
