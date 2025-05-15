using System;
using System.Collections.Generic;

namespace BrainStormEra.Models;

public partial class Comment
{
    public string CommentId { get; set; } = null!;

    public string? UserId { get; set; }

    public string? LessonId { get; set; }

    public string? CommentText { get; set; }

    public DateTime? CommentCreatedAt { get; set; }

    public virtual Lesson? Lesson { get; set; }

    public virtual Account? User { get; set; }
}
