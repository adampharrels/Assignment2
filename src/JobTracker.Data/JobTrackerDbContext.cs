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
/// </remarks>
public class JobTrackerDbContext : DbContext
{
    /// <summary>Creates the context with options supplied by dependency injection or a test.</summary>
    public JobTrackerDbContext(DbContextOptions<JobTrackerDbContext> options)
        : base(options)
    {
    }

    /// <summary>Job applications (K2, Sarah).</summary>
    public DbSet<JobApplication> Applications => Set<JobApplication>();

    /// <summary>Every status change of every application (K2, Sarah).</summary>
    public DbSet<StageEvent> StageEvents => Set<StageEvent>();

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

        // K2 (Sarah). Interviews are not linked by a foreign key yet: the seed data adds interviews
        // before applications are saved. The application service deletes them itself (see DeleteAsync).
        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.HasOne(application => application.Company)
                .WithMany()
                .HasForeignKey(application => application.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(application => application.StageHistory)
                .WithOne(stageEvent => stageEvent.Application)
                .HasForeignKey(stageEvent => stageEvent.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(application => application.Status);
            entity.Property(application => application.Status).HasConversion<string>();
            entity.Property(application => application.Source).HasConversion<string>();
            entity.Property(application => application.WorkMode).HasConversion<string>();
        });

        modelBuilder.Entity<StageEvent>(entity =>
        {
            entity.HasIndex(stageEvent => new { stageEvent.ApplicationId, stageEvent.ChangedAt });
            entity.Property(stageEvent => stageEvent.FromStatus).HasConversion<string>();
            entity.Property(stageEvent => stageEvent.ToStatus).HasConversion<string>();
        });

        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasIndex(skill => skill.Name).IsUnique();
            entity.Property(skill => skill.Category).HasConversion<string>();
        });
    }
}
