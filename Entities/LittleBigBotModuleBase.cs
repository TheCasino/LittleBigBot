using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using NLog;
using Qmmands;

namespace LittleBigBot.Entities
{
    internal static class LogStateContainer
    {
        internal static readonly ConcurrentDictionary<ulong, IDisposable> LoggerStates = new ConcurrentDictionary<ulong, IDisposable>();
    }

    public abstract class LittleBigBotModuleBase : ModuleBase<LittleBigBotExecutionContext>
    {
        protected Logger Logger { get; set; }

        protected override Task BeforeExecutedAsync(Command command)
        {
            Logger = LogManager.GetLogger(GetType().Name);
            LogStateContainer.LoggerStates[Context.Invoker.Id] = NestedDiagnosticsLogicalContext.Push($"Executing {command.Name} for {Context.Invoker} (ID {Context.Invoker.Id}) in {(Context.IsPrivate ? "their DM channel" : $"#{Context.Channel.Name} (ID {Context.Channel.Id})/{Context.Guild.Name} (ID {Context.Guild.Id})")} [Thread {Thread.CurrentThread.ManagedThreadId}]");
            return base.BeforeExecutedAsync(command);
        }

        protected override Task AfterExecutedAsync(Command command)
        {
            if (LogStateContainer.LoggerStates.TryRemove(Context.Invoker.Id, out var state))
                state.Dispose();
            return base.AfterExecutedAsync(command);
        }

        public Task<RestUserMessage> ReplyAsync(string content = "", bool isTts = false, Embed embed = null, RequestOptions options = null)
        {
            return Context.Channel.SendMessageAsync(content, isTts, embed, options);
        }
    }
}