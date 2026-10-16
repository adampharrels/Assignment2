using JobTracker.Core.Analytics;
using JobTracker.Core.Interfaces;

namespace JobTracker.Analytics.Demo;

/// <summary>
/// Temporary stand-in for the real application data while the application entity (K2) does not exist.
/// Generates 72 realistic applications. A fixed random seed makes the data identical on every run,
/// so the demo and the charts are repeatable.
/// </summary>
/// <remarks>
/// The data has real patterns built in (referrals, tailored resumes, skill match and applying early
/// all raise the interview chance) so the statistics and models have something to find.
/// Remove the registration in Program.cs once the application service implements IApplicationDataSource.
/// </remarks>
public class DemoApplicationDataSource : IApplicationDataSource
{
    private const int ApplicationCount = 72;
    private const int DefaultSeed = 2046;
    private static readonly DateTime FirstApplicationDate = new DateTime(2026, 7, 1);

    private static readonly RoleTemplate[] Roles =
    {
        new RoleTemplate("Graduate Frontend Engineer", 95_000, new[] { "React", "TypeScript", "JavaScript", "CSS", "HTML" }, new[] { "Jest", "Git", "GraphQL", "accessibility" }, new[] { "react", "typescript", "javascript" }),
        new RoleTemplate("Graduate Software Engineer (.NET)", 88_000, new[] { "C#", ".NET", "ASP.NET Core", "SQL Server" }, new[] { "Azure", "React", "unit testing", "Git" }, new[] { "c#", ".net", "sql" }),
        new RoleTemplate("Graduate Backend Engineer", 98_000, new[] { "Java", "Spring Boot", "SQL", "AWS" }, new[] { "Docker", "k8s", "microservices", "CI/CD" }, new[] { "sql" }),
        new RoleTemplate("Graduate Data Analyst", 80_000, new[] { "SQL", "Python", "Power BI", "Excel" }, new[] { "Tableau", "statistics", "stakeholder management" }, new[] { "sql", "python" }),
        new RoleTemplate("Graduate Data Scientist", 102_000, new[] { "Python", "machine learning", "statistics", "SQL" }, new[] { "pandas", "scikit-learn", "A/B testing", "Airflow" }, new[] { "python", "sql" }),
        new RoleTemplate("Junior Full Stack Developer", 78_000, new[] { "React", "Node.js", "TypeScript", "PostgreSQL" }, new[] { "Docker", "REST APIs", "Git" }, new[] { "react", "typescript" }),
        new RoleTemplate("Graduate Cloud Engineer", 92_000, new[] { "AWS", "Terraform", "Linux", "Docker" }, new[] { "Kubernetes", "Python", "CI/CD" }, new string[0]),
        new RoleTemplate("Graduate Cyber Security Analyst", 76_000, new[] { "networking", "Linux", "Python", "SIEM" }, new[] { "incident response", "Splunk" }, new[] { "python" }),
        new RoleTemplate("Graduate Technology Consultant", 75_000, new[] { "SQL", "Excel", "Agile", "communication" }, new[] { "Azure", "Power BI", "stakeholder management" }, new[] { "sql" }),
        new RoleTemplate("Graduate Machine Learning Engineer", 100_000, new[] { "Python", "PyTorch", "machine learning", "Docker" }, new[] { "NLP", "AWS", "MLOps" }, new[] { "python" })
    };

    private static readonly string[] Companies =
    {
        "Atlassian", "Canva", "Commonwealth Bank", "Westpac", "SafetyCulture", "Deloitte", "Accenture",
        "Telstra", "Harbour Analytics", "Xero", "REA Group", "Macquarie Group", "Airwallex", "Culture Amp", "NAB"
    };

    private static readonly string[] Locations = { "Sydney", "Sydney", "Sydney", "Melbourne", "Melbourne", "Brisbane", "Canberra", "Remote" };

    private static readonly string[] Sources = { "LinkedIn", "LinkedIn", "Seek", "Seek", "GradConnection", "Company website", "Referral", "Referral", "Career fair" };

    private static readonly string[] Openers =
    {
        "Join our graduate program and build products used by millions of customers.",
        "We are looking for a curious graduate who loves solving problems.",
        "Our team ships features every week and values learning and collaboration.",
        "This role suits a recent graduate who wants real ownership from day one."
    };

    private static readonly string[] Closers =
    {
        "Strong communication and teamwork are essential.",
        "You will be mentored by senior engineers and rotate across teams.",
        "We offer flexible working, learning budgets and a supportive culture.",
        "Applications close soon, so apply early."
    };

    private IReadOnlyList<ApplicationRecord>? _cache;

    /// <inheritdoc />
    public Task<IReadOnlyList<ApplicationRecord>> GetApplicationsAsync(CancellationToken cancellationToken = default)
    {
        if (_cache == null)
        {
            _cache = Generate();
        }

        return Task.FromResult(_cache);
    }

