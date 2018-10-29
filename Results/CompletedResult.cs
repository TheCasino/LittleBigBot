using Discord;
using Qmmands;

namespace LittleBigBot.Results
{
    public class CompletedResult: CommandResult
    {
        public override bool IsSuccessful => true;

        public string Content { get; }
        public EmbedBuilder Embed { get; }

        public CompletedResult(string content, EmbedBuilder embed)
        {
            Content = content;
            Embed = embed;
        }
    }
}