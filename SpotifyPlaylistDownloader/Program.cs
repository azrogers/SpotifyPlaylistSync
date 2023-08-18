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
			var conf = new Config();
			var p = new OptionSet()
			{
				{ "u|username=", "set your soulseek username (required)", v => conf.SoulseekUsername = v },
				{ "p|password=", "set your soulseek password (required)", v => conf.SoulseekPassword = v },
				{ "o|output=", "set the output directory", v => conf.OutputDirectory = v },
				{ "library=", "add a path of your music library", v => conf.MusicDirectories.Add(v) },
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