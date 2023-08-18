using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class Logger
	{
		public static Action<string> OnLog = (m) => Console.WriteLine(m);

		public static void Write(string message)
		{
			OnLog(message);
		}
	}
}
