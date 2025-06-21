using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;

namespace BusinessLogicLayer.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentUrlAsync(string userId, string courseId, decimal amount, string returnUrl);
        Task<string> CreateTopUpPaymentUrlAsync(string userId, decimal amount, string returnUrl);
        Task<PaymentResponseDto> ProcessPaymentReturnAsync(IDictionary<string, string?> vnpayData);
        Task<bool> UpdateUserPointsAsync(string userId, decimal points);
        Task<PaymentTransaction?> GetTransactionByIdAsync(string transactionId);
        Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId);
    }
}
