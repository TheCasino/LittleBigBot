using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using LittleBigBot.Checks;
using LittleBigBot.Common;
using LittleBigBot.Entities;
using LittleBigBot.Results;
using LittleBigBot.Services;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Qmmands;

namespace LittleBigBot.Modules
{
    [Name("Owner")]
    [Description("Provides commands for my creator.")]
    [RequireOwner]
    public class OwnerModule : LittleBigBotModuleBase
    {
        public ScriptingService Scripting { get; set; }
        public IServiceProvider Services { get; set; }
        public ApiStatsService ApiStats { get; set; }
        public CommandHandlerService Handler { get; set; }

        [Command("ApiStats")]
        [Description("Views API statistics for the current session.")]
        public async Task<BaseResult> Command_ViewApiStatsAsync()
        {
            string Stat(string name, object value)
            {
                return $"**`{name}`**: {value}";
            }

            return Ok(new StringBuilder()
                .AppendLine(Stat("MESSAGE_CREATE", ApiStats.MessageCreate))
                .AppendLine(Stat("MESSAGE_UPDATE", ApiStats.MessageUpdate))
                .AppendLine(Stat("MESSAGE_DELETE", ApiStats.MessageDelete))
                .AppendLine(Stat("HEARTBEAT",
                    ApiStats.Heartbeats + " (average heartbeat: " + (ApiStats.AverageHeartbeat?.ToString() ?? "none") +
                    ")"))
                .AppendLine(Stat("GUILD_AVAILABLE", ApiStats.GuildMadeAvailable))
                .AppendLine(Stat("GUILD_UNAVAILABLE", ApiStats.GuildMadeUnavailable))
                .AppendLine(Stat("Command Successes", Handler.CommandSuccesses))
                .AppendLine(Stat("Command Failures", Handler.CommandFailures))
                .ToString());
        }

        [Command("Clean", "Wipe")]
        [RunMode(RunMode.Parallel)]
        [Description("Cleans messages that I have sent.")]
        public async Task<BaseResult> Command_CleanAsync(
            [Name("Count")] [Description("The amount of messages to clean. Max of 30.")]
            int count = 20,
            [Name("Announce")] [Description("Whether to respond with a summary of the deleted messages.")]
            bool announce = true)
        {
            var messages = Context.Channel.CachedMessages.Where(a => a.Author.Id == Context.Client.CurrentUser.Id)
                .Take(count).Cast<IMessage>().ToList();
            var mcount = messages.Count;

            var successfulDeletes = 0;
            var failedDeletes = 0;

            foreach (var message in messages)
                try
                {
                    await message.DeleteAsync();
                    successfulDeletes++;
                }
                catch
                {
                    failedDeletes++;
                }

            if (announce)
                return Ok(
                    $"Attempted to delete ``{mcount}`` messages: ``{successfulDeletes}`` deleted successfully, while ``{failedDeletes}`` failed to delete.");
            return NoResponse();
        }

        [Command("SetGame", "Game")]
        [Description("Sets my current Discord activity.")]
        public async Task<BaseResult> Command_SetGameAsync(
            [Description("The verb to act.")] [Name("Type")]
            ActivityType type,
            [Description("The target of the verb.")] [Name("Target")]
            string game,
            [Description("The URL link (streaming only)")] [Name("Stream URL")]
            string streamUrl = null)
        {
            await Context.Client.SetGameAsync(game, streamUrl, type);
            return Ok($"Set game to `{type} {game} (url: {streamUrl ?? "None"})`.");
        }

