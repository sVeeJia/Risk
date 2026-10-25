using ClosedXML.Excel;
using Risk.Api.Data.Import;

namespace Risk.Tests.Data;

public class ExcelCountrySourceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.xlsx");

    public void Dispose() { if (File.Exists(_path)) File.Delete(_path); }

    private void WriteSheet(params (object rank, string name, string iso, object pop)[] rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Countries");
        ws.Cell(1, 1).Value = "Rank"; ws.Cell(1, 2).Value = "Name";
        ws.Cell(1, 3).Value = "ISO2"; ws.Cell(1, 4).Value = "Population";
        for (var i = 0; i < rows.Length; i++)
        {
            ws.Cell(i + 2, 1).Value = XLCellValue.FromObject(rows[i].rank);
            ws.Cell(i + 2, 2).Value = rows[i].name;
            ws.Cell(i + 2, 3).Value = rows[i].iso;
            ws.Cell(i + 2, 4).Value = XLCellValue.FromObject(rows[i].pop);
        }
        wb.SaveAs(_path);
    }

    [Fact]
    public void Read_ParsesValidRows()
    {
        WriteSheet((1, "India", "in", 1476625576L), (2, "China", "CN", 1412914089L));

        var rows = new ExcelCountrySource(_path).Read();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new CountryRow(1, "India", "IN", 1476625576L), rows[0]);
    }

    [Fact]
    public void Read_DuplicateIso_Throws()
    {
        WriteSheet((1, "India", "IN", 100L), (2, "Indonesia", "IN", 90L));
        Assert.Throws<InvalidDataException>(() => new ExcelCountrySource(_path).Read());
    }

    [Fact]
    public void Read_BadPopulation_Throws()
    {
        WriteSheet((1, "India", "IN", 937.282));
        Assert.Throws<InvalidDataException>(() => new ExcelCountrySource(_path).Read());
    }

    [Fact]
    public void Read_MissingFile_Throws() =>
        Assert.Throws<FileNotFoundException>(() => new ExcelCountrySource(_path).Read());
}