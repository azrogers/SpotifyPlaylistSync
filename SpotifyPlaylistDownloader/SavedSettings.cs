using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class SavedSettings
	{
		public static SavedSettings Instance
		{
			get
			{
				if(_instance == null && File.Exists(".settings.json"))
				{
					_instance = JsonConvert.DeserializeObject<SavedSettings>(File.ReadAllText(".settings.json"));
				}

				if(_instance == null)
				{
					_instance = new SavedSettings();
				}

				return _instance;
			}
		}

		private static SavedSettings? _instance = null;

		public string? SpotifyToken = null;

		public void Save()
		{
			File.WriteAllText(".settings.json", JsonConvert.SerializeObject(this));
		}
	}
}
