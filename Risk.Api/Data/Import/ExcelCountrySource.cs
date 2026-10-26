using ClosedXML.Excel;

namespace Risk.Api.Data.Import;

public sealed class ExcelCountrySource : ICountrySource
{
    private static readonly string[] RequiredHeaders = { "Rank", "Name", "ISO2", "Population" };
    private readonly string _path;

    public ExcelCountrySource(string path) => _path = path;

    public IReadOnlyList<CountryRow> Read()
    {
        if (!File.Exists(_path))
        {
            throw new FileNotFoundException($"Country spreadsheet not found: {_path}");          
        }

        using var workbook = new XLWorkbook(_path);
        var sheet = workbook.Worksheet(1);

        // Find columns by header name, so column order doesn't matter.
        var headerRow = sheet.Row(1);
        var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed()) 
        {
            columns[cell.GetString().Trim()] = cell.Address.ColumnNumber;
        }

        foreach (var header in RequiredHeaders)
        {
            if (!columns.ContainsKey(header))
            {
                throw new InvalidDataException($"Missing column '{header}' in {Path.GetFileName(_path)}.");
            }
        }

        var rows = new List<CountryRow>();
        var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;

        for (var r = 2; r <= lastRow; r++)
        {
            var row = sheet.Row(r);
            if (row.IsEmpty()) continue;

            var name = row.Cell(columns["Name"]).GetString().Trim();
            var iso2 = row.Cell(columns["ISO2"]).GetString().Trim().ToUpperInvariant();

            if (name.Length == 0)
            {
                throw new InvalidDataException($"Row {r}: name is empty.");
            }
            if (iso2.Length != 2 || !iso2.All(char.IsAsciiLetter))
            {
                throw new InvalidDataException($"Row {r} ({name}): ISO2 must be two letters, got '{iso2}'.");
            }
            if (!seenCodes.Add(iso2))
            {
                throw new InvalidDataException($"Row {r} ({name}): duplicate ISO2 code '{iso2}'.");
            }
            if (!row.Cell(columns["Rank"]).TryGetValue(out int rank) || rank <= 0)
            {
                throw new InvalidDataException($"Row {r} ({name}): invalid rank.");
            }
            if (!row.Cell(columns["Population"]).TryGetValue(out long population) || population <= 0)
            {
                throw new InvalidDataException($"Row {r} ({name}): population must be a positive whole number.");
            }
            rows.Add(new CountryRow(rank, name, iso2, population));
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException("The spreadsheet has no country rows.");
        }

        return rows;
    }
}