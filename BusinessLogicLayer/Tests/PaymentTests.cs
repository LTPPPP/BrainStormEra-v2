using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using BusinessLogicLayer.Utilities;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BusinessLogicLayer.Tests
{
    public class PaymentTests
    {
        private readonly BrainStormEraContext _context;
        private readonly IPaymentService _paymentService;
        private readonly IConfiguration _configuration;

        public PaymentTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<BrainStormEraContext>()
                .UseInMemoryDatabase(databaseName: "TestPaymentDb")
                .Options;

            _context = new BrainStormEraContext(options);

            // Setup configuration
            var configDict = new Dictionary<string, string?>
            {
                {"VNPAY_TMN_CODE", "ok"},
                {"VNPAY_HASH_SECRET", "ok"},
                {"VNPAY_BASE_URL", "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configDict)
                .Build();

            _paymentService = new PaymentService(_context, _configuration);

            // Initialize test data
            InitializeTestData().Wait();
        }

        private async Task InitializeTestData()
        {
            // Clear existing data
            _context.Accounts.RemoveRange(_context.Accounts);
            _context.PaymentTransactions.RemoveRange(_context.PaymentTransactions);
            await _context.SaveChangesAsync();

            // Add test user
            var testUser = new Account
            {
                UserId = "TEST001",
                Username = "testuser",
                UserRole = "User",
                UserEmail = "test@example.com",
                PasswordHash = "testpass",
                FullName = "Test User",
                PaymentPoint = 0,
                AccountCreatedAt = DateTime.Now,
                AccountUpdatedAt = DateTime.Now
            };

            _context.Accounts.Add(testUser);
            await _context.SaveChangesAsync();
        }

        public async Task RunPaymentTest()
        {
            try
            {
                Console.WriteLine("Starting Payment Test...");
                Console.WriteLine("------------------------");

                // Step 1: Create payment URL for top-up
                decimal amount = 50000; // 50,000 VND
                string userId = "TEST001";
                string returnUrl = "http://localhost:5216/Payment/PaymentReturn";

                Console.WriteLine($"Creating payment URL for user {userId} with amount {amount:N0} VND");
                var paymentUrl = await _paymentService.CreateTopUpPaymentUrlAsync(userId, amount, returnUrl);
                Console.WriteLine($"Payment URL created: {paymentUrl}");

                // Step 2: Simulate VNPAY return data
                var transactionId = DateTime.Now.ToString("yyyyMMddHHmmss") + new Random().Next(1000, 9999);
                var vnpayReturnData = new Dictionary<string, string?>
                {
                    {"vnp_Amount", (amount * 100).ToString()}, // VNPAY expects amount * 100
                    {"vnp_BankCode", "NCB"},
                    {"vnp_BankTranNo", "VNP13876554"},
                    {"vnp_CardType", "ATM"},
                    {"vnp_OrderInfo", "Account top up - 50,000 VND"},
                    {"vnp_PayDate", DateTime.Now.ToString("yyyyMMddHHmmss")},
                    {"vnp_ResponseCode", "00"}, // 00 means success
                    {"vnp_TmnCode", "YKR8MC1N"},
                    {"vnp_TransactionNo", "13876554"},
                    {"vnp_TransactionStatus", "00"},
                    {"vnp_TxnRef", transactionId},
                    {"vnp_SecureHash", ""} // Will be calculated below
                };

                // Calculate secure hash
                var signData = string.Join("&", vnpayReturnData
                    .Where(kv => !string.IsNullOrEmpty(kv.Key) &&
                               kv.Key.StartsWith("vnp_") &&
                               kv.Key != "vnp_SecureHash")
                    .OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={kv.Value}"));

                var secureHash = VnPayLibrary.HmacSHA512("RJPAAAPTEESBM3KH4L328VT6ELPI3PW5", signData);
                vnpayReturnData["vnp_SecureHash"] = secureHash;

                Console.WriteLine("\nProcessing payment return...");
                var result = await _paymentService.ProcessPaymentReturnAsync(vnpayReturnData);

                Console.WriteLine("\nPayment Result:");
                Console.WriteLine($"Success: {result.Success}");
                Console.WriteLine($"Message: {result.Message}");
                Console.WriteLine($"Transaction ID: {result.TransactionId}");
                Console.WriteLine($"Amount: {result.Amount:N0} VND");

                // Check user's updated points
                var user = await _context.Accounts.FindAsync("TEST001");
                Console.WriteLine($"\nUpdated user points: {user?.PaymentPoint:N0}");

                Console.WriteLine("\nTest completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError during test: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
