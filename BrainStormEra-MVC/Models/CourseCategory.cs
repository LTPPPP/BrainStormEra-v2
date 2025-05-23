using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class CourseCategory
{
    public string CourseCategoryId { get; set; } = null!;

    public string CourseCategoryName { get; set; } = null!;

    public string? CategoryDescription { get; set; }

    public string? CategoryIcon { get; set; }

    public bool? IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdateAt { get; set; }

    public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
}
