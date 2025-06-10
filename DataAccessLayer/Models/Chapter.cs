using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Chapter
{
    public string ChapterId { get; set; } = null!;

    public string CourseId { get; set; } = null!;

    public string ChapterName { get; set; } = null!;

    public string? ChapterDescription { get; set; }

    public int? ChapterOrder { get; set; }

    public int? ChapterStatus { get; set; }

    public bool? IsLocked { get; set; }

    public string? UnlockAfterChapterId { get; set; }

    public DateTime ChapterCreatedAt { get; set; }

    public DateTime ChapterUpdatedAt { get; set; }

    public virtual Status? ChapterStatusNavigation { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Chapter> InverseUnlockAfterChapter { get; set; } = new List<Chapter>();

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    public virtual Chapter? UnlockAfterChapter { get; set; }
}
