using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using LittleBigBot.Attributes;
using LittleBigBot.Checks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using LittleBigBot.Services;
using Microsoft.Extensions.Options;
using Octokit;
using Qmmands;

/**
 * Commands in this module
 *
 * - Uptime
 * - LittleBigBot
 * - Feedback
 * - Ping
 * - Permissions
 * - HasPerm
 * - Clean
 * - Stats
 * - DevInfo
 * - SetGame
 * - SetNickname
 * - Script
 * - Inspect
 * - Kill
 */
namespace LittleBigBot.Modules
{
    [Name("LittleBigBot")]
    [Description("Provides helpful commands to help you experiment with the LittleBigBot platform.")]
    public class LittleBigBotModule : LittleBigBotModuleBase
    {
        public IOptions<LittleBigBotConfig> AppConfig { get; set; }
        public CommandService CommandService { get; set; }
        public GitHubClient GHClient { get; set; }
        public IServiceProvider Services { get; set; }
        public ScriptingService Scripting { get; set; }

        [Command("Uptime")]
        [Description("Displays the time that this bot process has been running.")]
        public async Task<BaseResult> Command_GetUptimeAsync()
        {
            return Ok($"**Uptime:** {(DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize(20)}");
        }

        [Command("LittleBigBot", "Meta", "Info", "WhoAreYou", "About")]
        [RunMode(RunMode.Parallel)]
        [Description("Shows some information about me.")]
        public async Task<BaseResult> Command_GetLittleBigBotInfoAsync()
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var response = new EmbedBuilder
            {
                Author = ((SocketUser) Context.BotMember ?? Context.Bot).ToEmbedAuthorBuilder(),
                Color = ((SocketUser) Context.BotMember ?? Context.Bot).GetHighestRoleColourOrDefault(),
                ThumbnailUrl = Context.Client.CurrentUser.GetEffectiveAvatarUrl(),
                Description = string.IsNullOrEmpty(app.Description) ? "None" : app.Description
            };
            var commits =
                await GHClient.Repository.Commit.GetAll(GitHubModule.GitHubRepoOwner, GitHubModule.GitHubRepoName);

            response.Author.Name = "Information about LittleBigBot";

            response
                .AddField("Owner", app.Owner, true)
                .AddField("Uptime", DateTime.Now - Process.GetCurrentProcess().StartTime)
                .AddField("Heartbeat", Context.Client.Latency + "ms", true)
                .AddField("Commands", CommandService.GetCommands().Count(), true)
                .AddField("Modules", CommandService.GetModules().Count(), true)
                .AddField("Support Server", "https://discord.gg/bVUVjSr", true)
                .AddField("Source", $"https://github.com/{GitHubModule.GitHubRepoOwner}/{GitHubModule.GitHubRepoName}/")
                .AddField("Language/Library",
                    $"C# (on the .NET Core stack) | Discord.Net {DiscordConfig.Version} + Qmmands");

            var recentCommitBuilder = new StringBuilder();

            void AddCommit(GitHubCommit commit)
            {
                recentCommitBuilder.AppendLine(
                    $"`{commit.Sha.Substring(0, 7)}`: \"{commit.Commit.Message}\" by {commit.Committer.Login}");
            }

            AddCommit(commits[0]);
            AddCommit(commits[1]);
            AddCommit(commits[2]);
            var embed = new EmbedBuilder();
            if (response.Color != null) embed.WithColor(response.Color.Value);
            embed.WithAuthor(a => a.WithIconUrl(response.Author.IconUrl).WithName("Recent Updates"));
            embed.WithDescription(recentCommitBuilder.ToString());
            return Ok(response, embed);
        }

        [Command("Feedback", "Request")]
        [Description("Sends feedback to the developer.")]
        public async Task<BaseResult> Command_SendFeedbackAsync([Remainder] string feedback)
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var _ = app.Owner.SendMessageAsync(
                $"Feedback from {Context.Invoker} in {Context.Guild?.ToString() ?? "their DM channel"}:\n\"{feedback}\"");

