namespace LittleBigBot.Results
{
    public class OkResult: CompletedResult
    {
        public OkResult(string customResponse = null) : base(customResponse ?? "Okay.", null)
        {

        }
    }
}