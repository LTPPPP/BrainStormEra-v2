using BrainStormEra_MVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace BrainStormEra_MVC.Controllers
{
    public class AchievementController : Controller
    {
        private readonly BrainStormEraContext _context;

        public AchievementController(BrainStormEraContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Learner,learner")]
        public async Task<IActionResult> LearnerAchievements()
        {
            var userId = User?.FindFirst("UserId")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Index", "Login");
            }

            // Assign achievements based on completed courses
            await AssignAchievementsBasedOnCompletedCourses();

            // Retrieve the learner's achievements
            var learnerAchievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Join(_context.Achievements, ua => ua.AchievementId, a => a.AchievementId, (ua, a) => new
                {
                    AchievementId = a.AchievementId,
                    AchievementName = a.AchievementName,
                    AchievementDescription = a.AchievementDescription,
                    AchievementIcon = a.AchievementIcon,
                    ReceivedDate = ua.ReceivedDate
                })
                .ToListAsync();

            ViewData["UserId"] = userId;
            ViewData["Achievements"] = learnerAchievements;

            return View("~/Views/Achievement/LearnerAchievements.cshtml");
        }        // Get achievement details via AJAX
        [HttpGet]
        public async Task<IActionResult> GetAchievementDetails(string achievementId, string userId)
        {
            if (string.IsNullOrEmpty(achievementId) || string.IsNullOrEmpty(userId))
            {
                return PartialView("_AchievementDetailError", "Invalid achievement or user ID");
            }

            var achievement = await _context.UserAchievements
                .Where(ua => ua.UserId == userId && ua.AchievementId == achievementId)
                .Join(_context.Achievements, ua => ua.AchievementId, a => a.AchievementId, (ua, a) => new
                {
                    AchievementName = a.AchievementName,
                    AchievementDescription = a.AchievementDescription,
                    AchievementIcon = a.AchievementIcon,
                    ReceivedDate = ua.ReceivedDate,
                    PointsEarned = ua.PointsEarned,
                    RelatedCourseId = ua.RelatedCourseId
                })
                .FirstOrDefaultAsync();

            if (achievement == null)
            {
                return PartialView("_AchievementDetailError", "Achievement not found");
            }

            // Get related course name if available
            string? relatedCourseName = null;
            if (!string.IsNullOrEmpty(achievement.RelatedCourseId))
            {
                var course = await _context.Courses
                    .AsNoTracking()
                    .Where(c => c.CourseId == achievement.RelatedCourseId)
                    .Select(c => c.CourseName)
                    .FirstOrDefaultAsync();
                relatedCourseName = course;
            }

            var model = new
            {
                achievement.AchievementName,
                achievement.AchievementDescription,
                achievement.AchievementIcon,
                achievement.ReceivedDate,
                achievement.PointsEarned,
                RelatedCourseName = relatedCourseName
            };

            return PartialView("_AchievementDetail", model);
        }

        private async Task AssignAchievementsBasedOnCompletedCourses()
        {
            // Get all achievements with condition (number of completed courses)
            var achievements = (await _context.Achievements.ToListAsync())
                .Select(a => new
                {
                    AchievementId = a.AchievementId,
                    RequiredCourses = int.TryParse(a.AchievementDescription, out int conditionValue) ? conditionValue : 0
                })
                .ToList();

            // Get count of completed courses (enrollment_status = 5) per user
            var userCompletedCourses = await _context.Enrollments
                .Where(e => e.EnrollmentStatus == 5)
                .GroupBy(e => e.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CompletedCourses = g.Count()
                })
                .ToDictionaryAsync(g => g.UserId, g => g.CompletedCourses);

            // Insert achievements for each user based on completed courses
            foreach (var user in userCompletedCourses)
            {
                foreach (var achievement in achievements)
                {
                    if (user.Value >= achievement.RequiredCourses)
                    {
                        if (!await _context.UserAchievements.AnyAsync(ua => ua.UserId == user.Key && ua.AchievementId == achievement.AchievementId))
                        {
                            var userAchievement = new UserAchievement
                            {
                                UserId = user.Key,
                                AchievementId = achievement.AchievementId,
                                ReceivedDate = DateOnly.FromDateTime(DateTime.Today)
                            };

                            _context.UserAchievements.Add(userAchievement);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
