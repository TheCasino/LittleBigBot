using Discord;

namespace LittleBigBot.Results
{
    public abstract class FailedBaseResult: BaseResult
    {
        public override bool IsSuccessful => false;

        protected FailedBaseResult(string content) : base(content)
        {
        }
    }
}