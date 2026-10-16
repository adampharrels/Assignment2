using JobTracker.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobTracker.Data;

/// <summary>
/// The EF Core database context (SQLite file). Services should not keep one instance alive in
/// Blazor Server; they take an IDbContextFactory and create a short-lived context per operation.
/// </summary>
/// <remarks>
/// The schema is created with EnsureCreated, not migrations. After pulling a change to any entity,
/// delete your local jobtracker.db file and run the app again.
/// TODO (K2, Sarah): add the application and stage-history DbSets here.
/// </remarks>
public class JobTrackerDbContext : DbContext
{
    /// <summary>Creates the context with options supplied by dependency injection or a test.</summary>
    public JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
        : base(options)
    {
    }

    /// <summary>Companies (K3, Grace).</summary>
    public DbSet<Company> Companies => Set<Company>();

    /// <summary>Interviews (K3, Grace).</summary>
    public DbSet<Interview> Interviews => Set<Interview>();

    /// <summary>Known skills and whether the user has them (K1, Adam).</summary>
    public DbSet<Skill> Skills => Set<Skill>();

    /// <summary>The user's profile; holds a single row (K1, Adam).</summary>
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    /// <summary>Configures indexes and stores enums as readable text.</summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Company>(entity =>
        {
            entity.HasIndex(company => company.Name).IsUnique();
            entity.Property(company => company.Size).HasConversion<string>();
        });

        modelBuilder.Entity<Interview>(entity =>
        {
            entity.HasIndex(interview => interview.ApplicationId);
            entity.Property(interview => interview.Type).HasConversion<string>();
            entity.Property(interview => interview.Outcome).HasConversion<string>();
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(skill => skill.Name).IsUnique();
            entity.Property(skill => skill.Category).HasConversion<string>();
        });
    }
}