            return Ok("Feedback sent!");
        }

        [Command("Ping")]
        [Description("Benchmarks the connection to the Discord servers.")]
        public async Task<BaseResult> Command_PingAsync()
        {
            var sw = Stopwatch.StartNew();
            var initial = await ReplyAsync("Pinging...");
            var restTime = sw.ElapsedMilliseconds.ToString();

            Task Handler(SocketMessage msg)
            {
                if (msg.Id != initial.Id) return Task.CompletedTask;

                var _ = initial.ModifyAsync(m =>
                {
                    Context.Client.MessageReceived -= Handler;
                    m.Content = new StringBuilder()
                        .AppendLine("**Ping Results!**")
                        .AppendLine("Time: " + DateTime.UtcNow.ToString("R"))
                        .AppendLine($"- Heartbeat: {Context.Client.Latency}ms")
                        .AppendLine($"- REST: {restTime}ms")
                        .AppendLine($"- Round-trip: {sw.ElapsedMilliseconds}ms")
                        .ToString();
                });
                sw.Stop();

                return Task.CompletedTask;
            }

            Context.Client.MessageReceived += Handler;

            return NoResponse();
        }

        [Command("Permissions", "Perms", "PermList", "PermsList", "ListPerms")]
        [Description("Shows a list of a user's current guild-level permissions.")]
        [RequireDiscordContext(DiscordContextType.Server)]
        public async Task<BaseResult> Command_ShowPermissionsAsync(
            [Name("Target")]
            [Description("The user to get permissions for.")]
            [DefaultValueDescription("The user who invoked this command.")]
            SocketGuildUser user = null)
        {
            user = user ?? Context.InvokerMember; // Get the user (or the bot, if none specified)

            var embed = new EmbedBuilder();
            embed.WithAuthor((IUser) user);
            embed.WithColor(LittleBigBot.DefaultEmbedColour);

            if (user.Id == Context.Guild.OwnerId)
            {
                embed.WithDescription("User is owner of server, and has all permissions");
                return Ok(embed);
            }

            if (user.GuildPermissions.Administrator)
            {
                embed.WithDescription("User has Administrator permission, and has all permissions");
                return Ok(embed);
            }

            var guildPerms = user.GuildPermissions; // Get the user's permissions

            var booleanTypeProperties = guildPerms.GetType().GetProperties()
                .Where(a => a.PropertyType.IsAssignableFrom(typeof(bool)))
                .ToList(); // Get all properties that have a property type of Boolean

            var propDict = booleanTypeProperties.Select(a => (a.Name.Humanize(), (bool) a.GetValue(guildPerms)))
                .OrderByDescending(ab => ab.Item2 ? 1 : 0 /* Allowed permissions first */)
                .ToList(); // Store permissions as a tuple of (string Name, bool Allowed) and order by allowed permissions first

            var accept =
                propDict.Where(ab => ab.Item2).OrderBy(a => a.Item1); // Filter an array of accepted permissions
            var deny = propDict.Where(ab => !ab.Item2).OrderBy(a => a.Item2); // Filter an array of denied permissions

            var allowString = string.Join("\n", accept.Select(a => $"- {a.Item1}"));
            var denyString = string.Join("\n", deny.Select(a => $"- {a.Item1}"));
            embed.AddField("Allowed", string.IsNullOrEmpty(allowString) ? "- None" : allowString, true);
            embed.AddField("Denied", string.Join("\n", string.IsNullOrEmpty(denyString) ? "- None" : denyString), true);
            return Ok(embed);
        }

        [Command("HasPerm", "HavePerm", "HavePermission", "HasPermission")]
        [Description("Checks if I have a permission accepted.")]
        [RequireDiscordContext(DiscordContextType.Server)]
        public async Task<BaseResult> Command_HasPermissionAsync(
            [Name("Permission")] [Remainder] [Description("The permission to check for.")]
            string permission)
        {
            var guildPerms = Context.Guild.CurrentUser.GuildPermissions;
            var props = guildPerms.GetType().GetProperties();

            var boolProps = props.Where(a =>
                a.PropertyType.IsAssignableFrom(typeof(bool)) &&
                (a.Name.Equals(permission, StringComparison.OrdinalIgnoreCase) ||
                 a.Name.Humanize().Equals(permission, StringComparison.OrdinalIgnoreCase))).ToList();
            /* Get a list of all properties of Boolean type and that match either the permission specified, or match it   when humanized */

            if (boolProps.Count == 0) return BadRequest("Unknown permission :(");

            var perm = boolProps.First();
            var name = perm.Name.Humanize();
            var value = (bool) perm.GetValue(guildPerms);

            return Ok($"I have permission `{name}`: **{(value ? "Yes" : "No")}**");
        }

        [Command("Stats", "GInfo")]
        [Description("Retrieves statistics about the consumers of this bot.")]
        public async Task<BaseResult> Command_ViewStatsAsync()
        {
            return Ok(
                $"Total Users: {Context.Client.Guilds.SelectMany(a => a.Users).Select(a => a.Id).Distinct().Count()} | Total Guilds: {Context.Client.Guilds.Count}\n{Format.Code(string.Join("\n\n", Context.Client.Guilds.Select(a => $"[Name: {a.Name}, ID: {a.Id}, Members: {a.MemberCount}, Owner: {a.Owner}]")), "ini")}");
        }

        [Command("DevInfo", "DI", "Dev", "Dump")]
        [Description(
            "Dumps current information about the client, the commands system and the current execution environment.")]
        [RequireOwner]
        public async Task<BaseResult> Command_MemoryDumpAsync()
        {
            return Ok(new StringBuilder()
                .AppendLine("```json")
                .AppendLine("== Core ==")
                .AppendLine($"{Context.Client.Guilds.Count} guilds")
                .AppendLine($"{Context.Client.DMChannels.Count} DM channels")
                .AppendLine($"{Context.Client.GroupChannels.Count} group channels")
                .AppendLine("== Commands ==")
                .AppendLine($"{CommandService.GetModules().Count()} modules")
                .AppendLine($"{CommandService.GetCommands().Count()} commands")
                .AppendLine("== Environment ==")
                .AppendLine($"Operating System: {Environment.OSVersion}")
                .AppendLine($"Processor Count: {Environment.ProcessorCount}")
                .AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}")
                .AppendLine($"64-bit Process: {Environment.Is64BitProcess}")
                .AppendLine($"Current Thread ID: {Environment.CurrentManagedThreadId}")
                .AppendLine($"System Name: {Environment.MachineName}")
                .AppendLine($"CLR Version: {Environment.Version}")
                .AppendLine($"Culture: {CultureInfo.InstalledUICulture.EnglishName}")
                .AppendLine("```").ToString());
        }
    }
}