using System;
using System.Linq;
using Discord;
using Discord.WebSocket;

namespace LittleBigBot.Common
{
    public static class UserExtensions
    {
        public static EmbedAuthorBuilder ToEmbedAuthorBuilder(this IUser user)
        {
            var builder = new EmbedAuthorBuilder
            {
                IconUrl = user.GetEffectiveAvatarUrl()
            };

            if (user is IGuildUser guildUser)
                builder.WithName($"{(guildUser.Nickname != null ? $"({guildUser.Nickname}) " : "")}{guildUser.Username}#{guildUser.Discriminator}");
            else builder.WithName(user.ToString());

            return builder;
        }

        public static string GetEffectiveAvatarUrl(this IUser user, ushort size = 128)
        {
            return user.GetAvatarUrl(size: size) ??
                   CDN.GetDefaultUserAvatarUrl(user.DiscriminatorValue) + "?size=" + size;
        }

        public static Color GetHighestRoleColourOrDefault(this IUser normalUser)
        {
            if (!(normalUser is SocketGuildUser user)) return LittleBigBot.DefaultEmbedColour;
            var orderedRoles = user.GetHighestRoleOrDefault(r => r.Color.RawValue != 0);
            return orderedRoles?.Color ?? LittleBigBot.DefaultEmbedColour;
        }

        public static IRole GetHighestRoleOrDefault(this SocketGuildUser user)
        {
            var orderedRoles = user.Roles.OrderByDescending(r => r.Position);
            return orderedRoles.FirstOrDefault();
        }

        public static IRole GetHighestRoleOrDefault(this SocketGuildUser user, Func<IRole, bool> predicate)
        {
            return user.Roles.OrderByDescending(r => r.Position).Where(predicate).FirstOrDefault();
        }

        public static string GetActualName(this SocketUser user)
        {
            if (!(user is SocketGuildUser guildUser)) return user.Username;
            return guildUser.Nickname ?? user.Username;
        }
    }
}