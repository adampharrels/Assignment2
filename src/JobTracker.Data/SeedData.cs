using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;
using JobTracker.Data.Seed;
using Microsoft.EntityFrameworkCore;
using S = JobTracker.Data.Seed.SeedStatus;

namespace JobTracker.Data;

/// <summary>
/// G1 demo data: 15 companies, 56 submitted applications plus 3 wishlist items, and 22 interviews.
/// Every value is written out by hand with fixed dates between June and October 2026, so the data
/// is the same on every run and never depends on today's date.
/// </summary>
/// <remarks>
/// Patterns are built in so the analytics have something to find: referrals and tailored resumes
/// usually lead to interviews, roles that match the demo user's skills (.NET, React, SQL, Python)
/// do better than cloud, security and Java roles, and Seek applications rarely hear back.
/// The applications are saved with their original Ids, because the seeded interviews refer to them by Id.
/// </remarks>
public static class SeedData
{
    private const int Year = 2026;
    private const string OnSite = "On-site";
    private const string Hybrid = "Hybrid";
    private const string Remote = "Remote";

    private static readonly Role Frontend = new Role("Graduate Frontend Engineer",
        "We are looking for a Graduate Frontend Engineer to build customer-facing web apps in React and TypeScript. "
        + "You will write clean JavaScript, HTML and CSS, test your work with Jest and review code through Git pull requests. "
        + "Care for accessibility is a big plus.");

    private static readonly Role DotNet = new Role("Graduate Software Engineer (.NET)",
        "Join our platform team as a Graduate Software Engineer working on C# and .NET services. "
        + "You will build ASP.NET Core REST APIs on SQL Server with Entity Framework, deploy to Azure "
        + "and practise unit testing in an Agile squad.");

    private static readonly Role Backend = new Role("Graduate Backend Engineer",
        "As a Graduate Backend Engineer you will design Java microservices with Spring Boot, store data in PostgreSQL "
        + "and run them on AWS. Exposure to Docker, Kubernetes and CI/CD pipelines is highly regarded.");

    private static readonly Role DataAnalyst = new Role("Graduate Data Analyst",
        "Our insights team needs a Graduate Data Analyst who can write SQL, clean data with Python and Excel, "
        + "and build Power BI dashboards. Strong communication and stakeholder management skills are essential.");

    private static readonly Role DataScientist = new Role("Graduate Data Scientist",
        "As a Graduate Data Scientist you will build machine learning models in Python with pandas and scikit-learn, "
        + "run A/B testing on product changes and explain statistics to non-technical teams. SQL is a must; Spark is a bonus.");

    private static readonly Role FullStack = new Role("Junior Full Stack Developer",
        "We need a Junior Full Stack Developer to work across a React front end and a Node.js back end. "
        + "You will use TypeScript, PostgreSQL and REST APIs, ship with Docker and GitHub Actions, and work in a small Agile team.");

    private static readonly Role Cloud = new Role("Graduate Cloud Engineer",
        "As a Graduate Cloud Engineer you will automate infrastructure on AWS with Terraform. You should be comfortable "
        + "on Linux, scripting in Bash or Python, running Docker and Kubernetes, and improving our CI/CD.");

    private static readonly Role Security = new Role("Graduate Cyber Security Analyst",
        "Join our security operations centre as a Graduate Cyber Security Analyst. You will triage alerts in Splunk "
        + "and other SIEM tools, help with incident response and need a solid grasp of networking, Linux and Python scripting.");

    private static readonly Role Consultant = new Role("Graduate Technology Consultant",
        "As a Graduate Technology Consultant you will help clients with digital transformation, analyse data in SQL and Excel, "
        + "present findings in Power BI and work in Agile delivery teams. Excellent communication and problem solving are key.");

    private static readonly Role MachineLearning = new Role("Graduate Machine Learning Engineer",
        "As a Graduate Machine Learning Engineer you will train and deploy deep learning models with Python and PyTorch, "
        + "work on NLP features, run models on AWS with Docker and help build our MLOps pipeline.");