        [Command("SetNickname", "Nickname", "Nick", "SetNick")]
        [Description("Sets my current nickname for this server.")]
        [Remarks("You can provide `clear` to remove my current nickname (if any).")]
        [RequireDiscordContext(DiscordContextType.Server)]
        [RequireBotPermission(GuildPermission.ChangeNickname)]
        [RequireUserPermission(GuildPermission.ChangeNickname)]
        public async Task<BaseResult> Command_SetNicknameAsync(
            [Description("The nickname to set to. `clear` to remove one (if set).")] [Name("Nickname")] [Remainder]
            string nickname)
        {
            var user = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            if (nickname.Equals("clear", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(user.Nickname))
                return BadRequest("I don't have a nickname!");

            try
            {
                await user.ModifyAsync(a => { a.Nickname = nickname == "clear" ? null : nickname; }, new RequestOptions
                {
                    AuditLogReason = $"Action performed by {Context.Invoker}"
                });
                return Ok(nickname != "clear"
                    ? "Set my nickname to `" + nickname + "`."
                    : "Done!");
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.Forbidden)
            {
                return BadRequest("Received 403 Forbidden changing nickname!");
            }
            catch (HttpException e) when (e.HttpCode == HttpStatusCode.BadRequest)
            {
                return BadRequest("Received 400 Bad Request, try shortening or extending the name!");
            }
        }

        [Command("Script", "Eval", "CSharpEval", "CSharp", "C#")]
        [RunMode(RunMode.Parallel)]
        [Description("Evaluates a piece of C# code.")]
        public async Task<BaseResult> Command_EvaluateAsync(
            [Name("Code")] [Description("The code to execute.")] [Remainder]
            string script)
        {
            var props = new EvaluationHelper(Context, Services);
            var result = await Scripting.EvaluateScriptAsync(script, props);

            var sb = new StringBuilder();
            sb.AppendLine("**Scripting Result**");

            if (result.IsSuccess)
            {
                if (result.ReturnValue != null)
                {
                    string stringRep;
                    var special = false;

                    switch (result.ReturnValue)
                    {
                        case string str:
                            stringRep = str;
                            break;
                        case IDictionary dictionary:
                            var asb = new StringBuilder();
                            asb.AppendLine("Dictionary of type ``" + dictionary.GetType().Name + "``");
                            foreach (var ent in dictionary.Keys) asb.AppendLine($"- ``{ent}``: ``{dictionary[ent]}``");

                            stringRep = asb.ToString();
                            special = true;
                            break;
                        case IEnumerable enumerable:
                            var asb0 = new StringBuilder();
                            asb0.AppendLine("Enumerable of type ``" + enumerable.GetType().Name + "``");
                            foreach (var ent in enumerable) asb0.AppendLine($"- ``{ent}``");

                            stringRep = asb0.ToString();
                            special = true;
                            break;
                        default:
                            stringRep = result.ReturnValue.ToString();
                            break;
                    }

                    if (stringRep.StartsWith("```") && stringRep.EndsWith("```") || special)
                        sb.AppendLine(stringRep);
                    else sb.AppendLine($"``{result.ReturnValue.GetType().Name}``: ``{stringRep}``");
                }
                else
                {
                    sb.AppendLine("No results returned.");
                }
            }
            else
            {
                sb.AppendLine($"Scripting failed during stage **{FormatEnumMember(result.FailedStage)}**");
                if (result.CompilationDiagnostics != null && result.CompilationDiagnostics.Count > 0)
                {
                    foreach (var compilationDiagnostic in result.CompilationDiagnostics)
                        sb.AppendLine(
                            $" - ``{compilationDiagnostic.Id}`` ({FormatDiagnosticLocation(compilationDiagnostic.Location)}): **{compilationDiagnostic.GetMessage()}**");

                    if (result.Exception != null) sb.AppendLine();
                }

                if (result.Exception != null)
                    sb.AppendLine($"``{result.Exception.GetType().Name}``: ``{result.Exception.Message}``");
            }

            sb.AppendLine();
            if (result.CompilationTime != -1) sb.Append($"Compilation time: {result.CompilationTime}ms ");
            if (result.ExecutionTime != -1) sb.Append($"| Execution time: {result.ExecutionTime}ms");

            return Ok(sb.ToString());
        }

        public string FormatEnumMember(Enum value)
        {
            return value.ToString().Replace(value.GetType().Name + ".", "");
        }

        public string FormatDiagnosticLocation(Location loc)
        {
            if (!loc.IsInSource) return "Metadata";
            if (loc.SourceSpan.Start == loc.SourceSpan.End) return "Ch " + loc.SourceSpan.Start;
            return $"Ch {loc.SourceSpan.Start}-{loc.SourceSpan.End}";
        }

        [Command("Inspect")]
        [RunMode(RunMode.Parallel)]
        [Description("Evaluates and then inspects a type.")]
        [RequireOwner]
        public Task<BaseResult> Command_InspectObjectAsync([Remainder] string evaluateScript)
        {
            return Command_EvaluateAsync($"Inspect({evaluateScript})");
        }

        [Command("Kill", "Die", "Stop", "Terminate")]
        [Description("Stops the current bot process.")]
        [RequireOwner]
        public async Task<BaseResult> Command_ShutdownAsync()
        {
            await ReplyAsync("Noho mai rā! (Goodbye!)");
            Logger.Fatal($"Application terminated by user {Context.Invoker} (ID {Context.Invoker.Id})");
            LogManager.Shutdown();
            await Context.Client.LogoutAsync();
            await Context.Client.StopAsync();
            Context.Client.Dispose();

            Environment.Exit(0); // Clean exit - trigger daemon NOT to restart
            return NoResponse();
        }

        [Group("Services")]
        [Description("Provides commands to enable, disable, and reload LittleBigBot services.")]
        public class ServicesSubmodule : LittleBigBotModuleBase
        {
            public IServiceProvider Services { get; set; }

            public BaseService FindService(string name)
            {
                var matchingTypes = Assembly.GetEntryAssembly().GetTypes().Where(a =>
                {
                    if (typeof(BaseService).IsAssignableFrom(a) && !a.IsAbstract)
                    {
                        var serviceName = a.Name;
                        var nameAttribute = a.GetCustomAttribute<NameAttribute>();
                        if (nameAttribute != null) serviceName = nameAttribute.Name;
                        return serviceName.Equals(name, StringComparison.OrdinalIgnoreCase);
                    }

                    return false;
                }).ToList();

                if (matchingTypes.Any()) return Services.GetRequiredService(matchingTypes.First()) as BaseService;

                return null;
            }

            [Command]
            public Task<BaseResult> ServiceInfoAsync(
                [Name("The name of the service to get information on.")] [Remainder]
                string name)
            {
                var service = FindService(name);
                if (service == null)
                    return Task.FromResult<BaseResult>(NotFound($"Cannot find a service called ``{name}``."));
                var type = service.GetType();
                var serviceName = type.GetCustomAttribute<NameAttribute>()?.Name ?? type.Name;
                var serviceDescription = type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "None.";

                return Task.FromResult<BaseResult>(Ok(a =>
                {
                    a.WithAuthor($"Service: {serviceName}", Context.Bot.GetEffectiveAvatarUrl());
                    a.WithDescription(serviceDescription);
                    a.WithFooter($"LittleBigBot internal ID: {type.FullName}");
                }));
            }

            [Command("Reload")]
            [RunMode(RunMode.Parallel)]
            public async Task<BaseResult> ReloadServiceAsync([Name("The name of the service to reload.")] [Remainder]
                string name)
            {
                var service = FindService(name);

                if (service == null) return NotFound($"Cannot find a service called ``{name}``.");

                await service.ReloadAsync().ConfigureAwait(false);

                return Ok($"Reloaded service ``{service.GetType().Name}``.");
            }
        }
    }
}