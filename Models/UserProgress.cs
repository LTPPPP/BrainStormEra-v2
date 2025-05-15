using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class UserProgress
{
    public string UserId { get; set; } = null!;

    public string LessonId { get; set; } = null!;

    public bool? IsCompleted { get; set; }

    public DateTime? CompletedAt { get; set; }

    public virtual Lesson Lesson { get; set; } = null!;

    public virtual Account User { get; set; } = null!;
}
