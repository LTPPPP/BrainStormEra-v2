using DataAccessLayer.Models;
using DataAccessLayer.Models.DTOs;
using BusinessLogicLayer.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using DataAccessLayer.Data;
using BusinessLogicLayer.Utilities;

namespace BusinessLogicLayer.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly BrainStormEraContext _context;
        private readonly IConfiguration _configuration;

        // VNPAY Configuration from appsettings
        private readonly string _vnp_TmnCode;
        private readonly string _vnp_HashSecret;
        private readonly string _vnp_Url; public PaymentService(BrainStormEraContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;

            // Load VNPAY configuration from environment variables
            _vnp_TmnCode = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE") ?? "2QXUI4J4";
            _vnp_HashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? "OHVTVTPZNEJ6X6FUVXTZCRJNPUBOMVTZ";
            _vnp_Url = Environment.GetEnvironmentVariable("VNPAY_BASE_URL") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        }
        public async Task<string> CreatePaymentUrlAsync(string userId, string courseId, decimal amount, string returnUrl)
        {
            try
            {
                // Generate transaction ID
                var transactionId = GenerateTransactionId();

                // Create payment transaction record
                var transaction = new PaymentTransaction
                {
                    TransactionId = transactionId,
                    UserId = userId,
                    Amount = amount,
                    TransactionType = "points",
                    TransactionStatus = "PENDING",
                    PaymentGateway = "VNPAY",
                    CurrencyCode = "VND",
                    TransactionCreatedAt = DateTime.Now,
                    TransactionUpdatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();                // Get course info for description
                var course = await _context.Courses.FindAsync(courseId);
                var orderInfo = $"Course payment: {course?.CourseName ?? "Course"}";

                // Create VNPAY payment URL
                var paymentUrl = VnPayHelper.CreatePaymentUrl(
                    _vnp_Url,
                    _vnp_TmnCode,
                    _vnp_HashSecret,
                    transactionId,
                    orderInfo,
                    (long)(amount * 100), // VNPAY uses amount in VND * 100
                    "other",
                    "vi",
                    returnUrl,
                    DateTime.Now.ToString("yyyyMMddHHmmss")
                );

                return paymentUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating payment URL: {ex.Message}");
            }
        }
        public async Task<string> CreateTopUpPaymentUrlAsync(string userId, decimal amount, string returnUrl)
        {
            try
            {
                // Generate transaction ID
                var transactionId = GenerateTransactionId();

                // Create payment transaction record for top-up
                var transaction = new PaymentTransaction
                {
                    TransactionId = transactionId,
                    UserId = userId,
                    Amount = amount,
                    TransactionType = "topup", // Use topup type instead of course payment
                    TransactionStatus = "PENDING",
                    PaymentGateway = "VNPAY",
                    CurrencyCode = "VND",
                    TransactionCreatedAt = DateTime.Now,
                    TransactionUpdatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();                // Create order description for top-up
                var orderInfo = $"Account top up - {amount:N0} VND";

                // Create VNPAY payment URL
                var paymentUrl = VnPayHelper.CreatePaymentUrl(
                    _vnp_Url,
                    _vnp_TmnCode,
                    _vnp_HashSecret,
                    transactionId,
                    orderInfo,
                    (long)(amount * 100), // VNPAY uses amount in VND * 100
                    "other",
                    "vi",
                    returnUrl,
                    DateTime.Now.ToString("yyyyMMddHHmmss")
                );

                return paymentUrl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating top-up payment URL: {ex.Message}");
            }
        }

        public async Task<PaymentResponseDto> ProcessPaymentReturnAsync(IDictionary<string, string?> vnpayData)
        {
            try
            {
                // Log the received data for debugging
                Console.WriteLine("VNPAY Return Data:");
                foreach (var kvp in vnpayData)
                {
                    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }

                var isValidSignature = VnPayHelper.ValidateSignature(vnpayData, _vnp_HashSecret);

                if (!isValidSignature)
                {
                    Console.WriteLine("Invalid signature detected");
                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = "Chữ ký không hợp lệ"
                    };
                }

                var vnp_ResponseCode = vnpayData["vnp_ResponseCode"];
                var vnp_TxnRef = vnpayData["vnp_TxnRef"];
                var vnp_Amount = vnpayData["vnp_Amount"];
                var vnp_TransactionNo = vnpayData["vnp_TransactionNo"];

                Console.WriteLine($"Processing transaction: {vnp_TxnRef}, Response Code: {vnp_ResponseCode}");

                // Find transaction
                var transaction = await _context.PaymentTransactions
                    .FirstOrDefaultAsync(t => t.TransactionId == vnp_TxnRef);

                if (transaction == null)
                {
                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = "Không tìm thấy giao dịch"
                    };
                }

                if (vnp_ResponseCode == "00")
                {
                    // Payment successful
                    transaction.TransactionStatus = "SUCCESS";
                    transaction.PaymentDate = DateTime.Now;
                    transaction.PaymentGatewayRef = vnp_TransactionNo;
                    transaction.TransactionUpdatedAt = DateTime.Now;

                    // Update user points (1 VND = 1 point)                    await UpdateUserPointsAsync(transaction.UserId, transaction.Amount);

                    await _context.SaveChangesAsync(); return new PaymentResponseDto
                    {
                        Success = true,
                        Message = "Payment successful",
                        TransactionId = transaction.TransactionId,
                        Amount = transaction.Amount,
                        UserId = transaction.UserId,
                        TransactionType = transaction.TransactionType,
                        PaymentDate = transaction.PaymentDate ?? DateTime.Now,
                        PaymentGatewayRef = vnp_TransactionNo
                    };
                }
                else
                {
                    // Payment failed
                    transaction.TransactionStatus = "FAILED";
                    transaction.TransactionUpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = GetVnpayErrorMessage(vnp_ResponseCode),
                        TransactionId = transaction.TransactionId
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentResponseDto
                {
                    Success = false,
                    Message = $"Payment processing error: {ex.Message}"
                };
            }
        }

        public async Task<bool> UpdateUserPointsAsync(string userId, decimal points)
        {
            try
            {
                var user = await _context.Accounts.FindAsync(userId);
                if (user == null) return false;

                user.PaymentPoint = (user.PaymentPoint ?? 0) + points;
                user.AccountUpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<PaymentTransaction?> GetTransactionByIdAsync(string transactionId)
        {
            return await _context.PaymentTransactions
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<List<PaymentTransaction>> GetUserTransactionsAsync(string userId)
        {
            return await _context.PaymentTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionCreatedAt)
                .ToListAsync();
        }

        private string GenerateTransactionId()
        {
            return DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
        }
        private string GetVnpayErrorMessage(string? responseCode)
        {
            return responseCode switch
            {
                "07" => "Deduction successful. Suspicious transaction (related to fraud, unusual transaction).",
                "09" => "Customer's card/account has not registered for InternetBanking service at the bank.",
                "10" => "Customer authenticated card/account information incorrectly more than 3 times",
                "11" => "Payment timeout expired. Please make the transaction again.",
                "12" => "Customer's card/account is locked.",
                "13" => "You entered the wrong transaction authentication password (OTP). Please make the transaction again.",
                "24" => "Customer canceled the transaction",
                "51" => "Your account does not have sufficient balance to make the transaction.",
                "65" => "Your account has exceeded the daily transaction limit.",
                "75" => "Payment bank is under maintenance.",
                "79" => "Customer entered wrong payment password more than the specified number of times. Please make the transaction again",
                "99" => "Other errors (remaining errors, not in the listed error code list)",
                _ => "Payment unsuccessful"
            };
        }
    }
}
