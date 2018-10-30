using System;
using System.Threading.Tasks;
using Discord.WebSocket;
using Qmmands;

namespace LittleBigBot.Services
{
    [Name("Interactive")]
    [Description("Provides utility functions to help with creating interactive commands.")]
    public class InteractiveService : BaseService
    {
        public InteractiveService(DiscordSocketClient client)
        {
            Client = client;
        }

        public DiscordSocketClient Client { get; }

        public TimeSpan DefaultMessageTimeout => TimeSpan.FromSeconds(30);

        public async Task<SocketUserMessage> WaitForMessageAsync(Func<SocketUserMessage, bool> predicate,
            TimeSpan? timeout = null)
        {
            timeout = timeout ?? DefaultMessageTimeout;

            var tcs = new TaskCompletionSource<SocketUserMessage>();

            async Task CheckMessageAsync(SocketMessage message)
            {
                await Task.Yield();

                if (message is SocketUserMessage msg && predicate(msg))
                    tcs.SetResult(msg);
            }

            Client.MessageReceived += CheckMessageAsync;

            var tt = tcs.Task;
            var td = Task.Delay(timeout.Value);

            var twa = await Task.WhenAny(tt, td).ConfigureAwait(false);

            Client.MessageReceived -= CheckMessageAsync;

            return twa == tt ? await tt.ConfigureAwait(false) : null;
        }
    }
}