using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Services.Interfaces
{
    public interface IUserService
    {
        Task<Account?> GetUserByUsernameAsync(string username);
        Task<Account?> GetUserByEmailAsync(string email);
        Task<bool> UsernameExistsAsync(string username);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> CreateUserAsync(Account user);
        Task<bool> UpdateUserAsync(Account user);
        Task<bool> VerifyPasswordAsync(string password, string storedHash);
        Task UpdateLastLoginAsync(string userId);
        Task<bool> BanUserAsync(string userId, bool isBanned);
    }
}