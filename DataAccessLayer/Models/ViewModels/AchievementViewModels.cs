using System.ComponentModel.DataAnnotations;

namespace DataAccessLayer.Models.ViewModels
{
    /// <summary>
    /// ViewModel for displaying achievement summary in the achievements list
    /// </summary>
    public class AchievementSummaryViewModel
    {
        public string AchievementId { get; set; } = string.Empty;
        public string AchievementName { get; set; } = string.Empty;
        public string AchievementDescription { get; set; } = string.Empty;
        public string AchievementIcon { get; set; } = string.Empty;
        public string AchievementType { get; set; } = string.Empty;
        public int? PointsReward { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? RelatedCourseName { get; set; }

        /// <summary>
        /// Formatted received date for display
        /// </summary>
        public string FormattedReceivedDate => ReceivedDate.ToString("MMM dd, yyyy");

        /// <summary>
        /// Achievement description with context
        /// </summary>
        public string FormattedDescription =>
            string.IsNullOrEmpty(AchievementDescription) ? "Achievement unlocked!"
            : $"Award for completed {AchievementDescription} courses";
    }

    /// <summary>
    /// ViewModel for paginated achievements list
    /// </summary>
    public class AchievementListViewModel
    {
        public List<AchievementSummaryViewModel> Achievements { get; set; } = new List<AchievementSummaryViewModel>();
        public string? SearchQuery { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int TotalAchievements { get; set; }
        public int PageSize { get; set; } = 9;

        /// <summary>
        /// Whether there are achievements to display
        /// </summary>
        public bool HasAchievements => Achievements.Any();

        /// <summary>
        /// Whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => CurrentPage > 1;

        /// <summary>
        /// Whether there is a next page
        /// </summary>
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    /// <summary>
    /// ViewModel for achievement details
    /// </summary>
    public class AchievementDetailsViewModel
    {
        public string AchievementId { get; set; } = string.Empty;
        public string AchievementName { get; set; } = string.Empty;
        public string AchievementDescription { get; set; } = string.Empty;
        public string AchievementIcon { get; set; } = string.Empty;
        public string AchievementType { get; set; } = string.Empty;
        public int? PointsReward { get; set; }
        public DateTime ReceivedDate { get; set; }
        public string? RelatedCourseName { get; set; }
        public int? PointsEarned { get; set; }

        /// <summary>
        /// Formatted received date for display
        /// </summary>
        public string FormattedReceivedDate => ReceivedDate.ToString("MMM dd, yyyy");
    }
}
