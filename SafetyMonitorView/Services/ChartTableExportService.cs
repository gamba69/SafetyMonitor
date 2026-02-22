using DataStorage.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MiniExcelLibs;
using SafetyMonitorView.Models;
using SpreadsheetFont = DocumentFormat.OpenXml.Spreadsheet.Font;

namespace SafetyMonitorView.Services;

public class ChartTableExportService {

    #region Public Methods

    public void Export(string filePath, IReadOnlyList<MetricAggregation> metricAggregations, IReadOnlyList<ObservingData> aggregatedData, IReadOnlyList<ObservingData> rawData) {
        var aggregatedRows = BuildAggregatedRows(metricAggregations, aggregatedData);
        var rawRows = BuildRawRows(rawData);

        var sheets = new Dictionary<string, object> {
            ["Aggregated chart data"] = aggregatedRows,
            ["Full raw data"] = rawRows
        };

        MiniExcel.SaveAs(filePath, sheets, overwriteFile: true);
        ApplyWorkbookFormatting(filePath);
    }

    #endregion Public Methods

    #region Private Methods

    private static List<Dictionary<string, object?>> BuildAggregatedRows(
        IReadOnlyList<MetricAggregation> metricAggregations,
        IReadOnlyList<ObservingData> aggregatedData) {
        var rowsByTime = new SortedDictionary<DateTime, Dictionary<string, object?>>();

        foreach (var row in aggregatedData.OrderBy(x => x.Timestamp)) {
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
        }

        return [.. rowsByTime.Values];
    }

    private static List<Dictionary<string, object?>> BuildRawRows(IReadOnlyList<ObservingData> rawData) {
        var rows = new List<Dictionary<string, object?>>();

        foreach (var row in rawData.OrderBy(x => x.Timestamp)) {
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
        }

        return rows;
    }

    private static void ApplyWorkbookFormatting(string filePath) {
        using var document = SpreadsheetDocument.Open(filePath, true);
        var workbookPart = document.WorkbookPart;
        if (workbookPart == null) {
            return;
        }

        var (headerBoldStyleIndex, dateTimeStyleIndex, noBorderStyleIndex) = EnsureStyles(workbookPart);

        foreach (var worksheetPart in workbookPart.WorksheetParts) {
            var worksheet = worksheetPart.Worksheet;
            RemoveAutoFilter(worksheet);
            FreezeHeaderRow(worksheet);
            ApplyCellStyles(worksheet, headerBoldStyleIndex, dateTimeStyleIndex, noBorderStyleIndex);
            AutoFitColumns(worksheet, workbookPart.SharedStringTablePart?.SharedStringTable);
            worksheet.Save();
        }
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
}
