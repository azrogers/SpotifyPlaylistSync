using Newtonsoft.Json;

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
		/// The threshold used for fuzzy matching when matching partial ratios.
		/// </summary>
		public int PartialRatioDetectionThreshold = 95;
		/// <summary>
		/// The threshold used for fuzzy matching when comparing "artist - track name".
		/// </summary>
		public int SoulseekTrackMatchThreshold = 80;
		/// <summary>
		/// The threshold used for fuzzy matching when comparing "artist - album - track name"
		/// </summary>
		public int SoulseekTrackAlbumMatchThreshold = 90;

		/// <summary>
		/// If a download takes longer than this amount of time to initiate (start transferring), give up.
		/// </summary>
		public float DownloadInitiationTimeout = 10.0f;
		/// <summary>
		/// If a download takes longer than this amount of time to finish, give up.
		/// </summary>
		public float DownloadDurationTimeout = 120.0f;
		/// <summary>
		/// If a download takes longer than (this amount of time * number of megabytes in a file), give up.
		/// The total timeout duration will be the min of the above computed value and DownloadDurationTimeout.
		/// </summary>
		public float DownloadDurationTimeoutByMegabyte = 10.0f;

		/// <summary>
		/// A list of extensions to prefer over others (flac, mp3, etc)
		/// </summary>
		public HashSet<string> PreferFormats = new HashSet<string>();
		/// <summary>
		/// A list of extensions to avoid over others (flac, mp3, etc)
		/// </summary>
		public HashSet<string> AvoidFormats = new HashSet<string>();

		/// <summary>
		/// Minimum bitrate for MP3 files.
		/// </summary>
		public int MinMp3Bitrate = 200;

		/// <summary>
		/// The maximum number of concurrent downloads.
		/// </summary>
		public int MaxDownloads = 3;

		/// <summary>
		/// The maximum file size to download, in kbs/second.
		/// </summary>
		public int MaxKbsPerSecond = 600;

		public static Config Get()
		{
			if (System.IO.File.Exists("SpotifyPlaylistDownloader.json"))
			{
				return JsonConvert.DeserializeObject<Config>(File.ReadAllText("SpotifyPlaylistDownloader.json")) ?? new Config();
			}

			return new Config();
		}

		private Config() { }
	}
}
