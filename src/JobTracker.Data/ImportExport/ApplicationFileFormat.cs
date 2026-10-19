using JobTracker.Core.Entities;

namespace JobTracker.Data.ImportExport;

/// <summary>
/// A file format that applications can be exported to and imported from (G5). CSV and JSON are
/// subclasses: each one only knows how to write and read rows of text values in its own layout,
/// while this base class does the shared work (turning applications into rows and checking every
/// imported row with ApplicationColumns).
/// </summary>
/// <remarks>
/// Anything exported by a format can be imported again by the same format (round trip).
/// To add a format, subclass this and add an instance to ImportExportService.Formats.
/// </remarks>
public abstract class ApplicationFileFormat
{
    /// <summary>The name shown in the UI, e.g. "CSV".</summary>
    public abstract string Name { get; }

    /// <summary>The file extension including the dot, e.g. ".csv".</summary>
    public abstract string FileExtension { get; }

    /// <summary>The MIME type used for downloads, e.g. "text/csv".</summary>
    public abstract string ContentType { get; }

    /// <summary>True when the file name ends with this format's extension (ignoring case).</summary>
    public bool Matches(string fileName) =>
        !string.IsNullOrWhiteSpace(fileName) && fileName.Trim().EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase);

    /// <summary>Writes the applications as a file of this format. No applications gives a file with only the headings.</summary>
    /// <param name="applications">The applications to write, with their companies loaded.</param>
    /// <returns>The file's text.</returns>
    public string Export(IEnumerable<JobApplication> applications)
    {
        ArgumentNullException.ThrowIfNull(applications);
        return WriteRows(applications.Select(ApplicationColumns.ToValues).ToList());
    }

    /// <summary>
    /// Reads a file of this format and checks every row. Valid rows become applications; invalid rows
    /// are listed with their row numbers. Blank rows are ignored.
    /// </summary>
    /// <param name="content">The file's text.</param>
    /// <returns>The good rows and the bad rows, or a file error when the file can't be read at all.</returns>
    public ParseResult Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return ParseResult.ForFileError("The file is empty.");
        }

        IReadOnlyList<ImportRow> rows;
        try
        {
            rows = ReadRows(content);
        }
        catch (FormatException ex)
        {
            return ParseResult.ForFileError(ex.Message);
        }

        var applications = new List<ParsedApplication>();
        var errors = new List<ImportRowError>();
        foreach (ImportRow row in rows.Where(row => !ApplicationColumns.IsBlank(row)))
        {
            if (ApplicationColumns.TryConvert(row, out ParsedApplication? application, out ImportRowError? error))
            {
                applications.Add(application!);
            }
            else
            {
                errors.Add(error!);
            }
        }

        if (applications.Count == 0 && errors.Count == 0)
        {
            return ParseResult.ForFileError("The file has no applications in it.");
        }

        return new ParseResult(applications, errors);
    }

    /// <summary>Writes rows of text values in this format's layout.</summary>
    /// <param name="rows">One entry per application; each holds every column in ApplicationColumns.All order.</param>
    protected abstract string WriteRows(IReadOnlyList<IReadOnlyList<(ApplicationColumn Column, string? Value)>> rows);

    /// <summary>Splits a file into rows of text values, without checking the values.</summary>
    /// <param name="content">The file's text; never blank.</param>
    /// <exception cref="FormatException">The file can't be read at all; the message is shown to the user.</exception>
    protected abstract IReadOnlyList<ImportRow> ReadRows(string content);
}
