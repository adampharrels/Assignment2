using System.ComponentModel.DataAnnotations;

namespace JobTracker.Core.Entities;

/// <summary>The single user's own details. The resume text is compared with job descriptions for fit scoring.</summary>
public class UserProfile
{
    /// <summary>The database-generated identifier. The app only ever stores one profile.</summary>
    public int Id { get; set; }

    /// <summary>The user's name, shown on the profile page.</summary>
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    /// <summary>The kind of role the user is looking for, e.g. "Graduate Software Engineer".</summary>
    [StringLength(100)]
    public string? TargetRole { get; set; }

    /// <summary>Plain-text resume, used to compute how well the user fits each job description.</summary>
    public string ResumeText { get; set; } = string.Empty;
}
