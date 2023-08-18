using Soulseek;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

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

			Console.WriteLine($"Found {items.Count} items on playlist");
			return new Playlist(items);
		}

		public IEnumerable<ITrackProvider> CreateProviders(Library library, SoulseekClient client)
		{
			foreach(var item in _items)
			{
				var libraryFile = library.Find(item);
				if(libraryFile != null)
				{
					Console.WriteLine($"Found {string.Join(", ", libraryFile.Artists)} - {libraryFile.Title} in library already");
					yield return new LibraryTrackProvider(libraryFile);
				}
				else
				{
					yield return new SoulseekTrackProvider(client, item);
				}
			}
		}

		private Playlist(List<PlaylistItem> items)
		{
			_items = items.ToArray();
		}
	}

	internal class PlaylistItem
	{
		private static readonly Regex _versionTitleRegex = new Regex(@"-\s+(.+?)[Vv]ersion");

		public string Title;
		public string TitleSanitized => _versionTitleRegex.Replace(Title, "");
		public string[] Artists;
		public string Album;
		public string SpotifyId;
		public int LengthMs;
		public int TrackNumber;
	}
}
