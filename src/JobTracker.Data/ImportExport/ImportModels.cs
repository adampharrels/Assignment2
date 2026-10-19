using JobTracker.Core.Entities;

namespace JobTracker.Data.ImportExport;

/// <summary>
/// One row read from an import file, before it is checked. Values are kept as text so the CSV and
/// JSON formats can share the same checks (ApplicationColumns.Convert).
/// </summary>
/// <param name="RowNumber">The row number to show the user: the spreadsheet row for CSV (the header is row 1), or the position in the list for JSON (from 1).</param>
/// <param name="Fields">Column name (as written in the file) to value; a missing or blank value means "not given".</param>
/// <param name="Problem">Set when the row could not be read at all, e.g. a JSON entry that is not an object.</param>
public record ImportRow(int RowNumber, IReadOnlyDictionary<string, string?> Fields, string? Problem = null);

/// <summary>A row that was not imported, and why.</summary>
/// <param name="RowNumber">The row number, as in ImportRow.</param>
/// <param name="Message">Every problem with the row, as plain English, e.g. "Role is required."</param>
public record ImportRowError(int RowNumber, string Message);

/// <summary>A row that passed every check and is ready to be saved.</summary>
/// <param name="RowNumber">The row number, as in ImportRow.</param>
/// <param name="Application">The application to create. Its Status is the starting status.</param>
/// <param name="CompanyName">The company name, matched or created when the application is saved.</param>
public record ParsedApplication(int RowNumber, JobApplication Application, string CompanyName);

/// <summary>What a file format read from a file: the good rows, and the bad rows with their problems.</summary>
/// <param name="Applications">Rows that passed every check.</param>
/// <param name="Errors">Rows that failed, in row order.</param>
/// <param name="FileError">Set when the whole file could not be read (e.g. it is not valid JSON); the lists are then empty.</param>
public record ParseResult(
    IReadOnlyList<ParsedApplication> Applications,
    IReadOnlyList<ImportRowError> Errors,
    string? FileError = null)
{
    /// <summary>A result for a file that could not be read at all.</summary>
    public static ParseResult ForFileError(string message) =>
        new ParseResult(Array.Empty<ParsedApplication>(), Array.Empty<ImportRowError>(), message);
}

/// <summary>The outcome of importing a file, shown to the user after an upload.</summary>
/// <param name="Imported">How many applications were added.</param>
/// <param name="Skipped">Rows left out because the same application is already in the tracker.</param>
/// <param name="Errors">Rows that were not imported because of a problem, with their row numbers.</param>
/// <param name="FileError">Set when the whole file could not be read; nothing was imported.</param>
public record ImportResult(
    int Imported,
    IReadOnlyList<ImportRowError> Skipped,
    IReadOnlyList<ImportRowError> Errors,
    string? FileError = null)
{
    /// <summary>Every row that was read: imported, skipped or failed.</summary>
    public int TotalRows => Imported + Skipped.Count + Errors.Count;
}

/// <summary>A file ready to download.</summary>
/// <param name="FileName">The suggested file name, e.g. "job-applications-2026-10-19.csv".</param>
/// <param name="ContentType">The MIME type, e.g. "text/csv".</param>
/// <param name="Content">The file's text.</param>
public record ExportFile(string FileName, string ContentType, string Content);
