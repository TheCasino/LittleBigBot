namespace LittleBigBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var bot = new LittleBigBot();
            bot.StartAsync().GetAwaiter().GetResult();
        }
    }
}