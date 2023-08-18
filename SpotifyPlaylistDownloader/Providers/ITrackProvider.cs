using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader.Providers
{
    internal interface ITrackProvider
    {
        PlaylistItemInfo? GetInfo();
    }

    internal class ResolveProviderResult
    {
        public ITrackProvider? Provider;
        public bool Success;

        public ResolveProviderResult(bool success, ITrackProvider? provider)
        {
            Provider = provider;
            Success = success;
        }
    }

    class PlaylistItemInfo
    {
        public string Filename;
        public string Title;
        public int Length;
    }
}
