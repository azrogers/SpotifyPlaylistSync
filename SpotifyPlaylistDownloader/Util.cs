using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal static class Util
	{
		private static readonly Regex _versionTitleRegex = new Regex(@"-\s+(.+?)([Vv]ersion|[Rr]emaster|[Ee]dit|[F|f]ull [Ll]ength)");
		private static readonly Regex _sanitizeTitleRegex = new Regex(@"\((feat|ft|featuring).+?\)");

		public static string MakeValidFileName(string name)
		{
			string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + "#");
			string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

			return Regex.Replace(name, invalidRegStr, "_");
		}

		public static string SubstringSafe(string str, int substr)
		{
			if(str.Length < substr)
			{
				return string.Empty;
			}

			return str.Substring(substr);
		}

		public static void Ignore(this Task task) { }

		public static string GetExtension(this Soulseek.File file)
		{
			return string.IsNullOrWhiteSpace(file.Extension) ? Util.SubstringSafe(Path.GetExtension(file.Filename), 1).ToLower() : file.Extension.ToLower();
		}

		public static string SanitizeTrackTitle(string title)
		{
			return _versionTitleRegex.Replace(_sanitizeTitleRegex.Replace(title, ""), "").Trim();
		}
	}
}
