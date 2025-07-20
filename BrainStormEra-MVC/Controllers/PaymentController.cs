using Microsoft.AspNetCore.Mvc;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.Services.Implementations;
using DataAccessLayer.Models.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class PaymentController : BaseController
    {
        private readonly IPaymentService _paymentService;
        private readonly CourseService _courseService;

        public PaymentController(IPaymentService paymentService, CourseService courseService)
        {
            _paymentService = paymentService;
            _courseService = courseService;
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Auth");
            }

            var transactions = await _paymentService.GetUserTransactionsAsync(userId);
            return View(transactions);
        }
        [HttpPost]
        public async Task<IActionResult> CreatePayment(string courseId, decimal amount)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Please login" });
                }

                // Validate course exists
                var course = await _courseService.GetCourseByIdAsync(courseId);
                if (course == null)
                {
                    return Json(new { success = false, message = "Course does not exist" });
                }

                // Create return URL
                var returnUrl = Url.Action("PaymentReturn", "Payment", null, Request.Scheme);

                // Create payment URL
                var paymentUrl = await _paymentService.CreatePaymentUrlAsync(userId, courseId, amount, returnUrl!);

                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentReturn()
        {
            try
            {
                var vnpayData = Request.Query.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.FirstOrDefault()
                );

                var result = await _paymentService.ProcessPaymentReturnAsync(vnpayData);

                if (result.Success)
                {
                    TempData["SuccessMessage"] = result.Message;
                    TempData["TransactionId"] = result.TransactionId;
                    TempData["Amount"] = result.Amount;
                    return RedirectToAction("PaymentSuccess");
                }
                else
                {
                    TempData["ErrorMessage"] = result.Message;
                    return RedirectToAction("PaymentFailed");
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Payment processing error: {ex.Message}";
                return RedirectToAction("PaymentFailed");
            }
        }

        [HttpGet]
        public IActionResult PaymentSuccess()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "Payment successful";
            ViewBag.TransactionId = TempData["TransactionId"];
            ViewBag.Amount = TempData["Amount"];
            return View();
        }

        [HttpGet]
        public IActionResult PaymentFailed()
        {
            ViewBag.Message = TempData["ErrorMessage"] ?? "Payment failed";
            return View();
        }
        [HttpGet]
        public IActionResult TopUp()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> CreateTopUpPayment(decimal amount)
        {
            try
            {
                var userId = CurrentUserId;
                if (string.IsNullOrEmpty(userId))
                {
                    return Json(new { success = false, message = "Please login" });
                }

                if (amount < 10000)
                {
                    return Json(new { success = false, message = "Minimum top-up amount is 10,000 VND" });
                }

                // Create return URL with explicit scheme and host
                var returnUrl = $"{Request.Scheme}://{Request.Host}/Payment/PaymentReturn";
                Console.WriteLine($"Generated return URL: {returnUrl}");

                // Create payment URL for top-up (no courseId needed)
                var paymentUrl = await _paymentService.CreateTopUpPaymentUrlAsync(userId, amount, returnUrl);

                return Json(new { success = true, paymentUrl = paymentUrl });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CreateTopUpPayment: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> TransactionDetail(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var transaction = await _paymentService.GetTransactionByIdAsync(id);
            if (transaction == null)
            {
                return NotFound();
            }

            // Check if user owns this transaction
            var userId = CurrentUserId;
            if (transaction.UserId != userId)
            {
                return Forbid();
            }

            return View(transaction);
        }
    }
}
