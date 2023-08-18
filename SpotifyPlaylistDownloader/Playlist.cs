using SpotifyAPI.Web;
using SpotifyPlaylistDownloader.Providers;
using System.Text.RegularExpressions;

namespace SpotifyPlaylistDownloader
{
	internal class Playlist
	{
		private PlaylistItem[] _items;

		public static async Task<Playlist?> Create(SpotifyClient client, FullPlaylist playlist)
		{
			if(playlist.Tracks == null)
			{
				return null;
			}

			var items = new List<PlaylistItem>();

			await foreach (var item in client.Paginate(playlist.Tracks))
			{
				if(item.Track is FullTrack track)
				{
					items.Add(new PlaylistItem()
					{
						Album = track.Album.Name,
						Artists = track.Artists.Select(a => a.Name).ToArray(),
						Title = track.Name,
						SpotifyId = track.Id,
						LengthMs = track.DurationMs,
						TrackNumber = track.TrackNumber
					});
				}
			}

			Logger.Write($"Found {items.Count} items on playlist");
			return new Playlist(items);
		}

		public IEnumerable<PlaylistItem> GetItems()
		{
			return _items;
		}

		private Playlist(List<PlaylistItem> items)
		{
			_items = items.ToArray();
		}
	}

	internal class PlaylistItem
	{
		public string Title;
		public string TitleSanitized => Util.SanitizeTrackTitle(Title);
		public string[] Artists;
		public string Album;
		public string SpotifyId;
		public int LengthMs;
		public int TrackNumber;

		private ITrackProvider? _provider;

		public async Task<ITrackProvider?> GetProvider(Context context)
		{
			Logger.Write($"Resolving {string.Join(", ", Artists)} - {Title}");

			if(_provider != null)
			{
				return _provider;
			}

			var result = await LibraryTrackProvider.Resolve(this, context);
			if(result.Success)
			{
				return _provider = result.Provider;
			}

			result = await SoulseekTrackProvider.Resolve(this, context);
			if(result.Success)
			{
				return _provider = result.Provider;
			}

			return null;
		}
	}
}
