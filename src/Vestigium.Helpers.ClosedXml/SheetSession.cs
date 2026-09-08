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
        if (opts.HeaderNumberFormats && opts.HasHeaderRow)
            ApplyHeaderFormats(headers, lastRow, colCount);
        if (opts.OperatorPrint)
            ApplyOperatorPrint();
        if (!string.IsNullOrWhiteSpace(opts.HighlightColumn) && opts.HighlightGreaterThan is { } threshold)
            HighlightGreaterThan(opts.HighlightColumn, threshold);
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
        var colCount = _sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
        var r = last + 1;
        if (r < 1)
            r = 1;

        var count = 0;
        foreach (var row in rows)
        {
            colCount = Math.Max(colCount, row.Count);
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

    public SheetTable ReadUsedRange(SheetReadOptions? options = null)
    {
        _book.ThrowIfDisposed();
        var opts = options ?? SheetReadOptions.Default;
        var used = _sheet.RangeUsed();
        if (used is null)
        {
            HelperLog.Information(
                _book.AppId,
                VestigiumStatus.Success,
                HelperLog.AppIds.ClosedXml,
                $"Read sheet={Name} rows=0 cols=0");
            return new SheetTable { Headers = [], Rows = [], Name = Name };
        }

        var firstRow = used.FirstRow().RowNumber();
        var lastRow = used.LastRow().RowNumber();
        var firstCol = used.FirstColumn().ColumnNumber();
        var lastCol = used.LastColumn().ColumnNumber();
        var colCount = lastCol - firstCol + 1;

        var headers = new string[colCount];
        var dataStart = firstRow;
        if (opts.HasHeaderRow)
        {
            for (var i = 0; i < colCount; i++)
            {
                var text = _sheet.Cell(firstRow, firstCol + i).GetString();
                headers[i] = string.IsNullOrWhiteSpace(text) ? $"Column{i + 1}" : text;
            }

            dataStart = firstRow + 1;
        }
        else
        {
            for (var i = 0; i < colCount; i++)
                headers[i] = $"Column{i + 1}";
        }

        var rows = new List<IReadOnlyList<object?>>();
        for (var r = dataStart; r <= lastRow; r++)
        {
            var row = new object?[colCount];
            for (var i = 0; i < colCount; i++)
                row[i] = CellReader.Read(_sheet.Cell(r, firstCol + i));
            rows.Add(row);
        }

        var tableName = _sheet.Tables.FirstOrDefault()?.Name ?? Name;
        HelperLog.Information(
            _book.AppId,
            VestigiumStatus.Success,
            HelperLog.AppIds.ClosedXml,
            $"Read sheet={Name} rows={rows.Count} cols={colCount}");
        return new SheetTable { Headers = headers, Rows = rows, Name = tableName };
    }

    public void ApplyChrome(SheetChrome chrome)
    {
        _book.ThrowIfDisposed();
        var used = _sheet.RangeUsed();
        if (used is null)
        {
            ApplyTabColor(chrome.TabColor);
            if (chrome.OperatorPrint)
                ApplyOperatorPrint();
            return;
        }

        if (chrome.BoldHeader)
            StyleHeader(used.ColumnCount());
        if (chrome.FreezeHeader)
            _sheet.SheetView.FreezeRows(1);
        if (chrome.AutoFilter && !_sheet.Tables.Any())
            used.SetAutoFilter();
        ApplyTabColor(chrome.TabColor);
        if (chrome.OperatorPrint)
            ApplyOperatorPrint();
    }

    public void ApplyOperatorPrint()
    {
        _book.ThrowIfDisposed();
        var setup = _sheet.PageSetup;
        setup.PageOrientation = XLPageOrientation.Landscape;
        setup.FitToPages(1, 0);
        setup.Footer.Clear();
        setup.Footer.Left.AddText(_book.AppId, XLHFOccurrence.AllPages);
        setup.Footer.Left.AddText(_book.AppId, XLHFOccurrence.OddPages);
        setup.Footer.Right.AddText(XLHFPredefinedText.Date, XLHFOccurrence.AllPages);
        setup.Footer.Right.AddText(" ", XLHFOccurrence.AllPages);
        setup.Footer.Right.AddText(XLHFPredefinedText.Time, XLHFOccurrence.AllPages);
        setup.Footer.Right.AddText(XLHFPredefinedText.Date, XLHFOccurrence.OddPages);
        setup.Footer.Right.AddText(" ", XLHFOccurrence.OddPages);
        setup.Footer.Right.AddText(XLHFPredefinedText.Time, XLHFOccurrence.OddPages);
    }

    public void HighlightGreaterThan(string header, double threshold)
    {
        _book.ThrowIfDisposed();
        var name = HelperGuard.NotBlank(header, nameof(header));
        var used = _sheet.RangeUsed();
        if (used is null)
            throw new InvalidOperationException("Sheet has no used range to highlight.");

        var col = FindHeaderColumn(name, used);
        if (col is null)
            throw new ArgumentException($"Header '{name}' was not on this sheet.", nameof(header));

        var lastRow = used.LastRow().RowNumber();
        if (lastRow < 2)
            return;

        _sheet.ConditionalFormats.RemoveAll();
        var range = _sheet.Range(2, col.Value, lastRow, col.Value);
        var style = range.AddConditionalFormat().WhenGreaterThan(threshold);
        style.Fill.SetBackgroundColor(XLColor.FromHtml("#C00000"));
        style.Font.SetFontColor(XLColor.White);
    }

    private int? FindHeaderColumn(string header, IXLRange used)
    {
        var firstCol = used.FirstColumn().ColumnNumber();
        var lastCol = used.LastColumn().ColumnNumber();
        for (var c = firstCol; c <= lastCol; c++)
        {
            if (string.Equals(_sheet.Cell(1, c).GetString(), header, StringComparison.OrdinalIgnoreCase))
                return c;
        }

        return null;
    }

    private void ApplyHeaderFormats(IReadOnlyList<string> headers, int lastRow, int colCount)
    {
        if (lastRow < 2)
            return;
        var n = Math.Min(headers.Count, colCount);
        for (var c = 0; c < n; c++)
        {
            var format = HeaderFormats.For(headers[c]);
            if (format is null)
                continue;
            var body = _sheet.Range(2, c + 1, lastRow, c + 1);
            if (format == HeaderFormats.Utc)
                body.Style.DateFormat.Format = format;
            else
                body.Style.NumberFormat.Format = format;
        }
    }

    private void ApplyChrome(IXLRange range, int lastRow, int colCount, SheetWriteOptions opts, string? tableName)
    {
        if (opts.HasHeaderRow)
            StyleHeader(colCount);

        if (opts.CreateExcelTable && opts.HasHeaderRow && lastRow >= 1 && !_sheet.Tables.Any())
        {
            var name = UniqueTableName(ExcelNames.SanitizeTable(tableName, Name + "Table"));
            var table = range.CreateTable(name);
            table.ShowRowStripes = true;
            table.Theme = ExcelTableStyles.Resolve(opts.TableStyle ?? _book.TableStyle);
        }
        else if (opts.AutoFilter && opts.HasHeaderRow)
        {
            range.SetAutoFilter();
        }

        if (opts.FreezeHeader && opts.HasHeaderRow)
            _sheet.SheetView.FreezeRows(1);

        if (opts.Autosize)
        {
            _sheet.Columns(1, colCount).AdjustToContents();
            var cap = opts.AutosizeMaxWidth <= 0 ? 40 : opts.AutosizeMaxWidth;
            for (var c = 1; c <= colCount; c++)
            {
                if (_sheet.Column(c).Width > cap)
                    _sheet.Column(c).Width = cap;
            }
        }

        ApplyTabColor(opts.TabColor);
    }

    private void ApplyTabColor(string? hex)
    {
        var color = ExcelNames.ParseTabColor(hex);
        if (color is not null)
            _sheet.TabColor = color;
    }

    private void StyleHeader(int colCount)
    {
        var header = _sheet.Range(1, 1, 1, Math.Max(1, colCount));
        header.Style.Font.Bold = true;
    }

    private void ClearContent()
    {
        _sheet.Clear();
        _sheet.ConditionalFormats.RemoveAll();
    }

    private string UniqueTableName(string name)
    {
        var used = new HashSet<string>(
            _book.Workbook.Worksheets.SelectMany(w => w.Tables.Select(t => t.Name)),
            StringComparer.OrdinalIgnoreCase);
        if (!used.Contains(name))
            return name;
        for (var i = 2; i < 1000; i++)
        {
            var candidate = $"{name}_{i}";
            if (!used.Contains(candidate))
                return candidate;
        }

        return name + "_" + Guid.NewGuid().ToString("N")[..8];
    }
}
