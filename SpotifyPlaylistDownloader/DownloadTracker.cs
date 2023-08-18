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

		public static Action OnUpdate = () => { };

		public static IEnumerable<Progress> Downloads => _downloads.Values;

		public static Progress Start(string key, PlaylistItem item)
		{
			var dl = _downloads[key] = new Progress(key, item);
			OnUpdate();
			return dl;
		}

		public static void Stop(string key)
		{
			_downloads.Remove(key);
			OnUpdate();
		}

		public class Progress
		{
			public TransferStates CurrentState { get; private set; }
			public long BytesTransferred { get; private set; }
			public long BytesRemaining { get; private set; }
			public string Filename { get; private set; }
			public PlaylistItem Item { get; private set; }

			public float PercentageComplete => 1.0f - Math.Clamp(BytesRemaining / (float)(BytesRemaining + BytesTransferred), 0, 1);

			public Progress(string filename, PlaylistItem item)
			{
				Filename = filename;
				Item = item;
			}

			public void OnStateChanged((TransferStates previousState, Transfer transfer) args)
			{
				CurrentState = args.transfer.State;
				DownloadTracker.OnUpdate();
			}

			public void OnProgressUpdated((long PreviousBytesTransfered, Transfer transfer) args)
			{
				BytesTransferred = args.transfer.BytesTransferred;
				BytesRemaining = args.transfer.BytesRemaining;
				DownloadTracker.OnUpdate();
			}
		}
	}
}
