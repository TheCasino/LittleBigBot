using System.Threading.Tasks;

namespace LittleBigBot.Services
{
    public abstract class BaseService
    {
        public virtual Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task DeinitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task ReloadAsync()
        {
            return Task.WhenAll(InitializeAsync(), DeinitializeAsync());
        }
    }
}