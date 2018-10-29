using System.Linq;
using Discord;

namespace LittleBigBot.Results
{
    public class OkResult: BaseResult
    {
        public override bool IsSuccessful => true;

        public OkResult(string content, params EmbedBuilder[] embed) : base(content, embed.Select(e =>
        {
            if (e.Color == null)
            {
                e.Color = LittleBigBot.DefaultEmbedColour;
            }

            return e;
        }).ToArray())
        {

        }
    }
}