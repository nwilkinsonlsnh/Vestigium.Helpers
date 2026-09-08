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
        ClearContent();
        WriteAt(1, 1, table, options ?? SheetWriteOptions.Default);
    }

    /// <summary>
    /// Write a table starting at a cell without clearing the rest of the sheet.
    /// Letterhead chrome above or beside the origin stays put.
    /// </summary>
    public void WriteAt(int row, int column, SheetTable table, SheetWriteOptions? options = null)
    {
        _book.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(table);
        if (row < 1)
            throw new ArgumentOutOfRangeException(nameof(row), "Row is 1-based.");
        if (column < 1)
            throw new ArgumentOutOfRangeException(nameof(column), "Column is 1-based.");

        var opts = options ?? SheetWriteOptions.Default;
        var headers = table.Headers;
        var colCount = headers.Count;
        foreach (var dataRow in table.Rows)
            colCount = Math.Max(colCount, dataRow.Count);

        if (colCount == 0)
            throw new ArgumentException("A table needs at least one column.", nameof(table));

        var neutralized = 0;
        if (opts.HasHeaderRow)
        {
            for (var c = 0; c < headers.Count; c++)
                _sheet.Cell(row, column + c).Value = headers[c] ?? $"Column{c + 1}";
            StyleHeader(row, column, headers.Count);
        }

        var r = opts.HasHeaderRow ? row + 1 : row;
        foreach (var dataRow in table.Rows)
        {
            for (var c = 0; c < colCount; c++)
            {
                var value = c < dataRow.Count ? dataRow[c] : null;
                if (CellWriter.Write(_sheet.Cell(r, column + c), value, opts))
                    neutralized++;
            }

            r++;
        }

        var lastRow = Math.Max(row, r - 1);
        var lastCol = column + colCount - 1;
        var range = _sheet.Range(row, column, lastRow, lastCol);
        ApplyChrome(range, lastRow, colCount, opts, table.Name, row, column);
        if (opts.HeaderNumberFormats && opts.HasHeaderRow)
            ApplyHeaderFormats(headers, row, column, lastRow, colCount);
        if (opts.OperatorPrint)
            ApplyOperatorPrint();
        if (!string.IsNullOrWhiteSpace(opts.HighlightColumn) && opts.HighlightGreaterThan is { } threshold)
            HighlightGreaterThan(opts.HighlightColumn, threshold, row, column, lastRow, lastCol);
        HelperLog.Information(
            _book.AppId,
            VestigiumStatus.Success,
            HelperLog.AppIds.ClosedXml,
            $"Wrote sheet={Name} origin={ExcelNames.ColumnLetter(column)}{row} rows={table.Rows.Count} cols={colCount} neutralized={neutralized}");
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
            StyleHeader(used.FirstRow().RowNumber(), used.FirstColumn().ColumnNumber(), used.ColumnCount());
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
        var used = _sheet.RangeUsed() ?? throw new InvalidOperationException("Sheet has no used range to highlight.");
        HighlightGreaterThan(
            header,
            threshold,
            used.FirstRow().RowNumber(),
            used.FirstColumn().ColumnNumber(),
            used.LastRow().RowNumber(),
            used.LastColumn().ColumnNumber());
    }

    /// <summary>Embed an image whose top-left sits on this cell. Size is pixels.</summary>
    public void AddPicture(string imagePath, int row, int column, int widthPx = 160, int heightPx = 48, string? name = null)
    {
        _book.ThrowIfDisposed();
        var path = HelperGuard.NotBlank(imagePath, nameof(imagePath));
        if (!File.Exists(path))
            throw new FileNotFoundException("Image not found.", path);
        using var stream = File.OpenRead(path);
        AddPicture(stream, row, column, widthPx, heightPx, name);
    }

    public void AddPicture(Stream image, int row, int column, int widthPx = 160, int heightPx = 48, string? name = null)
    {
        _book.ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(image);
        if (row < 1)
            throw new ArgumentOutOfRangeException(nameof(row), "Row is 1-based.");
        if (column < 1)
            throw new ArgumentOutOfRangeException(nameof(column), "Column is 1-based.");

        using var copy = new MemoryStream();
        image.CopyTo(copy);
        copy.Position = 0;
        var pic = _sheet.AddPicture(copy);
        pic.MoveTo(_sheet.Cell(row, column));
        if (widthPx > 0)
            pic.Width = widthPx;
        if (heightPx > 0)
            pic.Height = heightPx;
        if (!string.IsNullOrWhiteSpace(name))
            pic.Name = name.Trim();
        HelperLog.Information(
            _book.AppId,
            VestigiumStatus.Success,
            HelperLog.AppIds.ClosedXml,
            $"Picture sheet={Name} cell={ExcelNames.ColumnLetter(column)}{row} {widthPx}x{heightPx}");
    }

    internal IXLWorksheet Worksheet => _sheet;

    private void HighlightGreaterThan(string header, double threshold, int headerRow, int firstCol, int lastRow, int lastCol)
    {
        var name = HelperGuard.NotBlank(header, nameof(header));
        var col = FindHeaderColumn(name, headerRow, firstCol, lastCol);
        if (col is null)
            throw new ArgumentException($"Header '{name}' was not on this sheet.", nameof(header));

        if (lastRow <= headerRow)
            return;

        _sheet.ConditionalFormats.RemoveAll();
        var range = _sheet.Range(headerRow + 1, col.Value, lastRow, col.Value);
        var style = range.AddConditionalFormat().WhenGreaterThan(threshold);
        style.Fill.SetBackgroundColor(XLColor.FromHtml("#C00000"));
        style.Font.SetFontColor(XLColor.White);
    }

    private int? FindHeaderColumn(string header, int headerRow, int firstCol, int lastCol)
    {
        for (var c = firstCol; c <= lastCol; c++)
        {
            if (string.Equals(_sheet.Cell(headerRow, c).GetString(), header, StringComparison.OrdinalIgnoreCase))
                return c;
        }

        return null;
    }

    private void ApplyHeaderFormats(IReadOnlyList<string> headers, int headerRow, int firstCol, int lastRow, int colCount)
    {
        if (lastRow <= headerRow)
            return;
        var n = Math.Min(headers.Count, colCount);
        for (var c = 0; c < n; c++)
        {
            var format = HeaderFormats.For(headers[c]);
            if (format is null)
                continue;
            var body = _sheet.Range(headerRow + 1, firstCol + c, lastRow, firstCol + c);
            if (format == HeaderFormats.Utc)
                body.Style.DateFormat.Format = format;
            else
                body.Style.NumberFormat.Format = format;
        }
    }

    private void ApplyChrome(
        IXLRange range,
        int lastRow,
        int colCount,
        SheetWriteOptions opts,
        string? tableName,
        int headerRow,
        int firstCol)
    {
        if (opts.HasHeaderRow)
            StyleHeader(headerRow, firstCol, colCount);

        if (opts.CreateExcelTable && opts.HasHeaderRow && lastRow >= headerRow && !_sheet.Tables.Any())
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

        if (opts.FreezeHeader && opts.HasHeaderRow && headerRow == 1)
            _sheet.SheetView.FreezeRows(1);

        if (opts.Autosize)
        {
            var lastCol = firstCol + colCount - 1;
            _sheet.Columns(firstCol, lastCol).AdjustToContents();
            var cap = opts.AutosizeMaxWidth <= 0 ? 40 : opts.AutosizeMaxWidth;
            for (var c = firstCol; c <= lastCol; c++)
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

    private void StyleHeader(int row, int firstCol, int colCount)
    {
        var header = _sheet.Range(row, firstCol, row, firstCol + Math.Max(1, colCount) - 1);
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
