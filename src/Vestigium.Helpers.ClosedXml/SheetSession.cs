using ClosedXML.Excel;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.ClosedXml;

public sealed class SheetSession
{
    private readonly WorkbookSession _book;
    private readonly IXLWorksheet _sheet;

    internal SheetSession(WorkbookSession book, IXLWorksheet sheet)
    {
        _book = book;
        _sheet = sheet;
    }

    public string Name => _sheet.Name;

    public void WriteTable(SheetTable table, SheetWriteOptions? options = null)
    {
        _book.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        var opts = options ?? SheetWriteOptions.Default;
        ClearContent();

        var headers = table.Headers;
        var colCount = headers.Count;
        foreach (var row in table.Rows)
            colCount = Math.Max(colCount, row.Count);

        if (colCount == 0)
            throw new ArgumentException("A table needs at least one column.", nameof(table));

        var neutralized = 0;
        if (opts.HasHeaderRow)
        {
            for (var c = 0; c < headers.Count; c++)
                _sheet.Cell(1, c + 1).Value = headers[c] ?? $"Column{c + 1}";
            StyleHeader(headers.Count);
        }

        var r = opts.HasHeaderRow ? 2 : 1;
        foreach (var row in table.Rows)
        {
            for (var c = 0; c < colCount; c++)
            {
                var value = c < row.Count ? row[c] : null;
                if (CellWriter.Write(_sheet.Cell(r, c + 1), value, opts))
                    neutralized++;
            }
            r++;
        }

        var lastRow = Math.Max(1, r - 1);
        var range = _sheet.Range(1, 1, lastRow, colCount);
        ApplyChrome(range, lastRow, colCount, opts, table.Name);
        HelperLog.Information(
            _book.AppId,
            VestigiumStatus.Success,
            HelperLog.AppIds.ClosedXml,
            $"Wrote sheet={Name} rows={table.Rows.Count} cols={colCount} neutralized={neutralized}");
    }

    public void AppendRows(IEnumerable<IReadOnlyList<object?>> rows, SheetWriteOptions? options = null)
    {
        _book.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(rows);
        var opts = options ?? SheetWriteOptions.Default;
        var last = _sheet.LastRowUsed()?.RowNumber() ?? 0;
        var r = Math.Max(1, last + 1);
        var count = 0;
        foreach (var row in rows)
        {
            for (var c = 0; c < row.Count; c++)
                CellWriter.Write(_sheet.Cell(r, c + 1), row[c], opts);
            r++;
            count++;
        }
        HelperLog.Information(
            _book.AppId,
            VestigiumStatus.Success,
            HelperLog.AppIds.ClosedXml,
            $"Appended sheet={Name} rows={count}");
    }

    public void ApplyChrome(SheetChrome chrome)
    {
        _book.ThrowIfDisposed();
        var used = _sheet.RangeUsed();
        if (used is null) return;
        if (chrome.BoldHeader) StyleHeader(used.ColumnCount());
        if (chrome.FreezeHeader) _sheet.SheetView.FreezeRows(1);
        if (chrome.AutoFilter && !_sheet.Tables.Any()) used.SetAutoFilter();
        ExcelNames.TryApplyTabColor(_sheet, chrome.TabColor);
    }

    private void ApplyChrome(IXLRange range, int lastRow, int colCount, SheetWriteOptions opts, string? tableName)
    {
        if (opts.HasHeaderRow) StyleHeader(colCount);
        if (opts.CreateExcelTable && opts.HasHeaderRow && lastRow >= 1 && !_sheet.Tables.Any())
        {
            var name = UniqueTableName(ExcelNames.SanitizeTable(tableName, Name + "Table"));
            range.CreateTable(name);
        }
        else if (opts.AutoFilter && opts.HasHeaderRow)
        {
            range.SetAutoFilter();
        }
        if (opts.FreezeHeader && opts.HasHeaderRow) _sheet.SheetView.FreezeRows(1);
        if (opts.Autosize)
        {
            _sheet.Columns(1, colCount).AdjustToContents();
            var cap = opts.AutosizeMaxWidth <= 0 ? 40 : opts.AutosizeMaxWidth;
            for (var c = 1; c <= colCount; c++)
            {
                if (_sheet.Column(c).Width > cap) _sheet.Column(c).Width = cap;
            }
        }
        ExcelNames.TryApplyTabColor(_sheet, opts.TabColor);
    }

    private void StyleHeader(int colCount)
    {
        _sheet.Range(1, 1, 1, Math.Max(1, colCount)).Style.Font.Bold = true;
    }

    private void ClearContent() => _sheet.Clear();

    private string UniqueTableName(string name)
    {
        var used = new HashSet<string>(
            _book.Workbook.Worksheets.SelectMany(w => w.Tables.Select(t => t.Name)),
            StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(name)) return name;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{name}_{i}";
            if (!used.Contains(candidate)) return candidate;
        }
        return name + "_" + Guid.NewGuid().ToString("N")[..8];
    }
}
