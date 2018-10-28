using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace LittleBigBot.Services
{
    public class DaemonService
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);
        private readonly DiscordSocketClient _discord;
        private readonly ILogger<DaemonService> _logger;
        private CancellationTokenSource _cts;

        public DaemonService(DiscordSocketClient discord, ILogger<DaemonService> logger)
        {
            _cts = new CancellationTokenSource();
            _discord = discord;
            _logger = logger;

            _discord.Connected += ConnectedAsync;
            _discord.Disconnected += DisconnectedAsync;
        }

        public Task ConnectedAsync()
        {
            // Cancel all previous state checks and reset the CancelToken - client is back online
            _logger.LogDebug("Client reconnected, resetting cancellation tokens...");
            _cts.Cancel();
            _cts = new CancellationTokenSource();

            return Task.CompletedTask;
        }

        public Task DisconnectedAsync(Exception e)
        {
            _logger.LogDebug("Client disconnected, starting timeout...");
            Task.Delay(Timeout, _cts.Token).ContinueWith(async _ =>
            {
                _logger.LogDebug("Timeout expired, checking client state...");
                await CheckStateAsync();
                _logger.LogDebug("Client state is OK");
            });

            return Task.CompletedTask;
        }

        private async Task CheckStateAsync()
        {
            if (_discord.ConnectionState == ConnectionState.Connected) return;

            _logger.LogDebug("Attempting to reset client...");

            var timeout = Task.Delay(Timeout);
            var connect = _discord.StartAsync();
            var task = await Task.WhenAny(timeout, connect);

            if (task == timeout)
            {
                _logger.LogCritical("Client reset timed out (deadlock?), killing process...");
                Fail();
            }
            else if (connect.IsFaulted)
            {
                _logger.LogCritical(connect.Exception, "Client reset faulted, killing process...");
                Fail();
            }
            else if (connect.IsCompletedSuccessfully)
            {
                _logger.LogDebug("Client reset successfully!");
            }
        }

        private void Fail()
        {
            Environment.Exit(1);
        }
    }
}