using System;
using System.Linq;
using Discord.WebSocket;
using LittleBigBot.Checks;
using Microsoft.Extensions.DependencyInjection;
using Qmmands;

namespace LittleBigBot.Entities
{
    public class LittleBigBotExecutionContext : ICommandContext
    {
        public LittleBigBotExecutionContext(SocketUserMessage message, IServiceProvider services)
        {
            Message = message;
            DmChannel = message.Channel as SocketDMChannel;
            GuildChannel = message.Channel as SocketTextChannel;
            Services = services;
            Client = Services.GetRequiredService<DiscordSocketClient>();
            Guild = GuildChannel?.Guild;
            Bot = Client?.CurrentUser;
            BotMember = Bot != null ? Guild?.GetUser(Bot.Id) : null;
            Invoker = message.Author;
            InvokerMember = message.Author as SocketGuildUser;
            Channel = message.Channel;
            Type = IsPrivate ? DiscordContextType.DM :
                GuildChannel != null ? DiscordContextType.Server : DiscordContextType.GroupDM;
        }

        public bool IsPrivate => DmChannel != null;
        public SocketUser Invoker { get; }
        public SocketGuildUser InvokerMember { get; }
        public SocketSelfUser Bot { get; }
        public SocketGuildUser BotMember { get; }
        public DiscordSocketClient Client { get; }
        public IServiceProvider Services { get; }
        public SocketUserMessage Message { get; }
        public SocketDMChannel DmChannel { get; }
        public ISocketMessageChannel Channel { get; }
        public SocketTextChannel GuildChannel { get; }
        public SocketGuild Guild { get; }
        public DiscordContextType Type { get; }

        public string FormatString(Command command)
        {
            return
                $"Executing {command.Aliases.First()} for {Invoker} (ID {Invoker.Id}) in {(GuildChannel != null ? $"#{GuildChannel.Name} (ID {GuildChannel.Id})/{GuildChannel.Guild.Name} (ID {GuildChannel.Guild.Id})" : "their DM channel")}";
        }
    }
}