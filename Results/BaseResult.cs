using System.Threading.Tasks;
using Discord;
using Qmmands;

namespace LittleBigBot.Results
{
    public abstract class BaseResult : CommandResult
    {
        protected BaseResult(string content, params EmbedBuilder[] embed)
        {
            Content = content;
            Embeds = embed;
        }

        public string Content { get; }
        public EmbedBuilder[] Embeds { get; }

        public static implicit operator Task<BaseResult>(BaseResult res)
        {
            return Task.FromResult(res);
        }
    }
}