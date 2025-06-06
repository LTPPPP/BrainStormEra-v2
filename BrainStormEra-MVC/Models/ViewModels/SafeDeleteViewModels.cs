using BrainStormEra_MVC.Models;

namespace BrainStormEra_MVC.Models.ViewModels
{
    /// <summary>
    /// View model for displaying deleted items with pagination
    /// </summary>
    public class AdminDeletedItemsViewModel
    {
        public IEnumerable<DeletedItemViewModel> DeletedItems { get; set; } = new List<DeletedItemViewModel>();
        public string SearchQuery { get; set; } = string.Empty;
        public string EntityType { get; set; } = "All"; // All, Course, Chapter, Lesson
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; } = 0;
        public int PageSize { get; set; } = 10;

        // Pagination properties
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public bool HasItems => DeletedItems.Any();
    }

    /// <summary>
    /// View model for individual deleted item
    /// </summary>
    public class DeletedItemViewModel
    {
        public string EntityId { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string EntityName { get; set; } = string.Empty;
        public DateTime? DeletedDate { get; set; }
        public string DeletedByUserId { get; set; } = string.Empty;
        public string DeletedByUserName { get; set; } = string.Empty;
        public string DeleteReason { get; set; } = string.Empty;
        public bool CanRestore { get; set; } = true;

        // Additional properties for display
        public string FormattedDeletedDate => DeletedDate?.ToString("MMM dd, yyyy HH:mm") ?? "Unknown";
        public string EntityTypeIcon => EntityType switch
        {
            "Course" => "fas fa-graduation-cap",
            "Chapter" => "fas fa-book-open",
            "Lesson" => "fas fa-play-circle",
            _ => "fas fa-question-circle"
        };
        public string EntityTypeBadgeClass => EntityType switch
        {
            "Course" => "badge-primary",
            "Chapter" => "badge-info",
            "Lesson" => "badge-success",
            _ => "badge-secondary"
        };
    }
}
