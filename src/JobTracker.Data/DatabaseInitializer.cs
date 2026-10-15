using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data;

/// <summary>Creates the database on first run and adds the starting profile and skill list.</summary>
public static class DatabaseInitializer
{
    /// <summary>Creates the schema if needed, then seeds the profile and skills when they are empty.</summary>
    public static async Task InitializeAsync(IDbContextFactory<JobTrackerDbContext> contextFactory)
    {
        await using JobTrackerDbContext context = await contextFactory.CreateDbContextAsync();
        await context.Database.EnsureCreatedAsync();

        if (!await context.UserProfiles.AnyAsync())
        {
            context.UserProfiles.Add(new UserProfile
            {
                FullName = "Demo Graduate",
                TargetRole = "Graduate Software Engineer",
                ResumeText = "Computer science graduate. Built full-stack web apps with C#, .NET, ASP.NET Core "
                    + "and React using JavaScript and TypeScript. Data projects in Python and SQL. "
                    + "Comfortable with Git, unit testing and Agile teamwork. Strong communication skills."
            });
        }

        if (!await context.Skills.AnyAsync())
        {
            context.Skills.AddRange(CreateStartingSkills());
        }

        await context.SaveChangesAsync();
    }

    /// <summary>A starting skill list; the first group is what the demo user already knows.</summary>
    private static List<Skill> CreateStartingSkills()
    {
        var skills = new List<Skill>
        {
            new Skill { Name = "c#", Category = SkillCategory.Language, InMyProfile = true },
            new Skill { Name = ".net", Category = SkillCategory.Framework, InMyProfile = true },
            new Skill { Name = "asp.net core", Category = SkillCategory.Framework, InMyProfile = true },
            new Skill { Name = "react", Category = SkillCategory.Framework, InMyProfile = true },
            new Skill { Name = "javascript", Category = SkillCategory.Language, InMyProfile = true },
            new Skill { Name = "typescript", Category = SkillCategory.Language, InMyProfile = true },
            new Skill { Name = "python", Category = SkillCategory.Language, InMyProfile = true },
            new Skill { Name = "sql", Category = SkillCategory.Data, InMyProfile = true },
            new Skill { Name = "git", Category = SkillCategory.Practice, InMyProfile = true },
            new Skill { Name = "agile", Category = SkillCategory.Practice, InMyProfile = true },
            new Skill { Name = "unit testing", Category = SkillCategory.Practice, InMyProfile = true },
            new Skill { Name = "communication", Category = SkillCategory.Soft, InMyProfile = true },

            new Skill { Name = "java", Category = SkillCategory.Language },
            new Skill { Name = "aws", Category = SkillCategory.Cloud },
            new Skill { Name = "azure", Category = SkillCategory.Cloud },
            new Skill { Name = "docker", Category = SkillCategory.Cloud },
            new Skill { Name = "kubernetes", Category = SkillCategory.Cloud },
            new Skill { Name = "node.js", Category = SkillCategory.Framework },
            new Skill { Name = "power bi", Category = SkillCategory.Data },
            new Skill { Name = "machine learning", Category = SkillCategory.Data },
            new Skill { Name = "statistics", Category = SkillCategory.Data },
            new Skill { Name = "linux", Category = SkillCategory.Practice }
        };
        return skills;
    }
}
