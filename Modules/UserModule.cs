using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using LittleBigBot.Attributes;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using Qmmands;

/**
 * Commands in this module
 *
 * - Avatar
 * - Hug
 * - User
 * - Spotify
 */
namespace LittleBigBot.Modules
{
    [Name("User Information")]
    [Description("Commands that help you interact with other Discord users.")]
    public class UserModule : LittleBigBotModuleBase
    {
        [Command("Avatar", "GetAvatar", "Picture", "Ava", "Av", "Pfp")]
        [Description("Grabs the avatar for a user.")]
        public Task<BaseResult> Command_GetAvatarAsync(
            [Name("User")] [Description("The user who you wish to get the avatar for.")] [DefaultValueDescription("The user who invoked this command.")]
            SocketUser target = null,
            [Name("Image_Size")] [Description("The size of the resulting image.")]
            int size = 1024)
        {
            target = target ?? Context.Invoker;

            return Ok(new EmbedBuilder
            {
                Author = target.ToEmbedAuthorBuilder().WithName($"Avatar for {target}"),
                ImageUrl = target.GetEffectiveAvatarUrl(Convert.ToUInt16(size))
            });
        }

        [Command("User", "UserInfo", "SnoopOn", "GetUser")]
        [Description("Grabs information around a member.")]
        public Task<BaseResult> Command_GetUserInfoAsync(
            [Name("Member")] [Description("The user to get information for.")] [DefaultValueDescription("The user who invoked this command.")]
            SocketUser member = null)
        {
            member = member ?? Context.Invoker;

            var embed = new EmbedBuilder
            {
                ThumbnailUrl = member.GetEffectiveAvatarUrl(256),
                Title = $"Information for user {member}",
                Color = member.GetHighestRoleColourOrDefault()
            };

            embed.AddField("Created", FormatOffset(member.CreatedAt), true);

            if (member is SocketGuildUser guildUser)
            {
                if (guildUser.JoinedAt != null) embed.AddField("Joined", FormatOffset(guildUser.JoinedAt.Value), true);
                embed.AddField("Position",
                    guildUser.Hierarchy == int.MaxValue ? "Server Owner" : "Position " + guildUser.Hierarchy, true);

                embed.AddField("Deafened", guildUser.IsDeafened, true);
                embed.AddField("Muted", guildUser.IsMuted, true);
                embed.AddField("Nickname", guildUser.Nickname ?? "None", true);
                embed.AddField("Voice Status", GetVoiceChannelStatus(guildUser), true);

                var roles = guildUser.Roles.Where(r => !r.IsEveryone);
                var socketRoles = roles as SocketRole[] ?? roles.ToArray();
                if (socketRoles.Length != 0)
                    embed.AddField("Roles", string.Join(", ", socketRoles.Select(r => r.Name)));
            }

            if (member.Activity != null)
                embed.AddField("Activity",
                    $"{member.Activity.Type.Humanize()} {member.Activity.Name}", true);
            embed.AddField("Status", member.Status.Humanize(), true);
            embed.AddField("Is Bot or Webhook", member.IsBot || member.IsWebhook, true);

            return Ok(embed);
        }

        [Command("Hug")]
        [Description("Gives them all your hugging potential.")]
        public Task<BaseResult> Command_HugUserAsync([Name("Member")] [Description("The user to hug.")]
            SocketUser hugee)
        {
            if (hugee.Id == Context.Invoker.Id) return BadRequest("**You can't hug yourself! (Sadly)**");

            return Ok($"**{Context.Invoker.GetActualName()}** hugs **{hugee.GetActualName()}**!");
        }

        public static string FormatOffset(DateTimeOffset offset)
        {
            return offset.DateTime.ToUniversalTime().ToString("s", CultureInfo.InvariantCulture);
        }

        private string GetVoiceChannelStatus(SocketGuildUser user)
        {
            return user.VoiceState == null ? "Not in a voice channel" : $"In {user.VoiceChannel.Name}";
        }
    }
}