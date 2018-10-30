using System;
using System.Threading.Tasks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using Qmmands;

namespace LittleBigBot.Checks
{
    public class RequireNsfwAttribute : CheckBaseAttribute
    {
        public override Task<CheckResult> CheckAsync(ICommandContext context0, IServiceProvider provider)
        {
            var context = context0.Cast<LittleBigBotExecutionContext>();

            if (context.GuildChannel == null || !context.GuildChannel.IsNsfw)
                return Task.FromResult(new CheckResult("This command can only be used in an NSFW channel."));
            return Task.FromResult(CheckResult.Successful);
        }
    }
}