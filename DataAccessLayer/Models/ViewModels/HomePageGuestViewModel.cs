using System.Collections.Generic;

namespace DataAccessLayer.Models.ViewModels
{
    public class HomePageGuestViewModel
    {
        public List<CourseViewModel> RecommendedCourses { get; set; } = new List<CourseViewModel>();
        public List<string> CourseCategories { get; set; } = new List<string>();
        public List<CourseCategoryViewModel> Categories { get; set; } = new List<CourseCategoryViewModel>();

        // Additional properties can be added here for other sections of the homepage
    }
    public class CourseListViewModel
    {
        public List<CourseViewModel> Courses { get; set; } = new List<CourseViewModel>();
        public List<CourseCategoryViewModel> Categories { get; set; } = new List<CourseCategoryViewModel>();
        public string? SearchQuery { get; set; }
        public string? SelectedCategory { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalCourses { get; set; }
        public int PageSize { get; set; } = 12;

        // Additional properties for view compatibility
        public string? CurrentSearch => SearchQuery;
        public string? CurrentCategory => SelectedCategory;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
    public class CourseDetailViewModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public string CourseImage { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string AuthorId { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string AuthorImage { get; set; } = string.Empty;
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int TotalStudents { get; set; }
        public int EstimatedDuration { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
        public List<string> Categories { get; set; } = new List<string>();
        public List<ChapterViewModel> Chapters { get; set; } = new List<ChapterViewModel>();
        public List<ReviewViewModel> Reviews { get; set; } = new List<ReviewViewModel>();
        public bool IsEnrolled { get; set; }
        public bool CanEnroll { get; set; }
        public string? ApprovalStatus { get; set; }
        public DateTime CourseCreatedAt { get; set; }
        public DateTime CourseUpdatedAt { get; set; }

        // Additional properties for view compatibility
        public List<string> CourseCategories => Categories;
        public string Description => CourseDescription;
        public string CreatedBy => AuthorName;
        public string CoursePicture => CourseImage;
        public double StarRating => AverageRating;
        public int EnrollmentCount => TotalStudents;
        public DateTime CreatedDate => CourseCreatedAt;
    }

    public class ChapterViewModel
    {
        public string ChapterId { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public string ChapterDescription { get; set; } = string.Empty;
        public int ChapterOrder { get; set; }
        public List<LessonViewModel> Lessons { get; set; } = new List<LessonViewModel>();
        public List<QuizViewModel> Quizzes { get; set; } = new List<QuizViewModel>();
    }

    public class LessonViewModel
    {
        public string LessonId { get; set; } = string.Empty;
        public string LessonName { get; set; } = string.Empty;
        public string LessonDescription { get; set; } = string.Empty;
        public int LessonOrder { get; set; }
        public string LessonType { get; set; } = string.Empty;
        public int EstimatedDuration { get; set; }
        public bool IsLocked { get; set; }
    }

    public class QuizViewModel
    {
        public string QuizId { get; set; } = string.Empty;
        public string QuizName { get; set; } = string.Empty;
        public string QuizDescription { get; set; } = string.Empty;
        public string? LessonId { get; set; }
        public string? LessonName { get; set; }
        public int? TimeLimit { get; set; }
        public decimal? PassingScore { get; set; }
        public int? MaxAttempts { get; set; }
        public bool IsFinalQuiz { get; set; }
        public bool IsPrerequisiteQuiz { get; set; }
        public bool BlocksLessonCompletion { get; set; }
        public DateTime QuizCreatedAt { get; set; }
        public DateTime QuizUpdatedAt { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new List<QuestionViewModel>();
    }

    public class QuestionViewModel
    {
        public string QuestionId { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string? QuestionType { get; set; }
        public decimal? Points { get; set; }
        public int? QuestionOrder { get; set; }
        public string? Explanation { get; set; }
        public List<AnswerOptionViewModel> AnswerOptions { get; set; } = new List<AnswerOptionViewModel>();
    }

    public class AnswerOptionViewModel
    {
        public string OptionId { get; set; } = string.Empty;
        public string OptionText { get; set; } = string.Empty;
        public bool? IsCorrect { get; set; }
        public int? OptionOrder { get; set; }
    }

    public class ReviewViewModel
    {
        public string ReviewId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserImage { get; set; } = string.Empty;
        public int StarRating { get; set; }
        public string ReviewComment { get; set; } = string.Empty;
        public DateTime ReviewDate { get; set; }
        public bool IsVerifiedPurchase { get; set; }
        public string? UserId { get; set; }
    }

    public class CourseViewModel
    {
        public required string CourseId { get; set; }
        public required string CourseName { get; set; }
        public required string CoursePicture { get; set; }
        public string CourseImage => CoursePicture; // Alias for compatibility
        public decimal Price { get; set; }
        public required string CreatedBy { get; set; }
        public string AuthorName => CreatedBy; // Alias for compatibility
        public string? Description { get; set; }
        public int StarRating { get; set; }
        public int EnrollmentCount { get; set; }
        public List<string> CourseCategories { get; set; } = new List<string>();
        public string? ApprovalStatus { get; set; }
        public int? CourseStatus { get; set; }
        public string? AuthorId { get; set; }
    }
    public class CourseCategoryViewModel
    {
        public required string CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public string? CategoryDescription { get; set; }
        public string? CategoryIcon { get; set; }
        public int CourseCount { get; set; }

        // Additional properties for view compatibility
        public string CourseCategoryName => CategoryName;
        public string CourseCategoryId => CategoryId;
    }
}
