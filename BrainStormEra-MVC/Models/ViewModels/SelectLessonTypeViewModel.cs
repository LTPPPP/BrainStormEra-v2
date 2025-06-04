using System.ComponentModel.DataAnnotations;

namespace BrainStormEra_MVC.Models.ViewModels
{
    public class SelectLessonTypeViewModel
    {
        [Required]
        public string ChapterId { get; set; } = null!;

        [Required]
        public string CourseId { get; set; } = null!;

        [Required(ErrorMessage = "Please select a lesson type")]
        public int? SelectedLessonTypeId { get; set; }

        // Additional properties for the view
        public string CourseName { get; set; } = null!;
        public string ChapterName { get; set; } = null!;
        public int ChapterOrder { get; set; }
        public IEnumerable<LessonType> LessonTypes { get; set; } = new List<LessonType>();
    }
}
