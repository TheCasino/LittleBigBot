using Discord;

namespace LittleBigBot.Results
{
    public class BadRequestResult: FailedBaseResult
    {
        public BadRequestResult(string content = null) : base(content == null ? "Bad request!" : ":negative_squared_cross_mark: | " + content)
        {
        }
    }
}