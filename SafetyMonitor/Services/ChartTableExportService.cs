using DataStorage.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MiniExcelLibs;
using SafetyMonitor.Models;
using SpreadsheetFont = DocumentFormat.OpenXml.Spreadsheet.Font;

namespace SafetyMonitor.Services;

public class ChartTableExportService {

    #region Public Methods

    public static void Export(string filePath, IReadOnlyList<MetricAggregation> metricAggregations, IReadOnlyList<ObservingData> aggregatedData, IReadOnlyList<ObservingData> rawData, Action<int>? progress = null) {
        progress?.Invoke(0);
        Thread.Sleep(250);
        
        var aggregatedRows = BuildAggregatedRows(metricAggregations, aggregatedData, progress, 0, 20);
        progress?.Invoke(20);
        Thread.Sleep(250);

        var rawRows = BuildRawRows(rawData, progress, 20, 50);
        progress?.Invoke(50);
        Thread.Sleep(250);
        
        var sheets = new Dictionary<string, object> {
            ["Aggregated chart data"] = aggregatedRows,
            ["Full raw data"] = rawRows
        };
        SaveWithProgress(filePath, sheets, aggregatedRows.Count + rawRows.Count, progress, 50, 75);
        progress?.Invoke(75);
        Thread.Sleep(250);
        
        ApplyWorkbookFormatting(filePath, progress, 75, 95);
        progress?.Invoke(100);
    }

    #endregion Public Methods

    #region Private Methods

    private static void SaveWithProgress(string filePath, Dictionary<string, object> sheets, int totalRows, Action<int>? progress, int progressStart, int progressEnd) {
        if (progress == null) {
            MiniExcel.SaveAs(filePath, sheets, overwriteFile: true);
            return;
        }

        var estimatedBytes = Math.Max((long)totalRows * 200, 4096);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        using var progressStream = new ProgressStream(fileStream, estimatedBytes, pct => {
            var scaled = progressStart + pct * (progressEnd - progressStart) / 100;
            progress.Invoke(Math.Clamp(scaled, progressStart, progressEnd));
        });
        progressStream.SaveAs(sheets);
    }

    private static List<Dictionary<string, object?>> BuildAggregatedRows(
        IReadOnlyList<MetricAggregation> metricAggregations,
        IReadOnlyList<ObservingData> aggregatedData,
        Action<int>? progress,
        int progressStart,
        int progressEnd) {
        var rowsByTime = new SortedDictionary<DateTime, Dictionary<string, object?>>();
        var sorted = aggregatedData.OrderBy(x => x.Timestamp).ToList();
        var totalCount = sorted.Count;
        var lastReported = -1;

        for (var i = 0; i < sorted.Count; i++) {
            var row = sorted[i];
            var timestamp = EnsureUtc(row.Timestamp).ToLocalTime();
            if (!rowsByTime.TryGetValue(timestamp, out var values)) {
                values = new Dictionary<string, object?> {
                    ["Time"] = timestamp
                };
                rowsByTime[timestamp] = values;
            }

            foreach (var aggregation in metricAggregations) {
                values[GetAggregatedColumnName(aggregation)] = RoundMetricValue(aggregation.Metric, aggregation.Metric.GetValue(row));
            }

            if (progress != null && totalCount > 0) {
                var pct = progressStart + (i + 1) * (progressEnd - progressStart) / totalCount;
                if (pct != lastReported) {
                    lastReported = pct;
                    progress.Invoke(pct);
                }
            }
        }

        return [.. rowsByTime.Values];
    }

