using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terminal.Gui;
using System.Threading.Tasks;
using SpotifyPlaylistDownloader.Providers;

namespace SpotifyPlaylistDownloader
{
	internal class MainWindow : Window
	{
		private TextView _consoleView;
		private Label _progressLabel;
		private ProgressBar _progressBar;
		private View _sidebar;
		private Dictionary<string, DownloadView> _downloads = new Dictionary<string, DownloadView>();

		public MainWindow(Config config, string input)
		{
			Title = "Spotify Playlist Downloader";

			_sidebar = new FrameView()
			{
				Width = 25,
				Height = Dim.Fill(),
				Title = "Status"
			};

			var mainView = new FrameView()
			{
				Width = Dim.Fill(),
				Height = Dim.Fill(),
				X = Pos.Right(_sidebar)
			};

			_consoleView = new TextView()
			{
				Width = Dim.Fill(),
				Height = Dim.Fill()
			};

			mainView.Add(_consoleView);

			_progressLabel = new Label()
			{
				Text = "Finished 0/0",
				Width = Dim.Fill()
			};

			_progressBar = new ProgressBar()
			{
				Width = Dim.Fill(),
				Height = 1,
				Y = Pos.Bottom(_progressLabel)
			};

			_sidebar.Add(_progressLabel, _progressBar);

			Logger.OnLog = (m) =>
			{
				Application.MainLoop.Invoke(() =>
				{
					_consoleView.InsertText(m + "\n");
				});
			};

			DownloadTracker.OnUpdate = () =>
			{
				Application.MainLoop.Invoke(() =>
				{
					UpdateDownloads(_progressBar);
				});
			};

			Add(mainView, _sidebar);

			Run(config, input).Ignore();
		}

		private void UpdateDownloads(View showBelow)
		{
			var stillDownloading = new HashSet<string>();

			foreach(var file in DownloadTracker.Downloads)
			{
				if(!_downloads.ContainsKey(file.Filename))
				{
					// add a new download element
					_downloads[file.Filename] = new DownloadView()
					{
						Width = Dim.Fill()
					};

					_sidebar.Add(_downloads[file.Filename]);
				}

				// update element and positioning
				var view = _downloads[file.Filename];
				view.Y = Pos.Bottom(showBelow);
				view.Update(file);

				showBelow = view;
				stillDownloading.Add(file.Filename);
			}

			// remove old downloads
			var toRemove = _downloads.Keys.Where(k => !stillDownloading.Contains(k)).ToList();
			foreach (var key in toRemove)
			{
				_sidebar.Remove(_downloads[key]);
				_downloads.Remove(key);
			}
		}

		private async Task Run(Config config, string input)
		{
			if (config.OutputDirectory == null)
			{
				config.OutputDirectory = Path.Combine(Environment.CurrentDirectory, "output");
			}

			if (!Directory.Exists(config.OutputDirectory))
			{
				Directory.CreateDirectory(config.OutputDirectory);
			}

			var libraryDirs = config.MusicDirectories.ToList();
			libraryDirs.Add(config.OutputDirectory);
			libraryDirs.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
			var library = new Library(config, libraryDirs.ToArray());

			var options = new Soulseek.SoulseekClientOptions();
			var client = new Soulseek.SoulseekClient(options);
			await client.ConnectAsync(config.SoulseekUsername, config.SoulseekPassword);

			var auth = new SpotifyAuthenticator();
			await auth.Authenticate();

			var context = new Context(client, library, config);

			var playlist = await auth.GetPlaylist(input);
			if(playlist == null)
			{
				Logger.Write("Can't obtain playlist?");
				return;
			}

			var items = playlist.GetItems().ToArray();
			var providers = new List<ITrackProvider?>();
			var slots = new Task<ITrackProvider?>?[config.MaxDownloads];
			_progressLabel.Text = $"Finished 0/{items.Length}";

			var finished = 0;
			var nextI = 0;
			await Task.Run(() =>
			{
				while(finished < items.Length)
				{
					for(var i = 0; i < slots.Length; i++)
					{
						var slot = slots[i];
						if(slot != null && !slot.IsCompleted)
						{
							// still working
							continue;
						}
						else if(slot != null && slot.IsCompleted)
						{
							// we're finished, add provider if we have it
							if (slot.Result != null)
							{
								providers.Add(slot.Result);
							}

							finished++;
							slots[i] = null;

							Application.MainLoop.Invoke(() =>
							{
								_progressLabel.Text = $"Finished {finished}/{items.Length}";
								_progressBar.Fraction = Math.Clamp(finished / (float)items.Length, 0, 1);
							});
						}

						if(nextI >= items.Length)
						{
							continue;
						}

						// start a new download
						var index = nextI++;
						slots[i] = Task.Run(async () =>
						{
							var provider = await items[index].GetProvider(context);
							if(provider == null)
							{
								Logger.Write($"Can't find {items[index].Title}");
							}
							return provider;
						});
					}

					Thread.Sleep(100);
				}
			});

			Application.MainLoop.Invoke(() =>
			{
				_progressLabel.Text = $"Finished {finished}/{items.Length}";
				_progressBar.Fraction = 1.0f;
			});

			// write playlist

			using (var output = new FileStream(Path.Combine(config.OutputDirectory, "playlist.pls"), FileMode.Create))
			using (var writer = new StreamWriter(output))
			{
				writer.WriteLine("[playlist]");

				var playlistInfos = providers.Where(p => p != null).Select(p => p.GetInfo());
				var i = 1;
				foreach (var info in playlistInfos)
				{
					if (info != null)
					{
						var relPath = Path.GetRelativePath(config.OutputDirectory, info.Filename);
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

			Logger.Write("Wrote playlist to playlist.pls");
			Logger.Write("Done!");
		}
	}
}
