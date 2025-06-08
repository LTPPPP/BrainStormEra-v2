using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;

namespace BrainStormEra_MVC.Controllers
{
    [Authorize]
    public class QuestionController : Controller
    {
        private readonly BrainStormEraContext _context;

        public QuestionController(BrainStormEraContext context)
        {
            _context = context;
        }

        // GET: Question/Create/5 (quizId)
        public async Task<IActionResult> Create(string quizId)
        {
            if (string.IsNullOrEmpty(quizId))
            {
                return NotFound();
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (quiz.Course?.AuthorId != userId)
            {
                return Forbid();
            }

            var viewModel = new CreateQuestionViewModel
            {
                QuizId = quizId,
                QuizName = quiz.QuizName,
                QuestionType = "multiple_choice", // Default type
                Points = 1,
                QuestionOrder = await GetNextQuestionOrderAsync(quizId),
                AnswerOptions = new List<CreateAnswerOptionViewModel>
                {
                    new CreateAnswerOptionViewModel { OptionOrder = 1, IsCorrect = false },
                    new CreateAnswerOptionViewModel { OptionOrder = 2, IsCorrect = false },
                    new CreateAnswerOptionViewModel { OptionOrder = 3, IsCorrect = false },
                    new CreateAnswerOptionViewModel { OptionOrder = 4, IsCorrect = false }
                }
            };

            return View(viewModel);
        }

        // POST: Question/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

            if (quiz == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (quiz.Course?.AuthorId != userId)
            {
                return Forbid();
            }

            var questionId = Guid.NewGuid().ToString(); var question = new Question
            {
                QuestionId = questionId,
                QuizId = model.QuizId,
                QuestionText = model.QuestionText,
                QuestionType = model.QuestionType,
                Points = model.Points,
                QuestionOrder = model.QuestionOrder,
                Explanation = model.Explanation,
                QuestionCreatedAt = DateTime.UtcNow
            };

            _context.Questions.Add(question);

            // Add answer options based on question type
            if (model.QuestionType == "multiple_choice" && model.AnswerOptions?.Any() == true)
            {
                var validOptions = model.AnswerOptions.Where(o => !string.IsNullOrWhiteSpace(o.OptionText)).ToList(); foreach (var option in validOptions)
                {
                    var answerOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        OptionText = option.OptionText,
                        IsCorrect = option.IsCorrect,
                        OptionOrder = option.OptionOrder
                    };
                    _context.AnswerOptions.Add(answerOption);
                }
            }
            else if (model.QuestionType == "true_false")
            {
                // Add True/False options
                _context.AnswerOptions.AddRange(new[]
                {
                    new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        OptionText = "True",
                        IsCorrect = model.TrueFalseAnswer == true,
                        OptionOrder = 1
                    },
                    new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        OptionText = "False",
                        IsCorrect = model.TrueFalseAnswer == false,
                        OptionOrder = 2
                    }
                });
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question created successfully!";
            return RedirectToAction("Details", "Course", new { id = quiz.CourseId, activeTab = "curriculum" });
        }

        // GET: Question/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Course)
                .Include(q => q.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (question.Quiz?.Course?.AuthorId != userId)
            {
                return Forbid();
            }
            var viewModel = new CreateQuestionViewModel
            {
                QuestionId = question.QuestionId,
                QuizId = question.QuizId,
                QuizName = question.Quiz?.QuizName ?? "",
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType ?? "multiple_choice",
                Points = (int)(question.Points ?? 1),
                QuestionOrder = question.QuestionOrder ?? 1,
                Explanation = question.Explanation,
                AnswerOptions = question.AnswerOptions.Select(ao => new CreateAnswerOptionViewModel
                {
                    OptionId = ao.OptionId,
                    OptionText = ao.OptionText,
                    IsCorrect = ao.IsCorrect ?? false,
                    OptionOrder = ao.OptionOrder ?? 1
                }).ToList()
            };

            // Set TrueFalseAnswer for true/false questions
            if (question.QuestionType == "true_false")
            {
                var trueOption = question.AnswerOptions.FirstOrDefault(ao => ao.OptionText.ToLower() == "true");
                viewModel.TrueFalseAnswer = trueOption?.IsCorrect == true;
            }

            return View("Create", viewModel);
        }

        // POST: Question/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CreateQuestionViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Create", model);
            }

            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Course)
                .Include(q => q.AnswerOptions)
                .FirstOrDefaultAsync(q => q.QuestionId == model.QuestionId);

            if (question == null)
            {
                return NotFound();
            }

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (question.Quiz?.Course?.AuthorId != userId)
            {
                return Forbid();
            }            // Update question properties
            question.QuestionText = model.QuestionText;
            question.QuestionType = model.QuestionType;
            question.Points = model.Points;
            question.QuestionOrder = model.QuestionOrder;
            question.Explanation = model.Explanation;

            // Remove existing answer options
            _context.AnswerOptions.RemoveRange(question.AnswerOptions);

            // Add updated answer options based on question type
            if (model.QuestionType == "multiple_choice" && model.AnswerOptions?.Any() == true)
            {
                var validOptions = model.AnswerOptions.Where(o => !string.IsNullOrWhiteSpace(o.OptionText)).ToList(); foreach (var option in validOptions)
                {
                    var answerOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = question.QuestionId,
                        OptionText = option.OptionText,
                        IsCorrect = option.IsCorrect,
                        OptionOrder = option.OptionOrder
                    };
                    _context.AnswerOptions.Add(answerOption);
                }
            }
            else if (model.QuestionType == "true_false")
            {
                // Add True/False options
                _context.AnswerOptions.AddRange(new[]
                {
                    new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = question.QuestionId,
                        OptionText = "True",
                        IsCorrect = model.TrueFalseAnswer == true,
                        OptionOrder = 1
                    },
                    new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = question.QuestionId,
                        OptionText = "False",
                        IsCorrect = model.TrueFalseAnswer == false,
                        OptionOrder = 2
                    }
                });
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question updated successfully!";
            return RedirectToAction("Details", "Course", new { id = question.Quiz?.CourseId, activeTab = "curriculum" });
        }

        // POST: Question/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Course)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null)
            {
                return NotFound();
            }            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            if (question.Quiz?.Course?.AuthorId != userId)
            {
                return Forbid();
            }

            var courseId = question.Quiz?.CourseId;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question deleted successfully!";
            return RedirectToAction("Details", "Course", new { id = courseId, activeTab = "curriculum" });
        }

        private async Task<int> GetNextQuestionOrderAsync(string quizId)
        {
            var maxOrder = await _context.Questions
                .Where(q => q.QuizId == quizId)
                .MaxAsync(q => (int?)q.QuestionOrder) ?? 0;
            return maxOrder + 1;
        }
    }
}
