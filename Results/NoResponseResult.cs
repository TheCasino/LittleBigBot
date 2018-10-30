namespace LittleBigBot.Results
{
    public class NoResponseResult : BaseResult
    {
        public NoResponseResult() : base(null)
        {
        }

        public override bool IsSuccessful => true;
    }
}