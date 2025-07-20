using DataAccessLayer.Models;

namespace BusinessLogicLayer.Services.Interfaces
{
    /// <summary>
    /// Lazy loading wrapper for achievement services to break circular dependency
    /// </summary>
    public interface ILazyAchievementService
    {
        /// <summary>
        /// Get the achievement unlock service when needed
        /// </summary>
        IAchievementUnlockService AchievementUnlockService { get; }
    }
}