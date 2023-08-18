using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class Config
	{
		public string? SoulseekUsername = null;
		public string? SoulseekPassword = null;
		public string? OutputDirectory = null;
		public List<string> MusicDirectories = new List<string>();
	}
}
