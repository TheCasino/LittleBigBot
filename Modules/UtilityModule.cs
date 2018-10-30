using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LittleBigBot.Attributes;
using LittleBigBot.Checks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using LittleBigBot.Services;
using Qmmands;

/**
 * Commands in this module
 *
 * - Echo
 * - Echod
 * - Delete
 * - ClearAll
 * - Time
 */
namespace LittleBigBot.Modules
{
    [Name("Utility")]
    [Description("Commands that provide useful utilities.")]
    public class UtilityModule : LittleBigBotModuleBase
    {
        public SpoilerService SpoilerService { get; set; }
        public InteractiveService InteractiveService { get; set; }
        public DiscordSocketClient Client { get; set; }

        [Command("Spoiler", "CreateSpoiler")]
        [Description("Creates a spoiler message, direct messaging users who would like to see the spoiler.")]
        [RequireDiscordContext(DiscordContextType.Server)]
        public async Task<BaseResult> Command_CreateSpoilerAsync(
            [Name("Safe Text")] [Description("A name for the spoiler, that everyone will be able to see.")]
            string safe, [Name("Spoiler")] [Description("The content of the spoiler.")] [Remainder]
            string spoiler)
        {
            await SpoilerService.CreateSpoilerMessageAsync(Context, safe, spoiler).ConfigureAwait(false);
            return NoResponse();
        }

        [Command("Echo")]
        [Description("Echoes the input text.")]
        public async Task<BaseResult> Command_EchoAsync([Name("Text")] [Remainder] string echocontent)
        {
            return Context.Invoker.Id == (await Context.Client.GetApplicationInfoAsync()).Owner.Id
                ? Ok(echocontent)
                : Ok(string.IsNullOrWhiteSpace(echocontent)
                    ? "Nothing provided."
                    : $"{Context.Invoker}: {echocontent}");
        }

        [Command("Echod")]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        [Description("Attempts to delete the source message, and then echoes the input text.")]
        public async Task<BaseResult> Command_EchoDeleteAsync([Name("Text")] [Remainder] string echocontent)
        {
            try
            {
                Context.Message.DeleteAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // ignored
            }

            return Context.Invoker.Id == (await Context.Client.GetApplicationInfoAsync()).Owner.Id
                ? Ok(echocontent)
                : Ok(string.IsNullOrWhiteSpace(echocontent)
                    ? "Nothing provided."
                    : $"{Context.Invoker}: {echocontent}");
        }

