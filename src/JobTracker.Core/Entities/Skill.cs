using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Enums;

namespace JobTracker.Core.Entities;

/// <summary>A skill the user can mark as one they have. Drives the skill-gap analysis.</summary>
public class Skill
{
    /// <summary>The database-generated identifier.</summary>
    public int Id { get; set; }

    /// <summary>The canonical lower-case skill name, e.g. "react" or "c#". Unique.</summary>
    [Required]
    [StringLength(60)]
    public string Name { get; set; } = string.Empty;

    /// <summary>The broad group the skill belongs to.</summary>
    [EnumDataType(typeof(SkillCategory))]
    public SkillCategory Category { get; set; } = SkillCategory.Other;

    /// <summary>True when the user has this skill.</summary>
    public bool InMyProfile { get; set; }
}
