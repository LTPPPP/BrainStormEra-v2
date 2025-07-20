using Microsoft.Extensions.DependencyInjection;
using BusinessLogicLayer.Services.Interfaces;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Lazy loading implementation for achievement services to break circular dependency
    /// </summary>
    public class LazyAchievementService : ILazyAchievementService
    {
        private readonly IServiceProvider _serviceProvider;
        private IAchievementUnlockService? _achievementUnlockService;

        public LazyAchievementService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IAchievementUnlockService AchievementUnlockService
        {
            get
            {
                if (_achievementUnlockService == null)
                {
                    _achievementUnlockService = _serviceProvider.GetRequiredService<IAchievementUnlockService>();
                }
                return _achievementUnlockService;
            }
        }
    }
}