        [Command("Delete")]
        [Description("Deletes a message by ID.")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task<BaseResult> Command_DeleteMessageAsync(
            [Name("Message")] [Description("The ID of the message to delete.")]
            ulong messageId,
            [Name("Silence")] [Description("Whether to respond with confirmation of the deletion.")]
            bool silent = false)
        {
            try
            {
                var message = await Context.Channel.GetMessageAsync(messageId);
                try
                {
                    var opt = RequestOptions.Default;
                    opt.AuditLogReason = $"Requested by {Context.Invoker} at {DateTime.UtcNow.ToUniversalTime():F}";
                    await message.DeleteAsync(opt);
                    if (!silent) return Ok($"Deleted message {messageId}.");
                    return NoResponse();
                }
                catch (Exception)
                {
                    return BadRequest("Failed to delete message. Do I have permissions?");
                }
            }
            catch (Exception)
            {
                return BadRequest("Failed to get message, did you pass an invalid ID?");
            }
        }

        [Command("ClearAll")]
        [Description("Clears a number of messages from a source message, in a certain direction.")]
        [RequireUserPermission(ChannelPermission.ManageMessages)]
        [RequireBotPermission(ChannelPermission.ManageMessages)]
        public async Task<BaseResult> Command_ClearAllAsync(
            [Name("Count")] [Description("The number of messages to delete.")]
            int count = 50,
            [Name("Direction")] [Description("The direction to delete in.")]
            Direction direction = Direction.Before,
            [Name("Source Message")] [Description("The ID of the message to start at.")]
            ulong sourceMessageId = 0
        )
        {
            if (sourceMessageId == 0) sourceMessageId = Context.Message.Id;

            if (count > 100) return BadRequest("Cannot delete more than 50 messages.");

            if (!(Context.Channel is SocketTextChannel gc))
                return BadRequest("Due to API limitations, this command can only be used in a server channel.");

            var messages = (await gc.GetMessagesAsync(sourceMessageId, direction, count).FlattenAsync()).ToList();

            await gc.DeleteMessagesAsync(messages);

            return Ok($"Deleted `{messages.Count}` messages.");
        }

        [Command("Time", "TimeZone", "TZ", "TimeNow")]
        [Description("Displays the current time in a specific timezone.")]
        [Remarks(
            "This command is difficult and unwieldly to use, because timezone data changes depending on the host platform for the bot.")]
        public Task<BaseResult> Command_GetTimeAsync(
            [Name("Timezone")] [Description("The timezone to view time data for.")] [DefaultValueDescription("The bot will show you a list of all timezones available on the system.")] [Remainder]
            string timezone)
        {
            timezone = timezone.Replace(" ", "_").Replace("UTC", "GMT").Replace("US", "America")
                .Replace("USA", "America");

            var timezones = TimeZoneInfo.GetSystemTimeZones();

            var idMatches = timezones.Where(a => a.Id.Equals(timezone, StringComparison.OrdinalIgnoreCase)).ToList();
            var displayMatches = timezones
                .Where(a => a.DisplayName.Equals(timezone, StringComparison.OrdinalIgnoreCase)).ToList();
            var standardMatches = timezones
                .Where(a => a.StandardName.Equals(timezone, StringComparison.OrdinalIgnoreCase)).ToList();

            TimeZoneInfo tz = null;
            var timezoneIds = "";
            if (idMatches.Any())
            {
                tz = idMatches.First();
                timezoneIds = string.Join(", ", idMatches.Select(a => a.Id));
            }

            if (displayMatches.Any())
            {
                tz = displayMatches.First();
                timezoneIds = string.Join(", ", displayMatches.Select(a => a.Id));
            }

            if (standardMatches.Any())
            {
                tz = standardMatches.First();
                timezoneIds = string.Join(", ", standardMatches.Select(a => a.Id));
            }

            var content = tz == null
                ? $"Cannot find timezone data for ``{timezone}``."
                : $"**{tz.StandardName} ({timezoneIds})**: {FormatTimezone(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz))}";

            return Ok(content);
        }

        private static string FormatTimezone(DateTime time)
        {
            return time.ToString("F");
        }

        [Command("Quote")]
        [Description("Quotes a message sent by a user.")]
        public async Task<BaseResult> Command_QuoteMessageAsync([Name("ID")] [Description("The ID of the message.")]
            ulong messageId)
        {
            var message = Context.Channel.GetCachedMessage(messageId) ??
                          await Context.Channel.GetMessageAsync(messageId);

            if (message == null) return BadRequest("Cannot find message.");

            var jumpurl = message.GetJumpUrl();

            var embed = new EmbedBuilder();
            embed.WithAuthor(new EmbedAuthorBuilder
            {
                Name = message.Author.ToString(),
                IconUrl = message.Author.GetEffectiveAvatarUrl(),
                Url = jumpurl
            });
            embed.WithTimestamp(message.Timestamp);
            embed.WithColor(message.Author.GetHighestRoleColourOrDefault());
            embed.WithDescription((string.IsNullOrWhiteSpace(message.Content) ? "<< No content >>" : message.Content) +
                                  "\n\n" + jumpurl);

            if (message.Attachments.Any())
            {
                var attach0 = message.Attachments.FirstOrDefault();
                if (attach0 != null) embed.WithImageUrl(attach0.Url);
            }

            return Ok(embed);
        }
    }
}