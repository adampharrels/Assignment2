using JobTracker.Core.Enums;

namespace JobTracker.Analytics.Text;

/// <summary>
/// The skills the extractor recognises, each with its category and the other ways job ads write it
/// ("js" means "javascript", "k8s" means "kubernetes"). Names are lower case and must match the
/// skill names stored in the database so the user's profile lines up with the analysis.
/// </summary>
public static class SkillDictionary
{
    private static readonly Dictionary<string, SkillCategory> Categories = new Dictionary<string, SkillCategory>();
    private static readonly Dictionary<string, string> PhraseToSkill = new Dictionary<string, string>();

    static SkillDictionary()
    {
        // Languages
        Add("c#", SkillCategory.Language, "csharp", "c sharp");
        Add("java", SkillCategory.Language);
        Add("python", SkillCategory.Language, "python3");
        Add("javascript", SkillCategory.Language, "js", "es6", "ecmascript");
        Add("typescript", SkillCategory.Language);
        Add("c++", SkillCategory.Language, "cpp");
        Add("golang", SkillCategory.Language, "go lang");
        Add("kotlin", SkillCategory.Language);
        Add("swift", SkillCategory.Language);
        Add("php", SkillCategory.Language);
        Add("ruby", SkillCategory.Language);
        Add("rust", SkillCategory.Language);
        Add("scala", SkillCategory.Language);
        Add("html", SkillCategory.Language, "html5");
        Add("css", SkillCategory.Language, "css3", "sass", "scss");
        Add("bash", SkillCategory.Language, "shell scripting");
        Add("powershell", SkillCategory.Language);

        // Frameworks and libraries
        Add(".net", SkillCategory.Framework, "dotnet", ".net core", "dot net");
        Add("asp.net core", SkillCategory.Framework, "asp.net", "asp.net mvc");
        Add("react", SkillCategory.Framework, "react.js", "reactjs");
        Add("angular", SkillCategory.Framework, "angularjs");
        Add("vue", SkillCategory.Framework, "vue.js", "vuejs");
        Add("next.js", SkillCategory.Framework, "nextjs");
        Add("node.js", SkillCategory.Framework, "node", "nodejs");
        Add("express.js", SkillCategory.Framework, "expressjs");
        Add("spring boot", SkillCategory.Framework);
        Add("django", SkillCategory.Framework);
        Add("flask", SkillCategory.Framework);
        Add("blazor", SkillCategory.Framework);
        Add("graphql", SkillCategory.Framework);
        Add("rest apis", SkillCategory.Framework, "restful", "rest api", "restful apis");
        Add("microservices", SkillCategory.Framework, "microservice");
        Add("jest", SkillCategory.Framework);
        Add("cypress", SkillCategory.Framework);
        Add("pandas", SkillCategory.Framework);
        Add("numpy", SkillCategory.Framework);
        Add("scikit-learn", SkillCategory.Framework, "sklearn", "scikit learn");
        Add("pytorch", SkillCategory.Framework);
        Add("tensorflow", SkillCategory.Framework);
        Add("entity framework", SkillCategory.Framework, "ef core");

        // Data
        Add("sql", SkillCategory.Data, "t-sql", "tsql");
        Add("sql server", SkillCategory.Data, "mssql", "ms sql");
        Add("postgresql", SkillCategory.Data, "postgres");
        Add("mysql", SkillCategory.Data);
        Add("mongodb", SkillCategory.Data, "mongo");
        Add("redis", SkillCategory.Data);
        Add("power bi", SkillCategory.Data, "powerbi");
        Add("tableau", SkillCategory.Data);
        Add("excel", SkillCategory.Data, "microsoft excel");
        Add("statistics", SkillCategory.Data, "statistical analysis", "statistical");
        Add("machine learning", SkillCategory.Data, "ml");
        Add("deep learning", SkillCategory.Data);
        Add("nlp", SkillCategory.Data, "natural language processing");
        Add("data visualisation", SkillCategory.Data, "data visualization");
        Add("a/b testing", SkillCategory.Data, "experimentation");
        Add("airflow", SkillCategory.Data, "apache airflow");
        Add("dbt", SkillCategory.Data);
        Add("spark", SkillCategory.Data, "apache spark", "pyspark");
        Add("etl", SkillCategory.Data);
        Add("mlops", SkillCategory.Data);

        // Cloud and DevOps
        Add("aws", SkillCategory.Cloud, "amazon web services");
        Add("azure", SkillCategory.Cloud, "microsoft azure");
        Add("gcp", SkillCategory.Cloud, "google cloud");
        Add("docker", SkillCategory.Cloud, "containers");
        Add("kubernetes", SkillCategory.Cloud, "k8s");
        Add("terraform", SkillCategory.Cloud);
        Add("ci/cd", SkillCategory.Cloud, "continuous integration", "github actions", "jenkins");
        Add("linux", SkillCategory.Cloud, "unix");
        Add("networking", SkillCategory.Cloud, "tcp/ip", "network security");
        Add("siem", SkillCategory.Cloud);
        Add("splunk", SkillCategory.Cloud);
        Add("incident response", SkillCategory.Cloud);

        // Practices and tools
        Add("git", SkillCategory.Practice, "github", "gitlab", "version control");
        Add("agile", SkillCategory.Practice, "scrum", "kanban");
        Add("unit testing", SkillCategory.Practice, "automated testing", "test-driven development", "tdd");
        Add("accessibility", SkillCategory.Practice, "wcag");
        Add("object-oriented programming", SkillCategory.Practice, "oop", "object oriented");
        Add("data structures", SkillCategory.Practice, "algorithms");
        Add("jira", SkillCategory.Practice);
        Add("figma", SkillCategory.Practice);

        // Soft skills
        Add("communication", SkillCategory.Soft, "communicator", "communication skills");
        Add("teamwork", SkillCategory.Soft, "collaboration", "team player");
        Add("stakeholder management", SkillCategory.Soft, "stakeholder engagement");
        Add("problem solving", SkillCategory.Soft, "problem-solving");
        Add("leadership", SkillCategory.Soft);
    }

    /// <summary>The canonical names of every recognised skill.</summary>
    public static IReadOnlyCollection<string> Skills => Categories.Keys;

    /// <summary>The longest phrase (in tokens) any skill is written as, e.g. 3 for "amazon web services".</summary>
    public static int LongestPhraseLength { get; private set; }

    /// <summary>The canonical skill a phrase stands for, or null if it is not a skill.</summary>
    public static string? FindSkill(string phrase)
    {
        return PhraseToSkill.TryGetValue(phrase, out string? skill) ? skill : null;
    }

    /// <summary>The category of a canonical skill, or Other when unknown.</summary>
    public static SkillCategory CategoryOf(string skill)
    {
        return Categories.TryGetValue(skill, out SkillCategory category) ? category : SkillCategory.Other;
    }

    /// <summary>Registers a skill under its canonical name and every synonym.</summary>
    private static void Add(string skill, SkillCategory category, params string[] synonyms)
    {
        Categories[skill] = category;
        foreach (string phrase in synonyms.Prepend(skill))
        {
            // Store phrases in tokenised form so lookups match exactly what the tokenizer produces.
            List<string> tokens = Tokenizer.Tokenize(phrase).ToList();
            if (tokens.Count == 0)
            {
                continue;
            }

            PhraseToSkill[string.Join(" ", tokens)] = skill;
            LongestPhraseLength = Math.Max(LongestPhraseLength, tokens.Count);
        }
    }
}
