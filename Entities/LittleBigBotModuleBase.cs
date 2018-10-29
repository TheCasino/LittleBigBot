using System;
using System.Collections.Concurrent;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using LittleBigBot.Results;
using NLog;
using Octokit;
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

        protected Task<RestUserMessage> ReplyAsync(string content = "", bool isTts = false, Embed embed = null, RequestOptions options = null)
        {
            return Context.Channel.SendMessageAsync(content, isTts, embed, options);
        }

        protected OkResult Ok(string content, params EmbedBuilder[] embed)
        {
            return new OkResult(content, embed);
        }

        protected OkResult Ok(string content)
        {
            return new OkResult(content);
        }

        protected OkResult Ok(params EmbedBuilder[] builder)
        {
            return new OkResult(null, builder);
        }

        protected OkResult Ok(Action<EmbedBuilder> actor)
        {
            var eb = new EmbedBuilder();
            actor(eb);
            return Ok(eb);
        }

        protected BadRequestResult BadRequest(string error = null)
        {
            return new BadRequestResult(error);
        }

        protected NotFoundResult NotFound(string error = null)
        {
            return new NotFoundResult(error);
        }

        protected NoResponseResult NoResponse()
        {
            return new NoResponseResult();
        }
    }
}