using System.ComponentModel.DataAnnotations;
using JobTracker.Core.Entities;
using JobTracker.Core.Interfaces;
using JobTracker.Data.ImportExport;

namespace JobTracker.Data.Services;

/// <summary>
/// Imports applications from CSV or JSON files and exports them for backup (G5, Grace).
/// </summary>
/// <remarks>
/// New applications are saved with IApplicationService.CreateAsync (S1, Sarah), so imported rows get exactly
/// the same checks, company matching and first stage event as applications added in the form (C6).
/// An application already in the tracker (same company, role and applied date) is skipped, so importing
/// the same backup twice does not create copies.
/// </remarks>
public class ImportExportService
{
    private readonly IApplicationService _applicationService;

    /// <summary>Creates the service.</summary>
    /// <param name="applicationService">Reads applications for export and saves imported ones.</param>
    public ImportExportService(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    /// <summary>The supported formats: CSV for spreadsheets and JSON for backups.</summary>
    public IReadOnlyList<ApplicationFileFormat> Formats { get; } = new ApplicationFileFormat[]
    {
        new CsvApplicationFormat(),
        new JsonApplicationFormat()
    };

    /// <summary>Finds the format for a file by its extension, or null when it is not supported.</summary>
    /// <param name="fileName">For example "applications.csv".</param>
    public ApplicationFileFormat? FindFormat(string fileName) =>
        Formats.FirstOrDefault(format => format.Matches(fileName));

    /// <summary>Exports every application in the tracker.</summary>
    /// <param name="format">The format to write.</param>
    /// <param name="cancellationToken">Cancels the export.</param>
    /// <returns>The file to download, named after today's date.</returns>
    public async Task<ExportFile> ExportAsync(ApplicationFileFormat format, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(format);

        IReadOnlyList<JobApplication> applications = await _applicationService.GetAllAsync(cancellationToken);
        return new ExportFile(FileNameFor(format, "job-applications"), format.ContentType, format.Export(applications));
    }

    /// <summary>An empty file with only the headings, to fill in by hand.</summary>
    /// <param name="format">The format to write.</param>
    public ExportFile CreateTemplate(ApplicationFileFormat format)
    {
        ArgumentNullException.ThrowIfNull(format);
        return new ExportFile($"job-applications-template{format.FileExtension}", format.ContentType,
            format.Export(Array.Empty<JobApplication>()));
    }

    /// <summary>
    /// Checks every row of a file, saves the good rows and reports the rest with their row numbers.
    /// </summary>
    /// <param name="format">The file's format.</param>
    /// <param name="content">The file's text.</param>
    /// <param name="cancellationToken">Cancels the import; rows already saved stay saved.</param>
    /// <returns>How many were imported, which were skipped as already in the tracker, and which failed.</returns>
    public async Task<ImportResult> ImportAsync(ApplicationFileFormat format, string content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(format);

        ParseResult parsed = format.Parse(content);
        if (parsed.FileError != null)
        {
            return new ImportResult(0, Array.Empty<ImportRowError>(), Array.Empty<ImportRowError>(), parsed.FileError);
        }

        IReadOnlyList<JobApplication> existing = await _applicationService.GetAllAsync(cancellationToken);
        var seen = existing
            .Select(application => DuplicateKey(application.Company?.Name, application))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        int imported = 0;
        var skipped = new List<ImportRowError>();
        var errors = new List<ImportRowError>(parsed.Errors);

        foreach (ParsedApplication row in parsed.Applications)
        {
            if (!seen.Add(DuplicateKey(row.CompanyName, row.Application)))
            {
                skipped.Add(new ImportRowError(row.RowNumber,
                    $"{row.Application.RoleTitle} at {row.CompanyName} is already in your tracker."));
                continue;
            }

            try
            {
                await _applicationService.CreateAsync(row.Application, row.CompanyName, cancellationToken);
                imported++;
            }
            catch (ValidationException ex)
            {
                // The service checks again (e.g. company name rules); report it against the row.
                errors.Add(new ImportRowError(row.RowNumber, ex.Message));
            }
        }

        return new ImportResult(imported, skipped, errors.OrderBy(error => error.RowNumber).ToList());
    }

    /// <summary>"name-2026-10-19.csv".</summary>
    private static string FileNameFor(ApplicationFileFormat format, string name) =>
        $"{name}-{DateTime.Today:yyyy-MM-dd}{format.FileExtension}";

    /// <summary>Company, role and applied date, trimmed; two applications with the same key are treated as the same one.</summary>
    private static string DuplicateKey(string? companyName, JobApplication application) =>
        string.Join('|',
            companyName?.Trim() ?? string.Empty,
            application.RoleTitle?.Trim() ?? string.Empty,
            application.DateApplied?.ToString(ApplicationColumns.DateFormat) ?? "wishlist");
}
