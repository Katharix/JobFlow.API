using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using JobFlow.API.Models;

namespace JobFlow.API.Services;

public sealed class ClientImportCsvService
{
    private const int MaxRows = 10000;

    private static readonly Dictionary<string, string> FieldHints = new(StringComparer.OrdinalIgnoreCase)
    {
        ["first"] = ClientImportTargetFields.FirstName,
        ["firstname"] = ClientImportTargetFields.FirstName,
        ["givenname"] = ClientImportTargetFields.FirstName,
        ["last"] = ClientImportTargetFields.LastName,
        ["lastname"] = ClientImportTargetFields.LastName,
        ["surname"] = ClientImportTargetFields.LastName,
        ["familyname"] = ClientImportTargetFields.LastName,
        ["fullname"] = ClientImportTargetFields.FullName,
        ["name"] = ClientImportTargetFields.FullName,
        ["clientname"] = ClientImportTargetFields.FullName,
        ["customername"] = ClientImportTargetFields.FullName,
        ["email"] = ClientImportTargetFields.EmailAddress,
        ["emailaddress"] = ClientImportTargetFields.EmailAddress,
        ["mail"] = ClientImportTargetFields.EmailAddress,
        ["phone"] = ClientImportTargetFields.PhoneNumber,
        ["phonenumber"] = ClientImportTargetFields.PhoneNumber,
        ["mobile"] = ClientImportTargetFields.PhoneNumber,
        ["telephone"] = ClientImportTargetFields.PhoneNumber,
        ["address"] = ClientImportTargetFields.Address1,
        ["address1"] = ClientImportTargetFields.Address1,
        ["street"] = ClientImportTargetFields.Address1,
        ["address2"] = ClientImportTargetFields.Address2,
        ["unit"] = ClientImportTargetFields.Address2,
        ["city"] = ClientImportTargetFields.City,
        ["state"] = ClientImportTargetFields.State,
        ["province"] = ClientImportTargetFields.State,
        ["zip"] = ClientImportTargetFields.ZipCode,
        ["zipcode"] = ClientImportTargetFields.ZipCode,
        ["postalcode"] = ClientImportTargetFields.ZipCode
    };

    public async Task<ParsedClientCsv> ParseAsync(IFormFile file, CancellationToken cancellationToken)
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

        return new ParsedClientCsv(headers, rows, suggestedMappings);
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

            mappings[header] = mapped ?? ClientImportTargetFields.Ignore;
        }

        return mappings;
    }

    private static string? GuessFieldByContains(string normalized)
    {
        if (normalized.Contains("first") && normalized.Contains("name")) return ClientImportTargetFields.FirstName;
        if (normalized.Contains("last") && normalized.Contains("name")) return ClientImportTargetFields.LastName;
        if (normalized.Contains("full") && normalized.Contains("name")) return ClientImportTargetFields.FullName;
        if (normalized.Contains("mail")) return ClientImportTargetFields.EmailAddress;
        if (normalized.Contains("phone") || normalized.Contains("mobile") || normalized.Contains("tel")) return ClientImportTargetFields.PhoneNumber;
        if (normalized.Contains("address2") || normalized.Contains("suite") || normalized.Contains("unit")) return ClientImportTargetFields.Address2;
        if (normalized.Contains("address") || normalized.Contains("street")) return ClientImportTargetFields.Address1;
        if (normalized.Contains("city")) return ClientImportTargetFields.City;
        if (normalized.Contains("state") || normalized.Contains("province")) return ClientImportTargetFields.State;
        if (normalized.Contains("zip") || normalized.Contains("postal")) return ClientImportTargetFields.ZipCode;

        return null;
    }

    private static string NormalizeHeader(string header)
    {
        return Regex.Replace(header, "[^a-zA-Z0-9]", string.Empty).ToLowerInvariant();
    }
}

public sealed record ParsedClientCsv(
    IReadOnlyList<string> Headers,
    IReadOnlyList<Dictionary<string, string?>> Rows,
    IReadOnlyDictionary<string, string?> SuggestedMappings);
