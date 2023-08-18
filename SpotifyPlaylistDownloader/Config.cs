using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class Config
	{
		/// <summary>
		/// The username used to log in to Soulseek.
		/// </summary>
		public string? SoulseekUsername = null;
		/// <summary>
		/// The password used to log in to Soulseek.
		/// </summary>
		public string? SoulseekPassword = null;
		/// <summary>
		/// The directory that downloaded files will be output into.
		/// </summary>
		public string? OutputDirectory = null;
		/// <summary>
		/// Directories that will be searched to build the user's library.
		/// </summary>
		public List<string> MusicDirectories = new List<string>();

		/// <summary>
		/// The threshold used for fuzzy matching when searching for existing library tracks.
		/// </summary>
		public int LibraryDetectionThreshold = 85;
		/// <summary>
		/// The threshold used for fuzzy matching when comparing "artist - track name".
		/// </summary>
		public int SoulseekTrackMatchThreshold = 80;
		/// <summary>
		/// The threshold used for fuzzy matching when comparing "artist - album - track name"
		/// </summary>
		public int SoulseekTrackAlbumMatchThreshold = 90;
	}
}