    /// <summary>The opening line of each company's job ads.</summary>
    private static readonly Dictionary<string, string> CompanyPitches = new Dictionary<string, string>
    {
        ["Atlassian"] = "Atlassian makes software that helps teams around the world plan and ship their work.",
        ["Canva"] = "Canva is on a mission to empower everyone in the world to design anything.",
        ["Commonwealth Bank"] = "Commonwealth Bank is building the next generation of digital banking for millions of Australians.",
        ["NAB"] = "NAB's technology graduates help modernise the systems behind everyday banking.",
        ["SafetyCulture"] = "SafetyCulture builds the platform frontline workers use to run inspections and stay safe at work.",
        ["Deloitte"] = "Deloitte's technology consulting practice helps government and enterprise clients deliver change.",
        ["Accenture"] = "Accenture partners with clients across industries to design and deliver technology projects.",
        ["Telstra"] = "Telstra connects millions of Australians and is investing heavily in its digital platforms.",
        ["Xero"] = "Xero builds beautiful accounting software for small businesses and their advisors.",
        ["REA Group"] = "REA Group runs Australia's leading property websites.",
        ["Macquarie Group"] = "Macquarie Group is a global financial services group with technology at the core of its business.",
        ["Airwallex"] = "Airwallex is building global payments infrastructure for modern businesses.",
        ["Harbour Analytics"] = "Harbour Analytics is a small Sydney consultancy that turns client data into decisions.",
        ["Bluegum Labs"] = "Bluegum Labs is a seed-stage start-up building scheduling software for clinics.",
        ["Wattle Systems"] = "Wattle Systems delivers IT projects for federal government agencies in Canberra."
    };

    /// <summary>Extra lines for some job ads, by application Id, so ads for the same role differ.</summary>
    private static readonly Dictionary<int, string> AdExtras = new Dictionary<int, string>
    {
        [1] = "You will join the Jira Cloud team in Sydney.",
        [6] = "Our internal tools are written in Blazor.",
        [8] = "Kotlin experience is a plus.",
        [11] = "Experience with Figma and design systems is valued.",
        [16] = "The team works mostly in Databricks with Spark.",
        [20] = "You will work on the services behind Confluence.",
        [25] = "You will build payment APIs; Golang is a bonus.",
        [28] = "Clients range from retailers to local councils.",
        [33] = "Applicants must hold full Australian working rights.",
        [36] = "The role sits in the product analytics team and also uses Tableau.",
        [40] = "You will analyse data from millions of inspections.",
        [43] = "Our design system is built with Next.js.",
        [47] = "Experience with TensorFlow is nice to have.",
        [49] = "You will help move legacy systems to .NET 8 on Azure.",
        [53] = "The role is in the team behind the NAB app.",
        [55] = "You will work on trading platform services written in C# and .NET."
    };

    /// <summary>Personal notes on some applications, by application Id.</summary>
    private static readonly Dictionary<int, string> ApplicationNotes = new Dictionary<int, string>
    {
        [2] = "2027 graduate intake.",
        [11] = "Met the team at the UTS career fair.",
        [15] = "Withdrew: wanted a role with more mentoring.",
        [19] = "Spoke to the recruiter at the career fair.",
        [22] = "They replied a month late.",
        [25] = "Declined the offer to wait for other final rounds.",
        [34] = "Withdrew: the role needs a security clearance.",
        [40] = "Declined after accepting the Commonwealth Bank offer.",
        [44] = "Accepted! Starts February 2027.",
        [55] = "Referred by a friend from uni."
    };

    /// <summary>Ways the user went the extra mile on an application.</summary>
    [Flags]
    private enum Tactics
    {
        None = 0,
        Referral = 1,
        Tailored = 2,
        CoverLetter = 4
    }

    /// <summary>Adds the demo companies and interviews to the context when their tables are empty. The caller saves.</summary>
    /// <exception cref="ArgumentNullException">The context is null.</exception>
    public static async Task AddDemoDataAsync(JobTrackerDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!await context.Companies.AnyAsync(cancellationToken))
        {
            context.Companies.AddRange(CreateCompanies());
        }

