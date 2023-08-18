using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class Util
	{
		public static string MakeValidFileName(string name)
		{
			string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(Path.GetInvalidFileNameChars()) + "#");
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
		}

		public static string SubstringSafe(string str, int substr)
		{
			if(str.Length < substr)
			{
				return string.Empty;
			}

			return str.Substring(substr);
		}
	}
}
