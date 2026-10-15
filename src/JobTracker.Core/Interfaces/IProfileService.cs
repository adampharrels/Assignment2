using JobTracker.Core.Entities;

namespace JobTracker.Core.Interfaces;

/// <summary>The user's profile and skill list. Used by the profile page (G6) and the skill-gap analysis.</summary>
/// <remarks>Invalid input throws ValidationException; unknown IDs throw KeyNotFoundException.</remarks>
public interface IProfileService
{
    /// <summary>Returns the stored profile, or a new empty one when none has been saved.</summary>
    Task<UserProfile> GetProfileAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates and saves the profile, creating it on first save.</summary>
    Task SaveProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Returns every known skill ordered by name.</summary>
    Task<IReadOnlyList<Skill>> GetSkillsAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns the lower-case names of the skills the user has.</summary>
    Task<IReadOnlyList<string>> GetProfileSkillNamesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks a skill as one the user has, or removes it from their profile.</summary>
    Task SetInProfileAsync(int skillId, bool inProfile, CancellationToken cancellationToken = default);
}