    /// <summary>Builds the full demo list. Public so tests can check it directly.</summary>
    /// <param name="seed">Random seed; the default gives the data used in the demo.</param>
    public static IReadOnlyList<ApplicationRecord> Generate(int seed = DefaultSeed)
    {
        var random = new Random(seed);
        var records = new List<ApplicationRecord>();

        for (int i = 0; i < ApplicationCount; i++)
        {
            RoleTemplate role = Roles[random.Next(Roles.Length)];
            string source = Sources[random.Next(Sources.Length)];
            string location = Locations[random.Next(Locations.Length)];
            DateTime applied = FirstApplicationDate.AddDays(i * 1.5).Date;
            int daysAfterPosting = random.Next(0, 10);
            decimal salaryMin = role.BaseSalary + random.Next(-6, 7) * 1_000;
            bool salaryStated = random.NextDouble() < 0.85;

            var record = new ApplicationRecord
            {
                Id = i + 1,
                CompanyName = Companies[random.Next(Companies.Length)],
                RoleTitle = role.Title,
                Location = location,
                WorkMode = location == "Remote" ? "Remote" : (random.NextDouble() < 0.7 ? "Hybrid" : "On-site"),
                Source = source,
                SalaryMin = salaryStated ? salaryMin : null,
                SalaryMax = salaryStated ? salaryMin + 10_000 : null,
                DatePosted = applied.AddDays(-daysAfterPosting),
                DateApplied = applied,
                HadReferral = source == "Referral",
                TailoredResume = random.NextDouble() < 0.5,
                WroteCoverLetter = random.NextDouble() < 0.4
            };
            record.JobDescription = WriteJobDescription(role, record.CompanyName, random);

            // Chance of an interview, on the log-odds scale, from the patterns described in the remarks.
            double logOdds = -2.2;
            logOdds += record.HadReferral ? 2.0 : 0.0;
            logOdds += record.TailoredResume ? 1.0 : 0.0;
            logOdds += record.WroteCoverLetter ? 0.3 : 0.0;
            logOdds += 0.7 * (role.SkillsUserHas.Length - 1);
            logOdds += daysAfterPosting <= 3 ? 1.0 : 0.0;
            logOdds += source == "Seek" ? -0.6 : 0.0;
            bool gotInterview = random.NextDouble() < 1.0 / (1.0 + Math.Exp(-logOdds));

            DecideOutcome(record, gotInterview, random);
            records.Add(record);
        }

        return records;
    }

    /// <summary>Sets how far the application got, how it ended and when the first reply came.</summary>
    private static void DecideOutcome(ApplicationRecord record, bool gotInterview, Random random)
    {
        if (gotInterview)
        {
            record.DateFirstResponse = record.DateApplied.AddDays(random.Next(3, 15));
            record.HighestStage = PipelineStage.Interview;
            if (random.NextDouble() < 0.45)
            {
                record.HighestStage = PipelineStage.FinalRound;
                if (random.NextDouble() < 0.5)
                {
                    record.HighestStage = PipelineStage.Offer;
                }
            }

            if (record.HighestStage == PipelineStage.Offer)
            {
                record.Outcome = random.NextDouble() < 0.5 ? ApplicationOutcome.Accepted : ApplicationOutcome.InProgress;
            }
            else
            {
                record.Outcome = random.NextDouble() < 0.65 ? ApplicationOutcome.Rejected : ApplicationOutcome.InProgress;
            }

            return;
        }

        double roll = random.NextDouble();
        if (roll < 0.5)
        {
            // The company replied, possibly after an online assessment, then rejected.
            record.DateFirstResponse = record.DateApplied.AddDays(random.Next(5, 28));
            record.HighestStage = random.NextDouble() < 0.35 ? PipelineStage.Assessment : PipelineStage.Applied;
            record.Outcome = ApplicationOutcome.Rejected;
        }
        else if (roll < 0.85)
        {
            record.Outcome = ApplicationOutcome.Ghosted;
        }
        else
        {
            record.Outcome = random.NextDouble() < 0.5 ? ApplicationOutcome.InProgress : ApplicationOutcome.Withdrawn;
        }
    }

    /// <summary>Writes a short job ad mentioning the role's core skills and some extras.</summary>
    private static string WriteJobDescription(RoleTemplate role, string company, Random random)
    {
        string opener = Openers[random.Next(Openers.Length)];
        string closer = Closers[random.Next(Closers.Length)];
        string extras = string.Join(", ", role.ExtraSkills.Where(skill => random.NextDouble() < 0.7));
        if (extras.Length == 0)
        {
            extras = role.ExtraSkills[0];
        }

        return $"{company} is hiring a {role.Title}. {opener} "
            + $"You will work with {string.Join(", ", role.CoreSkills)} every day. "
            + $"Experience with {role.CoreSkills[0]} is essential. "
            + $"Nice to have: {extras}. {closer}";
    }

    /// <summary>A kind of role the generator can produce.</summary>
    private sealed class RoleTemplate
    {
        public RoleTemplate(string title, decimal baseSalary, string[] coreSkills, string[] extraSkills, string[] skillsUserHas)
        {
            Title = title;
            BaseSalary = baseSalary;
            CoreSkills = coreSkills;
            ExtraSkills = extraSkills;
            SkillsUserHas = skillsUserHas;
        }

        public string Title { get; }
        public decimal BaseSalary { get; }
        public string[] CoreSkills { get; }
        public string[] ExtraSkills { get; }
        /// <summary>Core skills of this role that the demo user has; more overlap means more interviews.</summary>
        public string[] SkillsUserHas { get; }
    }
}
