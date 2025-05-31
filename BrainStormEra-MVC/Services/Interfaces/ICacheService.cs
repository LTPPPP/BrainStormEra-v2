using Microsoft.Extensions.Caching.Memory;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class;
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        bool TryGet<T>(string key, out T? value) where T : class;
    }
}
