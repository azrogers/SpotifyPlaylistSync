using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal interface ITrackProvider
	{
		bool IsResolved();
		Task<bool> Resolve(string outputDir);
		PlaylistItemInfo? GetInfo();
	}

	class PlaylistItemInfo
	{
		public string Filename;
		public string Title;
		public int Length;
	}
}
