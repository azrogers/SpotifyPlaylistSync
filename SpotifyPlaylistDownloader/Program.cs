using FuzzySharp;
using NDesk.Options;
using System.IO;

namespace SpotifyPlaylistDownloader
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var a = Fuzz.PartialRatio("I Love NYC", "04 - I Love NYC");
			var b = Fuzz.PartialRatio("I Love NYC", "Andrew W.K. - I Get Wet - 06. I Love NYC");
			var c = Fuzz.PartialRatio("Andrew W.K. - I Get Wet - I Love NYC", "Andrew W.K. - I Get Wet - 06. I Love NYC");
			var d = Fuzz.PartialRatio("Andrew W.K. - I Love NYC".ToLower(), "(04) andrew w.k. - i love nyc");
			var e = Fuzz.PartialRatio("Andrew W.K. - I Get Wet - Ready to Die", "Andrew W.K. - I Get Wet - 06. I Love NYC");
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
					Console.WriteLine("Error: missing required params");
				}

				Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [options] <playlist ID or URI>");
				Console.WriteLine("Available options:");
				p.WriteOptionDescriptions(Console.Out);

				return;
			}

			var input = string.Join(' ', extra);
			Run(conf, input).Wait();
		}

		private static async Task Run(Config config, string input)
		{
			var outputDir = config.OutputDirectory;
			if(outputDir == null)
			{
				outputDir = Path.Combine(Environment.CurrentDirectory, "output");
			}

			if(!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}

			var libraryDirs = config.MusicDirectories.ToList();
			libraryDirs.Add(outputDir);
			libraryDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
			var library = new Library(libraryDirs.ToArray());

			var options = new Soulseek.SoulseekClientOptions();
			var client = new Soulseek.SoulseekClient(options);
			await client.ConnectAsync(config.SoulseekUsername, config.SoulseekPassword);

			var auth = new SpotifyAuthenticator();
			await auth.Authenticate();

			var playlist = await auth.GetPlaylist(input);
			var providers = playlist.CreateProviders(library, client).ToArray();
			var resolved = 0;
			var tasks = providers.Select(p => Task.Run(async () =>
			{
				if(await p.Resolve(outputDir))
				{
					resolved++;
					Console.WriteLine($"Resolved {resolved}/{providers.Length}");
				}
				else
				{
					var info = p.GetInfo();
					Console.WriteLine($"Can't find {info?.Title}");
				}
			})).ToArray();

			Console.WriteLine($"Resolved {resolved}/{providers.Length}");
			Task.WaitAll(tasks.ToArray());

			// write playlist

			using (var output = new FileStream(Path.Combine(outputDir, "playlist.pls"), FileMode.Create))
			using (var writer = new StreamWriter(output))
			{
				writer.WriteLine("[playlist]");

				var playlistInfos = providers.Where(p => p.IsResolved()).Select(p => p.GetInfo());
				var i = 1;
				foreach (var info in playlistInfos)
				{
					if(info != null)
					{
						var relPath = Path.GetRelativePath(outputDir, info.Filename);
						var path = relPath.Length > info.Filename.Length ? info.Filename : relPath;

						writer.WriteLine();
						writer.WriteLine($"File{i}={path}");
						writer.WriteLine($"Length{i}={info.Length}");
						//writer.WriteLine($"Title{i}={info.Title}");
						i++;
					}
				}

				writer.WriteLine($"NumberOfEntries={i - 1}");
				writer.WriteLine("Version=2");
			}

			Console.WriteLine("Wrote playlist to playlist.pls");
		}
	}
}