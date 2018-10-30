using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using Qmmands;

namespace LittleBigBot.Parsers
{
    internal class UserParseResolveResult<T>
    {
        public float Score { get; set; }
        public T Value { get; set; }
    }

    public class DiscordUserTypeParser<T> : TypeParser<T> where T : class, IUser
    {
        public override async Task<TypeParserResult<T>> ParseAsync(string value, ICommandContext context0,
            IServiceProvider provider)
        {
            var context = context0.Cast<LittleBigBotExecutionContext>();
            var results = new Dictionary<ulong, UserParseResolveResult<T>>();
            var channelUsers = context.Channel.GetUsersAsync(CacheMode.CacheOnly).Flatten();
            IReadOnlyCollection<IGuildUser> guildUsers = ImmutableArray.Create<IGuildUser>();

            if (context.Guild != null)
                guildUsers = await ((IGuild) context.Guild).GetUsersAsync(CacheMode.CacheOnly).ConfigureAwait(false);

            //By Mention (1.0)
            if (MentionUtils.TryParseUser(value, out var id))
            {
                if (context.Guild != null)
                    AddResult(results,
                        await ((IGuild) context.Guild).GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T,
                        1.00f);
                else
                    AddResult(results,
                        await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 1.00f);
            }

            //By Id (0.9)
            if (ulong.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                if (context.Guild != null)
                    AddResult(results,
                        await ((IGuild) context.Guild).GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T,
                        0.90f);
                else
                    AddResult(results,
                        await context.Channel.GetUserAsync(id, CacheMode.CacheOnly).ConfigureAwait(false) as T, 0.90f);
            }

            var index = value.LastIndexOf('#');
            if (index >= 0)
            {
                var username = value.Substring(0, index);
                if (ushort.TryParse(value.Substring(index + 1), out var discriminator))
                {
                    var channelUser = await channelUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                                                                             string.Equals(username, x.Username,
                                                                                 StringComparison.OrdinalIgnoreCase))
                        .ConfigureAwait(false);
                    AddResult(results, channelUser as T, channelUser?.Username == username ? 0.85f : 0.75f);

                    var guildUser = guildUsers.FirstOrDefault(x => x.DiscriminatorValue == discriminator &&
                                                                   string.Equals(username, x.Username,
                                                                       StringComparison.OrdinalIgnoreCase));
                    AddResult(results, guildUser as T, guildUser?.Username == username ? 0.80f : 0.70f);
                }
            }

            {
                await channelUsers
                    .Where(x => string.Equals(value, x.Username, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser =>
                        AddResult(results, channelUser as T, channelUser.Username == value ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in guildUsers.Where(x =>
                    string.Equals(value, x.Username, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, guildUser as T, guildUser.Username == value ? 0.60f : 0.50f);
            }

            {
                await channelUsers
                    .Where(x => string.Equals(value, (x as IGuildUser)?.Nickname, StringComparison.OrdinalIgnoreCase))
                    .ForEachAsync(channelUser => AddResult(results, channelUser as T,
                        (channelUser as IGuildUser)?.Nickname == value ? 0.65f : 0.55f))
                    .ConfigureAwait(false);

                foreach (var guildUser in guildUsers.Where(x =>
                    string.Equals(value, x.Nickname, StringComparison.OrdinalIgnoreCase)))
                    AddResult(results, guildUser as T, guildUser.Nickname == value ? 0.60f : 0.50f);
            }

            return results.Count > 0
                ? new TypeParserResult<T>(results.Values.OrderByDescending(a => a.Value).First().Value)
                : new TypeParserResult<T>("User not found.");
        }

        private void AddResult(IDictionary<ulong, UserParseResolveResult<T>> results, T user, float score)
        {
            if (user != null && !results.ContainsKey(user.Id))
                results.Add(user.Id, new UserParseResolveResult<T> {Score = score, Value = user});
        }
    }
}