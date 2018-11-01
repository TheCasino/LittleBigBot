using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LittleBigBot.Attributes;
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

namespace LittleBigBot
{
    public sealed class LittleBigBot
    {
        public const string DefaultConfigurationFileLocation = "littlebigbot.ini";
        public const bool ReloadConfigOnChange = true;

        private readonly LittleBigBotConfig _appConfig;
        private readonly DiscordSocketClient _client;
        private readonly ILogger _discordLogger;
        private readonly string _configFileLocation;

        private readonly ILogger<LittleBigBot> _logger;
        private readonly IServiceProvider _services;

        public string ApplicationName => _applicationName ?? nameof(LittleBigBot);

        private string _applicationName;

        public LittleBigBot(string configFile = DefaultConfigurationFileLocation)
        {
            _configFileLocation = configFile;
            _services = ConfigureServices(new ServiceCollection());

            _client = _services.GetRequiredService<DiscordSocketClient>();
            _appConfig = _services.GetRequiredService<IOptions<LittleBigBotConfig>>().Value;
            _logger = _services.GetRequiredService<ILogger<LittleBigBot>>();
            _discordLogger = _services.GetRequiredService<ILoggerFactory>().CreateLogger("Discord");
        }

        public static Color DefaultEmbedColour => new Color(0xFFDBF4);

        private IServiceProvider ConfigureServices(IServiceCollection rootCollection)
        {
            var configuration = new ConfigurationBuilder().AddIniFile(_configFileLocation, false, ReloadConfigOnChange).Build();

            var baseServiceType = typeof(BaseService);
            var serviceTypes = Assembly.GetEntryAssembly().GetTypes().Where(a =>
                baseServiceType.IsAssignableFrom(a) && a.GetCustomAttribute<ServiceAttribute>() != null &&
                !a.IsAbstract && a.GetCustomAttribute<ServiceAttribute>().AutoAdd).ToList();

            foreach (var service in serviceTypes)
            {
                var serviceAttribute = service.GetCustomAttribute<ServiceAttribute>();
                rootCollection.Add(ServiceDescriptor.Describe(service, service,
                    serviceAttribute?.Lifetime ?? ServiceLifetime.Singleton));
            }

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
                    IgnoreExtraArguments = true,
                    CooldownBucketKeyGenerator = new LittleBigBotCooldownBucketKeyGenerator()
                }))
                .AddLogging(log => { log.AddLittleBig(); })
                .AddTransient<Random>()
                .AddTransient(a => new WebClient
                    {Headers = new WebHeaderCollection {[HttpRequestHeader.UserAgent] = nameof(LittleBigBot)}})
                .AddSingleton(services =>
                {
                    var options = services.GetRequiredService<IOptions<LittleBigBotConfig>>().Value.GitHub;
                    return new GitHubClient(new ProductHeaderValue(options.Username),
                        new InMemoryCredentialStore(new Credentials(options.Token)));
                })
                .AddSingleton(this)
                .Configure<LittleBigBotConfig>(configuration.Bind)
                .BuildServiceProvider();
        }

        public async Task StartAsync()
        {
            _logger.LogInformation("LittleBigBot client starting up!");

            var serviceTypes = Assembly.GetEntryAssembly().GetTypes().Where(a =>
                typeof(BaseService).IsAssignableFrom(a) && a.GetCustomAttribute<ServiceAttribute>() != null &&
                !a.IsAbstract).ToList();

            foreach (var startupServiceType in serviceTypes)
                if (_services.GetRequiredService(startupServiceType) is BaseService service && startupServiceType.GetCustomAttribute<ServiceAttribute>().AutoInit)
                    await service.InitializeAsync().ConfigureAwait(false);

            _client.Log += HandleLogAsync;

            _client.Ready += async () =>
            {
                _applicationName = (await _client.GetApplicationInfoAsync()).Name;
                await _services.GetRequiredService<CommandHandlerService>().InitializeAsync();
                await _client.SetGameAsync(_appConfig.LittleBigBot.PlayingStatus);
            };

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