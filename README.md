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
  -h, --help                 show this help
```