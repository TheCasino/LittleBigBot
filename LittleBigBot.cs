using System;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using Octokit.Internal;
using Qmmands;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;

namespace LittleBigBot
{
    public sealed class LittleBigBot
    {
        public const string ConfigurationFileLocation = "littlebigbot.ini";

        private readonly LittleBigBotConfig _appConfig;
        private readonly DiscordSocketClient _client;
        private readonly CommandHandlerService _commandHandler;
        private readonly ILogger _discordLogger;

        private readonly ILogger<LittleBigBot> _logger;

        public LittleBigBot()
        {
            var services = ConfigureServices(new ServiceCollection());

            _client = services.GetRequiredService<DiscordSocketClient>();
            _commandHandler = services.GetRequiredService<CommandHandlerService>();
            _appConfig = services.GetRequiredService<IOptions<LittleBigBotConfig>>().Value;
            _logger = services.GetRequiredService<ILogger<LittleBigBot>>();
            _discordLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Discord");
        }

        public static Color DefaultEmbedColour => new Color(0xFFDBF4);

        private IServiceProvider ConfigureServices(IServiceCollection rootCollection)
        {
            var configuration = new ConfigurationBuilder().AddIniFile(ConfigurationFileLocation, false, true).Build();
            var webclient = new WebClient();
            webclient.Headers.Add("User-Agent", "LittleBigBot");

            return rootCollection
                .AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
                {
                    AlwaysDownloadUsers = true,
                    MessageCacheSize = 100,
                    LogLevel = LogSeverity.Verbose
                }))
                .AddSingleton(new CommandService(new CommandServiceConfiguration
                {
                    CaseSensitive = false,
                    DefaultRunMode = RunMode.Sequential,
                    IgnoreExtraArguments = false,
                    CooldownBucketKeyGenerator = new LittleBigBotCooldownBucketKeyGenerator()
                }))
                .AddSingleton(new SpotifyService(new ClientCredentialsAuth
                {
                    ClientId = configuration.GetSection("Spotify")["ClientId"],
                    ClientSecret = configuration.GetSection("Spotify")["ClientSecret"],
                    Scope = Scope.None
                }, new SpotifyWebAPI
                {
                    UseAuth = true
                }))
                .AddLogging(log => { log.AddProvider(new LittleBigLoggingProvider()); })
                .AddSingleton<CommandHandlerService>()
                .AddSingleton<SpoilerService>()
                .AddTransient<Random>()
                .AddSingleton<ScriptingService>()
                .AddSingleton<InteractiveService>()
                .AddSingleton<ApiStatsService>()
                .AddSingleton(webclient)
                .AddSingleton(new GitHubClient(new ProductHeaderValue(configuration.GetSection("GitHub")["Username"]), new InMemoryCredentialStore(new Credentials(configuration.GetSection("GitHub")["Token"]))))
                .AddSingleton(configuration)
                .AddSingleton(this)
                .Configure<LittleBigBotConfig>(configuration.Bind)
                .BuildServiceProvider();
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("LittleBigBot client starting up!");

            await _commandHandler.InitialiseAsync().ConfigureAwait(false);
            _client.Log += HandleLogAsync;

            _client.Ready += () => _client.SetGameAsync(_appConfig.LittleBigBot.PlayingStatus);

            await _client.LoginAsync(TokenType.Bot, _appConfig.Discord.Token).ConfigureAwait(false);
            await _client.StartAsync().ConfigureAwait(false);

            await Task.Delay(-1);
        }

        private Task HandleLogAsync(LogMessage arg)
        {
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                    _discordLogger.LogCritical(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Error:
                    _discordLogger.LogError(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Warning:
                    _discordLogger.LogWarning(arg.Exception, arg.Message);
                    break;
                case LogSeverity.Info:
                    _discordLogger.LogInformation(arg.Message);
                    break;
                case LogSeverity.Verbose:
                    _discordLogger.LogTrace(arg.Message);
                    break;
                case LogSeverity.Debug:
                    _discordLogger.LogDebug(arg.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return Task.CompletedTask;
        }
    }
}