    private static List<Dictionary<string, object?>> BuildRawRows(
        IReadOnlyList<ObservingData> rawData,
        Action<int>? progress,
        int progressStart,
        int progressEnd) {
        var rows = new List<Dictionary<string, object?>>();
        var sorted = rawData.OrderBy(x => x.Timestamp).ToList();
        var totalCount = sorted.Count;
        var lastReported = -1;

        for (var i = 0; i < sorted.Count; i++) {
            var row = sorted[i];
            var timestamp = EnsureUtc(row.Timestamp).ToLocalTime();
            rows.Add(new Dictionary<string, object?> {
                ["Time"] = timestamp,
                [MetricType.Temperature.GetDisplayName()] = RoundMetricValue(MetricType.Temperature, row.Temperature),
                [MetricType.Humidity.GetDisplayName()] = RoundMetricValue(MetricType.Humidity, row.Humidity),
                [MetricType.Pressure.GetDisplayName()] = RoundMetricValue(MetricType.Pressure, row.Pressure),
                [MetricType.DewPoint.GetDisplayName()] = RoundMetricValue(MetricType.DewPoint, row.DewPoint),
                [MetricType.CloudCover.GetDisplayName()] = RoundMetricValue(MetricType.CloudCover, row.CloudCover),
                [MetricType.SkyTemperature.GetDisplayName()] = RoundMetricValue(MetricType.SkyTemperature, row.SkyTemperature),
                [MetricType.SkyBrightness.GetDisplayName()] = RoundMetricValue(MetricType.SkyBrightness, row.SkyBrightness),
                [MetricType.SkyQuality.GetDisplayName()] = RoundMetricValue(MetricType.SkyQuality, row.SkyQuality),
                [MetricType.RainRate.GetDisplayName()] = RoundMetricValue(MetricType.RainRate, row.RainRate),
                [MetricType.WindSpeed.GetDisplayName()] = RoundMetricValue(MetricType.WindSpeed, row.WindSpeed),
                [MetricType.WindGust.GetDisplayName()] = RoundMetricValue(MetricType.WindGust, row.WindGust),
                [MetricType.WindDirection.GetDisplayName()] = RoundMetricValue(MetricType.WindDirection, row.WindDirection),
                [MetricType.StarFwhm.GetDisplayName()] = RoundMetricValue(MetricType.StarFwhm, row.StarFwhm),
                [MetricType.IsSafe.GetDisplayName()] = RoundMetricValue(MetricType.IsSafe, row.IsSafeInt.HasValue ? row.IsSafeInt * 100.0 : null)
            });

            if (progress != null && totalCount > 0) {
                var pct = progressStart + (i + 1) * (progressEnd - progressStart) / totalCount;
                if (pct != lastReported) {
                    lastReported = pct;
                    progress.Invoke(pct);
                }
            }
        }

        return rows;
    }

    private static void ApplyWorkbookFormatting(string filePath, Action<int>? progress, int progressStart, int progressEnd) {
        using var document = SpreadsheetDocument.Open(filePath, true);
        var workbookPart = document.WorkbookPart;
        if (workbookPart == null) {
            return;
        }

        var (headerBoldStyleIndex, dateTimeStyleIndex, noBorderStyleIndex) = EnsureStyles(workbookPart);

        var worksheetParts = workbookPart.WorksheetParts.ToList();
        const int stepsPerSheet = 5;
        var totalSteps = worksheetParts.Count * stepsPerSheet;
        var completedSteps = 0;

        foreach (var worksheetPart in worksheetParts) {
            var worksheet = worksheetPart.Worksheet;

            RemoveAutoFilter(worksheet);
            completedSteps++;
            ReportStepProgress(progress, progressStart, progressEnd, completedSteps, totalSteps);

            FreezeHeaderRow(worksheet);
            completedSteps++;
            ReportStepProgress(progress, progressStart, progressEnd, completedSteps, totalSteps);

            ApplyCellStyles(worksheet, headerBoldStyleIndex, dateTimeStyleIndex, noBorderStyleIndex);
            completedSteps++;
            ReportStepProgress(progress, progressStart, progressEnd, completedSteps, totalSteps);

            AutoFitColumns(worksheet, workbookPart.SharedStringTablePart?.SharedStringTable);
            completedSteps++;
            ReportStepProgress(progress, progressStart, progressEnd, completedSteps, totalSteps);

            worksheet.Save();
            completedSteps++;
            ReportStepProgress(progress, progressStart, progressEnd, completedSteps, totalSteps);
        }
    }

