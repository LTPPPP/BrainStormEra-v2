namespace BrainStormEra_MVC.Models.ViewModels
{
    /// <summary>
    /// ViewModel for displaying certificate summary in the certificates list
    /// </summary>
    public class CertificateSummaryViewModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseImage { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public DateTime CompletedDate { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public decimal FinalScore { get; set; }

        /// <summary>
        /// Formatted completion duration for display
        /// </summary>
        public string CompletionDuration
        {
            get
            {
                var duration = (CompletedDate - EnrollmentDate).TotalDays;
                return duration < 1 ? "Less than 1 day" : $"{Math.Round(duration)} days";
            }
        }

        /// <summary>
        /// Progress percentage as integer for display
        /// </summary>
        public int ProgressPercentage => (int)Math.Round(FinalScore);
    }

    /// <summary>
    /// ViewModel for displaying detailed certificate information
    /// </summary>
    public class CertificateDetailsViewModel
    {
        public string CourseId { get; set; } = string.Empty;
        public string CourseName { get; set; } = string.Empty;
        public string CourseDescription { get; set; } = string.Empty;
        public string CourseImage { get; set; } = string.Empty;
        public string LearnerName { get; set; } = string.Empty;
        public string LearnerEmail { get; set; } = string.Empty;
        public string InstructorName { get; set; } = string.Empty;
        public DateTime CompletedDate { get; set; }
        public DateTime EnrollmentDate { get; set; }
        public int CompletionDurationDays { get; set; }
        public decimal FinalScore { get; set; }
        public string CertificateCode { get; set; } = string.Empty;

        /// <summary>
        /// Progress percentage as integer for display
        /// </summary>
        public int ProgressPercentage => (int)Math.Round(FinalScore);

        /// <summary>
        /// Formatted completion date for certificate display
        /// </summary>
        public string FormattedCompletedDate => CompletedDate.ToString("MMMM dd, yyyy");

        /// <summary>
        /// Formatted enrollment date for certificate display
        /// </summary>
        public string FormattedEnrollmentDate => EnrollmentDate.ToString("MMMM dd, yyyy");
    }
}
