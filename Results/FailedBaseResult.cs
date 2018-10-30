namespace LittleBigBot.Results
{
    public abstract class FailedBaseResult : BaseResult
    {
        protected FailedBaseResult(string content) : base(content)
        {
        }

        public override bool IsSuccessful => false;
    }
}