using BrainStormEra_MVC.Models;
using BrainStormEra_MVC.Models.ViewModels;
using BrainStormEra_MVC.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BrainStormEra_MVC.Services.Implementations
{
    public class QuizServiceImpl : IQuizService
    {
        private readonly BrainStormEraContext _context;
        private readonly ILessonService _lessonService;
        private readonly ILogger<QuizServiceImpl> _logger;

        public QuizServiceImpl(BrainStormEraContext context, ILessonService lessonService, ILogger<QuizServiceImpl> logger)
        {
            _context = context;
            _lessonService = lessonService;
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
                var lessonId = quiz.LessonId ?? "";
                _context.Quizzes.Remove(quiz);
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
    }
}