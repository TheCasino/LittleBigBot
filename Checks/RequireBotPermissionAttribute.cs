using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using Qmmands;

namespace LittleBigBot.Checks
{
    public class RequireBotPermissionAttribute : CheckBaseAttribute
    {
        private readonly List<ChannelPermission> _channelPermissions = new List<ChannelPermission>();
        private readonly List<GuildPermission> _guildPermissions = new List<GuildPermission>();

        public RequireBotPermissionAttribute(params ChannelPermission[] permissions)
        {
            _channelPermissions.AddRange(permissions);
        }

        public RequireBotPermissionAttribute(params GuildPermission[] permissions)
        {
            _guildPermissions.AddRange(permissions);
        }

        public override Task<CheckResult> CheckAsync(ICommandContext context0, IServiceProvider provider)
        {
            var context = context0.Cast<LittleBigBotExecutionContext>();

            if (context.BotMember != null)
            {
                var cperms = context.BotMember.GetPermissions(context.GuildChannel);
                foreach (var gperm in _guildPermissions)
                    if (!context.BotMember.GuildPermissions.Has(gperm))
                        return Task.FromResult(new CheckResult($"This command requires me to have the \"{gperm.Humanize()}\" server-level permission, but I do not have it!"));

                foreach (var cperm in _channelPermissions)
                    if (!cperms.Has(cperm))
                        return Task.FromResult(new CheckResult($"This command requires me to have the \"{cperm.Humanize()}\" channel-level permission, but I do not have it!"));
            }

            return Task.FromResult(CheckResult.Successful);
        }
    }
}