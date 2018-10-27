using System;
using Microsoft.Extensions.DependencyInjection;

namespace LittleBigBot
{
    public class LittleBigBot
    {
        public LittleBigBot()
        {
            var services = ConfigureServices();
        }

        public static IServiceProvider ConfigureServices()
        {
            var collection = new ServiceCollection();

            collection
                .AddSingleton(collection);

            return collection.BuildServiceProvider(true);
        }
    }
}