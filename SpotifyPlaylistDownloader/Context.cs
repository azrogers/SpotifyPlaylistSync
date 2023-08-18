using Soulseek;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class Context
	{
		public SoulseekClient Client;
		public Library Library;
		public Config Config;

		public Context(SoulseekClient client, Library library, Config config)
		{
			Client = client;
			Library = library;
			Config = config;
		}
	}
}
