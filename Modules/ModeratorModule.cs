using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using LittleBigBot.Attributes;
using LittleBigBot.Checks;
using LittleBigBot.Entities;
using Qmmands;

/**
 * Commands in this module
 *
 * - Ban
 */
namespace LittleBigBot.Modules
{
    [Name("Moderation")]
    [Description("Commands that help you moderate and protect your server.")]
    [RequireDiscordContext(DiscordContextType.Server)]
    public class ModeratorModule : LittleBigBotModuleBase
    {
        [Command("Ban", "Banno", "Banne", "Permaban", "Dropkick")]
        [Description("Bans a member from this server.")]
        [Remarks("Requires that both the user and the bot have the 'Ban Members' permission.")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireBotPermission(GuildPermission.BanMembers)]
        public async Task BanUserAsync(
            [Name("Ban Target")] [Description("The user to ban.")]
            SocketGuildUser target,
            [Name("Ban Reason")] [Description("The audit log reason for the ban.")] [DefaultValueDescription("None")]
            string reason = null,
            [Name("Prune Day Count")] [Description("The amount of days going back that messages sent from this user will be removed.")]
            int pruneDays = 0)
        {
            if (target.Id == Context.Invoker.Id)
            {
                await ReplyAsync("I can't ban you!");
                return;
            }

            if (target.Id == Context.Client.CurrentUser.Id)
            {
                await ReplyAsync("I can't ban myself!");
                return;
            }

            try
            {
                await target.BanAsync(pruneDays,
                    reason != null
                        ? $"Action performed by {Context.Invoker} (ID {Context.Invoker.Id}) with reason: {reason}"
                        : $"Action performed by {Context.Invoker} (ID {Context.Invoker.Id}) with no reason");
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
            {
                await ReplyAsync(
                    $":negative_squared_cross_mark: Cannot ban '{target.Nickname ?? target.Username}' because that user is more powerful than me!");
                return;
            }

            await ReplyAsync(
                $":white_check_mark: Banned '{target.Nickname ?? target.Username}'{(reason != null ? " with reason '" + reason + "'" : "")}. {(pruneDays != 0 ? $"Removing {pruneDays} worth of messages from them." : "")}");
        }
    }
}