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
        }        // GET: Question/Create/5 (quizId)
        public async Task<IActionResult> Create(string quizId)
        {
            Console.WriteLine("=== CREATE QUESTION GET METHOD CALLED ===");
            Console.WriteLine($"QuizId: {quizId}");

            if (string.IsNullOrEmpty(quizId))
            {
                Console.WriteLine("ERROR: QuizId is null or empty");
                return NotFound();
            }

            Console.WriteLine("=== FETCHING QUIZ DATA ===");
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
            {
                Console.WriteLine("ERROR: Quiz not found");
                return NotFound();
            }

            Console.WriteLine($"Quiz found: {quiz.QuizName}");
            Console.WriteLine($"Course: {quiz.Course?.CourseName}");

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            Console.WriteLine($"Current UserId: {userId}");
            Console.WriteLine($"Course AuthorId: {quiz.Course?.AuthorId}");

            if (quiz.Course?.AuthorId != userId)
            {
                Console.WriteLine("ERROR: User is not authorized to create questions for this quiz");
                return Forbid();
            }

            Console.WriteLine("=== GETTING NEXT QUESTION ORDER ===");
            var nextOrder = await GetNextQuestionOrderAsync(quizId);
            Console.WriteLine($"Next question order: {nextOrder}");

            Console.WriteLine("=== CREATING VIEW MODEL ===");
            var viewModel = new CreateQuestionViewModel
            {
                QuizId = quizId,
                QuizName = quiz.QuizName,
                QuestionType = "multiple_choice", // Default type
                Points = 1,
                QuestionOrder = nextOrder,
                AnswerOptions = new List<CreateAnswerOptionViewModel>
                {
                    new CreateAnswerOptionViewModel { OptionOrder = 1, IsCorrect = false },
                    new CreateAnswerOptionViewModel { OptionOrder = 2, IsCorrect = false },
                    new CreateAnswerOptionViewModel { OptionOrder = 3, IsCorrect = false },
                    new CreateAnswerOptionViewModel { OptionOrder = 4, IsCorrect = false }
                }
            };

            Console.WriteLine("=== VIEW MODEL CREATED SUCCESSFULLY ===");
            Console.WriteLine($"ViewModel - QuizId: {viewModel.QuizId}");
            Console.WriteLine($"ViewModel - QuizName: {viewModel.QuizName}");
            Console.WriteLine($"ViewModel - QuestionType: {viewModel.QuestionType}");
            Console.WriteLine($"ViewModel - Points: {viewModel.Points}");
            Console.WriteLine($"ViewModel - QuestionOrder: {viewModel.QuestionOrder}");
            Console.WriteLine($"ViewModel - AnswerOptions Count: {viewModel.AnswerOptions?.Count}");

            return View(viewModel);
        }        // POST: Question/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQuestionViewModel model)
        {
            Console.WriteLine("=== CREATE QUESTION POST METHOD CALLED ===");
            Console.WriteLine($"Model - QuizId: {model?.QuizId}");
            Console.WriteLine($"Model - QuestionText: {model?.QuestionText}");
            Console.WriteLine($"Model - QuestionType: {model?.QuestionType}");
            Console.WriteLine($"Model - Points: {model?.Points}");
            Console.WriteLine($"Model - QuestionOrder: {model?.QuestionOrder}");
            Console.WriteLine($"Model - Explanation: {model?.Explanation}");
            Console.WriteLine($"Model - TrueFalseAnswer: {model?.TrueFalseAnswer}"); if (model?.AnswerOptions != null)
            {
                Console.WriteLine($"AnswerOptions Count: {model.AnswerOptions.Count}");

                var correctOptionsFromModel = model.AnswerOptions.Where(o => o.IsCorrect).ToList();
                Console.WriteLine($"Correct options from model: {correctOptionsFromModel.Count}");

                for (int i = 0; i < model.AnswerOptions.Count; i++)
                {
                    var option = model.AnswerOptions[i];
                    Console.WriteLine($"  Option {i + 1}: Text='{option?.OptionText}', IsCorrect={option?.IsCorrect}, Order={option?.OptionOrder}");
                }

                if (correctOptionsFromModel.Count > 0)
                {
                    Console.WriteLine("SUCCESS: Found correct answer(s) in submitted data");
                    foreach (var correct in correctOptionsFromModel)
                    {
                        Console.WriteLine($"  Correct Answer: '{correct.OptionText}'");
                    }
                }
                else
                {
                    Console.WriteLine("WARNING: No correct answers found in submitted data");
                }
            }

            if (model == null)
            {
                Console.WriteLine("ERROR: Model is null");
                return BadRequest("Invalid model data");
            }
            Console.WriteLine("=== CHECKING MODEL STATE ===");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}"); if (!ModelState.IsValid)
            {
                Console.WriteLine("MODEL STATE ERRORS:");
                foreach (var error in ModelState)
                {
                    if (error.Value.Errors.Count > 0)
                    {
                        Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }

                // Additional logging for multiple choice validation
                if (model.QuestionType == "multiple_choice")
                {
                    Console.WriteLine("=== MULTIPLE CHOICE VALIDATION DETAILS ===");
                    if (model.AnswerOptions != null)
                    {
                        var correctOptions = model.AnswerOptions.Where(o => o.IsCorrect).ToList();
                        var validOptions = model.AnswerOptions.Where(o => !string.IsNullOrWhiteSpace(o.OptionText)).ToList();

                        Console.WriteLine($"Total AnswerOptions: {model.AnswerOptions.Count}");
                        Console.WriteLine($"Valid options (with text): {validOptions.Count}");
                        Console.WriteLine($"Correct options: {correctOptions.Count}");

                        for (int i = 0; i < model.AnswerOptions.Count; i++)
                        {
                            var option = model.AnswerOptions[i];
                            Console.WriteLine($"  Option {i + 1}: Text='{option?.OptionText}', IsCorrect={option?.IsCorrect}, IsValid={!string.IsNullOrWhiteSpace(option?.OptionText)}");
                        }

                        if (correctOptions.Count == 0)
                        {
                            Console.WriteLine("ERROR: No correct answers selected for multiple choice question");
                        }
                    }
                    else
                    {
                        Console.WriteLine("ERROR: AnswerOptions is null for multiple choice question");
                    }
                }// For true/false, essay, and fill_blank questions, clear AnswerOptions validation errors
                if (model.QuestionType == "true_false" || model.QuestionType == "essay" || model.QuestionType == "fill_blank")
                {
                    Console.WriteLine($"=== CLEARING ANSWER OPTIONS ERRORS FOR {model.QuestionType.ToUpper()} ===");
                    var keysToRemove = ModelState.Keys.Where(k => k.StartsWith("AnswerOptions")).ToList();
                    foreach (var key in keysToRemove)
                    {
                        ModelState.Remove(key);
                        Console.WriteLine($"Removed validation error for: {key}");
                    }// Re-check model state after clearing irrelevant errors
                    Console.WriteLine($"ModelState.IsValid after cleanup: {ModelState.IsValid}");

                    // If only AnswerOptions errors were present, proceed with creation
                    if (ModelState.IsValid)
                    {
                        Console.WriteLine($"=== MODEL STATE IS NOW VALID AFTER CLEANUP FOR {model.QuestionType.ToUpper()} ===");
                    }
                    else
                    {
                        Console.WriteLine($"=== STILL HAVE VALIDATION ERRORS AFTER CLEANUP FOR {model.QuestionType.ToUpper()} ===");
                        Console.WriteLine("REMAINING ERRORS:");
                        foreach (var error in ModelState)
                        {
                            if (error.Value.Errors.Count > 0)
                            {
                                Console.WriteLine($"  {error.Key}: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                            }
                        }
                        return View(model);
                    }
                }
                else
                {
                    Console.WriteLine("=== RETURNING VIEW WITH VALIDATION ERRORS ===");
                    return View(model);
                }
            }

            Console.WriteLine("=== FETCHING QUIZ DATA FOR VALIDATION ===");
            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

            if (quiz == null)
            {
                Console.WriteLine("ERROR: Quiz not found during POST");
                return NotFound();
            }

            Console.WriteLine($"Quiz found: {quiz.QuizName}");
            Console.WriteLine($"Course: {quiz.Course?.CourseName}");

            // Check if user is the author of the course
            var userId = User.FindFirst("UserId")?.Value;
            Console.WriteLine($"Current UserId: {userId}");
            Console.WriteLine($"Course AuthorId: {quiz.Course?.AuthorId}");

            if (quiz.Course?.AuthorId != userId)
            {
                Console.WriteLine("ERROR: User is not authorized to create questions for this quiz");
                return Forbid();
            }

            var questionId = Guid.NewGuid().ToString();
            Console.WriteLine($"=== CREATING QUESTION WITH ID: {questionId} ===");

            var question = new Question
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

            Console.WriteLine("=== QUESTION OBJECT CREATED ===");
            Console.WriteLine($"QuestionId: {question.QuestionId}");
            Console.WriteLine($"QuizId: {question.QuizId}");
            Console.WriteLine($"QuestionText: {question.QuestionText}");
            Console.WriteLine($"QuestionType: {question.QuestionType}");
            Console.WriteLine($"Points: {question.Points}");
            Console.WriteLine($"QuestionOrder: {question.QuestionOrder}");

            _context.Questions.Add(question);
            Console.WriteLine("=== QUESTION ADDED TO CONTEXT ===");            // Add answer options based on question type
            if (model.QuestionType == "multiple_choice" && model.AnswerOptions?.Any() == true)
            {
                Console.WriteLine("=== PROCESSING MULTIPLE CHOICE OPTIONS ===");
                var validOptions = model.AnswerOptions.Where(o => !string.IsNullOrWhiteSpace(o.OptionText)).ToList();
                var correctOptions = validOptions.Where(o => o.IsCorrect).ToList();

                Console.WriteLine($"Total options: {model.AnswerOptions.Count}");
                Console.WriteLine($"Valid options count: {validOptions.Count}");
                Console.WriteLine($"Correct options count: {correctOptions.Count}");

                if (correctOptions.Count == 0)
                {
                    Console.WriteLine("WARNING: No correct options found for multiple choice question");
                }

                foreach (var option in validOptions)
                {
                    var answerOption = new AnswerOption
                    {
                        OptionId = Guid.NewGuid().ToString(),
                        QuestionId = questionId,
                        OptionText = option.OptionText,
                        IsCorrect = option.IsCorrect,
                        OptionOrder = option.OptionOrder
                    };

                    Console.WriteLine($"Creating answer option: ID={answerOption.OptionId}, Text='{answerOption.OptionText}', IsCorrect={answerOption.IsCorrect}, Order={answerOption.OptionOrder}");
                    _context.AnswerOptions.Add(answerOption);
                }
                Console.WriteLine("=== MULTIPLE CHOICE OPTIONS ADDED TO CONTEXT ===");
            }
            else if (model.QuestionType == "true_false")
            {
                Console.WriteLine("=== PROCESSING TRUE/FALSE OPTIONS ===");
                Console.WriteLine($"TrueFalseAnswer: {model.TrueFalseAnswer}");

                // Add True/False options
                var trueOption = new AnswerOption
                {
                    OptionId = Guid.NewGuid().ToString(),
                    QuestionId = questionId,
                    OptionText = "True",
                    IsCorrect = model.TrueFalseAnswer == true,
                    OptionOrder = 1
                };

                var falseOption = new AnswerOption
                {
                    OptionId = Guid.NewGuid().ToString(),
                    QuestionId = questionId,
                    OptionText = "False",
                    IsCorrect = model.TrueFalseAnswer == false,
                    OptionOrder = 2
                };

                Console.WriteLine($"True option: ID={trueOption.OptionId}, IsCorrect={trueOption.IsCorrect}");
                Console.WriteLine($"False option: ID={falseOption.OptionId}, IsCorrect={falseOption.IsCorrect}");

                _context.AnswerOptions.AddRange(new[] { trueOption, falseOption });
                Console.WriteLine("=== TRUE/FALSE OPTIONS ADDED TO CONTEXT ===");
            }
            else if (model.QuestionType == "essay")
            {
                Console.WriteLine("=== PROCESSING ESSAY QUESTION ===");
                Console.WriteLine("Essay questions do not require answer options");
                // Essay questions don't need answer options
            }
            else if (model.QuestionType == "fill_blank")
            {
                Console.WriteLine("=== PROCESSING FILL IN THE BLANK QUESTION ===");
                Console.WriteLine("Fill in the blank questions do not require answer options");
                // Fill in the blank questions don't need predefined answer options
                // The correct answers will be stored in the question text or explanation
            }

            Console.WriteLine("=== SAVING CHANGES TO DATABASE ===");
            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine("=== CHANGES SAVED SUCCESSFULLY ===");

                TempData["SuccessMessage"] = "Question created successfully!";
                Console.WriteLine("=== SUCCESS MESSAGE SET ===");

                Console.WriteLine($"=== REDIRECTING TO QUIZ DETAILS: {model.QuizId} ===");
                return RedirectToAction("Details", "Quiz", new { id = model.QuizId });
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ERROR SAVING TO DATABASE ===");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Exception Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                TempData["ErrorMessage"] = "An error occurred while creating the question. Please try again.";
                return View(model);
            }
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
                    }                });
            }
            // Essay and fill_blank questions don't need answer options
            // The handling is already done by removing existing options above

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question updated successfully!";
            return RedirectToAction("Details", "Quiz", new { id = model.QuizId });
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
            var quizId = question.Quiz?.QuizId;

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question deleted successfully!";
            return RedirectToAction("Details", "Quiz", new { id = quizId });
        }

        // POST: Question/Reorder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReorderQuestions(string quizId, List<string> questionIds)
        {
            if (string.IsNullOrEmpty(quizId) || questionIds == null || !questionIds.Any())
            {
                return BadRequest("Invalid data");
            }

            var quiz = await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
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

            // Update question orders
            for (int i = 0; i < questionIds.Count; i++)
            {
                var question = quiz.Questions.FirstOrDefault(q => q.QuestionId == questionIds[i]);
                if (question != null)
                {
                    question.QuestionOrder = i + 1;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Question order updated successfully!" });
        }

        // POST: Question/Duplicate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Duplicate(string id)
        {
            var question = await _context.Questions
                .Include(q => q.Quiz)
                    .ThenInclude(quiz => quiz.Course)
                .Include(q => q.AnswerOptions)
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

            var newQuestionId = Guid.NewGuid().ToString();
            var newQuestion = new Question
            {
                QuestionId = newQuestionId,
                QuizId = question.QuizId,
                QuestionText = question.QuestionText + " (Copy)",
                QuestionType = question.QuestionType,
                Points = question.Points,
                QuestionOrder = await GetNextQuestionOrderAsync(question.QuizId),
                Explanation = question.Explanation,
                QuestionCreatedAt = DateTime.UtcNow
            };

            _context.Questions.Add(newQuestion);

            // Duplicate answer options
            foreach (var option in question.AnswerOptions)
            {
                var newOption = new AnswerOption
                {
                    OptionId = Guid.NewGuid().ToString(),
                    QuestionId = newQuestionId,
                    OptionText = option.OptionText,
                    IsCorrect = option.IsCorrect,
                    OptionOrder = option.OptionOrder
                };
                _context.AnswerOptions.Add(newOption);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Question duplicated successfully!";
            return RedirectToAction("Details", "Quiz", new { id = question.QuizId });
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
