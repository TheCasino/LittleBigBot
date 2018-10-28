using System;
using LittleBigBot.Common;
using Qmmands;

namespace LittleBigBot.Entities
{
    public class LittleBigBotCooldownBucketKeyGenerator : ICooldownBucketKeyGenerator
    {
        public object GenerateBucketKey(object bucketType, ICommandContext context0, IServiceProvider provider)
        {
            var type = bucketType.Cast<CooldownType>();
            var context = context0.Cast<LittleBigBotExecutionContext>();

            switch (type)
            {
                case CooldownType.Server:
                    return context.Guild?.Id ?? context.Invoker.Id;
                case CooldownType.Channel:
                    return context.Channel.Id;
                case CooldownType.User:
                    return context.Invoker.Id;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}