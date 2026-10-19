using System.Text;

namespace JobTracker.Data.ImportExport;

/// <summary>
/// Comma-separated values, for spreadsheets such as Excel or Google Sheets (G5).
/// The first row holds the column headings; each following row is one application.
/// </summary>
/// <remarks>
/// Follows RFC 4180: a value that contains a comma, a quote or a line break is wrapped in quotes,
/// and quotes inside it are doubled. Row numbers match the spreadsheet: the headings are row 1.
/// </remarks>
public class CsvApplicationFormat : ApplicationFileFormat
{
    /// <summary>A byte-order mark, so Excel opens the file as UTF-8 (otherwise "–" or "é" show as junk).</summary>
    private const char ByteOrderMark = '﻿';

    private static readonly char[] QuotedCharacters = { ',', '"', '\r', '\n' };

    /// <inheritdoc />
    public override string Name => "CSV";

    /// <inheritdoc />
    public override string FileExtension => ".csv";

    /// <inheritdoc />
    public override string ContentType => "text/csv";

    /// <inheritdoc />
    protected override string WriteRows(IReadOnlyList<IReadOnlyList<(ApplicationColumn Column, string? Value)>> rows)
    {
        var builder = new StringBuilder();
        builder.Append(ByteOrderMark);
        AppendLine(builder, ApplicationColumns.All.Select(column => column.Header));

        foreach (IReadOnlyList<(ApplicationColumn Column, string? Value)> row in rows)
        {
            AppendLine(builder, row.Select(cell => cell.Value));
        }

        return builder.ToString();
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ImportRow> ReadRows(string content)
    {
        List<List<string>> records = SplitRecords(content.TrimStart(ByteOrderMark));
        if (records.Count == 0)
        {
            throw new FormatException("The file is empty.");
        }

        List<string> headers = records[0].Select(header => header.Trim()).ToList();
        List<string> missing = ApplicationColumns.Required
            .Where(column => !headers.Any(header => ApplicationColumns.Find(header) == column))
            .Select(column => column.Header)
            .ToList();
        if (missing.Count > 0)
        {
            throw new FormatException(
                $"The first row must hold the column headings. Missing: {string.Join(", ", missing)}. "
                + "Download the CSV template to see every heading.");
        }

        var rows = new List<ImportRow>();
        for (int index = 1; index < records.Count; index++)
        {
            List<string> record = records[index];
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (int column = 0; column < headers.Count; column++)
            {
                // Later duplicate headings are ignored; short rows leave the missing cells blank.
                fields.TryAdd(headers[column], column < record.Count ? record[column] : null);
            }

            // Spreadsheet row number: the headings are row 1, so the first application is row 2.
            string? problem = record.Count > headers.Count && record.Skip(headers.Count).Any(cell => !string.IsNullOrWhiteSpace(cell))
                ? $"The row has {record.Count} values but there are only {headers.Count} headings. Check for a comma that should be inside quotes."
                : null;
            rows.Add(new ImportRow(index + 1, fields, problem));
        }

        return rows;
    }

    /// <summary>Writes one line of values, quoting where needed, ending with CRLF as RFC 4180 asks.</summary>
    private static void AppendLine(StringBuilder builder, IEnumerable<string?> values)
    {
        builder.AppendJoin(',', values.Select(Quote));
        builder.Append("\r\n");
    }

    /// <summary>Wraps a value in quotes when it holds a comma, quote, line break or surrounding spaces.</summary>
    private static string Quote(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool needsQuotes = value.IndexOfAny(QuotedCharacters) >= 0 || value.Trim().Length != value.Length;
        return needsQuotes ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    /// <summary>
    /// Splits CSV text into records of values. Quoted values may hold commas, doubled quotes and line breaks.
    /// Accepts \r\n, \n or \r line endings.
    /// </summary>
    /// <exception cref="FormatException">A quoted value is never closed.</exception>
    private static List<List<string>> SplitRecords(string content)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var value = new StringBuilder();
        bool inQuotes = false;
        int line = 1;
        int quoteStartLine = 0;

        for (int index = 0; index < content.Length; index++)
        {
            char character = content[index];

            if (inQuotes)
            {
                if (character == '"' && index + 1 < content.Length && content[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    if (character == '\n')
                    {
                        line++;
                    }

                    value.Append(character);
                }

                continue;
            }

            switch (character)
            {
                case '"' when value.ToString().Trim().Length == 0:
                    // An opening quote (spaces before it are dropped).
                    value.Clear();
                    inQuotes = true;
                    quoteStartLine = line;
                    break;
                case ',':
                    record.Add(value.ToString());
                    value.Clear();
                    break;
                case '\r' or '\n':
                    if (character == '\r' && index + 1 < content.Length && content[index + 1] == '\n')
                    {
                        index++;
                    }

                    record.Add(value.ToString());
                    value.Clear();
                    records.Add(record);
                    record = new List<string>();
                    line++;
                    break;
                default:
                    value.Append(character);
                    break;
            }
        }

        if (inQuotes)
        {
            throw new FormatException($"A quoted value that starts on line {quoteStartLine} is never closed. Check for a missing \".");
        }

        // The last line usually has no line break after it.
        if (value.Length > 0 || record.Count > 0)
        {
            record.Add(value.ToString());
            records.Add(record);
        }

        // Empty lines stay in the list so later row numbers still match the spreadsheet; they are skipped as blank rows.
        return records;
    }
}
