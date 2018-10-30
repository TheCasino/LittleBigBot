using System;
using System.Threading.Tasks;
using LittleBigBot.Attributes;
using LittleBigBot.Entities;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace LittleBigBot.Services
{
    [Service("Spotify", "Provides access to Spotify's API resources.")]
    public sealed class SpotifyService : BaseService
    {
        public SpotifyService(IOptions<LittleBigBotConfig> config)
        {
            Api = new SpotifyWebAPI { UseAuth = true };
            Credentials = new ClientCredentialsAuth
            {
                ClientId = config.Value.Spotify.ClientId,
                ClientSecret = config.Value.Spotify.ClientSecret,
                Scope = Scope.None
            };
        }

        public Token CurrentToken { get; private set; }
        public ClientCredentialsAuth Credentials { get; }
        public SpotifyWebAPI Api { get; }
        
        public async Task<T> RequestAsync<T>(Func<SpotifyWebAPI, Task<T>> actor)
        {
            await EnsureAuthenticatedAsync().ConfigureAwait(false);
            return await actor(Api);
        }

        public override Task InitializeAsync()
        {
            return EnsureAuthenticatedAsync();
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