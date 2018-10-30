using System;
using System.Threading.Tasks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using Qmmands;

namespace LittleBigBot.Checks
{
    public class RequireOwnerAttribute : CheckBaseAttribute
    {
        public override async Task<CheckResult> CheckAsync(ICommandContext context0, IServiceProvider provider)
        {
            var context = context0.Cast<LittleBigBotExecutionContext>();

            var owner = (await context.Client.GetApplicationInfoAsync()).Owner;
            var invokerId = context.Invoker.Id;

            return owner.Id == invokerId
                ? CheckResult.Successful
                : new CheckResult($"This command can only be executed by my owner, `{owner}`!");
        }
    }
}