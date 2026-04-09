using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using JobFlow.API.Models;

namespace JobFlow.API.Services;

public sealed class EmployeeImportCsvService
{
    private const int MaxRows = 10000;

    private static readonly Dictionary<string, string> FieldHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["first"] = EmployeeImportTargetFields.FirstName,
        ["firstname"] = EmployeeImportTargetFields.FirstName,
        ["givenname"] = EmployeeImportTargetFields.FirstName,
        ["last"] = EmployeeImportTargetFields.LastName,
        ["lastname"] = EmployeeImportTargetFields.LastName,
        ["surname"] = EmployeeImportTargetFields.LastName,
        ["familyname"] = EmployeeImportTargetFields.LastName,
        ["fullname"] = EmployeeImportTargetFields.FullName,
        ["name"] = EmployeeImportTargetFields.FullName,
        ["employeename"] = EmployeeImportTargetFields.FullName,
        ["email"] = EmployeeImportTargetFields.Email,
        ["emailaddress"] = EmployeeImportTargetFields.Email,
        ["mail"] = EmployeeImportTargetFields.Email,
        ["phone"] = EmployeeImportTargetFields.PhoneNumber,
        ["phonenumber"] = EmployeeImportTargetFields.PhoneNumber,
        ["mobile"] = EmployeeImportTargetFields.PhoneNumber,
        ["telephone"] = EmployeeImportTargetFields.PhoneNumber
    };

    public async Task<ParsedEmployeeCsv> ParseAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("The uploaded CSV file is empty.");
        }

        await using var fileStream = file.OpenReadStream();
        using var streamReader = new StreamReader(fileStream);
        using var csv = new CsvReader(streamReader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            IgnoreBlankLines = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        });

        if (!await csv.ReadAsync())
        {
            throw new InvalidOperationException("Could not read the CSV header row.");
        }

        csv.ReadHeader();

        var rawHeaders = csv.HeaderRecord ?? Array.Empty<string>();
        var headers = rawHeaders
            .Where(h => !string.IsNullOrWhiteSpace(h))
            .Select(h => h.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (headers.Count == 0)
        {
            throw new InvalidOperationException("No valid header columns were found in this CSV file.");
        }

        var rows = new List<Dictionary<string, string?>>(capacity: Math.Min(2000, MaxRows));

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (rows.Count >= MaxRows)
            {
                throw new InvalidOperationException($"CSV row limit exceeded. Maximum supported rows: {MaxRows}.");
            }

            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header)?.Trim();
            }

            rows.Add(row);
        }

        var suggestedMappings = BuildSuggestedMappings(headers);

        return new ParsedEmployeeCsv(headers, rows, suggestedMappings);
    }

    public static Dictionary<string, string?> BuildSuggestedMappings(IEnumerable<string> headers)
    {
        var mappings = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            var normalized = NormalizeHeader(header);

            var mapped = FieldHints.TryGetValue(normalized, out var exact)
                ? exact
                : GuessFieldByContains(normalized);

            mappings[header] = mapped ?? EmployeeImportTargetFields.Ignore;
        }

        return mappings;
    }

    private static string? GuessFieldByContains(string normalized)
    {
        if (normalized.Contains("first") && normalized.Contains("name")) return EmployeeImportTargetFields.FirstName;
        if (normalized.Contains("last") && normalized.Contains("name")) return EmployeeImportTargetFields.LastName;
        if (normalized.Contains("full") && normalized.Contains("name")) return EmployeeImportTargetFields.FullName;
        if (normalized.Contains("mail")) return EmployeeImportTargetFields.Email;
        if (normalized.Contains("phone") || normalized.Contains("mobile") || normalized.Contains("tel")) return EmployeeImportTargetFields.PhoneNumber;

        return null;
    }

    private static string NormalizeHeader(string header)
    {
        return Regex.Replace(header, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
    }
}

public sealed record ParsedEmployeeCsv(
    IReadOnlyList<string> Headers,
    IReadOnlyList<Dictionary<string, string?>> Rows,
    IReadOnlyDictionary<string, string?> SuggestedMappings);
