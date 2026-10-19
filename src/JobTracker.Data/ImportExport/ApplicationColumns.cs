using System.ComponentModel.DataAnnotations;
using System.Globalization;
using JobTracker.Core.Entities;
using JobTracker.Core.Enums;
using JobTracker.Core.Extensions;

namespace JobTracker.Data.ImportExport;

/// <summary>The kind of value a column holds; the JSON format uses it to write numbers and true/false unquoted.</summary>
public enum ColumnKind
{
    /// <summary>Free text.</summary>
    Text,
    /// <summary>One of a fixed list, e.g. a status.</summary>
    Choice,
    /// <summary>A decimal number.</summary>
    Number,
    /// <summary>Yes or no.</summary>
    Boolean,
    /// <summary>A date, written as YYYY-MM-DD.</summary>
    Date
}

/// <summary>One column of an import/export file.</summary>
/// <param name="Key">The JSON property name, e.g. "dateApplied".</param>
/// <param name="Header">The CSV header, e.g. "Date applied".</param>
/// <param name="Kind">The kind of value.</param>
/// <param name="Read">Gets the column's value from an application, as text.</param>
public record ApplicationColumn(string Key, string Header, ColumnKind Kind, Func<JobApplication, string?> Read);

/// <summary>
/// The columns shared by every file format (G5), and the conversion between a JobApplication and a row
/// of text values. Keeping this in one place means CSV and JSON files hold the same data and are checked
/// by the same rules.
/// </summary>
/// <remarks>
/// Column names are matched ignoring case, spaces, hyphens and underscores, so "Date applied",
/// "dateApplied" and "date_applied" are the same column. Unknown columns are ignored.
/// </remarks>
public static class ApplicationColumns
{
    /// <summary>The date format written to files.</summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Date formats accepted on import: ISO first, then Australian day-first formats from spreadsheets.</summary>
    private static readonly string[] DateFormats =
    {
        "yyyy-MM-dd", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-ddTHH:mm:ss.FFFFFFF",
        "d/M/yyyy", "d/M/yy", "d-M-yyyy", "d MMM yyyy", "d MMMM yyyy"
    };

    /// <summary>Every column, in file order.</summary>
    public static readonly IReadOnlyList<ApplicationColumn> All = new[]
    {
        new ApplicationColumn("company", "Company", ColumnKind.Text, application => application.Company?.Name),
        new ApplicationColumn("role", "Role", ColumnKind.Text, application => application.RoleTitle),
        new ApplicationColumn("location", "Location", ColumnKind.Text, application => application.Location),
        new ApplicationColumn("workMode", "Work mode", ColumnKind.Choice, application => application.WorkMode.ToDisplayText()),
        new ApplicationColumn("source", "Source", ColumnKind.Choice, application => application.Source.ToDisplayText()),
        new ApplicationColumn("status", "Status", ColumnKind.Choice, application => application.Status.ToDisplayText()),
        new ApplicationColumn("salaryMin", "Salary min", ColumnKind.Number, application => FormatNumber(application.SalaryMin)),
        new ApplicationColumn("salaryMax", "Salary max", ColumnKind.Number, application => FormatNumber(application.SalaryMax)),
        new ApplicationColumn("datePosted", "Date posted", ColumnKind.Date, application => FormatDate(application.DatePosted)),
        new ApplicationColumn("dateApplied", "Date applied", ColumnKind.Date, application => FormatDate(application.DateApplied)),
        new ApplicationColumn("referral", "Referral", ColumnKind.Boolean, application => FormatBoolean(application.HadReferral)),
        new ApplicationColumn("tailoredResume", "Tailored resume", ColumnKind.Boolean, application => FormatBoolean(application.TailoredResume)),
        new ApplicationColumn("coverLetter", "Cover letter", ColumnKind.Boolean, application => FormatBoolean(application.WroteCoverLetter)),
        new ApplicationColumn("jobUrl", "Job URL", ColumnKind.Text, application => application.JobUrl),
        new ApplicationColumn("jobDescription", "Job description", ColumnKind.Text, application => application.JobDescription),
        new ApplicationColumn("notes", "Notes", ColumnKind.Text, application => application.Notes)
    };

