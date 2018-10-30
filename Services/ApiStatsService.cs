using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Qmmands;

namespace LittleBigBot.Services
{
    [Name("API Stats")]
    [Description("Provides statistical data about API requests and errors that have occurred in this shard.")]
    public sealed class ApiStatsService : BaseService
    {
        public ApiStatsService(DiscordSocketClient client)
        {
            client.MessageReceived += HandleMessageReceivedAsync;
            client.MessageUpdated += HandleMessageUpdatedAsync;
            client.MessageDeleted += HandleMessageDeletedAsync;
            client.LatencyUpdated += HandleHeartbeatAsync;
            client.GuildAvailable += HandleGuildAvailableAsync;
            client.GuildUnavailable += HandleGuildUnavailableAsync;
        }

        public int MessageCreate { get; private set; }
        public int MessageUpdate { get; private set; }
        public int MessageDelete { get; private set; }
        public int Heartbeats { get; private set; }
        public List<int> HeartbeatsList { get; } = new List<int>();
        public double? AverageHeartbeat => HeartbeatsList.Any() ? new double?(HeartbeatsList.Average()) : null;
        public int GuildMadeAvailable { get; private set; }
        public int GuildMadeUnavailable { get; private set; }

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