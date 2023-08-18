using ByteSizeLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace SpotifyPlaylistDownloader
{
	internal class DownloadView : FrameView
	{
		private Label _title;
		private ProgressBar _progress;

		public DownloadView() : base()
		{
			Height = 4;

			_title = new Label()
			{
				Text = "",
				Width = Dim.Fill()
			};

			_progress = new ProgressBar()
			{
				Width = Dim.Fill(),
				Y = Pos.Bottom(_title)
			};

			Add(_title, _progress);
		}

		public void Update(DownloadTracker.Progress progress)
		{
			Application.MainLoop.Invoke(() =>
			{
				Title = progress.Item.Title;
				var transferred = ByteSize.FromBytes(progress.BytesTransferred).ToString();
				var total = ByteSize.FromBytes(progress.BytesTransferred + progress.BytesRemaining).ToString();
				_title.Text = $"{transferred}/{total}";
				_progress.Fraction = progress.PercentageComplete;
			});
		}
	}
}
