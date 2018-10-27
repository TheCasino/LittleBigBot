using System;
using System.Threading.Tasks;
using LittleBigBot.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LittleBigBot
{
    public class LittleBigBot
    {
        public LittleBigBot()
        {
            var services = ConfigureServices();
        }

        public async Task StartAsync()
        {

        }

        public static IServiceProvider ConfigureServices()
        {
            var collection = new ServiceCollection();

            collection
                .AddSingleton<DaemonService>()
                .AddSingleton(collection);

            return collection.BuildServiceProvider(true);
        }
    }
}