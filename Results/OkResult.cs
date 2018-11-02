using System.Linq;
using Discord;

namespace LittleBigBot.Results
{
    public class OkResult : BaseResult
    {
        public OkResult(string content, params EmbedBuilder[] embed) : base(content, embed.Select(e => e.WithColor(e.Color ?? LittleBigBot.DefaultEmbedColour)).ToArray())
        {
        }

        public override bool IsSuccessful => true;
    }
}