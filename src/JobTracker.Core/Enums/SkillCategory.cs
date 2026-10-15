namespace JobTracker.Core.Enums;

/// <summary>Broad groups of skills, used to colour and filter skill charts.</summary>
public enum SkillCategory
{
    /// <summary>A skill that does not fit another category.</summary>
    Other = 0,
    /// <summary>A programming language, e.g. C# or Python.</summary>
    Language = 1,
    /// <summary>A framework or library, e.g. React or ASP.NET Core.</summary>
    Framework = 2,
    /// <summary>A database or data tool, e.g. SQL or Power BI.</summary>
    Data = 3,
    /// <summary>A cloud platform or DevOps tool, e.g. AWS or Docker.</summary>
    Cloud = 4,
    /// <summary>A development tool or practice, e.g. Git or Agile.</summary>
    Practice = 5,
    /// <summary>A soft skill, e.g. communication.</summary>
    Soft = 6
}
