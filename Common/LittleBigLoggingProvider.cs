using Microsoft.Extensions.Logging;

namespace LittleBigBot.Common
{
    public class LittleBigLoggingProvider: ILoggerProvider
    {
        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new LittleBigLogger(categoryName);
        }
    }

    public static class LittleBigLoggingExtensions
    {
        public static ILoggingBuilder AddLittleBig(this ILoggingBuilder builder)
        {
            return builder.AddProvider(new LittleBigLoggingProvider());
        }
    }
}