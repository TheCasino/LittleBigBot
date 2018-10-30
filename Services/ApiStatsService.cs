using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LittleBigBot.Attributes;

namespace LittleBigBot.Services
{
    [Service("API Stats", "Provides statistical data about API requests and errors that have occurred in this shard.")]
    public sealed class ApiStatsService : BaseService
    {
        private readonly DiscordSocketClient _client;

        public ApiStatsService(DiscordSocketClient client)
        {
            _client = client;
        }

        public int MessageCreate { get; private set; }
        public int MessageUpdate { get; private set; }
        public int MessageDelete { get; private set; }
        public int Heartbeats { get; private set; }
        public List<int> HeartbeatsList { get; } = new List<int>();
        public double? AverageHeartbeat => HeartbeatsList.Any() ? new double?(HeartbeatsList.Average()) : null;
        public int GuildMadeAvailable { get; private set; }
        public int GuildMadeUnavailable { get; private set; }

        public override Task InitializeAsync()
        {
            _client.MessageReceived += HandleMessageReceivedAsync;
            _client.MessageUpdated += HandleMessageUpdatedAsync;
            _client.MessageDeleted += HandleMessageDeletedAsync;
            _client.LatencyUpdated += HandleHeartbeatAsync;
            _client.GuildAvailable += HandleGuildAvailableAsync;
            _client.GuildUnavailable += HandleGuildUnavailableAsync;
            return Task.CompletedTask;
        }

        public override Task DeinitializeAsync()
        {
            _client.MessageReceived -= HandleMessageReceivedAsync;
            _client.MessageUpdated -= HandleMessageUpdatedAsync;
            _client.MessageDeleted -= HandleMessageDeletedAsync;
            _client.LatencyUpdated -= HandleHeartbeatAsync;
            _client.GuildAvailable -= HandleGuildAvailableAsync;
            _client.GuildUnavailable -= HandleGuildUnavailableAsync;

            MessageCreate = 0;
            MessageUpdate = 0;
            MessageDelete = 0;
            Heartbeats = 0;
            HeartbeatsList.Clear();
            GuildMadeAvailable = 0;
            GuildMadeUnavailable = 0;
            return Task.CompletedTask;
        }

        private Task HandleGuildUnavailableAsync(SocketGuild arg)
        {
            GuildMadeUnavailable++;
            return Task.CompletedTask;
        }

        private Task HandleGuildAvailableAsync(SocketGuild arg)
        {
            GuildMadeAvailable++;
            return Task.CompletedTask;
        }

        private Task HandleMessageReceivedAsync(SocketMessage _)
        {
            MessageCreate++;
            return Task.CompletedTask;
        }

        private Task HandleMessageUpdatedAsync(Cacheable<IMessage, ulong> old, SocketMessage @new,
            ISocketMessageChannel channel)
        {
            MessageUpdate++;
            return Task.CompletedTask;
        }

        private Task HandleMessageDeletedAsync(Cacheable<IMessage, ulong> deleted, ISocketMessageChannel channel)
        {
            MessageDelete++;
            return Task.CompletedTask;
        }

        private Task HandleHeartbeatAsync(int old, int @new)
        {
            Heartbeats++;
            HeartbeatsList.Add(@new);
            return Task.CompletedTask;
        }
    }
}