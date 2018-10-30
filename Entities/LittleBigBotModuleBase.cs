using System;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using LittleBigBot.Results;
using Qmmands;

namespace LittleBigBot.Entities
{
    public abstract class LittleBigBotModuleBase : ModuleBase<LittleBigBotExecutionContext>
    {
        protected Task<RestUserMessage> ReplyAsync(string content = "", bool isTts = false, Embed embed = null,
            RequestOptions options = null)
        {
            return Context.Channel.SendMessageAsync(content, isTts, embed, options);
        }

        protected OkResult Ok(string content, params EmbedBuilder[] embed)
        {
            return new OkResult(content, embed);
        }

        protected OkResult Ok(string content)
        {
            return new OkResult(content);
        }

        protected OkResult Ok(params EmbedBuilder[] builder)
        {
            return new OkResult(null, builder);
        }

        protected OkResult Ok(Action<EmbedBuilder> actor)
        {
            var eb = new EmbedBuilder();
            actor(eb);
            return Ok(eb);
        }

        protected OkResult ImageEmbed(string title, string imageUrl)
        {
            return Ok(e =>
            {
                e.ImageUrl = imageUrl;
                e.Title = title;
            });
        }

        protected BadRequestResult BadRequest(string error = null)
        {
            return new BadRequestResult(error);
        }

        protected NotFoundResult NotFound(string error = null)
        {
            return new NotFoundResult(error);
        }

        protected NoResponseResult NoResponse()
        {
            return new NoResponseResult();
        }
    }
}