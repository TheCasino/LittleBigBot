using System;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace LittleBigBot.Services
{
    public class InteractiveService
    {
        public InteractiveService(DiscordSocketClient client)
        {
            Client = client;
        }

        public DiscordSocketClient Client { get; }

        public TimeSpan DefaultMessageTimeout => TimeSpan.FromSeconds(30);

        public async Task<SocketUserMessage> WaitForMessageAsync(Func<SocketUserMessage, bool> predicate, TimeSpan? timeout = null)
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