using Discord;

namespace LittleBigBot.Results
{
    public class BadRequestResult: BaseResult
    {
        public BadRequestResult(string content = null) : base(content == null ? "Bad request!" : ":negative_squared_cross_mark: | " + content)
        {
        }

        public override bool IsSuccessful => false;
    }
}