    private static void ReportStepProgress(Action<int>? progress, int start, int end, int completed, int total) {
        if (progress == null || total <= 0) {
            return;
        }

        progress.Invoke(start + completed * (end - start) / total);
    }







    private static (uint headerBoldStyleIndex, uint dateTimeStyleIndex, uint noBorderStyleIndex) EnsureStyles(WorkbookPart workbookPart) {
        var stylesPart = workbookPart.WorkbookStylesPart ?? workbookPart.AddNewPart<WorkbookStylesPart>();
        stylesPart.Stylesheet ??= new Stylesheet();

        var stylesheet = stylesPart.Stylesheet;
        stylesheet.Fonts ??= new Fonts(new SpreadsheetFont());
        stylesheet.Fills ??= new Fills(
            new Fill(new PatternFill { PatternType = PatternValues.None }),
            new Fill(new PatternFill { PatternType = PatternValues.Gray125 }));
        stylesheet.Borders ??= new Borders(new Border());
        stylesheet.CellStyleFormats ??= new CellStyleFormats(new CellFormat());
        stylesheet.CellFormats ??= new CellFormats(new CellFormat());
        stylesheet.NumberingFormats ??= new NumberingFormats();

        var boldFontId = AppendBoldFont(stylesheet.Fonts);
        var dateTimeFormatId = AppendDateTimeNumberFormat(stylesheet.NumberingFormats);

        var noBorderStyleIndex = AppendCellFormat(stylesheet.CellFormats, new CellFormat {
            FontId = 0U,
            FillId = 0U,
            BorderId = 0U,
            FormatId = 0U,
            NumberFormatId = 0U
        });

        var headerBoldStyleIndex = AppendCellFormat(stylesheet.CellFormats, new CellFormat {
            FontId = boldFontId,
            FillId = 0U,
            BorderId = 0U,
            FormatId = 0U,
            NumberFormatId = 0U,
            ApplyFont = true
        });

        var dateTimeStyleIndex = AppendCellFormat(stylesheet.CellFormats, new CellFormat {
            FontId = 0U,
            FillId = 0U,
            BorderId = 0U,
            FormatId = 0U,
            NumberFormatId = dateTimeFormatId,
            ApplyNumberFormat = true
        });

        stylesheet.Save();
        return (headerBoldStyleIndex, dateTimeStyleIndex, noBorderStyleIndex);
    }

    private static uint AppendBoldFont(Fonts fonts) {
        fonts.AppendChild(new SpreadsheetFont(new Bold()));
        fonts.Count = (uint)fonts.ChildElements.Count;
        return fonts.Count!.Value - 1;
    }

    private static uint AppendDateTimeNumberFormat(NumberingFormats numberingFormats) {
        var existing = numberingFormats.Elements<NumberingFormat>()
            .FirstOrDefault(x => string.Equals(x.FormatCode?.Value, "yyyy-mm-dd hh:mm:ss", StringComparison.Ordinal));
        if (existing?.NumberFormatId != null) {
            return existing.NumberFormatId.Value;
        }

        var nextId = numberingFormats.Elements<NumberingFormat>()
            .Select(x => x.NumberFormatId?.Value ?? 163U)
            .DefaultIfEmpty(163U)
            .Max() + 1;

        numberingFormats.AppendChild(new NumberingFormat {
            NumberFormatId = nextId,
            FormatCode = StringValue.FromString("yyyy-mm-dd hh:mm:ss")
        });
        numberingFormats.Count = (uint)numberingFormats.ChildElements.Count;
        return nextId;
    }

    private static uint AppendCellFormat(CellFormats cellFormats, CellFormat format) {
        cellFormats.AppendChild(format);
        cellFormats.Count = (uint)cellFormats.ChildElements.Count;
        return cellFormats.Count!.Value - 1;
    }

