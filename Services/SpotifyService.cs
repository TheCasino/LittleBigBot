using System;
using System.Threading.Tasks;
using Qmmands;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Models;

namespace LittleBigBot.Services
{
    [Name("Spotify")]
    [Description("Provides access to Spotify's API resources.")]
    [DontAutoAdd]
    public sealed class SpotifyService: BaseService
    {
        public SpotifyService(ClientCredentialsAuth auth, SpotifyWebAPI api)
        {
            Api = api;
            Credentials = auth;
        }

        public Token CurrentToken { get; private set; }
        public ClientCredentialsAuth Credentials { get; }
        public SpotifyWebAPI Api { get; }

        public async Task<T> RequestAsync<T>(Func<SpotifyWebAPI, Task<T>> actor)
        {
            await EnsureAuthenticatedAsync().ConfigureAwait(false);
            return await actor(Api);
        }

        public async Task EnsureAuthenticatedAsync()
        {
            if (CurrentToken != null && !CurrentToken.IsExpired()) return;
            CurrentToken = await Credentials.DoAuthAsync().ConfigureAwait(false);
            Api.TokenType = CurrentToken.TokenType;
            Api.AccessToken = CurrentToken.AccessToken;
        }
    }
}