        if (!await context.Interviews.AnyAsync(cancellationToken))
        {
            context.Interviews.AddRange(CreateInterviews());
        }
    }

    /// <summary>Returns new instances of the 15 demo companies, ordered by name.</summary>
    public static IReadOnlyList<Company> CreateCompanies()
    {
        var companies = new List<Company>
        {
            new Company { Name = "Accenture", Industry = "Consulting", Size = CompanySize.Large, Website = "https://www.accenture.com" },
            new Company { Name = "Airwallex", Industry = "Fintech", Size = CompanySize.Large, Website = "https://www.airwallex.com" },
            new Company { Name = "Atlassian", Industry = "Software", Size = CompanySize.Large, Website = "https://www.atlassian.com", Notes = "Dream company. Grad applications open in July." },
            new Company { Name = "Bluegum Labs", Industry = "Health tech start-up", Size = CompanySize.Small, Notes = "Friend works here; small team of 12." },
            new Company { Name = "Canva", Industry = "Software", Size = CompanySize.Large, Website = "https://www.canva.com" },
            new Company { Name = "Commonwealth Bank", Industry = "Banking", Size = CompanySize.Large, Website = "https://www.commbank.com.au" },
            new Company { Name = "Deloitte", Industry = "Consulting", Size = CompanySize.Large, Website = "https://www.deloitte.com" },
            new Company { Name = "Harbour Analytics", Industry = "Data consulting", Size = CompanySize.Small },
            new Company { Name = "Macquarie Group", Industry = "Financial services", Size = CompanySize.Large, Website = "https://www.macquarie.com" },
            new Company { Name = "NAB", Industry = "Banking", Size = CompanySize.Large, Website = "https://www.nab.com.au" },
            new Company { Name = "REA Group", Industry = "Property technology", Size = CompanySize.Large, Website = "https://www.rea-group.com" },
            new Company { Name = "SafetyCulture", Industry = "Software", Size = CompanySize.Large, Website = "https://www.safetyculture.com" },
            new Company { Name = "Telstra", Industry = "Telecommunications", Size = CompanySize.Large, Website = "https://www.telstra.com.au" },
            new Company { Name = "Wattle Systems", Industry = "Government IT services", Size = CompanySize.Medium, Notes = "Most roles need a security clearance." },
            new Company { Name = "Xero", Industry = "Software", Size = CompanySize.Large, Website = "https://www.xero.com" }
        };
        return companies;
    }

    /// <summary>
    /// Adds the demo applications, with their stage history, when the applications table is empty.
    /// Call it after AddDemoDataAsync, in the same context; the caller saves.
    /// </summary>
    /// <remarks>
    /// Kept separate from AddDemoDataAsync because tests create applications of their own and count them,
    /// so only the app (and the "Reset demo data" button) turns the demo applications on.
    /// </remarks>
    /// <exception cref="ArgumentNullException">The context is null.</exception>
    public static async Task AddDemoApplicationsAsync(JobTrackerDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.Applications.AnyAsync(cancellationToken))
        {
            return;
        }

        // Companies added by AddDemoDataAsync have no Id until the caller saves, so load the saved ones
        // next to them and link each application through its Company navigation instead.
        await context.Companies.LoadAsync(cancellationToken);
        var companiesByName = context.Companies.Local.ToDictionary(company => company.Name, StringComparer.OrdinalIgnoreCase);

        foreach (SeedApplication seed in CreateApplications())
        {
            // A demo company the user deleted is added again.
            if (!companiesByName.TryGetValue(seed.CompanyName, out Company? company))
            {
                company = new Company { Name = seed.CompanyName };
                companiesByName.Add(company.Name, company);
            }

            context.Applications.Add(ToEntity(seed, company));
        }
    }

    /// <summary>
    /// Turns one seed application into the stored entity with its stage history. The Id is kept because
    /// the seeded interviews refer to it.
    /// </summary>
    /// <exception cref="InvalidOperationException">The seed uses a source or work mode the entity has no value for.</exception>
    public static JobApplication ToEntity(SeedApplication seed, Company company)
    {
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentNullException.ThrowIfNull(company);

        if (!ApplicationEnumExtensions.TryParseSource(seed.Source, out ApplicationSource source))
        {
            throw new InvalidOperationException($"Seed application {seed.Id} has an unknown source \"{seed.Source}\".");
        }

        if (!ApplicationEnumExtensions.TryParseWorkMode(seed.WorkMode, out WorkMode workMode))
        {
            throw new InvalidOperationException($"Seed application {seed.Id} has an unknown work mode \"{seed.WorkMode}\".");
        }

        return new JobApplication
        {
            Id = seed.Id,
            Company = company,
            RoleTitle = seed.RoleTitle,
            Location = seed.Location,
            WorkMode = workMode,
            Source = source,
            // SeedStatus mirrors ApplicationStatus value for value, so the cast is safe.
            Status = (ApplicationStatus)seed.Status,
            SalaryMin = seed.SalaryMin,
            SalaryMax = seed.SalaryMax,
            JobDescription = seed.JobDescription,
            DatePosted = seed.DatePosted,
            DateApplied = seed.DateApplied,
            HadReferral = seed.HadReferral,
            TailoredResume = seed.TailoredResume,
            WroteCoverLetter = seed.WroteCoverLetter,
            Notes = seed.Notes,
            StageHistory = seed.History
                .Select(stageEvent => new StageEvent
                {
                    FromStatus = (ApplicationStatus?)stageEvent.FromStatus,
                    ToStatus = (ApplicationStatus)stageEvent.ToStatus,
                    ChangedAt = stageEvent.ChangedAt
                })
                .ToList()
        };
    }

    /// <summary>Returns the 56 submitted applications (Ids 1-56) and 3 wishlist items (Ids 57-59).</summary>
    public static IReadOnlyList<SeedApplication> CreateApplications()
    {
        var applications = new List<SeedApplication>
        {
            // July: mostly big-company graduate programs.
            Applied(1, "Atlassian", Frontend, "Sydney", Hybrid, "LinkedIn", 95, 105, D(6, 29), D(7, 1), Tactics.Tailored, (S.Assessment, 5), (S.Interview, 12), (S.FinalRound, 20), (S.Rejected, 29)),
            Applied(2, "Commonwealth Bank", DotNet, "Sydney", Hybrid, "GradConnection", 85, 92, D(6, 18), D(7, 2), Tactics.None, (S.Rejected, 18)),
            Applied(3, "Telstra", Cloud, "Melbourne", OnSite, "Seek", 82, 90, D(6, 24), D(7, 3), Tactics.None, (S.Ghosted, 42)),
            Applied(4, "Canva", Frontend, "Sydney", Hybrid, "Referral", 100, 112, D(7, 4), D(7, 6), Tactics.Referral | Tactics.Tailored | Tactics.CoverLetter, (S.Interview, 7), (S.FinalRound, 16), (S.Rejected, 25)),
            Applied(5, "Deloitte", Consultant, "Sydney", OnSite, "GradConnection", 72, 78, D(6, 30), D(7, 7), Tactics.CoverLetter, (S.Assessment, 4), (S.Rejected, 15)),
            Applied(6, "Xero", DotNet, "Melbourne", Hybrid, "LinkedIn", 88, 96, D(7, 7), D(7, 8), Tactics.Tailored, (S.Assessment, 3), (S.Interview, 10), (S.Rejected, 19)),
            Applied(7, "NAB", DataAnalyst, "Melbourne", Hybrid, "Seek", null, null, D(7, 1), D(7, 10), Tactics.None, (S.Ghosted, 40)),
            Applied(8, "Macquarie Group", Backend, "Sydney", OnSite, "LinkedIn", 95, 105, D(7, 2), D(7, 11), Tactics.None, (S.Assessment, 6), (S.Rejected, 14)),
            Applied(9, "Harbour Analytics", DataAnalyst, "Sydney", Hybrid, "Company website", 70, 78, D(7, 12), D(7, 13), Tactics.Tailored | Tactics.CoverLetter, (S.Interview, 5), (S.Rejected, 16)),
            Applied(10, "Airwallex", FullStack, "Melbourne", Hybrid, "LinkedIn", 80, 90, D(7, 8), D(7, 14), Tactics.None, (S.Ghosted, 38)),
            Applied(11, "SafetyCulture", Frontend, "Sydney", Hybrid, "Career fair", 92, 100, D(7, 15), D(7, 15), Tactics.Tailored, (S.Interview, 9), (S.FinalRound, 18), (S.Rejected, 24)),
            Applied(12, "Accenture", Consultant, "Brisbane", OnSite, "GradConnection", 70, 76, D(7, 3), D(7, 17), Tactics.None, (S.Rejected, 21)),
            Applied(13, "REA Group", FullStack, "Melbourne", Hybrid, "Seek", 85, 95, D(7, 11), D(7, 20), Tactics.None, (S.Ghosted, 45)),
            Applied(14, "Telstra", Security, "Melbourne", OnSite, "GradConnection", 74, 80, D(7, 14), D(7, 21), Tactics.None, (S.Assessment, 7), (S.Interview, 12), (S.Rejected, 20)),
            Applied(15, "Bluegum Labs", DotNet, Remote, Remote, "Referral", 80, 88, D(7, 21), D(7, 22), Tactics.Referral, (S.Interview, 4), (S.Withdrawn, 11)),
            Applied(16, "Commonwealth Bank", DataScientist, "Sydney", Hybrid, "LinkedIn", 100, 110, D(7, 15), D(7, 24), Tactics.Tailored, (S.Assessment, 5), (S.Rejected, 17)),
            Applied(17, "Canva", DataScientist, "Sydney", Hybrid, "Company website", null, null, D(7, 26), D(7, 27), Tactics.Tailored | Tactics.CoverLetter, (S.Ghosted, 41)),
            Applied(18, "Wattle Systems", DotNet, "Canberra", OnSite, "Seek", 78, 85, D(7, 20), D(7, 28), Tactics.None, (S.Rejected, 12)),
            Applied(19, "Macquarie Group", DataAnalyst, "Sydney", OnSite, "Career fair", 82, 88, D(7, 29), D(7, 30), Tactics.Tailored, (S.Interview, 8), (S.Rejected, 15)),

            // August.
            Applied(20, "Atlassian", Backend, "Sydney", Hybrid, "LinkedIn", 98, 108, D(7, 28), D(8, 3), Tactics.None, (S.Ghosted, 40)),
            Applied(21, "Deloitte", DataAnalyst, "Melbourne", Hybrid, "Referral", 75, 82, D(8, 3), D(8, 4), Tactics.Referral | Tactics.Tailored, (S.Assessment, 3), (S.Interview, 9), (S.FinalRound, 17), (S.Rejected, 26)),
            Applied(22, "Xero", Frontend, Remote, Remote, "LinkedIn", 90, 98, D(8, 4), D(8, 5), Tactics.Tailored | Tactics.CoverLetter, (S.Ghosted, 28), (S.Interview, 36), (S.Rejected, 44)),
            Applied(23, "NAB", DotNet, "Melbourne", Hybrid, "GradConnection", 84, 92, D(7, 31), D(8, 7), Tactics.None, (S.Assessment, 6), (S.Interview, 14), (S.Rejected, 22)),
            Applied(24, "Telstra", DataAnalyst, "Brisbane", Hybrid, "Seek", 72, 80, D(8, 1), D(8, 10), Tactics.None, (S.Ghosted, 39)),
            Applied(25, "Airwallex", DotNet, "Sydney", Hybrid, "Referral", 95, 105, D(8, 10), D(8, 11), Tactics.Referral | Tactics.Tailored | Tactics.CoverLetter, (S.Interview, 5), (S.FinalRound, 13), (S.Offer, 22), (S.Withdrawn, 27)),
            Applied(26, "Accenture", Cloud, "Sydney", OnSite, "LinkedIn", 75, 82, D(8, 2), D(8, 12), Tactics.None, (S.Rejected, 9)),
            Applied(27, "SafetyCulture", FullStack, "Sydney", Hybrid, "Seek", 85, 92, D(8, 6), D(8, 14), Tactics.None, (S.Ghosted, 35)),
            Applied(28, "Harbour Analytics", DataScientist, "Sydney", Hybrid, "LinkedIn", 90, 98, D(8, 15), D(8, 17), Tactics.Tailored, (S.Assessment, 4), (S.Interview, 11), (S.Rejected, 18)),
            Applied(29, "Macquarie Group", MachineLearning, "Sydney", Hybrid, "GradConnection", 100, 110, D(8, 10), D(8, 18), Tactics.None, (S.Assessment, 5), (S.Rejected, 13)),
            Applied(30, "REA Group", Frontend, "Melbourne", Hybrid, "Referral", 92, 100, D(8, 18), D(8, 19), Tactics.Referral | Tactics.Tailored, (S.Interview, 6), (S.FinalRound, 14), (S.Rejected, 22)),
            Applied(31, "Bluegum Labs", FullStack, Remote, Remote, "Company website", 70, 80, D(8, 20), D(8, 21), Tactics.CoverLetter, (S.Interview, 6), (S.Rejected, 14)),
            Applied(32, "Commonwealth Bank", Security, "Sydney", OnSite, "Seek", 76, 84, D(8, 12), D(8, 24), Tactics.None, (S.Ghosted, 36)),
            Applied(33, "Canva", Cloud, "Sydney", Hybrid, "LinkedIn", 95, 105, D(8, 22), D(8, 25), Tactics.None, (S.Assessment, 5), (S.Rejected, 12)),
            Applied(34, "Wattle Systems", Consultant, "Canberra", Hybrid, "GradConnection", 68, 75, D(8, 20), D(8, 27), Tactics.Tailored, (S.Interview, 7), (S.Withdrawn, 12)),
            Applied(35, "Deloitte", Security, "Sydney", Hybrid, "Career fair", 74, 80, D(8, 28), D(8, 28), Tactics.None, (S.Assessment, 6), (S.Rejected, 17)),

            // September: the busiest month.
            Applied(36, "Atlassian", DataAnalyst, Remote, Remote, "LinkedIn", 88, 96, D(8, 31), D(9, 1), Tactics.Tailored, (S.Assessment, 4), (S.Interview, 10), (S.FinalRound, 19), (S.Rejected, 27)),
            Applied(37, "Xero", DataAnalyst, "Melbourne", Hybrid, "Seek", null, null, D(8, 24), D(9, 2), Tactics.None, (S.Rejected, 15)),
            Applied(38, "Telstra", FullStack, "Sydney", Hybrid, "LinkedIn", 80, 88, D(9, 3), D(9, 4), Tactics.Tailored, (S.Interview, 8)),
            Applied(39, "NAB", DataScientist, "Melbourne", Hybrid, "GradConnection", 95, 102, D(8, 30), D(9, 7), Tactics.None, (S.Ghosted, 35)),
            Applied(40, "SafetyCulture", DataAnalyst, "Sydney", Hybrid, "Referral", 80, 88, D(9, 7), D(9, 8), Tactics.Referral | Tactics.CoverLetter, (S.Interview, 5), (S.FinalRound, 12), (S.Offer, 21), (S.Withdrawn, 35)),
            Applied(41, "Accenture", DotNet, "Melbourne", Hybrid, "LinkedIn", 78, 85, D(9, 4), D(9, 9), Tactics.Tailored, (S.Assessment, 4), (S.Rejected, 13)),
            Applied(42, "Macquarie Group", Cloud, "Sydney", OnSite, "Seek", 90, 98, D(8, 29), D(9, 10), Tactics.None, (S.Rejected, 8)),
            Applied(43, "Airwallex", Frontend, "Sydney", Hybrid, "LinkedIn", 95, 105, D(9, 10), D(9, 11), Tactics.Tailored | Tactics.CoverLetter, (S.Assessment, 3), (S.Interview, 9), (S.FinalRound, 18)),
            Applied(44, "Commonwealth Bank", DotNet, "Sydney", Hybrid, "Referral", 88, 95, D(9, 12), D(9, 14), Tactics.Referral | Tactics.Tailored | Tactics.CoverLetter, (S.Interview, 6), (S.FinalRound, 13), (S.Offer, 22), (S.Accepted, 29)),
            Applied(45, "Harbour Analytics", Consultant, "Sydney", Hybrid, "Company website", 65, 72, D(9, 15), D(9, 15), Tactics.None, (S.Interview, 7), (S.Rejected, 14)),
            Applied(46, "Deloitte", FullStack, "Brisbane", Hybrid, "GradConnection", 74, 80, D(9, 8), D(9, 16), Tactics.None, (S.Assessment, 5), (S.Rejected, 19)),
            Applied(47, "REA Group", DataScientist, "Melbourne", Hybrid, "LinkedIn", 95, 105, D(9, 11), D(9, 18), Tactics.None),
            Applied(48, "Bluegum Labs", Cloud, Remote, Remote, "Seek", 72, 80, D(9, 14), D(9, 21), Tactics.None, (S.Rejected, 10)),
            Applied(49, "Telstra", DotNet, "Melbourne", Hybrid, "Career fair", 80, 88, D(9, 23), D(9, 23), Tactics.Tailored, (S.Interview, 8)),
            Applied(50, "Xero", Security, Remote, Remote, "Seek", null, null, D(9, 15), D(9, 25), Tactics.None),
            Applied(51, "Atlassian", MachineLearning, "Sydney", Hybrid, "LinkedIn", 105, 115, D(9, 25), D(9, 28), Tactics.Tailored, (S.Assessment, 6)),
            Applied(52, "Wattle Systems", DataAnalyst, "Canberra", Hybrid, "Seek", 70, 76, D(9, 22), D(9, 30), Tactics.None, (S.Rejected, 9)),

            // October: still waiting to hear back on most of these.
            Applied(53, "NAB", Frontend, "Melbourne", Hybrid, "LinkedIn", 90, 98, D(10, 1), D(10, 2), Tactics.Tailored | Tactics.CoverLetter, (S.Interview, 7)),
            Applied(54, "Canva", MachineLearning, "Sydney", Hybrid, "GradConnection", 105, 115, D(9, 29), D(10, 5), Tactics.None),
            Applied(55, "Macquarie Group", DotNet, "Sydney", Hybrid, "Referral", 92, 100, D(10, 6), D(10, 7), Tactics.Referral, (S.Assessment, 3)),
            Applied(56, "Accenture", DataAnalyst, "Perth", OnSite, "Seek", 68, 75, D(9, 30), D(10, 9), Tactics.None),

            // Wishlist: saved but never submitted, so the analytics must ignore them.
            Wishlist(57, "Airwallex", DataScientist, "Sydney", Hybrid, "LinkedIn", 100, 110, D(10, 8), D(10, 10)),
            Wishlist(58, "Xero", DotNet, Remote, Remote, "Company website", 88, 96, D(10, 11), D(10, 12)),
            Wishlist(59, "Telstra", DataScientist, "Melbourne", Hybrid, "Seek", null, null, D(10, 9), D(10, 11))
        };
        return applications;
    }

    /// <summary>
    /// Returns new instances of the 22 demo interviews, ordered by start time. Times are UTC
    /// (10am Sydney is 00:00 UTC in winter and 23:00 UTC the day before in summer time).
    /// The last three are still Pending and fall after mid-October 2026.
    /// </summary>
    public static IReadOnlyList<Interview> CreateInterviews()
    {
        var interviews = new List<Interview>
        {
            Iv(4, InterviewType.Behavioural, Utc(7, 15, 3), 45, "Google Meet", "Tom W.", InterviewOutcome.Passed, "Have STAR stories ready for teamwork and conflict."),
            Iv(1, InterviewType.Technical, Utc(7, 16, 0), 60, "Zoom", "Priya N.", InterviewOutcome.Passed, "Practise React hooks and talk through the take-home task."),
            Iv(6, InterviewType.Technical, Utc(7, 21, 4), 90, "Xero Melbourne office", "Lena K.", InterviewOutcome.Failed, "Revise C# async/await and SQL joins."),
            Iv(9, InterviewType.Behavioural, Utc(7, 22, 2), 60, "Microsoft Teams", "Sam O.", InterviewOutcome.Failed, null),
            Iv(1, InterviewType.FinalRound, Utc(7, 24, 1), 120, "Atlassian Sydney office", "Daniel R.", InterviewOutcome.Failed, "Values interview plus a pair-programming session."),
            Iv(4, InterviewType.FinalRound, Utc(7, 27, 0), 180, "Canva Sydney office", "Mia C.", InterviewOutcome.Failed, "Three back-to-back sessions; bring a portfolio project."),
            Iv(15, InterviewType.Screening, Utc(8, 4, 6), 30, "Phone", "Jess P.", InterviewOutcome.Cancelled, null),
            Iv(21, InterviewType.Behavioural, Utc(8, 15, 0), 60, "Microsoft Teams", "Aaron L.", InterviewOutcome.Passed, null),
            Iv(25, InterviewType.Technical, Utc(8, 19, 5), 90, "Zoom", "Wei Z.", InterviewOutcome.Passed, "Live coding in C#; review REST API design."),
            Iv(21, InterviewType.AssessmentCentre, Utc(8, 25, 23), 240, "Deloitte Melbourne office", null, InterviewOutcome.Failed, "Group case study and a short presentation."),
            Iv(25, InterviewType.FinalRound, Utc(8, 27, 1), 120, "Airwallex Sydney office", "Hannah B.", InterviewOutcome.Passed, null),
            Iv(36, InterviewType.Technical, Utc(9, 14, 0), 60, "Zoom", "Chris M.", InterviewOutcome.Passed, "SQL window functions and a dashboard critique."),
            Iv(38, InterviewType.Technical, Utc(9, 16, 0), 60, "Microsoft Teams", "Olivia S.", InterviewOutcome.Passed, null),
            Iv(40, InterviewType.Behavioural, Utc(9, 16, 4), 45, "Zoom", "Ben T.", InterviewOutcome.Passed, null),
            Iv(44, InterviewType.Technical, Utc(9, 23, 1), 90, "Microsoft Teams", "Raj P.", InterviewOutcome.Passed, "Expect questions on Entity Framework and unit testing."),
            Iv(43, InterviewType.Technical, Utc(9, 23, 5), 90, "Zoom", "Emily H.", InterviewOutcome.Passed, "Build a small React component live."),
            Iv(36, InterviewType.FinalRound, Utc(9, 23, 23), 120, "Zoom", "Liam G.", InterviewOutcome.Failed, null),
            Iv(40, InterviewType.FinalRound, Utc(9, 24, 2), 120, "SafetyCulture Sydney office", "Nina F.", InterviewOutcome.Passed, null),
            Iv(44, InterviewType.AssessmentCentre, Utc(10, 1, 22), 240, "Commonwealth Bank Sydney office", null, InterviewOutcome.Passed, "Group exercise, then a one-on-one with the team lead."),
            Iv(43, InterviewType.FinalRound, Utc(10, 20, 0), 120, "Airwallex Sydney office", "Hannah B.", InterviewOutcome.Pending, "Prepare a frontend system design walkthrough."),
            Iv(49, InterviewType.Technical, Utc(10, 21, 3), 60, "Microsoft Teams", "Jack D.", InterviewOutcome.Pending, "Revise .NET dependency injection and Azure basics."),
            Iv(53, InterviewType.Behavioural, Utc(10, 22, 23), 45, "Zoom", "Sophie R.", InterviewOutcome.Pending, "Research the NAB app and prepare questions.")
        };
        return interviews;
    }

    /// <summary>Builds a submitted application; each step is a status and the days after applying it happened.</summary>
    private static SeedApplication Applied(int id, string company, Role role, string location, string workMode, string source,
        int? salaryMinThousands, int? salaryMaxThousands, DateTime posted, DateTime applied, Tactics tactics,
        params (SeedStatus Status, int Day)[] steps)
    {
        var history = new List<SeedStageEvent> { new SeedStageEvent { ToStatus = SeedStatus.Applied, ChangedAt = applied } };
        foreach ((SeedStatus status, int day) in steps)
        {
            history.Add(new SeedStageEvent { FromStatus = history[^1].ToStatus, ToStatus = status, ChangedAt = applied.AddDays(day) });
        }

        return Build(id, company, role, location, workMode, source, salaryMinThousands, salaryMaxThousands, posted, applied, tactics, history);
    }

    /// <summary>Builds a wishlist item that was saved on the given date and never submitted.</summary>
    private static SeedApplication Wishlist(int id, string company, Role role, string location, string workMode, string source,
        int? salaryMinThousands, int? salaryMaxThousands, DateTime posted, DateTime saved)
    {
        var history = new List<SeedStageEvent> { new SeedStageEvent { ToStatus = SeedStatus.Wishlist, ChangedAt = saved } };
        return Build(id, company, role, location, workMode, source, salaryMinThousands, salaryMaxThousands, posted, null, Tactics.None, history);
    }

    private static SeedApplication Build(int id, string company, Role role, string location, string workMode, string source,
        int? salaryMinThousands, int? salaryMaxThousands, DateTime posted, DateTime? applied, Tactics tactics,
        IReadOnlyList<SeedStageEvent> history)
    {
        string description = $"{CompanyPitches[company]} {role.Description}";
        if (AdExtras.TryGetValue(id, out string? extra))
        {
            description += " " + extra;
        }

        return new SeedApplication
        {
            Id = id,
            CompanyName = company,
            RoleTitle = role.Title,
            Location = location,
            WorkMode = workMode,
            Source = source,
            SalaryMin = salaryMinThousands * 1_000m,
            SalaryMax = salaryMaxThousands * 1_000m,
            JobDescription = description,
            DatePosted = posted,
            DateApplied = applied,
            HadReferral = tactics.HasFlag(Tactics.Referral),
            TailoredResume = tactics.HasFlag(Tactics.Tailored),
            WroteCoverLetter = tactics.HasFlag(Tactics.CoverLetter),
            Notes = ApplicationNotes.GetValueOrDefault(id),
            History = history
        };
    }

    private static Interview Iv(int applicationId, InterviewType type, DateTime scheduledAtUtc, int durationMinutes,
        string locationOrLink, string? interviewer, InterviewOutcome outcome, string? prepNotes)
    {
        return new Interview
        {
            ApplicationId = applicationId,
            Type = type,
            ScheduledAt = scheduledAtUtc,
            DurationMinutes = durationMinutes,
            LocationOrLink = locationOrLink,
            Interviewer = interviewer,
            PrepNotes = prepNotes,
            Outcome = outcome
        };
    }

    private static DateTime D(int month, int day) => new DateTime(Year, month, day);

    private static DateTime Utc(int month, int day, int hour) => new DateTime(Year, month, day, hour, 0, 0, DateTimeKind.Utc);

    /// <summary>A kind of role: its title and the body of its job ad.</summary>
    private sealed record Role(string Title, string Description);
}