    /// <summary>The columns every file must have: company, role and location.</summary>
    public static readonly IReadOnlyList<ApplicationColumn> Required = All.Take(3).ToList();

    private static readonly Dictionary<string, ApplicationColumn> ByName = All
        .SelectMany(column => new[] { (Normalise(column.Key), column), (Normalise(column.Header), column) })
        .DistinctBy(pair => pair.Item1)
        .ToDictionary(pair => pair.Item1, pair => pair.column);

    /// <summary>Finds a column by its JSON key or CSV header, ignoring case, spaces, hyphens and underscores.</summary>
    /// <returns>The column, or null for an unknown name.</returns>
    public static ApplicationColumn? Find(string? name) =>
        ByName.TryGetValue(Normalise(name), out ApplicationColumn? column) ? column : null;

    /// <summary>An application as text values, keyed by column, for writing to a file.</summary>
    public static IReadOnlyList<(ApplicationColumn Column, string? Value)> ToValues(JobApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        return All.Select(column => (column, column.Read(application))).ToList();
    }

    /// <summary>True when every value in the row is blank, e.g. an empty line at the end of a spreadsheet.</summary>
    public static bool IsBlank(ImportRow row) => row.Problem == null && row.Fields.Values.All(string.IsNullOrWhiteSpace);

    /// <summary>
    /// Checks one row and turns it into an application. Every problem in the row is reported, not just the first.
    /// </summary>
    /// <param name="row">The row as read from the file.</param>
    /// <param name="application">The application when the row is valid, otherwise null.</param>
    /// <param name="error">Every problem with the row when it is invalid, otherwise null.</param>
    /// <returns>True when the row is valid.</returns>
    public static bool TryConvert(ImportRow row, out ParsedApplication? application, out ImportRowError? error)
    {
        ArgumentNullException.ThrowIfNull(row);
        application = null;
        error = null;

        if (row.Problem != null)
        {
            error = new ImportRowError(row.RowNumber, row.Problem);
            return false;
        }

        // Trimmed values keyed by the column's JSON key; blank values are left out.
        var values = new Dictionary<string, string>();
        foreach ((string name, string? value) in row.Fields)
        {
            if (Find(name) is ApplicationColumn column && !string.IsNullOrWhiteSpace(value))
            {
                values[column.Key] = value.Trim();
            }
        }

        var problems = new List<string>();
        string Get(string key) => values.GetValueOrDefault(key, string.Empty);

        string companyName = Get("company");
        if (companyName.Length == 0)
        {
            problems.Add("Company is required.");
        }

        DateTime? dateApplied = ReadDate(Get("dateApplied"), "Date applied", problems);

        var parsed = new JobApplication
        {
            RoleTitle = Get("role"),
            Location = Get("location"),
            WorkMode = ReadChoice<WorkMode>(Get("workMode"), "Work mode", WorkMode.OnSite, ApplicationEnumExtensions.TryParseWorkMode, ApplicationEnumExtensions.ToDisplayText, problems),
            Source = ReadChoice<ApplicationSource>(Get("source"), "Source", ApplicationSource.Other, ApplicationEnumExtensions.TryParseSource, ApplicationEnumExtensions.ToDisplayText, problems),
            // With no status, a dated row was sent (Applied) and an undated one is still on the wishlist.
            Status = ReadChoice<ApplicationStatus>(Get("status"), "Status", dateApplied.HasValue ? ApplicationStatus.Applied : ApplicationStatus.Wishlist,
                ApplicationEnumExtensions.TryParseStatus, ApplicationEnumExtensions.ToDisplayText, problems),
            SalaryMin = ReadNumber(Get("salaryMin"), "Salary min", problems),
            SalaryMax = ReadNumber(Get("salaryMax"), "Salary max", problems),
            DatePosted = ReadDate(Get("datePosted"), "Date posted", problems),
            DateApplied = dateApplied,
            HadReferral = ReadBoolean(Get("referral"), "Referral", problems),
            TailoredResume = ReadBoolean(Get("tailoredResume"), "Tailored resume", problems),
            WroteCoverLetter = ReadBoolean(Get("coverLetter"), "Cover letter", problems),
            JobUrl = NullIfEmpty(Get("jobUrl")),
            JobDescription = Get("jobDescription"),
            Notes = NullIfEmpty(Get("notes"))
        };

        // The same rules as the application form (attributes and JobApplication.Validate).
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(parsed, new ValidationContext(parsed), results, validateAllProperties: true))
        {
            problems.AddRange(results.Select(result => result.ErrorMessage ?? "A value is invalid."));
        }

