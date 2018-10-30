using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using LittleBigBot.Attributes;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Qmmands;

/**
 * Commands in this module
 * - Help [all]
 * - Help [command/module]
 */
namespace LittleBigBot.Modules
{
    [Name("Help")]
    [Description("Contains helpful commands to help you discover your way around the LittleBigBot platform.")]
    public class HelpModule : LittleBigBotModuleBase
    {
        public static readonly ImmutableDictionary<Type, (string Singular, string Multiple)> FriendlyNames =
            new Dictionary<Type, (string, string)>(11)
            {
                [typeof(SocketUser)] = ("a Discord user", "a list of Discord users"),
                [typeof(SocketGuildUser)] = ("a server member", "a list of server members"),
                [typeof(string)] = ("any text", "a list of words"),
                [typeof(int)] = ("any number", "a list of numbers"),
                [typeof(bool)] = ("true or false", "a list of true or false values"),
                [typeof(SocketRole)] = ("any role", "a list of roles"),
                [typeof(SocketTextChannel)] = ("any text channel", "a list of text channels"),
                [typeof(ActivityType)] = ("an activity type (e.g. playing, streaming)",
                    "a list of activity types (e.g. playing, streaming)"),
                [typeof(Color)] = ("a colour", "a list of colours"),
                [typeof(Direction)] = ("before, around or after", "a list of options from [before, around, after]"),
                [typeof(ulong)] = ("a Discord entity ID", "a list of Discord entity IDs")
            }.ToImmutableDictionary();

        public CommandService CommandService { get; set; }

        public DiscordSocketClient Client { get; set; }

        public IOptions<LittleBigBotConfig> AppConfig { get; set; }

        public IServiceProvider Services { get; set; }

        [Command("Prefix")]
        [Description("Shows you the current prefix.")]
        public Task<BaseResult> Command_ViewPrefixesAsync()
        {
            return Task.FromResult(
                Ok($"The current prefix is ``{AppConfig.Value.LittleBigBot.Prefix}``.") as BaseResult);
        }

        [Command("Help", "Commands")]
        [Description("Retrieves a list of commands that you can use.")]
        [Remarks(
            "Use `command <command name>` to see help on a specific command, or `module <module name>` for help on a specific module.")]
        public async Task<BaseResult> Command_ListCommandsAsync()
        {
            var sb = new StringBuilder();
            sb
                .AppendLine("**__LittleBigBot Commands__**")
                .AppendLine("Here is a list of commands you can use.")
                .AppendLine(
                    $"You can use `{AppConfig.Value.LittleBigBot.Prefix}help <command name>` to see help on a specific command, or `{AppConfig.Value.LittleBigBot.Prefix}module <module name>` to see help on a specific module.")
                .AppendLine();

            async Task AppendModules(IEnumerable<Module> modules, Module parent, int indent = -4)
            {
                indent += 4;
                foreach (var module in modules.Where(module =>
                    !module.HasAttribute<HiddenAttribute>() && (module.Parent == null || module.Parent == parent)))
                {
                    var list = new List<string>();
                    foreach (var command in module.Commands.Where(command => !command.HasAttribute<HiddenAttribute>()))
                    {
                        if (!await CanShowCommandAsync(command)) continue;
                        list.Add(FormatCommandShort(command));
                    }

                    var cr = string.Join(", ", list);
                    sb.AppendLine(new string(' ', indent) +
                                  $"- **{module.Name}:** {(!list.Any() ? "<No commands available>" : cr)}");
                    await AppendModules(module.Submodules, module, indent + 4);
                }
            }

            await AppendModules(CommandService.GetModules(), null);

            return Ok(sb.ToString());
        }

        private async Task<bool> CanShowCommandAsync(Command command)
        {
            if (!(await command.RunChecksAsync(Context, Services).ConfigureAwait(false)).IsSuccessful)
                return false;
            return !command.HasAttribute<HiddenAttribute>();
        }

        private static string FormatCommandShort(Command command)
        {
            return Format.Code(command.FullAliases.FirstOrDefault() ?? "[Error]");
        }

        [Command("Module", "ModuleInfo", "MInfo", "M")]
        [Description("Displays information about a LittleBigBot module.")]
        [Remarks("You can use the 'Help <command>' command for more information about a specific command.")]
        public Task<BaseResult> Command_GetModuleInfoAsync([Remainder] string query)
        {
            var module = CommandService.GetModules().Search(query.Replace("\"", ""));
            if (module == null) return Result(NotFound($"No module found for `{query}`."));

            var embed = new EmbedBuilder
            {
                Timestamp = DateTimeOffset.Now, Color = LittleBigBot.DefaultEmbedColour,
                Title = $"Module '{module.Name}'"
            };

            if (!string.IsNullOrWhiteSpace(module.Description)) embed.Description = module.Description;

            if (module.Parent != null) embed.AddField("Parent", module.Parent.Aliases.FirstOrDefault());

            var commands = module.Commands.Where(a => !a.HasAttribute<HiddenAttribute>()).ToList();

            embed.AddField("Commands",
                commands.Any()
                    ? string.Join(", ", commands.Select(a => a.Aliases.FirstOrDefault())) + " (" + commands.Count + ")"
                    : "None (all hidden)");

            return Result(Ok(embed));
        }

