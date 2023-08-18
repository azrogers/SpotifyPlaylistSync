using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SpotifyPlaylistSync
{
	/// <summary>
	/// Interaction logic for LoginWindow.xaml
	/// </summary>
	public partial class LoginWindow : Window
	{
		private static EmbedIOAuthServer _server;

		public LoginWindow()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			Task.Run(async () =>
			{
				_server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
				await _server.Start();

				_server.AuthorizationCodeReceived += _server_AuthorizationCodeReceived;
				_server.ErrorReceived += _server_ErrorReceived;

				var request = new LoginRequest(_server.BaseUri, )
			});
		}

		private Task _server_ErrorReceived(object arg1, string arg2, string? arg3)
		{
			throw new NotImplementedException();
		}

		private Task _server_AuthorizationCodeReceived(object arg1, AuthorizationCodeResponse arg2)
		{
			throw new NotImplementedException();
		}
	}
}