        if (problems.Count > 0)
        {
            error = new ImportRowError(row.RowNumber, string.Join(" ", problems.Distinct()));
            return false;
        }

        application = new ParsedApplication(row.RowNumber, parsed, companyName);
        return true;
    }

    /// <summary>Lower case with spaces, hyphens and underscores removed.</summary>
    private static string Normalise(string? text) =>
        new string((text ?? string.Empty).Where(character => character is not (' ' or '-' or '_')).ToArray())
            .ToLowerInvariant();

    private static string? NullIfEmpty(string text) => text.Length == 0 ? null : text;

    private static string? FormatNumber(decimal? value) => value?.ToString("0.##", CultureInfo.InvariantCulture);

    private static string? FormatDate(DateTime? value) => value?.ToString(DateFormat, CultureInfo.InvariantCulture);

    private static string FormatBoolean(bool value) => value ? "Yes" : "No";

    /// <summary>Reads a list value such as a status; blank gives the default.</summary>
    private static TEnum ReadChoice<TEnum>(string text, string label, TEnum fallback, TryParse<TEnum> tryParse,
        Func<TEnum, string> toDisplayText, List<string> problems)
        where TEnum : struct, Enum
    {
        if (text.Length == 0)
        {
            return fallback;
        }

        if (tryParse(text, out TEnum value))
        {
            return value;
        }

        string allowed = string.Join(", ", Enum.GetValues<TEnum>().Select(toDisplayText));
        problems.Add($"{label} \"{text}\" is not one of: {allowed}.");
        return fallback;
    }

    /// <summary>Reads a salary such as "85000", "$85,000" or "85000.50"; blank gives null.</summary>
    private static decimal? ReadNumber(string text, string label, List<string> problems)
    {
        if (text.Length == 0)
        {
            return null;
        }

        string cleaned = text.Replace("$", string.Empty).Replace(",", string.Empty).Replace(" ", string.Empty);
        if (decimal.TryParse(cleaned, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value))
        {
            return value;
        }

        problems.Add($"{label} \"{text}\" is not a number.");
        return null;
    }

    /// <summary>Reads a date in one of the accepted formats; blank gives null.</summary>
    private static DateTime? ReadDate(string text, string label, List<string> problems)
    {
        if (text.Length == 0)
        {
            return null;
        }

        if (DateTime.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out DateTime value))
        {
            return value.Date;
        }

        problems.Add($"{label} \"{text}\" is not a date. Use YYYY-MM-DD or DD/MM/YYYY.");
        return null;
    }

    /// <summary>Reads yes/no, true/false, y/n or 1/0; blank means no.</summary>
    private static bool ReadBoolean(string text, string label, List<string> problems)
    {
        switch (text.ToLowerInvariant())
        {
            case "" or "no" or "n" or "false" or "0":
                return false;
            case "yes" or "y" or "true" or "1" or "x":
                return true;
            default:
                problems.Add($"{label} \"{text}\" should be Yes or No.");
                return false;
        }
    }

    /// <summary>The shape of the ApplicationEnumExtensions.TryParse methods.</summary>
    private delegate bool TryParse<TEnum>(string? text, out TEnum value);
}
