using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader.Providers
{
    internal class LibraryTrackProvider : ITrackProvider
    {
        private Library.LibraryFile _file;

        public LibraryTrackProvider(Library.LibraryFile file)
        {
            _file = file;
        }

        public PlaylistItemInfo GetInfo()
        {
            return new PlaylistItemInfo() { Filename = _file.Filename, Length = _file.Length, Title = _file.Title };
        }

        public static Task<ResolveProviderResult> Resolve(PlaylistItem item, Context context)
        {
            var libraryFile = context.Library.Find(item);
            if (libraryFile != null)
            {
                Logger.Write($"Found {string.Join(", ", libraryFile.Artists)} - {libraryFile.Title} in library already");
                return Task.FromResult(new ResolveProviderResult(true, new LibraryTrackProvider(libraryFile)));
            }

            return Task.FromResult(new ResolveProviderResult(false, null));
        }
    }
}
