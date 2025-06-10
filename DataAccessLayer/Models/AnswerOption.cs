using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class AnswerOption
{
    public string OptionId { get; set; } = null!;

    public string QuestionId { get; set; } = null!;

    public string OptionText { get; set; } = null!;

    public bool? IsCorrect { get; set; }

    public int? OptionOrder { get; set; }

    public virtual Question Question { get; set; } = null!;

    public virtual ICollection<UserAnswer> UserAnswers { get; set; } = new List<UserAnswer>();
}
