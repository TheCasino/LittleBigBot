using Discord;
using Qmmands;

namespace LittleBigBot.Results
{
    public abstract class BaseResult: CommandResult
    {
        public string Content { get; }
        public EmbedBuilder[] Embeds { get; }

        public BaseResult(string content, params EmbedBuilder[] embed)
        {
            Content = content;
            Embeds = embed;
        }
    }
}