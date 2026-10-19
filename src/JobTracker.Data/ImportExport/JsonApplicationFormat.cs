using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace JobTracker.Data.ImportExport;

/// <summary>
/// A JSON backup of every application (G5). Numbers and yes/no values are written as JSON numbers and
/// true/false, so the file is easy to read and to use in other programs.
/// </summary>
/// <remarks>
/// Export writes <c>{ "format": "jobtracker-applications", "version": 1, "exportedAt": ..., "applications": [ ... ] }</c>.
/// Import accepts that object or a plain array of application objects. Row numbers are positions in the
/// list, starting at 1.
/// </remarks>
public class JsonApplicationFormat : ApplicationFileFormat
{
    /// <summary>The value of the "format" property, so a backup can be recognised.</summary>
    public const string FormatId = "jobtracker-applications";

    /// <summary>The version of the layout, for future changes.</summary>
    public const int Version = 1;

    private static readonly JsonSerializerOptions WriteOptions = new JsonSerializerOptions { WriteIndented = true };

    private static readonly JsonDocumentOptions ReadOptions = new JsonDocumentOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    /// <inheritdoc />
    public override string Name => "JSON";

    /// <inheritdoc />
    public override string FileExtension => ".json";

    /// <inheritdoc />
    public override string ContentType => "application/json";

    /// <inheritdoc />
    protected override string WriteRows(IReadOnlyList<IReadOnlyList<(ApplicationColumn Column, string? Value)>> rows)
    {
        var applications = new JsonArray();
        foreach (IReadOnlyList<(ApplicationColumn Column, string? Value)> row in rows)
        {
            var application = new JsonObject();
            foreach ((ApplicationColumn column, string? value) in row)
            {
                application[column.Key] = ToJson(column, value);
            }

            applications.Add(application);
        }

        var root = new JsonObject
        {
            ["format"] = FormatId,
            ["version"] = Version,
            ["exportedAt"] = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            ["applications"] = applications
        };

        return root.ToJsonString(WriteOptions);
    }

    /// <inheritdoc />
    protected override IReadOnlyList<ImportRow> ReadRows(string content)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(content, ReadOptions);
        }
        catch (JsonException ex)
        {
            string where = ex.LineNumber.HasValue ? $" (line {ex.LineNumber + 1})" : string.Empty;
            throw new FormatException($"The file is not valid JSON{where}.");
        }

        using (document)
        {
            JsonElement list = FindApplications(document.RootElement);
            var rows = new List<ImportRow>();
            int rowNumber = 0;
            foreach (JsonElement item in list.EnumerateArray())
            {
                rowNumber++;
                rows.Add(item.ValueKind == JsonValueKind.Object
                    ? new ImportRow(rowNumber, ReadFields(item))
                    : new ImportRow(rowNumber, new Dictionary<string, string?>(), "This entry is not an application object."));
            }

            return rows;
        }
    }

    /// <summary>The list of applications: the root itself, or its "applications" property.</summary>
    /// <exception cref="FormatException">Neither is a list.</exception>
    private static JsonElement FindApplications(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            return root;
        }

        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (string.Equals(property.Name, "applications", StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.Array)
                {
                    return property.Value;
                }
            }
        }

        throw new FormatException("The JSON must be a list of applications, or an object with an \"applications\" list.");
    }

    /// <summary>An application object's properties as text values.</summary>
    private static Dictionary<string, string?> ReadFields(JsonElement item)
    {
        var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (JsonProperty property in item.EnumerateObject())
        {
            fields[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString(),
                JsonValueKind.True => "Yes",
                JsonValueKind.False => "No",
                JsonValueKind.Null or JsonValueKind.Undefined => null,
                // Numbers keep their exact text; objects and arrays are kept as raw JSON and fail the checks.
                _ => property.Value.GetRawText()
            };
        }

        return fields;
    }

    /// <summary>A text value as a JSON value of the column's kind: a number, true/false, a string or null.</summary>
    private static JsonNode? ToJson(ApplicationColumn column, string? value)
    {
        if (value == null)
        {
            return null;
        }

        return column.Kind switch
        {
            ColumnKind.Number when decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number)
                => JsonValue.Create(number),
            ColumnKind.Boolean => JsonValue.Create(value == "Yes"),
            _ => JsonValue.Create(value)
        };
    }
}
