using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpotifyPlaylistDownloader
{
	internal class SpotifyAuthenticator
	{
		private static readonly Regex _idRegex = new Regex("^[A-Za-z0-9]+$");

		private EmbedIOAuthServer? _server;
		private SpotifyClient? _client;
		private EventWaitHandle _wait = new EventWaitHandle(false, EventResetMode.ManualReset);

		public async Task<Playlist?> GetPlaylist(string uriOrId)
		{
			if(_client == null)
			{
				if(!await Authenticate())
				{
					return null;
				}
			}

			var id = GetPlaylistId(uriOrId);
			if(id == null)
			{
				throw new ArgumentException($"Invalid spotify playlist ID {id}");
			}

			return await Playlist.Create(_client, await _client.Playlists.Get(id));
		}

		public async Task<bool> Authenticate()
		{
			_client = null;
			_wait.Reset();

			if(SavedSettings.Instance.SpotifyToken != null)
			{
				try
				{
					_client = new SpotifyClient(SavedSettings.Instance.SpotifyToken);
					await _client.UserProfile.Current();
					return true;
				}
				catch(APIUnauthorizedException e)
				{
					_client = null;
				}
			}

			_server = new EmbedIOAuthServer(new Uri("http://localhost:5543/callback"), 5543);
			await _server.Start();

			_server.AuthorizationCodeReceived += _server_AuthorizationCodeReceived;
			_server.ErrorReceived += _server_ErrorReceived;

			var request = new LoginRequest(_server.BaseUri, ApiConstants.ClientId, LoginRequest.ResponseType.Code)
			{
				Scope = new List<string> { Scopes.PlaylistReadPrivate, Scopes.UserReadEmail, Scopes.UserLibraryRead }
			};

			BrowserUtil.Open(request.ToUri());
			_wait.WaitOne();

			return _client != null;
		}

		private async Task _server_ErrorReceived(object sender, string error, string? state)
		{
			await _server.Stop();
			Console.WriteLine($"Aborting authorization, error received: {error}");

			_wait.Set();
		}

		private async Task _server_AuthorizationCodeReceived(object sender, AuthorizationCodeResponse res)
		{
			await _server.Stop();

			var config = SpotifyClientConfig.CreateDefault();
			var tokenResponse = await new OAuthClient(config).RequestToken(
				new AuthorizationCodeTokenRequest(
					ApiConstants.ClientId, ApiConstants.ClientSecret, res.Code, new Uri("http://localhost:5543/callback")));

			_client = new SpotifyClient(tokenResponse.AccessToken);

			SavedSettings.Instance.SpotifyToken = tokenResponse.AccessToken;
			SavedSettings.Instance.Save();

			_wait.Set();
		}

		private string? GetPlaylistId(string uriOrId)
		{
			if(_idRegex.IsMatch(uriOrId))
			{
				return uriOrId;
			}

			var uri = new Uri(uriOrId);
			if(uri.Host == "open.spotify.com" && uri.Segments.Count() > 2 && uri.Segments[1] == "playlist/")
			{
				return uri.Segments[2];
			}

			return null;
		}
	}
}
