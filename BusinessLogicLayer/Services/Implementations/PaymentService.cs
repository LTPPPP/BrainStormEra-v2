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
            _vnp_TmnCode = Environment.GetEnvironmentVariable("VNPAY_TMN_CODE") ?? "ok";
            _vnp_HashSecret = Environment.GetEnvironmentVariable("VNPAY_HASH_SECRET") ?? "ok";
            _vnp_Url = Environment.GetEnvironmentVariable("VNPAY_BASE_URL") ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        }
        public async Task<string> CreatePaymentUrlAsync(string userId, string courseId, decimal amount, string returnUrl)
        {
            try
            {
                Console.WriteLine($"Creating payment URL for User: {userId}, Course: {courseId}, Amount: {amount}");

                // Generate transaction ID
                var transactionId = GenerateTransactionId();
                Console.WriteLine($"Generated Transaction ID: {transactionId}");

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
                await _context.SaveChangesAsync();
                Console.WriteLine($"Created transaction record in database: {transactionId}");

                // Get course info for description
                var course = await _context.Courses.FindAsync(courseId);
                Console.WriteLine($"Found course: {course?.CourseName ?? "Course not found"}");

                var orderInfo = $"Course payment: {course?.CourseName ?? "Course"}";

                // Log VNPAY configuration
                Console.WriteLine("VNPAY Configuration:");
                Console.WriteLine($"URL: {_vnp_Url}");
                Console.WriteLine($"TMN Code: {_vnp_TmnCode}");

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

                Console.WriteLine($"Generated VNPAY URL: {paymentUrl}");
                return paymentUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreatePaymentUrlAsync:");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                throw new Exception($"Error creating payment URL: {ex.Message}");
            }
        }
        public async Task<string> CreateTopUpPaymentUrlAsync(string userId, decimal amount, string returnUrl)
        {
            try
            {
                Console.WriteLine($"Creating top-up payment URL for User: {userId}, Amount: {amount}");

                // Validate amount
                if (amount <= 0)
                {
                    throw new ArgumentException("Amount must be greater than 0");
                }

                // Convert amount to VND (remove decimal points)
                long vnpayAmount = (long)amount;
                Console.WriteLine($"Converted amount for VNPAY: {vnpayAmount}");

                // Generate transaction ID
                var transactionId = GenerateTransactionId();
                Console.WriteLine($"Generated Transaction ID: {transactionId}");

                // Create payment transaction record for top-up
                var transaction = new PaymentTransaction
                {
                    TransactionId = transactionId,
                    UserId = userId,
                    Amount = amount,
                    TransactionType = "topup",
                    TransactionStatus = "PENDING",
                    PaymentGateway = "VNPAY",
                    CurrencyCode = "VND",
                    TransactionCreatedAt = DateTime.Now,
                    TransactionUpdatedAt = DateTime.Now
                };

                _context.PaymentTransactions.Add(transaction);
                await _context.SaveChangesAsync();
                Console.WriteLine($"Created top-up transaction record in database: {transactionId}");

                var orderInfo = $"Account top up - {amount:N0} VND";

                // Log VNPAY configuration
                Console.WriteLine("VNPAY Configuration for top-up:");
                Console.WriteLine($"URL: {_vnp_Url}");
                Console.WriteLine($"TMN Code: {_vnp_TmnCode}");

                string createDate = DateTime.Now.ToString("yyyyMMddHHmmss");
                Console.WriteLine($"Create Date: {createDate}");

                // Create VNPAY payment URL
                var paymentUrl = VnPayHelper.CreatePaymentUrl(
                    _vnp_Url,
                    _vnp_TmnCode,
                    _vnp_HashSecret,
                    transactionId,
                    orderInfo,
                    vnpayAmount,
                    "other",
                    "vi",
                    returnUrl,
                    createDate
                );

                Console.WriteLine($"Generated VNPAY URL for top-up: {paymentUrl}");
                return paymentUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in CreateTopUpPaymentUrlAsync:");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
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

                // Check if required parameters are present
                if (!vnpayData.ContainsKey("vnp_ResponseCode") || !vnpayData.ContainsKey("vnp_TxnRef"))
                {
                    Console.WriteLine("Missing required parameters in return data");
                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = "Invalid payment return data"
                    };
                }

                var isValidSignature = VnPayHelper.ValidateSignature(vnpayData, _vnp_HashSecret);

                if (!isValidSignature)
                {
                    Console.WriteLine("Invalid signature detected");
                    return new PaymentResponseDto
                    {
                        Success = false,
                        Message = "Invalid signature"
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
                        Message = "Transaction not found"
                    };
                }

                if (vnp_ResponseCode == "00")
                {
                    // Payment successful
                    transaction.TransactionStatus = "SUCCESS";
                    transaction.PaymentDate = DateTime.Now;
                    transaction.PaymentGatewayRef = vnp_TransactionNo;
                    transaction.TransactionUpdatedAt = DateTime.Now;

                    // Update user points (1 VND = 1 point)
                    await UpdateUserPointsAsync(transaction.UserId, transaction.Amount);

                    await _context.SaveChangesAsync();

                    return new PaymentResponseDto
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
                Console.WriteLine($"Error processing payment return: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
                Console.WriteLine($"Attempting to update points for User: {userId}, Points to add: {points}");

                var user = await _context.Accounts.FindAsync(userId);
                if (user == null)
                {
                    Console.WriteLine($"ERROR: User not found with ID: {userId}");
                    return false;
                }

                decimal currentPoints = user.PaymentPoint ?? 0;
                user.PaymentPoint = currentPoints + points;
                user.AccountUpdatedAt = DateTime.Now;

                Console.WriteLine($"Updating user points:");
                Console.WriteLine($"Current points: {currentPoints}");
                Console.WriteLine($"Points to add: {points}");
                Console.WriteLine($"New total points: {user.PaymentPoint}");

                await _context.SaveChangesAsync();
                Console.WriteLine($"Successfully updated points for user {userId}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in UpdateUserPointsAsync:");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
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
