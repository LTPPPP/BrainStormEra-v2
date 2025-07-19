using Microsoft.Extensions.Logging;
using DataAccessLayer.Data;
using DataAccessLayer.Models;
using DataAccessLayer.Models.ViewModels;
using BusinessLogicLayer.Services.Interfaces;
using BusinessLogicLayer.DTOs.Common;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BusinessLogicLayer.Services.Implementations
{
    /// <summary>
    /// Implementation class that handles complex business logic for Quiz operations.
    /// This class sits between Controller and Service layer to handle authentication, 
    /// authorization, validation, and error handling.
    /// </summary>
    public class QuizServiceImpl : IQuizService
    {
        private readonly BrainStormEraContext _context;
        private readonly LessonServiceImpl _lessonService;
        private readonly IAchievementUnlockService _achievementUnlockService;
        private readonly ILogger<QuizServiceImpl> _logger;

        public QuizServiceImpl(
            BrainStormEraContext context,
            LessonServiceImpl lessonService,
            IAchievementUnlockService achievementUnlockService,
            ILogger<QuizServiceImpl> logger)
        {
            _context = context;
            _lessonService = lessonService;
            _achievementUnlockService = achievementUnlockService;
            _logger = logger;
        }

        public async Task<ServiceResult<CreateQuizViewModel>> GetCreateQuizViewModelAsync(string chapterId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(chapterId))
                {
                    return ServiceResult<CreateQuizViewModel>.Failure("Chapter ID is required");
                }

                var chapter = await _context.Chapters
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == chapterId);

                if (chapter == null)
                {
                    return ServiceResult<CreateQuizViewModel>.Failure("Chapter not found");
                }

                // Check authorization
                if (chapter.Course.AuthorId != userId)
                {
                    return ServiceResult<CreateQuizViewModel>.Failure("Unauthorized");
                }

                // Get lessons in the chapter to find the last one
                var lessonsInChapter = await _lessonService.GetLessonsInChapterAsync(chapterId);
                var orderedLessons = lessonsInChapter.OrderBy(l => l.LessonOrder).ToList();
                var lastLesson = orderedLessons.LastOrDefault(); var viewModel = new CreateQuizViewModel
                {
                    ChapterId = chapterId,
                    CourseId = chapter.CourseId,
                    CourseName = chapter.Course.CourseName,
                    ChapterName = chapter.ChapterName,
                    LessonId = lastLesson?.LessonId ?? "",
                    LessonName = lastLesson?.LessonName ?? "",
                    AvailableLessons = orderedLessons.Select(l => new QuizLessonViewModel
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription ?? "",
                        LessonOrder = l.LessonOrder,
                        LessonType = l.LessonType?.LessonTypeName ?? "Content",
                        EstimatedDuration = 0,
                        IsLocked = l.IsLocked ?? false
                    }).ToList(),
                    TimeLimit = 30,
                    MaxAttempts = 3,
                    PassingScore = 70,
                    IsFinalQuiz = false,
                    IsPrerequisiteQuiz = false,
                    BlocksLessonCompletion = false
                };

                return ServiceResult<CreateQuizViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting create quiz view model for chapter {ChapterId}", chapterId);
                return ServiceResult<CreateQuizViewModel>.Failure("An error occurred while preparing the quiz creation form");
            }
        }

        public async Task<ServiceResult<string>> CreateQuizAsync(CreateQuizViewModel model, string userId)
        {
            try
            {
                // Validate chapter and authorization
                var chapter = await _context.Chapters
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == model.ChapterId);

                if (chapter == null)
                {
                    return ServiceResult<string>.Failure("Chapter not found");
                }

                if (chapter.Course.AuthorId != userId)
                {
                    return ServiceResult<string>.Failure("Unauthorized");
                }

                // Validate lesson exists
                var lesson = await _context.Lessons.FindAsync(model.LessonId);
                if (lesson == null)
                {
                    return ServiceResult<string>.Failure("Selected lesson not found");
                }
                var quizId = Guid.NewGuid().ToString();
                var quiz = new Quiz
                {
                    QuizId = quizId,
                    LessonId = model.LessonId,
                    CourseId = model.CourseId,
                    QuizName = model.QuizTitle,
                    QuizDescription = model.QuizDescription,
                    TimeLimit = model.TimeLimit,
                    MaxAttempts = model.MaxAttempts,
                    PassingScore = model.PassingScore,
                    IsFinalQuiz = model.IsFinalQuiz,
                    IsPrerequisiteQuiz = model.IsPrerequisiteQuiz,
                    BlocksLessonCompletion = model.BlocksLessonCompletion,
                    QuizCreatedAt = DateTime.UtcNow,
                    QuizUpdatedAt = DateTime.UtcNow
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                return ServiceResult<string>.Success(quizId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quiz {QuizTitle}", model.QuizTitle);
                return ServiceResult<string>.Failure("An error occurred while creating the quiz");
            }
        }

        public async Task<ServiceResult<CreateQuizViewModel>> GetEditQuizViewModelAsync(string quizId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(quizId))
                {
                    return ServiceResult<CreateQuizViewModel>.Failure("Quiz ID is required");
                }
                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return ServiceResult<CreateQuizViewModel>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<CreateQuizViewModel>.Failure("Unauthorized");
                }
                var viewModel = new CreateQuizViewModel
                {
                    QuizId = quiz.QuizId,
                    ChapterId = quiz.Lesson?.ChapterId ?? "",
                    CourseId = quiz.Lesson?.Chapter?.CourseId ?? "",
                    CourseName = quiz.Lesson?.Chapter?.Course?.CourseName ?? "",
                    ChapterName = quiz.Lesson?.Chapter?.ChapterName ?? "",
                    LessonId = quiz.LessonId,
                    QuizTitle = quiz.QuizName,
                    QuizDescription = quiz.QuizDescription,
                    TimeLimit = quiz.TimeLimit ?? 30,
                    MaxAttempts = quiz.MaxAttempts ?? 3,
                    PassingScore = quiz.PassingScore ?? 70,
                    IsFinalQuiz = quiz.IsFinalQuiz ?? false,
                    IsPrerequisiteQuiz = quiz.IsPrerequisiteQuiz ?? false,
                    BlocksLessonCompletion = quiz.BlocksLessonCompletion ?? false
                };

                return ServiceResult<CreateQuizViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting edit quiz view model for quiz {QuizId}", quizId);
                return ServiceResult<CreateQuizViewModel>.Failure("An error occurred while loading the quiz for editing");
            }
        }

        public async Task<ServiceResult<bool>> UpdateQuizAsync(CreateQuizViewModel model, string userId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

                if (quiz == null)
                {
                    return ServiceResult<bool>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<bool>.Failure("Unauthorized");
                }

                // Validate lesson exists
                var lesson = await _context.Lessons.FindAsync(model.LessonId);
                if (lesson == null)
                {
                    return ServiceResult<bool>.Failure("Selected lesson not found");
                }                // Update quiz properties
                quiz.LessonId = model.LessonId;
                quiz.QuizName = model.QuizTitle;
                quiz.QuizDescription = model.QuizDescription;
                quiz.TimeLimit = model.TimeLimit;
                quiz.MaxAttempts = model.MaxAttempts;
                quiz.PassingScore = model.PassingScore;
                quiz.IsFinalQuiz = model.IsFinalQuiz;
                quiz.IsPrerequisiteQuiz = model.IsPrerequisiteQuiz;
                quiz.BlocksLessonCompletion = model.BlocksLessonCompletion;
                quiz.QuizUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quiz {QuizId}", model.QuizId);
                return ServiceResult<bool>.Failure("An error occurred while updating the quiz");
            }
        }

        public async Task<ServiceResult<string>> DeleteQuizAsync(string quizId, string userId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .Include(q => q.QuizAttempts)
                    .Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return ServiceResult<string>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<string>.Failure("Unauthorized");
                }

                // Check if quiz has active attempts (not completed)
                var activeAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime == null).Count() ?? 0;
                if (activeAttempts > 0)
                {
                    return ServiceResult<string>.Failure($"Cannot delete quiz: {activeAttempts} student(s) are currently taking this quiz");
                }

                // Check if quiz has completed attempts
                var completedAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime != null).Count() ?? 0;
                if (completedAttempts > 0)
                {
                    return ServiceResult<string>.Failure($"Cannot delete quiz: {completedAttempts} student(s) have already taken this quiz. Consider archiving instead.");
                }

                // Check quiz status - don't allow deletion of published/active quizzes
                if (quiz.QuizStatus == 1 || quiz.QuizStatus == 2) // Published or Active
                {
                    return ServiceResult<string>.Failure("Cannot delete a published/active quiz. Please change the status to Draft first.");
                }

                // Check if quiz is a prerequisite for other content
                if (quiz.IsPrerequisiteQuiz == true)
                {
                    return ServiceResult<string>.Failure("Cannot delete quiz: This quiz is set as a prerequisite for course progression");
                }

                // Check if quiz blocks lesson completion
                if (quiz.BlocksLessonCompletion == true)
                {
                    return ServiceResult<string>.Failure("Cannot delete quiz: This quiz blocks lesson completion. Please change the settings first.");
                }

                var lessonId = quiz.LessonId ?? "";

                // Soft delete by changing status to Archived (4) instead of hard delete
                quiz.QuizStatus = 4; // Archived
                quiz.QuizUpdatedAt = DateTime.UtcNow;

                // Also archive related questions
                foreach (var question in quiz.Questions)
                {
                    // You might want to add a status field to questions as well
                    // For now, we'll keep the questions but mark the quiz as archived
                }

                await _context.SaveChangesAsync();

                return ServiceResult<string>.Success(lessonId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting quiz {QuizId}", quizId);
                return ServiceResult<string>.Failure("An error occurred while deleting the quiz");
            }
        }
        public async Task<ServiceResult<QuizDetailsViewModel>> GetQuizDetailsAsync(string quizId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(quizId))
                {
                    return ServiceResult<QuizDetailsViewModel>.Failure("Quiz ID is required");
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .Include(q => q.Questions.OrderBy(question => question.QuestionOrder))
                        .ThenInclude(question => question.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return ServiceResult<QuizDetailsViewModel>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<QuizDetailsViewModel>.Failure("Unauthorized");
                }
                var viewModel = new QuizDetailsViewModel
                {
                    QuizId = quiz.QuizId,
                    QuizName = quiz.QuizName,
                    QuizDescription = quiz.QuizDescription,
                    CourseId = quiz.Lesson?.Chapter?.CourseId ?? "",
                    CourseName = quiz.Lesson?.Chapter?.Course?.CourseName ?? "",
                    LessonId = quiz.LessonId,
                    LessonName = quiz.Lesson?.LessonName ?? "",
                    TimeLimit = quiz.TimeLimit ?? 0,
                    PassingScore = quiz.PassingScore ?? 0,
                    QuizCreatedAt = quiz.QuizCreatedAt,
                    QuizUpdatedAt = quiz.QuizUpdatedAt,
                    Questions = quiz.Questions?.Select(q => new QuestionSummaryViewModel
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType ?? "",
                        Points = (int)(q.Points ?? 0),
                        QuestionOrder = q.QuestionOrder ?? 0,
                        AnswerOptionsCount = q.AnswerOptions?.Count ?? 0,
                        QuestionCreatedAt = q.QuestionCreatedAt,
                        QuestionUpdatedAt = q.QuestionCreatedAt // Use CreatedAt since UpdatedAt doesn't exist
                    }).ToList() ?? new List<QuestionSummaryViewModel>()
                };

                return ServiceResult<QuizDetailsViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz details for quiz {QuizId}", quizId);
                return ServiceResult<QuizDetailsViewModel>.Failure("An error occurred while loading the quiz details");
            }
        }
        public async Task<ServiceResult<QuizPreviewViewModel>> GetQuizPreviewAsync(string quizId, string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(quizId))
                {
                    return ServiceResult<QuizPreviewViewModel>.Failure("Quiz ID is required");
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .Include(q => q.Questions.OrderBy(question => question.QuestionOrder))
                        .ThenInclude(question => question.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return ServiceResult<QuizPreviewViewModel>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<QuizPreviewViewModel>.Failure("Unauthorized");
                }
                var viewModel = new QuizPreviewViewModel
                {
                    QuizId = quiz.QuizId,
                    QuizName = quiz.QuizName,
                    QuizDescription = quiz.QuizDescription,
                    CourseId = quiz.Lesson?.Chapter?.CourseId ?? "",
                    CourseName = quiz.Lesson?.Chapter?.Course?.CourseName ?? "",
                    TimeLimit = quiz.TimeLimit ?? 0,
                    PassingScore = quiz.PassingScore ?? 0,
                    Questions = quiz.Questions?.Select(q => new QuestionPreviewViewModel
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType ?? "",
                        Points = (int)(q.Points ?? 0),
                        QuestionOrder = q.QuestionOrder ?? 0,
                        Explanation = q.Explanation,
                        AnswerOptions = q.AnswerOptions?.Select(ao => new AnswerOptionPreviewViewModel
                        {
                            OptionId = ao.OptionId,
                            OptionText = ao.OptionText,
                            OptionOrder = ao.OptionOrder ?? 0
                        }).ToList() ?? new List<AnswerOptionPreviewViewModel>()
                    }).ToList() ?? new List<QuestionPreviewViewModel>()
                };

                return ServiceResult<QuizPreviewViewModel>.Success(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz preview for quiz {QuizId}", quizId);
                return ServiceResult<QuizPreviewViewModel>.Failure("An error occurred while loading the quiz preview");
            }
        }

        #region Business Logic Methods for Controller Operations

        /// <summary>
        /// Handle Create Quiz GET request with user authentication and authorization
        /// </summary>
        public async Task<CreateQuizResult> GetCreateQuizAsync(ClaimsPrincipal user, string chapterId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateQuizResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                var result = await GetCreateQuizViewModelAsync(chapterId, userId); if (!result.IsSuccess)
                {
                    if (result.Message == "Chapter not found")
                    {
                        return new CreateQuizResult
                        {
                            Success = false,
                            IsNotFound = true
                        };
                    }
                    else if (result.Message == "Unauthorized")
                    {
                        return new CreateQuizResult
                        {
                            Success = false,
                            IsForbidden = true
                        };
                    }
                    return new CreateQuizResult
                    {
                        Success = false,
                        ErrorMessage = result.Message
                    };
                }

                return new CreateQuizResult
                {
                    Success = true,
                    ViewModel = result.Data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetCreateQuizAsync for chapter {ChapterId}", chapterId);
                return new CreateQuizResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the quiz creation page"
                };
            }
        }

        /// <summary>
        /// Handle Create Quiz POST request with validation and user authentication
        /// </summary>
        public async Task<CreateQuizResult> CreateQuizAsync(ClaimsPrincipal user, CreateQuizViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new CreateQuizResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                if (!modelState.IsValid)
                {
                    // Repopulate lesson data if validation fails
                    var validationLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
                    var validationOrderedLessons = validationLessons.OrderBy(l => l.LessonOrder).ToList();
                    var validationLastLesson = validationOrderedLessons.LastOrDefault();

                    model.LessonId = validationLastLesson?.LessonId;
                    model.LessonName = validationLastLesson?.LessonName;
                    model.AvailableLessons = validationOrderedLessons.Select(l => new QuizLessonViewModel
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription ?? "",
                        LessonOrder = l.LessonOrder,
                        LessonType = l.LessonType?.LessonTypeName ?? "Content",
                        EstimatedDuration = 0,
                        IsLocked = l.IsLocked ?? false
                    }).ToList();

                    return new CreateQuizResult
                    {
                        Success = false,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                // Validate chapter and authorization
                var chapter = await _context.Chapters
                    .Include(c => c.Course)
                    .FirstOrDefaultAsync(c => c.ChapterId == model.ChapterId);

                if (chapter == null)
                {
                    return new CreateQuizResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                if (chapter.Course.AuthorId != userId)
                {
                    return new CreateQuizResult
                    {
                        Success = false,
                        IsForbidden = true
                    };
                }

                // Automatically find the last lesson in the chapter
                var chapterLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
                var finalLesson = chapterLessons.OrderBy(l => l.LessonOrder).LastOrDefault();

                if (finalLesson == null)
                {
                    modelState.AddModelError("", "Cannot create quiz: No lessons found in this chapter. Please add at least one lesson first.");
                    model.AvailableLessons = new List<QuizLessonViewModel>();

                    return new CreateQuizResult
                    {
                        Success = false,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                // Set the lesson ID to the last lesson
                model.LessonId = finalLesson.LessonId;

                var result = await CreateQuizAsync(model, userId); if (!result.IsSuccess)
                {
                    modelState.AddModelError("", result.Message);
                    return new CreateQuizResult
                    {
                        Success = false,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                return new CreateQuizResult
                {
                    Success = true,
                    QuizId = result.Data,
                    SuccessMessage = $"Quiz '{model.QuizTitle}' created successfully and automatically associated with the last lesson '{finalLesson.LessonName}'!",
                    RedirectAction = "Details",
                    RedirectController = "Quiz"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateQuizAsync for model {QuizTitle}", model.QuizTitle);
                return new CreateQuizResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while creating the quiz"
                };
            }
        }

        /// <summary>
        /// Handle Edit Quiz GET request with user authentication and authorization
        /// </summary>
        public async Task<EditQuizResult> GetEditQuizAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                if (string.IsNullOrEmpty(quizId))
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .Include(q => q.Lesson)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                if (quiz.Course?.AuthorId != userId)
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        IsForbidden = true
                    };
                }

                // Get chapter info
                var chapter = await _context.Chapters
                    .FirstOrDefaultAsync(c => c.CourseId == quiz.CourseId);

                if (chapter == null)
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        ErrorMessage = "Chapter not found for this quiz."
                    };
                }

                // Get lessons in the chapter to find the last one
                var lessonsInChapter = await _lessonService.GetLessonsInChapterAsync(chapter.ChapterId);
                var orderedLessons = lessonsInChapter.OrderBy(l => l.LessonOrder).ToList();
                var lastLesson = orderedLessons.LastOrDefault();

                var viewModel = new CreateQuizViewModel
                {
                    QuizId = quiz.QuizId,
                    QuizTitle = quiz.QuizName,
                    QuizDescription = quiz.QuizDescription,
                    ChapterId = chapter.ChapterId,
                    CourseId = quiz.CourseId ?? "",
                    CourseName = quiz.Course?.CourseName ?? "",
                    ChapterName = chapter.ChapterName,
                    LessonId = lastLesson?.LessonId,
                    LessonName = lastLesson?.LessonName,
                    AvailableLessons = orderedLessons.Select(l => new QuizLessonViewModel
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription ?? "",
                        LessonOrder = l.LessonOrder,
                        LessonType = l.LessonType?.LessonTypeName ?? "Content",
                        EstimatedDuration = 0,
                        IsLocked = l.IsLocked ?? false
                    }).ToList(),
                    TimeLimit = quiz.TimeLimit,
                    PassingScore = quiz.PassingScore,
                    MaxAttempts = 3,
                    IsFinalQuiz = false,
                    IsPrerequisiteQuiz = false,
                    BlocksLessonCompletion = false
                };

                return new EditQuizResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEditQuizAsync for quiz {QuizId}", quizId);
                return new EditQuizResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the quiz for editing"
                };
            }
        }

        /// <summary>
        /// Handle Edit Quiz POST request with validation and user authentication
        /// </summary>
        public async Task<EditQuizResult> UpdateQuizAsync(ClaimsPrincipal user, CreateQuizViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }

                if (!modelState.IsValid)
                {
                    // Repopulate lesson data if validation fails
                    var validationLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
                    var validationOrderedLessons = validationLessons.OrderBy(l => l.LessonOrder).ToList();
                    var validationLastLesson = validationOrderedLessons.LastOrDefault();

                    model.LessonId = validationLastLesson?.LessonId;
                    model.LessonName = validationLastLesson?.LessonName;
                    model.AvailableLessons = validationOrderedLessons.Select(l => new QuizLessonViewModel
                    {
                        LessonId = l.LessonId,
                        LessonName = l.LessonName,
                        LessonDescription = l.LessonDescription ?? "",
                        LessonOrder = l.LessonOrder,
                        LessonType = l.LessonType?.LessonTypeName ?? "Content",
                        EstimatedDuration = 0,
                        IsLocked = l.IsLocked ?? false
                    }).ToList();

                    return new EditQuizResult
                    {
                        Success = false,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == model.QuizId);

                if (quiz == null)
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                if (quiz.Course?.AuthorId != userId)
                {
                    return new EditQuizResult
                    {
                        Success = false,
                        IsForbidden = true
                    };
                }

                // Automatically find the last lesson in the chapter
                var chapterLessons = await _lessonService.GetLessonsInChapterAsync(model.ChapterId);
                var finalLesson = chapterLessons.OrderBy(l => l.LessonOrder).LastOrDefault();

                if (finalLesson == null)
                {
                    modelState.AddModelError("", "Cannot update quiz: No lessons found in this chapter.");
                    model.AvailableLessons = new List<QuizLessonViewModel>();

                    return new EditQuizResult
                    {
                        Success = false,
                        ViewModel = model,
                        ReturnView = true
                    };
                }

                quiz.QuizName = model.QuizTitle;
                quiz.QuizDescription = model.QuizDescription;
                quiz.LessonId = finalLesson.LessonId;
                quiz.TimeLimit = model.TimeLimit;
                quiz.PassingScore = model.PassingScore;
                quiz.QuizUpdatedAt = DateTime.Now;

                _context.Update(quiz);
                await _context.SaveChangesAsync();

                return new EditQuizResult
                {
                    Success = true,
                    QuizId = quiz.QuizId,
                    SuccessMessage = $"Quiz '{model.QuizTitle}' updated successfully and associated with the last lesson '{finalLesson.LessonName}'!",
                    RedirectAction = "Details",
                    RedirectController = "Quiz"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateQuizAsync for quiz {QuizId}", model.QuizId);
                return new EditQuizResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while updating the quiz"
                };
            }
        }

        /// <summary>
        /// Handle Delete Quiz POST request with user authentication and authorization
        /// </summary>
        public async Task<DeleteQuizResult> DeleteQuizAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Course)
                    .Include(q => q.QuizAttempts)
                    .Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                if (quiz.Course?.AuthorId != userId)
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        IsForbidden = true
                    };
                }

                // Check if quiz has active attempts (not completed)
                var activeAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime == null).Count() ?? 0;
                if (activeAttempts > 0)
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot delete quiz: {activeAttempts} student(s) are currently taking this quiz"
                    };
                }

                // Check if quiz has completed attempts
                var completedAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime != null).Count() ?? 0;
                if (completedAttempts > 0)
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        ErrorMessage = $"Cannot delete quiz: {completedAttempts} student(s) have already taken this quiz. Consider archiving instead."
                    };
                }

                // Check quiz status - don't allow deletion of published/active quizzes
                if (quiz.QuizStatus == 1 || quiz.QuizStatus == 2) // Published or Active
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot delete a published/active quiz. Please change the status to Draft first."
                    };
                }

                // Check if quiz is a prerequisite for other content
                if (quiz.IsPrerequisiteQuiz == true)
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot delete quiz: This quiz is set as a prerequisite for course progression"
                    };
                }

                // Check if quiz blocks lesson completion
                if (quiz.BlocksLessonCompletion == true)
                {
                    return new DeleteQuizResult
                    {
                        Success = false,
                        ErrorMessage = "Cannot delete quiz: This quiz blocks lesson completion. Please change the settings first."
                    };
                }

                var courseId = quiz.CourseId;

                // Soft delete by changing status to Archived (4) instead of hard delete
                quiz.QuizStatus = 4; // Archived
                quiz.QuizUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return new DeleteQuizResult
                {
                    Success = true,
                    CourseId = courseId,
                    SuccessMessage = "Quiz archived successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteQuizAsync for quiz {QuizId}", quizId);
                return new DeleteQuizResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while deleting the quiz"
                };
            }
        }

        /// <summary>
        /// Handle Quiz Details GET request with user authentication and authorization
        /// </summary>
        public async Task<QuizDetailsResult> GetQuizDetailsAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new QuizDetailsResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }
                var result = await GetQuizDetailsAsync(quizId, userId);
                if (!result.IsSuccess)
                {
                    if (result.Message == "Quiz not found")
                    {
                        return new QuizDetailsResult
                        {
                            Success = false,
                            IsNotFound = true
                        };
                    }
                    else if (result.Message == "Unauthorized")
                    {
                        return new QuizDetailsResult
                        {
                            Success = false,
                            IsForbidden = true
                        };
                    }
                    return new QuizDetailsResult
                    {
                        Success = false,
                        ErrorMessage = result.Message
                    };
                }

                return new QuizDetailsResult
                {
                    Success = true,
                    ViewModel = result.Data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetQuizDetailsAsync for quiz {QuizId}", quizId);
                return new QuizDetailsResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the quiz details"
                };
            }
        }

        /// <summary>
        /// Handle Quiz Preview GET request with user authentication and authorization
        /// </summary>
        public async Task<QuizPreviewResult> GetQuizPreviewAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new QuizPreviewResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Index",
                        RedirectController = "Login"
                    };
                }
                var result = await GetQuizPreviewAsync(quizId, userId);
                if (!result.IsSuccess)
                {
                    if (result.Message == "Quiz not found")
                    {
                        return new QuizPreviewResult
                        {
                            Success = false,
                            IsNotFound = true
                        };
                    }
                    else if (result.Message == "Unauthorized")
                    {
                        return new QuizPreviewResult
                        {
                            Success = false,
                            IsForbidden = true
                        };
                    }
                    return new QuizPreviewResult
                    {
                        Success = false,
                        ErrorMessage = result.Message
                    };
                }

                return new QuizPreviewResult
                {
                    Success = true,
                    ViewModel = result.Data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetQuizPreviewAsync for quiz {QuizId}", quizId);
                return new QuizPreviewResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the quiz preview"
                };
            }
        }

        /// <summary>
        /// Handle Quiz Take GET request with user authentication and authorization
        /// </summary>
        public async Task<QuizTakeResult> GetQuizTakeAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                // Clean up abandoned attempts first
                await CleanupAbandonedAttemptsAsync();

                var userId = user.FindFirst("UserId")?.Value;
                var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    Console.WriteLine($"[TakeQuiz] User not authenticated. quizId={quizId}");
                    _logger.LogWarning($"[TakeQuiz] User not authenticated. quizId={quizId}");
                    return new QuizTakeResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Login",
                        RedirectController = "Auth"
                    };
                }

                // Get quiz with questions and course details
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions.OrderBy(qu => qu.QuestionOrder))
                        .ThenInclude(qu => qu.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .Include(q => q.QuizAttempts.Where(qa => qa.UserId == userId))
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    Console.WriteLine($"[TakeQuiz] Quiz not found. quizId={quizId}, userId={userId}");
                    _logger.LogWarning($"[TakeQuiz] Quiz not found. quizId={quizId}, userId={userId}");
                    return new QuizTakeResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Check if user is enrolled in the course (learners only)
                if (userRole == "learner")
                {
                    var isEnrolled = await _context.Enrollments
                        .AnyAsync(e => e.UserId == userId && e.CourseId == quiz.CourseId);

                    if (!isEnrolled)
                    {
                        Console.WriteLine($"[TakeQuiz] User not enrolled. quizId={quizId}, userId={userId}, courseId={quiz.CourseId}");
                        _logger.LogWarning($"[TakeQuiz] User not enrolled. quizId={quizId}, userId={userId}, courseId={quiz.CourseId}");
                        return new QuizTakeResult
                        {
                            Success = false,
                            ErrorMessage = "You must be enrolled in this course to take the quiz",
                            RedirectAction = "Details",
                            RedirectController = "Course",
                            RouteValues = new { id = quiz.CourseId }
                        };
                    }
                }

                // Check if quiz has questions
                if (!quiz.Questions.Any())
                {
                    Console.WriteLine($"[TakeQuiz] Quiz has no questions. quizId={quizId}, userId={userId}");
                    _logger.LogWarning($"[TakeQuiz] Quiz has no questions. quizId={quizId}, userId={userId}");
                    return new QuizTakeResult
                    {
                        Success = false,
                        ErrorMessage = "This quiz does not have any questions yet",
                        RedirectAction = "Details",
                        RedirectController = "Course",
                        RouteValues = new { id = quiz.CourseId }
                    };
                }

                // Check attempts - but don't create attempt yet, just check if user can attempt
                var userAttempts = quiz.QuizAttempts.Where(qa => qa.UserId == userId && qa.EndTime != null).ToList();
                var completedAttempts = userAttempts.Count;
                var maxAttempts = quiz.MaxAttempts ?? 3;

                // Check if user has an ongoing (unsubmitted) attempt
                var ongoingAttempt = quiz.QuizAttempts.FirstOrDefault(qa => qa.UserId == userId && qa.EndTime == null);

                Console.WriteLine($"[TakeQuiz] Attempt status. quizId={quizId}, userId={userId}, completedAttempts={completedAttempts}, maxAttempts={maxAttempts}, ongoingAttemptId={ongoingAttempt?.AttemptId}");
                _logger.LogInformation($"[TakeQuiz] Attempt status. quizId={quizId}, userId={userId}, completedAttempts={completedAttempts}, maxAttempts={maxAttempts}, ongoingAttemptId={ongoingAttempt?.AttemptId}");

                string attemptId;
                DateTime startTime;
                int currentAttemptNumber;
                bool isOngoingAttempt;

                if (ongoingAttempt != null)
                {
                    // User has an ongoing attempt, use it
                    attemptId = ongoingAttempt.AttemptId;
                    startTime = ongoingAttempt.StartTime ?? DateTime.UtcNow;
                    currentAttemptNumber = ongoingAttempt.AttemptNumber ?? 1;
                    isOngoingAttempt = true;
                    Console.WriteLine($"[TakeQuiz] Resuming ongoing attempt. attemptId={attemptId}, startTime={startTime}, currentAttemptNumber={currentAttemptNumber}");
                    _logger.LogInformation($"[TakeQuiz] Resuming ongoing attempt. attemptId={attemptId}, startTime={startTime}, currentAttemptNumber={currentAttemptNumber}");
                }
                else
                {
                    // Check if user can start a new attempt
                    var nextAttemptNumber = completedAttempts + 1;

                    if (nextAttemptNumber > maxAttempts)
                    {
                        Console.WriteLine($"[TakeQuiz] Exceeded max attempts. quizId={quizId}, userId={userId}, completedAttempts={completedAttempts}, maxAttempts={maxAttempts}");
                        _logger.LogWarning($"[TakeQuiz] Exceeded max attempts. quizId={quizId}, userId={userId}, completedAttempts={completedAttempts}, maxAttempts={maxAttempts}");
                        return new QuizTakeResult
                        {
                            Success = false,
                            ErrorMessage = "You have exceeded the maximum number of attempts for this quiz",
                            RedirectAction = "Details",
                            RedirectController = "Course",
                            RouteValues = new { id = quiz.CourseId }
                        };
                    }

                    // Create new attempt (but don't set EndTime yet, meaning not submitted)
                    attemptId = Guid.NewGuid().ToString();
                    startTime = DateTime.UtcNow;
                    currentAttemptNumber = nextAttemptNumber;
                    isOngoingAttempt = false;

                    var attempt = new QuizAttempt
                    {
                        AttemptId = attemptId,
                        UserId = userId,
                        QuizId = quizId,
                        StartTime = startTime,
                        AttemptNumber = currentAttemptNumber
                        // EndTime is null, meaning not submitted yet
                    };

                    _context.QuizAttempts.Add(attempt);
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"[TakeQuiz] Created new attempt. attemptId={attemptId}, startTime={startTime}, currentAttemptNumber={currentAttemptNumber}");
                    _logger.LogInformation($"[TakeQuiz] Created new attempt. attemptId={attemptId}, startTime={startTime}, currentAttemptNumber={currentAttemptNumber}");
                }

                // Build view model
                var viewModel = new QuizTakeViewModel
                {
                    QuizId = quiz.QuizId,
                    QuizName = quiz.QuizName,
                    QuizDescription = quiz.QuizDescription,
                    TimeLimit = quiz.TimeLimit,
                    PassingScore = quiz.PassingScore,
                    MaxAttempts = maxAttempts,
                    CurrentAttemptNumber = currentAttemptNumber,
                    RemainingAttempts = maxAttempts - completedAttempts - 1, // -1 for current ongoing attempt
                    CanAttempt = true,
                    CourseId = quiz.CourseId,
                    CourseName = quiz.Lesson?.Chapter?.Course?.CourseName,
                    LessonId = quiz.LessonId,
                    LessonTitle = quiz.Lesson?.LessonName,
                    StartTime = startTime,
                    AttemptId = attemptId,
                    IsPrerequisiteQuiz = quiz.IsPrerequisiteQuiz ?? false,
                    BlocksLessonCompletion = quiz.BlocksLessonCompletion ?? false,
                    IsOngoingAttempt = isOngoingAttempt,
                    Questions = quiz.Questions.Select(q => new QuizQuestionViewModel
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Points = q.Points,
                        QuestionOrder = q.QuestionOrder,
                        Explanation = q.Explanation,
                        AnswerOptions = q.AnswerOptions.Select(ao => new QuizAnswerOptionViewModel
                        {
                            OptionId = ao.OptionId,
                            OptionText = ao.OptionText,
                            OptionOrder = ao.OptionOrder,
                            IsSelected = false
                        }).ToList()
                    }).ToList()
                };

                Console.WriteLine($"[TakeQuiz] Returning quiz take view. quizId={quizId}, userId={userId}, attemptId={attemptId}, isOngoingAttempt={isOngoingAttempt}");
                _logger.LogInformation($"[TakeQuiz] Returning quiz take view. quizId={quizId}, userId={userId}, attemptId={attemptId}, isOngoingAttempt={isOngoingAttempt}");

                return new QuizTakeResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TakeQuiz] Exception occurred. quizId={quizId}, userId={user?.FindFirst("UserId")?.Value}, ex={ex.Message}");
                _logger.LogError(ex, $"[TakeQuiz] Exception occurred. quizId={quizId}, userId={user?.FindFirst("UserId")?.Value}");
                return new QuizTakeResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while preparing the quiz"
                };
            }
        }

        /// <summary>
        /// Handle Quiz Submit POST request
        /// </summary>
        public async Task<QuizSubmitResult> SubmitQuizAsync(ClaimsPrincipal user, QuizTakeSubmitViewModel model, ModelStateDictionary modelState)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return new QuizSubmitResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                if (!modelState.IsValid)
                {
                    return new QuizSubmitResult
                    {
                        Success = false,
                        ReturnView = true,
                        ErrorMessage = "Please check your answers and try again"
                    };
                }

                // Get attempt with quiz and questions
                var attempt = await _context.QuizAttempts
                    .Include(qa => qa.Quiz)
                        .ThenInclude(q => q.Questions)
                            .ThenInclude(qu => qu.AnswerOptions)
                    .FirstOrDefaultAsync(qa => qa.AttemptId == model.AttemptId && qa.UserId == userId);

                if (attempt == null)
                {
                    return new QuizSubmitResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Check if already submitted
                if (attempt.EndTime.HasValue)
                {
                    return new QuizSubmitResult
                    {
                        Success = false,
                        ErrorMessage = "This quiz attempt has already been submitted"
                    };
                }

                // Check time limit
                if (attempt.Quiz.TimeLimit.HasValue)
                {
                    var timeElapsed = (DateTime.UtcNow - attempt.StartTime!.Value).TotalMinutes;
                    if (timeElapsed > attempt.Quiz.TimeLimit.Value)
                    {
                        // Auto-submit with current answers
                        model.SubmissionTime = DateTime.UtcNow;
                    }
                }

                // Process answers
                decimal totalPoints = 0;
                decimal earnedPoints = 0;

                foreach (var question in attempt.Quiz.Questions)
                {
                    totalPoints += question.Points ?? 1;

                    var userAnswerSubmission = model.UserAnswers
                        .FirstOrDefault(ua => ua.QuestionId == question.QuestionId);

                    if (userAnswerSubmission != null)
                    {
                        var isCorrect = false;
                        decimal pointsEarned = 0;

                        if (question.QuestionType == "multiple_choice")
                        {
                            // Handle multiple choice with multiple correct answers
                            var selectedOptionIds = userAnswerSubmission.SelectedOptionIds ?? new List<string>();
                            var correctOptionIds = question.AnswerOptions
                                .Where(ao => ao.IsCorrect == true)
                                .Select(ao => ao.OptionId)
                                .ToList();

                            // Check if all correct answers are selected and no incorrect answers are selected
                            var allCorrectSelected = correctOptionIds.All(id => selectedOptionIds.Contains(id));
                            var noIncorrectSelected = selectedOptionIds.All(id => correctOptionIds.Contains(id));

                            if (allCorrectSelected && noIncorrectSelected && selectedOptionIds.Count > 0)
                            {
                                isCorrect = true;
                                pointsEarned = question.Points ?? 1;
                                earnedPoints += pointsEarned;
                            }
                        }
                        else if (question.QuestionType == "true_false" && !string.IsNullOrEmpty(userAnswerSubmission.SelectedOptionId))
                        {
                            var selectedOption = question.AnswerOptions
                                .FirstOrDefault(ao => ao.OptionId == userAnswerSubmission.SelectedOptionId);

                            if (selectedOption?.IsCorrect == true)
                            {
                                isCorrect = true;
                                pointsEarned = question.Points ?? 1;
                                earnedPoints += pointsEarned;
                            }
                        }
                        else if ((question.QuestionType == "essay" || question.QuestionType == "fill_blank") && !string.IsNullOrEmpty(userAnswerSubmission.AnswerText))
                        {
                            // For text questions, mark as correct if answered (manual grading can be implemented later)
                            isCorrect = true;
                            pointsEarned = question.Points ?? 1;
                            earnedPoints += pointsEarned;
                        }

                        // Save user answer
                        var userAnswer = new UserAnswer
                        {
                            UserId = userId,
                            QuestionId = question.QuestionId,
                            AttemptId = model.AttemptId,
                            SelectedOptionId = question.QuestionType == "multiple_choice" ? null : userAnswerSubmission.SelectedOptionId,
                            SelectedOptionIds = question.QuestionType == "multiple_choice" ? string.Join(",", userAnswerSubmission.SelectedOptionIds ?? new List<string>()) : null,
                            AnswerText = userAnswerSubmission.AnswerText,
                            IsCorrect = isCorrect,
                            PointsEarned = pointsEarned
                        };

                        _context.UserAnswers.Add(userAnswer);
                    }
                }

                // Calculate final score
                var percentageScore = totalPoints > 0 ? (earnedPoints / totalPoints) * 100 : 0;
                var passingScore = attempt.Quiz.PassingScore ?? 70;
                var isPassed = percentageScore >= passingScore;

                // Update attempt
                attempt.EndTime = model.SubmissionTime;
                attempt.Score = earnedPoints;
                attempt.TotalPoints = totalPoints;
                attempt.PercentageScore = percentageScore;
                attempt.IsPassed = isPassed;

                if (attempt.StartTime.HasValue)
                {
                    var timeSpentMinutes = (attempt.EndTime.Value - attempt.StartTime.Value).TotalMinutes;
                    attempt.TimeSpent = (int)Math.Round(timeSpentMinutes);
                }

                await _context.SaveChangesAsync();

                // Check and unlock quiz achievements
                try
                {
                    var unlockedAchievements = await _achievementUnlockService.CheckQuizAchievementsAsync(
                        userId, attempt.QuizId, percentageScore, isPassed);

                    if (unlockedAchievements.Any())
                    {
                        _logger.LogInformation("Unlocked {AchievementCount} quiz achievements for user {UserId} on quiz {QuizId}",
                            unlockedAchievements.Count, userId, attempt.QuizId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking quiz achievements for user {UserId}, quiz {QuizId}", userId, attempt.QuizId);
                }

                return new QuizSubmitResult
                {
                    Success = true,
                    AttemptId = attempt.AttemptId,
                    SuccessMessage = "Quiz submitted successfully!"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitQuizAsync for attempt {AttemptId}", model.AttemptId);
                return new QuizSubmitResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while submitting the quiz"
                };
            }
        }

        /// <summary>
        /// Handle Quiz Result GET request
        /// </summary>
        public async Task<QuizResultResult> GetQuizResultAsync(ClaimsPrincipal user, string attemptId)
        {
            try
            {
                var userId = user.FindFirst("UserId")?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    return new QuizResultResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated",
                        RedirectAction = "Login",
                        RedirectController = "Auth"
                    };
                }

                // Get attempt with quiz, questions, and user answers
                var attempt = await _context.QuizAttempts
                    .Include(qa => qa.Quiz)
                        .ThenInclude(q => q.Lesson)
                            .ThenInclude(l => l!.Chapter)
                                .ThenInclude(c => c!.Course)
                    .Include(qa => qa.Quiz)
                        .ThenInclude(q => q.Questions.OrderBy(qu => qu.QuestionOrder))
                            .ThenInclude(qu => qu.AnswerOptions.OrderBy(ao => ao.OptionOrder))
                    .Include(qa => qa.UserAnswers)
                    .FirstOrDefaultAsync(qa => qa.AttemptId == attemptId && qa.UserId == userId);

                if (attempt == null)
                {
                    return new QuizResultResult
                    {
                        Success = false,
                        IsNotFound = true
                    };
                }

                // Check if quiz is completed
                if (!attempt.EndTime.HasValue)
                {
                    return new QuizResultResult
                    {
                        Success = false,
                        ErrorMessage = "This quiz attempt has not been completed yet",
                        RedirectAction = "Take",
                        RedirectController = "Quiz",
                        RouteValues = new { id = attempt.QuizId }
                    };
                }

                // Check if user can retake
                var totalAttempts = await _context.QuizAttempts
                    .CountAsync(qa => qa.UserId == userId && qa.QuizId == attempt.QuizId);
                var maxAttempts = attempt.Quiz.MaxAttempts ?? 3;
                var canRetake = totalAttempts < maxAttempts && (attempt.IsPassed != true);

                // Build view model
                var viewModel = new QuizResultViewModel
                {
                    AttemptId = attempt.AttemptId,
                    QuizId = attempt.QuizId,
                    QuizName = attempt.Quiz.QuizName,
                    QuizDescription = attempt.Quiz.QuizDescription,
                    Score = attempt.Score,
                    TotalPoints = attempt.TotalPoints,
                    PercentageScore = attempt.PercentageScore,
                    PassingScore = attempt.Quiz.PassingScore,
                    IsPassed = attempt.IsPassed,
                    StartTime = attempt.StartTime,
                    EndTime = attempt.EndTime,
                    TimeSpent = attempt.TimeSpent,
                    AttemptNumber = attempt.AttemptNumber,
                    MaxAttempts = maxAttempts,
                    RemainingAttempts = maxAttempts - totalAttempts,
                    CourseId = attempt.Quiz.CourseId,
                    CourseName = attempt.Quiz.Lesson?.Chapter?.Course?.CourseName,
                    LessonId = attempt.Quiz.LessonId,
                    LessonTitle = attempt.Quiz.Lesson?.LessonName,
                    CanRetake = canRetake,
                    IsPrerequisiteQuiz = attempt.Quiz.IsPrerequisiteQuiz ?? false,
                    BlocksLessonCompletion = attempt.Quiz.BlocksLessonCompletion ?? false,
                    QuestionResults = attempt.Quiz.Questions.Select(q =>
                    {
                        var userAnswer = attempt.UserAnswers.FirstOrDefault(ua => ua.QuestionId == q.QuestionId);
                        var correctOptions = q.AnswerOptions.Where(ao => ao.IsCorrect == true).ToList();

                        // Parse selected option IDs for multiple choice
                        var selectedOptionIds = new List<string>();
                        if (q.QuestionType == "multiple_choice" && !string.IsNullOrEmpty(userAnswer?.SelectedOptionIds))
                        {
                            selectedOptionIds = userAnswer.SelectedOptionIds.Split(',').ToList();
                        }

                        return new QuestionResultViewModel
                        {
                            QuestionId = q.QuestionId,
                            QuestionText = q.QuestionText,
                            QuestionType = q.QuestionType,
                            Points = q.Points,
                            PointsEarned = userAnswer?.PointsEarned,
                            IsCorrect = userAnswer?.IsCorrect,
                            UserAnswer = userAnswer?.AnswerText ??
                                        (q.QuestionType == "multiple_choice" && selectedOptionIds.Any() ?
                                         string.Join(", ", q.AnswerOptions.Where(ao => selectedOptionIds.Contains(ao.OptionId)).Select(ao => ao.OptionText)) :
                                         (userAnswer?.SelectedOptionId != null ?
                                         q.AnswerOptions.FirstOrDefault(ao => ao.OptionId == userAnswer.SelectedOptionId)?.OptionText :
                                         null)),
                            CorrectAnswer = q.QuestionType == "multiple_choice" ?
                                           string.Join(", ", correctOptions.Select(ao => ao.OptionText)) :
                                           correctOptions.FirstOrDefault()?.OptionText,
                            Explanation = q.Explanation,
                            AnswerOptions = q.AnswerOptions.Select(ao => new AnswerOptionResultViewModel
                            {
                                OptionId = ao.OptionId,
                                OptionText = ao.OptionText,
                                IsCorrect = ao.IsCorrect ?? false,
                                IsSelected = q.QuestionType == "multiple_choice" ?
                                            selectedOptionIds.Contains(ao.OptionId) :
                                            userAnswer?.SelectedOptionId == ao.OptionId
                            }).ToList()
                        };
                    }).ToList()
                };

                return new QuizResultResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetQuizResultAsync for attempt {AttemptId}", attemptId);
                return new QuizResultResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading the quiz results"
                };
            }
        }

        /// <summary>
        /// Get quiz questions management view model with authorization and validation
        /// </summary>
        public async Task<GetQuizQuestionsResult> GetQuizQuestionsAsync(ClaimsPrincipal user, string quizId)
        {
            try
            {
                if (string.IsNullOrEmpty(quizId))
                {
                    return new GetQuizQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "Quiz ID is required"
                    };
                }

                // Get user ID from claims
                var userId = user.FindFirst("UserId")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return new GetQuizQuestionsResult
                    {
                        Success = false,
                        ErrorMessage = "User not authenticated"
                    };
                }

                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .Include(q => q.Questions)
                        .ThenInclude(q => q.AnswerOptions)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz?.Lesson?.Chapter?.Course == null)
                {
                    _logger.LogWarning("Quiz {QuizId} not found", quizId);
                    return new GetQuizQuestionsResult
                    {
                        Success = false,
                        IsNotFound = true,
                        ErrorMessage = "Quiz not found"
                    };
                }

                // Check authorization - user must be the course instructor
                if (quiz.Lesson.Chapter.Course.AuthorId != userId)
                {
                    _logger.LogWarning("User {UserId} not authorized to manage questions for quiz {QuizId}", userId, quizId);
                    return new GetQuizQuestionsResult
                    {
                        Success = false,
                        IsForbidden = true,
                        ErrorMessage = "You are not authorized to manage questions for this quiz"
                    };
                }

                var questions = quiz.Questions?
                    .OrderBy(q => q.QuestionOrder)
                    .Select(q => new QuestionSummaryViewModel
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType ?? "multiple_choice",
                        Points = (int)(q.Points ?? 1),
                        QuestionOrder = q.QuestionOrder ?? 1,
                        AnswerOptionsCount = q.AnswerOptions?.Count ?? 0,
                        QuestionCreatedAt = q.QuestionCreatedAt,
                        QuestionUpdatedAt = q.QuestionCreatedAt // Use CreatedAt since UpdatedAt doesn't exist
                    }).ToList() ?? new List<QuestionSummaryViewModel>();

                var viewModel = new QuestionListViewModel
                {
                    QuizId = quiz.QuizId,
                    QuizName = quiz.QuizName,
                    CourseId = quiz.Lesson.Chapter.Course.CourseId,
                    CourseName = quiz.Lesson.Chapter.Course.CourseName,
                    Questions = questions
                };

                return new GetQuizQuestionsResult
                {
                    Success = true,
                    ViewModel = viewModel
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz questions for quiz {QuizId}", quizId);
                return new GetQuizQuestionsResult
                {
                    Success = false,
                    ErrorMessage = "An error occurred while loading quiz questions"
                };
            }
        }

        /// <summary>
        /// Clean up abandoned quiz attempts that have been started but not submitted 
        /// for more than the quiz time limit plus a grace period
        /// </summary>
        public async Task CleanupAbandonedAttemptsAsync()
        {
            try
            {
                var now = DateTime.UtcNow;

                // Find all unsubmitted attempts
                var abandonedAttempts = await _context.QuizAttempts
                    .Include(qa => qa.Quiz)
                    .Where(qa => qa.EndTime == null) // Not submitted
                    .ToListAsync();

                var attemptsToCleanup = new List<QuizAttempt>();

                foreach (var attempt in abandonedAttempts)
                {
                    if (attempt.StartTime.HasValue)
                    {
                        var timeElapsed = (now - attempt.StartTime.Value).TotalMinutes;
                        var timeLimit = attempt.Quiz?.TimeLimit ?? 60; // Default 60 minutes if no time limit
                        var graceMinutes = 30; // 30 minutes grace period

                        // If elapsed time exceeds time limit + grace period, mark as abandoned
                        if (timeElapsed > (timeLimit + graceMinutes))
                        {
                            attemptsToCleanup.Add(attempt);
                        }
                    }
                }

                // Remove abandoned attempts
                if (attemptsToCleanup.Any())
                {
                    _context.QuizAttempts.RemoveRange(attemptsToCleanup);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Cleaned up {Count} abandoned quiz attempts", attemptsToCleanup.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up abandoned quiz attempts");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get quiz status information for display
        /// </summary>
        public static (string StatusText, string StatusClass, string StatusIcon) GetQuizStatusInfo(int? quizStatus)
        {
            return quizStatus switch
            {
                0 => ("Draft", "text-secondary", "fas fa-edit"),
                1 => ("Published", "text-success", "fas fa-check-circle"),
                2 => ("Active", "text-primary", "fas fa-play-circle"),
                3 => ("Inactive", "text-warning", "fas fa-pause-circle"),
                4 => ("Archived", "text-muted", "fas fa-archive"),
                5 => ("Suspended", "text-danger", "fas fa-ban"),
                6 => ("Completed", "text-info", "fas fa-flag-checkered"),
                7 => ("In Progress", "text-warning", "fas fa-clock"),
                _ => ("Unknown", "text-muted", "fas fa-question-circle")
            };
        }

        /// <summary>
        /// Check if quiz can be deleted based on its current state
        /// </summary>
        public static async Task<(bool CanDelete, string Reason)> CanDeleteQuizAsync(BrainStormEraContext context, string quizId)
        {
            var quiz = await context.Quizzes
                .Include(q => q.QuizAttempts)
                .FirstOrDefaultAsync(q => q.QuizId == quizId);

            if (quiz == null)
                return (false, "Quiz not found");

            // Check if quiz has active attempts (not completed)
            var activeAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime == null).Count() ?? 0;
            if (activeAttempts > 0)
                return (false, $"{activeAttempts} student(s) are currently taking this quiz");

            // Check if quiz has completed attempts
            var completedAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime != null).Count() ?? 0;
            if (completedAttempts > 0)
                return (false, $"{completedAttempts} student(s) have already taken this quiz");

            // Check quiz status - don't allow deletion of published/active quizzes
            if (quiz.QuizStatus == 1 || quiz.QuizStatus == 2) // Published or Active
                return (false, "Cannot delete a published/active quiz");

            // Check if quiz is a prerequisite for other content
            if (quiz.IsPrerequisiteQuiz == true)
                return (false, "This quiz is set as a prerequisite for course progression");

            // Check if quiz blocks lesson completion
            if (quiz.BlocksLessonCompletion == true)
                return (false, "This quiz blocks lesson completion");

            return (true, "Quiz can be deleted");
        }

        /// <summary>
        /// Update quiz status
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateQuizStatusAsync(string quizId, string userId, int newStatus)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return ServiceResult<bool>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<bool>.Failure("Unauthorized");
                }

                // Validate status
                if (newStatus < 0 || newStatus > 7)
                {
                    return ServiceResult<bool>.Failure("Invalid status value");
                }

                // Check if quiz has active attempts when trying to change to active status
                if (newStatus == 2) // Active
                {
                    var activeAttempts = await _context.QuizAttempts
                        .Where(qa => qa.QuizId == quizId && qa.EndTime == null)
                        .CountAsync();

                    if (activeAttempts > 0)
                    {
                        return ServiceResult<bool>.Failure($"Cannot activate quiz: {activeAttempts} student(s) are currently taking this quiz");
                    }
                }

                quiz.QuizStatus = newStatus;
                quiz.QuizUpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quiz status {QuizId} to {NewStatus}", quizId, newStatus);
                return ServiceResult<bool>.Failure("An error occurred while updating the quiz status");
            }
        }

        /// <summary>
        /// Get quiz statistics for instructor dashboard
        /// </summary>
        public async Task<ServiceResult<QuizStatisticsViewModel>> GetQuizStatisticsAsync(string quizId, string userId)
        {
            try
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.QuizAttempts)
                    .Include(q => q.Questions)
                    .Include(q => q.Lesson)
                        .ThenInclude(l => l!.Chapter)
                            .ThenInclude(c => c!.Course)
                    .FirstOrDefaultAsync(q => q.QuizId == quizId);

                if (quiz == null)
                {
                    return ServiceResult<QuizStatisticsViewModel>.Failure("Quiz not found");
                }

                // Check authorization
                if (quiz.Lesson?.Chapter?.Course?.AuthorId != userId)
                {
                    return ServiceResult<QuizStatisticsViewModel>.Failure("Unauthorized");
                }

                var totalAttempts = quiz.QuizAttempts?.Count ?? 0;
                var completedAttempts = quiz.QuizAttempts?.Where(qa => qa.EndTime != null).Count() ?? 0;
                var passedAttempts = quiz.QuizAttempts?.Where(qa => qa.IsPassed == true).Count() ?? 0;
                var averageScore = quiz.QuizAttempts?.Where(qa => qa.PercentageScore.HasValue).Average(qa => qa.PercentageScore) ?? 0;
                var totalQuestions = quiz.Questions?.Count ?? 0;

                var statistics = new QuizStatisticsViewModel
                {
                    QuizId = quiz.QuizId,
                    QuizName = quiz.QuizName,
                    TotalAttempts = totalAttempts,
                    CompletedAttempts = completedAttempts,
                    PassedAttempts = passedAttempts,
                    PassRate = completedAttempts > 0 ? (decimal)passedAttempts / completedAttempts * 100 : 0,
                    AverageScore = averageScore,
                    TotalQuestions = totalQuestions,
                    QuizStatus = quiz.QuizStatus,
                    IsPrerequisiteQuiz = quiz.IsPrerequisiteQuiz ?? false,
                    BlocksLessonCompletion = quiz.BlocksLessonCompletion ?? false
                };

                return ServiceResult<QuizStatisticsViewModel>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quiz statistics for quiz {QuizId}", quizId);
                return ServiceResult<QuizStatisticsViewModel>.Failure("An error occurred while loading quiz statistics");
            }
        }

        #endregion
    }

    #region Result Classes

    public class CreateQuizResult
    {
        public bool Success { get; set; }
        public CreateQuizViewModel? ViewModel { get; set; }
        public string? QuizId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public bool ReturnView { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class EditQuizResult
    {
        public bool Success { get; set; }
        public CreateQuizViewModel? ViewModel { get; set; }
        public string? QuizId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool ReturnView { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class DeleteQuizResult
    {
        public bool Success { get; set; }
        public string? CourseId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class QuizDetailsResult
    {
        public bool Success { get; set; }
        public QuizDetailsViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class QuizPreviewResult
    {
        public bool Success { get; set; }
        public QuizPreviewViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class QuizTakeResult
    {
        public bool Success { get; set; }
        public QuizTakeViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class QuizSubmitResult
    {
        public bool Success { get; set; }
        public string? AttemptId { get; set; }
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public bool ReturnView { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class QuizResultResult
    {
        public bool Success { get; set; }
        public QuizResultViewModel? ViewModel { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RedirectAction { get; set; }
        public string? RedirectController { get; set; }
        public object? RouteValues { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    public class GetQuizQuestionsResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public QuestionListViewModel? ViewModel { get; set; }
        public bool IsNotFound { get; set; }
        public bool IsForbidden { get; set; }
    }

    #endregion
}








