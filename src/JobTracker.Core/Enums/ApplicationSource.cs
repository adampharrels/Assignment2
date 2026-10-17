namespace JobTracker.Core.Enums;

/// <summary>Where the user found the job. Shown as display text by ApplicationEnumExtensions.</summary>
public enum ApplicationSource
{
    /// <summary>Anywhere not listed below.</summary>
    Other = 0,
    /// <summary>A LinkedIn job ad.</summary>
    LinkedIn = 1,
    /// <summary>A Seek job ad.</summary>
    Seek = 2,
    /// <summary>A GradConnection listing.</summary>
    GradConnection = 3,
    /// <summary>Someone inside the company referred the user.</summary>
    Referral = 4,
    /// <summary>Met the company at a career fair.</summary>
    CareerFair = 5,
    /// <summary>The careers page on the company's own website.</summary>
    CompanyWebsite = 6
}
