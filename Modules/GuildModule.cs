using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Humanizer;
using LittleBigBot.Checks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using Qmmands;

/**
 * Commands in this module
 *
 * - AuditLog
 * - Users
 * - Server
 */
namespace LittleBigBot.Modules
{
    [Name("Server Information")]
    [Description("Commands that help you interact with your server in useful and efficient ways.")]
    [RequireDiscordContext(DiscordContextType.Server)]
    public class GuildModule : LittleBigBotModuleBase
    {
        // pending rewrite
        /*[Command("AuditLog")]
        [Description("Gets an interactive panel for viewing audit log data for this server.")]
        [RequireBotPermission(GuildPermission.ViewAuditLog)]
        [RequireUserPermission(GuildPermission.ViewAuditLog)]
        public async Task<BaseResult> Command_ViewAuditLogsAsync(
            [Description("The amount of audit logs to download.")] [Name("Download Count")]
            int limit = 50)
        {
            var auditLogs = await Context.Guild.GetAuditLogsAsync(limit).FlattenAsync();
            var sb = new StringBuilder()
                .AppendLine("```json")
                .AppendLine("<< Audit Log Data for " + Context.Guild.Name + " >>")
                .AppendLine("===");

            foreach (var auditLog in auditLogs) sb.AppendLine($"{auditLog.User}: {auditLog.Action.Humanize()}");

            try
            {
                await ReplyAsync(sb.AppendLine("```").ToString());
            }
            catch
            {
                await ReplyAsync("Too many audit logs! Try lowering your request limit.");
            }
        }*/

        [Command("Server", "ServerInfo", "Guild", "GuildInfo")]
        [Description("Grabs information around this server.")]
        [RequireDiscordContext(DiscordContextType.Server)]
        public async Task<BaseResult> Command_GuildInfoAsync()
        {
            var embed = new EmbedBuilder
            {
                Color = LittleBigBot.DefaultEmbedColour,
                Author = new EmbedAuthorBuilder
                {
                    Name = "Information for server " + Context.Guild.Name,
                    IconUrl = Context.Guild.IconUrl
                },
                ThumbnailUrl = Context.Guild.IconUrl,
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Owner",
                        Value = Context.Guild.Owner.ToString(),
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Created At",
                        Value = UserModule.FormatOffset(Context.Guild.CreatedAt),
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Channels",
                        Value = Context.Guild.Channels.Select(c => c.Name).Join(", ") + " (" +
                                Context.Guild.Channels.Count + ")",
                        IsInline = true
                    },
                    new EmbedFieldBuilder
                    {
                        Name = "Voice Channels",
                        Value = Context.Guild.VoiceChannels.Select(vc => vc.Name).Join(", ") + " (" +
                                Context.Guild.VoiceChannels.Count + ")",
                        IsInline = true
                    }
                }
            };

            return Ok(embed);
        }
    }
}