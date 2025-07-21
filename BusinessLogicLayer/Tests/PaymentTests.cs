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
using Microsoft.AspNetCore.Http;
using DataAccessLayer.Repositories.Interfaces;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using DataAccessLayer.Models.ViewModels;

namespace BusinessLogicLayer.Tests
{
    /*
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

            // Create simple mock points service for testing
            var mockPointsService = new SimpleMockPointsService();

            // Create simple mock repositories
            var mockPaymentRepo = new SimpleMockPaymentRepo();
            var mockCourseRepo = new SimpleMockCourseRepo();

            _paymentService = new PaymentService(mockPaymentRepo, mockCourseRepo, _configuration, mockPointsService);

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

        // Simple mock implementation for testing
        public class SimpleMockPointsService : IPointsService
        {
            public Task<decimal> GetUserPointsAsync(string userId)
            {
                return Task.FromResult(0m);
            }

            public Task<bool> UpdateUserPointsAsync(string userId, decimal points)
            {
                return Task.FromResult(true);
            }

            public Task<bool> RefreshUserPointsClaimAsync(HttpContext httpContext, string userId)
            {
                return Task.FromResult(true);
            }
        }
    }

    public class SimpleMockPaymentRepo : IBaseRepo<PaymentTransaction>
    {
        public Task<PaymentTransaction?> GetByIdAsync(object id)
        {
            return Task.FromResult<PaymentTransaction?>(null);
        }

        public Task<IEnumerable<PaymentTransaction>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<PaymentTransaction>>(new List<PaymentTransaction>());
        }

        public Task<IEnumerable<PaymentTransaction>> FindAsync(Expression<Func<PaymentTransaction, bool>> predicate)
        {
            return Task.FromResult<IEnumerable<PaymentTransaction>>(new List<PaymentTransaction>());
        }

        public Task<PaymentTransaction?> SingleOrDefaultAsync(Expression<Func<PaymentTransaction, bool>> predicate)
        {
            return Task.FromResult<PaymentTransaction?>(null);
        }

        public Task<bool> AnyAsync(Expression<Func<PaymentTransaction, bool>> predicate)
        {
            return Task.FromResult(false);
        }

        public Task<int> CountAsync(Expression<Func<PaymentTransaction, bool>>? predicate = null)
        {
            return Task.FromResult(0);
        }

        public Task<PaymentTransaction> AddAsync(PaymentTransaction entity)
        {
            return Task.FromResult(entity);
        }

        public Task AddRangeAsync(IEnumerable<PaymentTransaction> entities)
        {
            return Task.CompletedTask;
        }

        public Task<PaymentTransaction> UpdateAsync(PaymentTransaction entity)
        {
            return Task.FromResult(entity);
        }

        public Task UpdateRangeAsync(IEnumerable<PaymentTransaction> entities)
        {
            return Task.CompletedTask;
        }

        public Task<bool> DeleteAsync(object id)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(PaymentTransaction entity)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteRangeAsync(IEnumerable<PaymentTransaction> entities)
        {
            return Task.FromResult(true);
        }

        public IQueryable<PaymentTransaction> GetQueryable()
        {
            return new List<PaymentTransaction>().AsQueryable();
        }

        public IQueryable<PaymentTransaction> GetQueryable(Expression<Func<PaymentTransaction, bool>> predicate)
        {
            return new List<PaymentTransaction>().AsQueryable();
        }

        public Task<(IEnumerable<PaymentTransaction> items, int totalCount)> GetPagedAsync(int page, int pageSize, Expression<Func<PaymentTransaction, bool>>? predicate = null, Func<IQueryable<PaymentTransaction>, IOrderedQueryable<PaymentTransaction>>? orderBy = null)
        {
            return Task.FromResult((new List<PaymentTransaction>() as IEnumerable<PaymentTransaction>, 0));
        }

        public Task<int> SaveChangesAsync()
        {
            return Task.FromResult(1);
        }

        public Task<bool> ExistsAsync(Expression<Func<PaymentTransaction, bool>> predicate)
        {
            return Task.FromResult(false);
        }
    }

    public class SimpleMockCourseRepo : ICourseRepo
    {
        public IQueryable<Course> GetActiveCourses()
        {
            return new List<Course>().AsQueryable();
        }

        public Task<Course?> GetCourseDetailAsync(string courseId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<Course?> GetCourseDetailAsync(string courseId, string? currentUserId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<Course?> GetCourseWithChaptersAsync(string courseId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<Course?> GetCourseWithChaptersAsync(string courseId, string authorId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<Course?> GetCourseWithEnrollmentsAsync(string courseId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<List<Course>> SearchCoursesAsync(string? search, string? category, int page, int pageSize, string? sortBy)
        {
            return Task.FromResult(new List<Course>());
        }

        public Task<Course?> GetCourseByIdAsync(string courseId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<string> CreateCourseAsync(Course course)
        {
            return Task.FromResult(course.CourseId);
        }

        public Task<bool> UpdateCourseAsync(Course course)
        {
            return Task.FromResult(true);
        }

        public Task<bool> UpdateCourseImageAsync(string courseId, string imagePath)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCourseAsync(string courseId, string authorId)
        {
            return Task.FromResult(true);
        }

        public Task<Course?> GetCourseForEditAsync(string courseId, string authorId)
        {
            return Task.FromResult<Course?>(null);
        }

        public Task<bool> UpdateCourseApprovalStatusAsync(string courseId, string approvalStatus)
        {
            return Task.FromResult(true);
        }

        public Task<List<Chapter>> GetChaptersByCourseIdAsync(string courseId)
        {
            return Task.FromResult(new List<Chapter>());
        }

        public Task<List<Lesson>> GetLessonsByChapterIdAsync(string chapterId)
        {
            return Task.FromResult(new List<Lesson>());
        }

        public Task<CourseListViewModel> GetInstructorCoursesAsync(string authorId, string? search, string? category, int page, int pageSize)
        {
            return Task.FromResult(new CourseListViewModel());
        }

        public void RefreshCategoryCache()
        {
        }

        public Task<CourseIndexResult> GetCoursesAsync(ClaimsPrincipal user, string? search, string? category, int page = 1, int pageSize = 50)
        {
            return Task.FromResult(new CourseIndexResult());
        }

        public Task<CourseSearchResult> SearchCoursesAsync(ClaimsPrincipal user, string? courseSearch, string? categorySearch, int page = 1, int pageSize = 50, string? sortBy = "newest", string? price = null, string? difficulty = null, string? duration = null)
        {
            return Task.FromResult(new CourseSearchResult());
        }

        public Task<CourseDetailResult> GetCourseDetailsAsync(ClaimsPrincipal user, string courseId, string? tab = null)
        {
            return Task.FromResult(new CourseDetailResult());
        }

        public Task<EnrollmentResult> EnrollInCourseAsync(ClaimsPrincipal user, string courseId)
        {
            return Task.FromResult(new EnrollmentResult());
        }

        public Task<CreateCourseResult> GetCreateCourseViewModelAsync()
        {
            return Task.FromResult(new CreateCourseResult());
        }

        public Task<CreateCourseResult> CreateCourseAsync(ClaimsPrincipal user, CreateCourseViewModel model)
        {
            return Task.FromResult(new CreateCourseResult());
        }

        public Task<EditCourseResult> GetCourseForEditAsync(ClaimsPrincipal user, string courseId)
        {
            return Task.FromResult(new EditCourseResult());
        }

        public Task<EditCourseResult> UpdateCourseAsync(ClaimsPrincipal user, string courseId, CreateCourseViewModel model)
        {
            return Task.FromResult(new EditCourseResult());
        }

        public Task<DeleteCourseResult> DeleteCourseAsync(ClaimsPrincipal user, string courseId)
        {
            return Task.FromResult(new DeleteCourseResult());
        }

        public Task<InstructorCoursesResult> GetUserCoursesAsync(ClaimsPrincipal user)
        {
            return Task.FromResult(new InstructorCoursesResult());
        }

        public Task<CourseApprovalResult> RequestCourseApprovalAsync(ClaimsPrincipal user, string courseId)
        {
            return Task.FromResult(new CourseApprovalResult());
        }

        public Task<LearnManagementResult> GetLearnManagementDataAsync(ClaimsPrincipal user, string courseId)
        {
            return Task.FromResult(new LearnManagementResult());
        }

        public Task<bool> EnrollUserAsync(string userId, string courseId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> IsUserEnrolledAsync(string userId, string courseId)
        {
            return Task.FromResult(false);
        }

        public Task<List<CourseCategoryViewModel>> GetCategoriesAsync()
        {
            return Task.FromResult(new List<CourseCategoryViewModel>());
        }

        public Task<List<CategoryAutocompleteItem>> SearchCategoriesAsync(string searchTerm)
        {
            return Task.FromResult(new List<CategoryAutocompleteItem>());
        }

        public Task<List<string>> GetEnrolledUserIdsAsync(string courseId)
        {
            return Task.FromResult(new List<string>());
        }

        public Task<List<Course>> GetFeaturedCoursesAsync(int count = 6)
        {
            return Task.FromResult(new List<Course>());
        }

        public Task<List<CourseCategory>> GetCategoriesWithCourseCountAsync(int count = 8)
        {
            return Task.FromResult(new List<CourseCategory>());
        }

        public Task<List<Course>> GetRecentCoursesAsync(int count = 5)
        {
            return Task.FromResult(new List<Course>());
        }

        public Task<int> GetTotalStudentsForCourseAsync(string courseId)
        {
            return Task.FromResult(0);
        }

        public Task<decimal> GetAverageRatingForCourseAsync(string courseId)
        {
            return Task.FromResult(0m);
        }

        public Task<int> GetTotalReviewsForCourseAsync(string courseId)
        {
            return Task.FromResult(0);
        }

        public Task<List<Course>> GetAllCoursesAsync(string? search = null, string? categoryFilter = null, int page = 1, int pageSize = 10)
        {
            return Task.FromResult(new List<Course>());
        }

        public Task<int> GetCourseCountAsync(string? search = null, string? categoryFilter = null)
        {
            return Task.FromResult(0);
        }

        public Task<int> GetApprovedCourseCountAsync()
        {
            return Task.FromResult(0);
        }

        public Task<int> GetPendingCourseCountAsync()
        {
            return Task.FromResult(0);
        }

        public Task<int> GetRejectedCourseCountAsync()
        {
            return Task.FromResult(0);
        }

        public Task<int> GetFreeCourseCountAsync()
        {
            return Task.FromResult(0);
        }

        public Task<int> GetPaidCourseCountAsync()
        {
            return Task.FromResult(0);
        }

        public Task<bool> UpdateCourseApprovalAsync(string courseId, bool isApproved)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteCourseAsync(string courseId)
        {
            return Task.FromResult(true);
        }
    }
    */
}