    private static void ApplyCellStyles(Worksheet worksheet, uint headerBoldStyleIndex, uint dateTimeStyleIndex, uint noBorderStyleIndex) {
        var sheetData = worksheet.GetFirstChild<SheetData>();
        if (sheetData == null) {
            return;
        }

        foreach (var row in sheetData.Elements<Row>()) {
            var isHeader = (row.RowIndex?.Value ?? 0U) == 1U;

            foreach (var cell in row.Elements<Cell>()) {
                if (isHeader) {
                    cell.StyleIndex = headerBoldStyleIndex;
                    continue;
                }

                var isTimeColumn = (cell.CellReference?.Value ?? string.Empty).StartsWith("A", StringComparison.OrdinalIgnoreCase);
                cell.StyleIndex = isTimeColumn ? dateTimeStyleIndex : noBorderStyleIndex;
            }
        }
    }

    private static void RemoveAutoFilter(Worksheet worksheet) {
        worksheet.RemoveAllChildren<AutoFilter>();
    }

    private static void FreezeHeaderRow(Worksheet worksheet) {
        worksheet.SheetViews ??= new SheetViews(new SheetView { WorkbookViewId = 0U });
        var sheetView = worksheet.SheetViews.Elements<SheetView>().FirstOrDefault();
        if (sheetView == null) {
            sheetView = new SheetView { WorkbookViewId = 0U };
            worksheet.SheetViews.Append(sheetView);
        }

        sheetView.Pane = new Pane {
            VerticalSplit = 1D,
            TopLeftCell = "A2",
            ActivePane = PaneValues.BottomLeft,
            State = PaneStateValues.Frozen
        };

        sheetView.RemoveAllChildren<Selection>();
        sheetView.Append(new Selection { Pane = PaneValues.BottomLeft, ActiveCell = "A2", SequenceOfReferences = new ListValue<StringValue> { InnerText = "A2" } });
    }




    private static void AutoFitColumns(Worksheet worksheet, SharedStringTable? sharedStringTable) {
        var sheetData = worksheet.GetFirstChild<SheetData>();
        if (sheetData == null) {
            return;
        }

        var headerRow = sheetData.Elements<Row>().FirstOrDefault(r => r.RowIndex?.Value == 1U);
        if (headerRow == null) {
            return;
        }

        var headerWidths = new Dictionary<uint, int>();
        foreach (var cell in headerRow.Elements<Cell>()) {
            var cellReference = cell.CellReference?.Value;
            if (string.IsNullOrWhiteSpace(cellReference)) {
                continue;
            }

            var columnIndex = GetColumnIndex(cellReference);
            if (columnIndex == 0) {
                continue;
            }

            var headerText = GetCellText(cell, sharedStringTable);
            headerWidths[columnIndex] = Math.Max(0, headerText.Length);
        }

        var timeWidthFromValues = 0;
        foreach (var row in sheetData.Elements<Row>().Where(r => (r.RowIndex?.Value ?? 0) > 1)) {
            var timeCell = row.Elements<Cell>().FirstOrDefault(c => (c.CellReference?.Value ?? string.Empty).StartsWith("A", StringComparison.OrdinalIgnoreCase));
            if (timeCell == null) {
                continue;
            }

            var text = GetCellText(timeCell, sharedStringTable);
            if (timeCell.DataType == null && double.TryParse(text, out _)) {
                // Excel serialized DateTime numeric values should fit full datetime pattern
                timeWidthFromValues = Math.Max(timeWidthFromValues, 19);
                continue;
            }

            timeWidthFromValues = Math.Max(timeWidthFromValues, text.Length);
        }

        var columns = GetOrCreateColumns(worksheet);
        columns.RemoveAllChildren<Column>();

        foreach (var (columnIndex, headerLength) in headerWidths.OrderBy(x => x.Key)) {
            var baseLength = headerLength;
            if (columnIndex == 1) {
                baseLength = Math.Max(baseLength, timeWidthFromValues);
            }

            var adjustedWidth = Math.Clamp(baseLength + 2D, 10D, 60D);
            columns.Append(new Column {
                Min = columnIndex,
                Max = columnIndex,
                Width = adjustedWidth,
                CustomWidth = true
            });
        }
    }

