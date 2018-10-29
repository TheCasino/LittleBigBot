using Discord;

namespace LittleBigBot.Results
{
    public class NotFoundResult: FailedBaseResult
    {
        public NotFoundResult(string content = null) : base(content ?? "The specified resource was not found!")
        {
        }
    }
}