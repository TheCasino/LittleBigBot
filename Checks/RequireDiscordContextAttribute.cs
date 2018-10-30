using System;
using System.Threading.Tasks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using Qmmands;

namespace LittleBigBot.Checks
{
    public class RequireDiscordContextAttribute : CheckBaseAttribute
    {
        private readonly DiscordContextType _type;

        public RequireDiscordContextAttribute(DiscordContextType type)
        {
            _type = type;
        }

        public override Task<CheckResult> CheckAsync(ICommandContext context0, IServiceProvider provider)
        {
            var context = context0.Cast<LittleBigBotExecutionContext>();
            return _type.HasFlag(context.Type)
                ? Task.FromResult(CheckResult.Successful)
                : Task.FromResult(new CheckResult(
                    $"This command can only be used in a {_type:G}, but we're currently in a {context.Type:G}"));
        }
    }
}