    private static Columns GetOrCreateColumns(Worksheet worksheet) {
        var columns = worksheet.GetFirstChild<Columns>();
        if (columns != null) {
            return columns;
        }

        columns = new Columns();

        var sheetData = worksheet.GetFirstChild<SheetData>();
        if (sheetData != null) {
            worksheet.InsertBefore(columns, sheetData);
            return columns;
        }

        worksheet.Append(columns);
        return columns;
    }



    private static string GetCellText(Cell cell, SharedStringTable? sharedStringTable) {
        var value = cell.InnerText ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString
            && int.TryParse(value, out var sharedStringIndex)
            && sharedStringTable != null
            && sharedStringIndex >= 0
            && sharedStringIndex < sharedStringTable.ChildElements.Count) {
            return sharedStringTable.Elements<SharedStringItem>().ElementAt(sharedStringIndex).InnerText;
        }

        return value;
    }

    private static uint GetColumnIndex(string cellReference) {
        uint columnIndex = 0;

        foreach (var ch in cellReference) {
            if (!char.IsLetter(ch)) {
                break;
            }

            columnIndex = (columnIndex * 26) + (uint)(char.ToUpperInvariant(ch) - 'A' + 1);
        }

        return columnIndex;
    }

    private static double? RoundMetricValue(MetricType metric, double? value) {
        if (!value.HasValue) {
            return null;
        }

        var decimals = metric == MetricType.IsSafe
            ? 0
            : Math.Clamp(MetricDisplaySettingsStore.GetSettingOrDefault(metric).Decimals, 0, 10);

        return Math.Round(value.Value, decimals, MidpointRounding.AwayFromZero);
    }

    private static string GetAggregatedColumnName(MetricAggregation aggregation) {
        if (!string.IsNullOrWhiteSpace(aggregation.Label)) {
            return aggregation.Label;
        }

        return $"{aggregation.Metric.GetDisplayName()} ({aggregation.Function})";
    }

    private static DateTime EnsureUtc(DateTime value) => value.Kind switch {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    #endregion Private Methods

    #region Private Classes

    private sealed class ProgressStream(Stream inner, long estimatedTotal, Action<int> onProgress) : Stream {

        private readonly Stream _inner = inner;
        private readonly long _estimatedTotal = Math.Max(estimatedTotal, 1);
        private readonly Action<int> _onProgress = onProgress;
        private long _bytesWritten;
        private int _lastReported = -1;

        public void SaveAs(Dictionary<string, object> sheets) {
            MiniExcel.SaveAs(this, sheets);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            _inner.Write(buffer, offset, count);
            _bytesWritten += count;
            ReportProgress();
        }

        public override void Write(ReadOnlySpan<byte> buffer) {
            _inner.Write(buffer);
            _bytesWritten += buffer.Length;
            ReportProgress();
        }

        public override void WriteByte(byte value) {
            _inner.WriteByte(value);
            _bytesWritten++;
            ReportProgress();
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
            _bytesWritten += count;
            var task = _inner.WriteAsync(buffer, offset, count, cancellationToken);
            ReportProgress();
            return task;
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) {
            _bytesWritten += buffer.Length;
            var task = _inner.WriteAsync(buffer, cancellationToken);
            ReportProgress();
            return task;
        }

        private void ReportProgress() {
            var pct = (int)Math.Min(_bytesWritten * 100 / _estimatedTotal, 99);
            if (pct != _lastReported) {
                _lastReported = pct;
                _onProgress(pct);
            }
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => _inner.CanSeek;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;

        public override long Position {
            get => _inner.Position;
            set => _inner.Position = value;
        }

        public override void Flush() => _inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);
        public override void SetLength(long value) => _inner.SetLength(value);

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _inner.Flush();
            }

            base.Dispose(disposing);
        }
    }

    #endregion Private Classes
}
