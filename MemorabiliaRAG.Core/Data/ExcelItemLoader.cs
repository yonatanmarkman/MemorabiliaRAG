using ClosedXML.Excel;
using MemorabiliaRAG.Core.Models;

namespace MemorabiliaRAG.Core.Data;

public class ExcelItemLoader
{
    public List<MemorabiliaItem> Load(string filePath, string sheetName = "Main Sheet")
    {
        List<MemorabiliaItem> items = [];

        using var workbook = new XLWorkbook(filePath);
        IXLWorksheet worksheet = workbook.Worksheet(sheetName);

        // Row 1 is the header, so data starts at row 2
        IEnumerable<IXLRangeRow> rows = worksheet
            .RangeUsed()!
            .RowsUsed()
            .Skip(1);

        int id = 1;

        foreach (IXLRangeRow row in rows)
        {
            string itemDescription = row.Cell(1).GetString().Trim();

            if (string.IsNullOrWhiteSpace(itemDescription))
                continue; // Skip blank rows

            string? obtainedAt = row.Cell(2).GetString().Trim() is { Length: > 0 } o ? o : null;

            int? signingYear = null;
            DateOnly? signingDate = null;
            IXLCell dateCell = row.Cell(3);

            if (dateCell.DataType == XLDataType.DateTime)
            {
                DateTime dt = dateCell.GetDateTime();
                signingDate = DateOnly.FromDateTime(dt);
            }
            else if (dateCell.DataType == XLDataType.Number)
            {
                // stored as a bare year, e.g. 2016
                signingYear = (int)dateCell.GetDouble();
            }

            string? notes = row.Cell(4).GetString().Trim() is { Length: > 0 } n ? n : null;

            items.Add(new MemorabiliaItem
            {
                Id = id.ToString(),
                ItemDescription = itemDescription,
                ObtainedAt = obtainedAt,
                SigningYear = signingYear,
                SigningDate = signingDate,
                Notes = notes
            });

            id++;
        }

        return items;
    }
}