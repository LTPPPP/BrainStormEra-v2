using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class LessonType
{
    public int LessonTypeId { get; set; }

    public string LessonTypeName { get; set; } = null!;

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
