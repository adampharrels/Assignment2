using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data.Services;

/// <summary>Stores the user's profile and skill list in the database.</summary>
public class ProfileService : IProfileService
{
    private readonly IDbContextFactory<JobTrackerDbContext> _contextFactory;

    /// <summary>Creates the service. A new database context is created for every call.</summary>
    public ProfileService(IDbContextFactory<JobTrackerDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public async Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        UserProfile? profile = await context.UserProfiles.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        return profile ?? new UserProfile();
    }

    /// <inheritdoc />
    public async Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default)
    {
        Validator.ValidateObject(profile, new ValidationContext(profile), validateAllProperties: true);

        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        UserProfile? existing = await context.UserProfiles.OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (existing == null)
        {
            context.UserProfiles.Add(profile);
        }
        else
        {
            existing.FullName = profile.FullName;
            existing.TargetRole = profile.TargetRole;
            existing.ResumeText = profile.ResumeText;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Skills.AsNoTracking().OrderBy(skill => skill.Name).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetProfileSkillNamesAsync(CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Skills
            .Where(skill => skill.InMyProfile)
            .OrderBy(skill => skill.Name)
            .Select(skill => skill.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetInProfileAsync(int skillId, bool inProfile, CancellationToken cancellationToken = default)
    {
        await using JobTrackerDbContext context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        Skill? skill = await context.Skills.FindAsync(new object[] { skillId }, cancellationToken);
        if (skill == null)
        {
            throw new KeyNotFoundException($"Skill {skillId} does not exist.");
        }

        skill.InMyProfile = inProfile;
        await context.SaveChangesAsync(cancellationToken);
    }
}
