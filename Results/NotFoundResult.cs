using Discord;

namespace LittleBigBot.Results
{
    public class NotFoundResult: FailedBaseResult
    {
        public NotFoundResult(string content = null) : base(content == null ? "The specified resource was not found." : ":negative_squared_cross_mark: | " + content)
        {
        }
    }
}