using System;
using System.Diagnostics;
using System.Linq;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BrainStormEra_MVC.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BrainStormEraContext _context;

        public HomeController(ILogger<HomeController> logger, BrainStormEraContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            var viewModel = new HomePageGuestViewModel();

            try
            {
                // Kiểm tra kết nối database
                bool isDatabaseConnected = IsDatabaseConnected();
                if (!isDatabaseConnected)
                {
                    _logger.LogError("Cannot connect to database");
                    ViewBag.DatabaseError = "Cannot connect to database. Please check your connection settings.";
                    return View(viewModel); // Trả về view với thông báo lỗi
                }

                // Get top 4 featured courses (based on the sequence diagram)
                var recommendedCourses = _context.Courses
                    .Where(c => c.IsFeatured == true && c.CourseStatus == 1) // Assuming 1 is active status
                    .Include(c => c.Author)
                    .Include(c => c.CourseCategories)
                    .Take(4)
                    .ToList();

                if (recommendedCourses != null && recommendedCourses.Any())
                {
                    // Courses found - map to view model
                    viewModel.RecommendedCourses = recommendedCourses.Select(c => new CourseViewModel
                    {
                        CourseId = c.CourseId,
                        CourseName = c.CourseName,
                        CoursePicture = c.CourseImage ?? "~/lib/img/default-course.jpg",
                        Price = c.Price,
                        CreatedBy = c.Author.Username ?? "Unknown",
                        StarRating = CalculateAverageRating(c.CourseId),
                        CourseCategories = c.CourseCategories.Select(cc => cc.CourseCategoryName).ToList()
                    }).ToList();
                }
                // If no courses found, viewModel.RecommendedCourses will be an empty list
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recommended courses");
                ViewBag.Error = "An error occurred while loading courses. Please try again later.";
                // Keep viewModel.RecommendedCourses as an empty list
            }

            return View(viewModel);
        }

        private bool IsDatabaseConnected()
        {
            try
            {
                // Thử thực hiện một truy vấn đơn giản để kiểm tra kết nối
                _context.Database.CanConnect();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");
                return false;
            }
        }

        private int CalculateAverageRating(string courseId)
        {
            try
            {
                // Calculate average rating based on feedback
                var ratings = _context.Feedbacks
                    .Where(f => f.CourseId == courseId && f.StarRating.HasValue)
                    .Select(f => (int)f.StarRating!.Value);

                if (!ratings.Any())
                    return 0;

                return (int)Math.Round(ratings.Average());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating average rating for course {CourseId}", courseId);
                return 0;
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
