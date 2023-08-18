using FuzzySharp;
using NDesk.Options;
using System.IO;
using Terminal.Gui;

namespace SpotifyPlaylistDownloader
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var help = false;
			var conf = Config.Get();
			var p = new OptionSet()
			{
				{ "u|username=", "set your soulseek username (required)", v => conf.SoulseekUsername = v },
				{ "p|password=", "set your soulseek password (required)", v => conf.SoulseekPassword = v },
				{ "o|output=", "set the output directory", v => conf.OutputDirectory = v },
				{ "library=", "add a path of your music library", v => conf.MusicDirectories.Add(v) },
				{
					"prefer=",
					"a comma-separated list of extensions to prefer over others when downloading.",
					v => { conf.PreferFormats = new HashSet<string>(v.Split(',').Select(s => s.ToLower())); }
				},
				{
					"avoid=",
					"a comma-separated list of extensions to avoid over others when downloading.",
					v => { conf.AvoidFormats = new HashSet<string>(v.Split(',').Select(s => s.ToLower())); }
				},
				{
					"max-downloads=",
					"the maximum number of concurrent downloads. default 3, min 1.",
					v => { conf.MaxDownloads = Math.Max(1, int.Parse(v)); }
				},
				{
					"max-kbps-per-second=",
					"the maximum number of kbps per second to consider when downloading. lower this to get smaller files. default 600, min 1.",
					v => { conf.MaxKbsPerSecond = Math.Max(1, int.Parse(v)); }
				},
				{
					"download-timeout=",
					"the maximum number of seconds to wait for a song to download before giving up. default 120, min 0.",
					v => { conf.DownloadDurationTimeout = Math.Max(0, float.Parse(v)); }
				},
				{
					"download-initiation-timeout=",
					"the maximum number of seconds to wait for a download to begin before giving up. default 10, min 0.",
					v => { conf.DownloadInitiationTimeout = Math.Max(0, float.Parse(v)); }
				},
				{
					"download-timeout-per-mb=",
					"the number of seconds to wait per megabyte for a song to download before giving up. default 10, min 0. " +
					"this value doesn't overwrite download-timeout, and if (this * megabytes) > download-timeout, download-timeout will be hit first.",
					v => { conf.DownloadDurationTimeoutByMegabyte = Math.Max(0, float.Parse(v));  }
				},
				{ "h|help", "show this help", v => help = v != null }
			};

			var extra = p.Parse(args);
			if(help || conf.SoulseekUsername == null || conf.SoulseekPassword == null || extra.Count == 0)
			{
				if(!help)
				{
					Logger.Write("Error: missing required params");
				}

				Logger.Write($"Usage: {AppDomain.CurrentDomain.FriendlyName} [options] <playlist ID or URI>");
				Logger.Write("Available options:");
				p.WriteOptionDescriptions(Console.Out);

				return;
			}

			var input = string.Join(' ', extra);
			Application.Init();
			var window = new MainWindow(conf, input);
			Application.Run(window);
			Application.Shutdown();
		}
	}
}