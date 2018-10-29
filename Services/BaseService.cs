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

        public async Task ReloadAsync()
        {
            await DeinitializeAsync();
            await InitializeAsync();
        }
    }
}