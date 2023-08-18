# SpotifyPlaylistSync

A tool to take a Spotify playlist and turn it into a playlist of local files on your hard drive.

## Setup

Create a new app on the [Spotify API dashboard](https://developer.spotify.com/dashboard/). Set the redirect URI to `http://localhost:5543/callback`.

Create the file `SpotifyPlaylistDownloader/ApiConstants.cs`. It should look like this:

```
namespace SpotifyPlaylistDownloader
{
	internal class ApiConstants
	{
		public static string ClientId = "<your client ID here>";
		public static string ClientSecret = "<your client secret here>";
	}
}
```

## Usage

```
SpotifyPlaylistDownloader [options] <playlist ID or URI>
Available options:
  -u, --username=VALUE       set your soulseek username (required)
  -p, --password=VALUE       set your soulseek password (required)
  -o, --output=VALUE         set the output directory
      --library=VALUE        add a path of your music library
      --prefer=VALUE         a comma-separated list of extensions to prefer
                               over others when downloading.
      --avoid=VALUE          a comma-separated list of extensions to avoid
                               over others when downloading.
      --max-downloads=VALUE  the maximum number of concurrent downloads.
                               default 3, min 1.
      --max-kbps-per-second=VALUE
                             the maximum number of kbps per second to
                               consider when downloading. lower this to get
                               smaller files. default 600, min 1.
      --download-timeout=VALUE
                             the maximum number of seconds to wait for a song
                               to download before giving up. default 120, min 0.
      --download-initiation-timeout=VALUE
                             the maximum number of seconds to wait for a
                               download to begin before giving up. default 10,
                               min 0.
      --download-timeout-per-mb=VALUE
                             the number of seconds to wait per megabyte for a
                               song to download before giving up. default 10,
                               min 0. this value doesn't overwrite download-
                               timeout, and if (this * megabytes) > download-
                               timeout, download-timeout will be hit first.
  -h, --help                 show this help
```