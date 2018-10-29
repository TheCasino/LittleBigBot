using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Humanizer;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Modules;
using LittleBigBot.Parsers;
using LittleBigBot.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Qmmands;

namespace LittleBigBot.Services
{
    [Name("Command Handler")]
    [Description("Receives messages and attempts to parse them into commands to be executed.")]
    public sealed class CommandHandlerService: BaseService
    {
        public const string UnknownCommandReaction = "❓";

        private readonly DiscordSocketClient _client;
        private readonly CommandService _commandService;
        private readonly ILogger _commandsTracking;
        private readonly LittleBigBotConfig _config;

        private readonly ILogger<CommandHandlerService> _logger;

        private readonly IServiceProvider _services;

        public int CommandFailures;
        public int CommandSuccesses;

        public CommandHandlerService(DiscordSocketClient client, CommandService commandService,
            IOptions<LittleBigBotConfig> config,
            IServiceProvider services, ILogger<CommandHandlerService> logger, ILoggerFactory loggerFactory)
        {
            _client = client;
            _commandService = commandService;
            _config = config.Value;
            _services = services;
            _logger = logger;
            _commandsTracking = loggerFactory.CreateLogger("CommandsTracking");
        }

        public override async Task InitializeAsync()
        {
            _client.MessageReceived += HandleMessageAsync;
            _client.MessageUpdated += (cacheable, message, arg3) => HandleMessageAsync(message);
            _commandService.CommandErrored += HandleCommandErrorAsync;
            _commandService.CommandExecuted += HandleCommandExecutedAsync;

            _commandService.AddTypeParser(new DiscordUserTypeParser<SocketGuildUser>());
            _commandService.AddTypeParser(new DiscordUserTypeParser<SocketUser>());

            var modulesLoaded = await _commandService.AddModulesAsync(Assembly.GetEntryAssembly());
            _logger.LogInformation($"{modulesLoaded.Count} total modules loaded | {modulesLoaded.Sum(a => a.Commands.Count)} total commands loaded | 2 type parsers loaded");
        }

        private async Task HandleCommandFinishedGlobalAsync(Command command, CommandResult result,
            LittleBigBotExecutionContext context)
        {
            if (result.IsSuccessful) CommandSuccesses++;
            else CommandFailures++;

            var baseResult = result.Cast<BaseResult>();

            if (baseResult.Content != null && !string.IsNullOrWhiteSpace(baseResult.Content))
            {
                await context.Channel.SendMessageAsync(baseResult.Content);
            }

            foreach (var embed in baseResult.Embeds)
            {
                await context.Channel.SendMessageAsync(string.Empty, false, embed.Build());
            }

            if (result is FailedBaseResult fbr)
            {
                LogCommandRuntimeFailure(context, command, fbr);
            }

            else LogCommandSuccess(context, command);
        }

        public Task HandleCommandExecutedAsync(Command command, CommandResult result, ICommandContext context, IServiceProvider arg4)
        {
            return HandleCommandFinishedGlobalAsync(command, result, context.Cast<LittleBigBotExecutionContext>());
        }

        private async Task HandleCommandErrorAsync(ExecutionFailedResult result, ICommandContext context0, IServiceProvider provider)
        {
            if (result.IsSuccessful) return; // Theoretically shouldn't ever happen?

            var context = context0.Cast<LittleBigBotExecutionContext>();

            var command = result.Command;
            var exception = result.Exception;

            var embed = new EmbedBuilder
            {
                Color = Color.Red,
                Title = $"Command '{command.Name}' failed to run.",
                Description = result.Reason,
                ThumbnailUrl = context.Bot.GetEffectiveAvatarUrl(),
                Fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder
                    {
                        Name = "Message",
                        Value = exception.Message,
                        IsInline = true
                    }
                },
                Footer = new EmbedFooterBuilder
                {
                    Text = $"If you believe this error is not because of your input, please contact {(await _client.GetApplicationInfoAsync()).Owner}!"
                }
            };
            _commandsTracking.LogError(exception, context.FormatString(command) + " == DETAILS WILL FOLLOW ==");
            LogCommandGeneralFailure(context, command, result);
            await context.Channel.SendMessageAsync(string.Empty, false, embed.Build());
        }

        private string GenerateLogString(LittleBigBotExecutionContext context, Command command)
        {
            return
                $"Executed command '{command.Aliases.First()}' for {context.Invoker} (ID {context.Invoker.Id}) in {context.Channel.Name} (ID {context.Channel.Id}){(context.Guild != null ? $" in guild {context.Guild.Name} (ID {context.Guild.Id})" : "")}";
        }

