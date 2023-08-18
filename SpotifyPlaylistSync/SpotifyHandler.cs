using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistSync
{
	internal class SpotifyHandler
	{
		public static SpotifyHandler Instance { get; private set; } = new SpotifyHandler();


		private IConfigurationRoot GetConfiguration()
		{
			var builder = new ConfigurationBuilder();
			builder.AddUserSecrets<SpotifyHandler>();
			return builder.Build();
		}
	}
}
