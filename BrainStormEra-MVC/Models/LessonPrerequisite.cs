using System;
using System.Collections.Generic;

namespace BrainStormEra_MVC.Models;

public partial class LessonPrerequisite
{
    public string LessonId { get; set; } = null!;

    public string PrerequisiteLessonId { get; set; } = null!;

    public string? PrerequisiteType { get; set; }

    public decimal? RequiredScore { get; set; }

    public int? RequiredTime { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Lesson PrerequisiteLesson { get; set; } = null!;
}