        private void LogCommandSuccess(LittleBigBotExecutionContext context, Command command)
        {
            _commandsTracking.LogInformation(GenerateLogString(context, command) + " successfully");
        }

        private void LogCommandGeneralFailure(LittleBigBotExecutionContext context, Command command, FailedResult failure)
        {
            _commandsTracking.LogInformation(GenerateLogString(context, command) + $" unsuccessfully, with pre-run reason \"{failure.Reason}\"");
        }

        private void LogCommandRuntimeFailure(LittleBigBotExecutionContext context, Command command,
            FailedBaseResult fbr)
        {
            _commandsTracking.LogInformation(GenerateLogString(context, command) + $" unsuccessfully, with run-time reason \"{fbr.Content}\"");
        }

        private async Task HandleMessageAsync(SocketMessage incomingMessage)
        {
            if (!(incomingMessage is SocketUserMessage message) || incomingMessage.Author is SocketWebhookUser)
                return; // Ignore web-hooks or system messages

            if (incomingMessage.Author.IsBot && incomingMessage.Author.Id != _client.CurrentUser.Id) return;
            // Ignore bots (except ourselves)

            var argPos = 0;

            if (!message.HasStringPrefix(_config.LittleBigBot.Prefix, ref argPos) && !message.HasMentionPrefix(_client.CurrentUser, ref argPos)) return;

            var context = new LittleBigBotExecutionContext(message, _services);

            try
            {
                var result = await _commandService.ExecuteAsync(message.Content.Substring(argPos) /* Remove prefix from string */, context, _services);

                if (result.IsSuccessful) return;

                Command command;

                switch (result)
                {
                    case CommandNotFoundResult _:
                        await context.Message.AddReactionAsync(new Emoji(UnknownCommandReaction));
                        return;
                    case ChecksFailedResult cfr:
                        command = cfr.Command;
                        await context.Channel.SendMessageAsync($"The following check{(cfr.FailedChecks.Count == 0 ? "" : "s")} failed, so I couldn't execute the command: \n" + cfr.FailedChecks.Select(a => $"- {a.Error}").Join("\n"));
                        break;
                    case ParseFailedResult pfr:
                        command = pfr.Command;
                        if (pfr.ParseFailure == ParseFailure.TooFewArguments)
                        {
                            await context.Channel.SendMessageAsync($"Sorry, but you didn't supply enough information for this command! Here is the command listing for `{pfr.Command.Aliases.First()}`:", false, HelpModule.CreateCommandEmbed(pfr.Command, context));
                            break;
                        }

                        await context.Channel.SendMessageAsync($"Parsing of your input failed: {pfr.ParseFailure.Humanize()}.");
                        break;
                    case TypeParserFailedResult tpfr:
                        command = tpfr.Parameter.Command;
                        await context.Channel.SendMessageAsync($"Sorry, but \"{tpfr.Value}\" is not a valid form of {tpfr.Parameter.GetFriendlyName()}! Here is the command listing for `{tpfr.Parameter.Command.Aliases.First()}`:", false, HelpModule.CreateCommandEmbed(tpfr.Parameter.Command, context));
                        break;
                    case ExecutionFailedResult _:
                        return;
                    case CommandOnCooldownResult cdr:
                        command = cdr.Command;
                        var msg = new StringBuilder();
                        msg.AppendLine("This command is on cooldown!");
                        foreach (var cdv in cdr.Cooldowns)
                        {
                            msg.AppendLine();
                            msg.AppendLine($"**Cooldown type:** {cdv.Cooldown.BucketType.Cast<CooldownType>()}");
                            msg.AppendLine($"**Limit:** {cdv.Cooldown.Amount} requests per {cdv.Cooldown.Per:g}");
                            msg.AppendLine($"**Retry after:** {cdv.RetryAfter:g}");
                        }

                        await context.Channel.SendMessageAsync(msg.ToString());
                        break;
                    case CommandResult _:
                        return;
                    default:
                        await context.Channel.SendMessageAsync($"Generic failure: {result}");
                        return;
                }

                LogCommandGeneralFailure(context, command, result as FailedResult);
            }
            catch (Exception)
            {
                // Ignored - caught through CommandErrored event!
                // Should (theoretically) never happen, but Qmmands is an early library and errors could occur.
                // I don't know though.
            }
        }
    }
}