        public static Embed CreateCommandEmbed(Command command, LittleBigBotExecutionContext context)
        {
            var embed = new EmbedBuilder
            {
                Title = $"Command '{command.Aliases.FirstOrDefault()}'",
                Color = context.Invoker.GetHighestRoleColourOrDefault(),
                Description = string.IsNullOrEmpty(command.Description) ? "None" : command.Description,
                Timestamp = DateTimeOffset.Now
            };
            embed.AddField("Aliases", string.Join(", ", command.Aliases), true);
            var index = 1;
            if (command.Parameters.Any())
                embed.AddField("Parameters",
                    string.Join("\n", command.Parameters.Select(p => $"**{index++})** {FormatParameter(p)}")));
            if (command.Remarks != null) embed.AddField("Remarks", command.Remarks);
            embed.AddField("Usage", FormatUsageString(command, context.Services));
            embed.WithFooter("You can use quotes to encapsulate inputs that are more than one word long.",
                context.Bot.GetEffectiveAvatarUrl());

            if (command.HasAttribute<ThumbnailAttribute>(out var imageUrlAttribute))
            {
                embed.Author = new EmbedAuthorBuilder().WithName($"Command {command.Aliases.FirstOrDefault()}")
                    .WithIconUrl(imageUrlAttribute.ImageUrl);
                embed.Title = null;
            }

            return embed.Build();
        }

        [Command("Help", "CommandInfo", "CInfo", "C")]
        [Description("Displays information about a LittleBigBot command.")]
        [Remarks("You can use the 'Module' command for more information about a specific module.")]
        public Task<BaseResult> Command_GetCommandInfoAsync([Remainder] string query)
        {
            var search = CommandService.FindCommands(query).ToList();
            if (!search.Any()) return Result(NotFound($"No command found for `{query}`."));

            return Result(Ok(search.Where(c => !c.Command.HasAttribute<HiddenAttribute>())
                .Select(a => CreateCommandEmbed(a.Command, Context).ToEmbedBuilder()).ToArray()));
        }

        private static string FormatParameter(Parameter parameterInfo)
        {
            var fres = FriendlyNames.GetValueOrDefault(parameterInfo.Type);

            var typename = parameterInfo.Type.IsEnum
                ? string.Join(", ", parameterInfo.Type.GetEnumNames())
                : parameterInfo.Type.ToString();

            var type = parameterInfo.IsMultiple ? fres.Multiple ?? typename : fres.Singular ?? typename;

            return
                $"`{parameterInfo.Name}`: {type}{FormatParameterTags(parameterInfo)}";
        }

        private static string FormatParameterTags(Parameter parameterInfo)
        {
            var sb = new StringBuilder();

            sb.AppendLine();

            if (!string.IsNullOrEmpty(parameterInfo.Description))
                sb.AppendLine($"- Description: {parameterInfo.Description}");

            sb.AppendLine(
                $"- Optional: {(parameterInfo.HasAttribute<ParameterArrayOptionalAttribute>() ? "Yes" : parameterInfo.IsOptional ? "Yes" : "No")}");

            if (!parameterInfo.IsOptional) return sb.ToString();

            if (parameterInfo.HasAttribute<DefaultValueDescriptionAttribute>(out var defaultValueDescription))
                sb.AppendLine($" - Default: {defaultValueDescription.DefaultValueDescription}");
            else if (parameterInfo.DefaultValue != null)
                sb.AppendLine(" - Default: " + parameterInfo.DefaultValue);
            else
                sb.AppendLine(" - Default: None specified");

            return sb.ToString();
        }

        private static string FormatUsageString(Command command, IServiceProvider services)
        {
            return
                $"`{services.GetRequiredService<IOptions<LittleBigBotConfig>>().Value.LittleBigBot.Prefix ?? services.GetRequiredService<DiscordSocketClient>().CurrentUser.Mention + " "}{command.Aliases.First()} {string.Join(", ", command.Parameters.Select(c => c.Name.Replace(" ", "_")))}`";
        }

        public class TypeNamePair
        {
            private TypeNamePair(string singular, string multiple)
            {
                SingularName = singular;
                MultipleName = multiple;
            }

            public string SingularName { get; }
            public string MultipleName { get; }

            public static TypeNamePair Of(string singular, string multiple)
            {
                return new TypeNamePair(singular, multiple);
            }
        }
    }
}