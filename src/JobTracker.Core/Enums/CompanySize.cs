namespace JobTracker.Core.Enums;

/// <summary>Company size bands used by company forms and analytics.</summary>
public enum CompanySize
{
    /// <summary>The company size is not known.</summary>
    Unknown = 0,
    /// <summary>Between 1 and 49 employees.</summary>
    Small = 1,
    /// <summary>Between 50 and 249 employees.</summary>
    Medium = 2,
    /// <summary>250 or more employees.</summary>
    Large = 3
}
