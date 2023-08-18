using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal static class DownloadTracker
	{
		private static Dictionary<string, Progress> _downloads = new Dictionary<string, Progress>();

		public static Progress Start(string key)
		{
			return _downloads[key] = new Progress();
		}

		public static void Stop(string key)
		{
			_downloads.Remove(key);
		}

		public class Progress
		{
			public void OnStateChanged((TransferStates previousState, Transfer transfer) args)
			{

			}

			public void OnProgressUpdated((long PreviousBytesTransfered, Transfer transfer) args)
			{

			}
		}
	}
}
