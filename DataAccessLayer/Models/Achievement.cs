using System;
using System.Collections.Generic;

namespace DataAccessLayer.Models;

public partial class Achievement
{
    public string AchievementId { get; set; } = null!;

    public string AchievementName { get; set; } = null!;

    public string? AchievementDescription { get; set; }

    public string? AchievementIcon { get; set; }

    public string? AchievementType { get; set; }

    public int? PointsReward { get; set; }

    public DateTime AchievementCreatedAt { get; set; }

    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}
