using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public class ExportService : IExportService
{
    public async Task ExportToExcelAsync(PivotReportResult report, string filePath, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Крос-таблична справка");

            // 1. Заглавна част
            ws.Cell("A1").Value = "ОБОБЩЕНА КРОС-ТАБЛИЧНА СПРАВКА ПО ДНИ";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 16;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1F4E79");

            ws.Cell("A2").Value = $"Училище / Група: {report.Filter.GroupName}";
            ws.Cell("A2").Style.Font.Bold = true;

            string termInfo = report.Filter.TerminalId > 0 
                ? $"Терминал: {report.Filter.TerminalName}" 
                : "Всички терминали";
            ws.Cell("A3").Value = $"Период: {report.Filter.StartDate:dd.MM.yyyy} — {report.Filter.EndDate:dd.MM.yyyy}  |  {termInfo}";
            ws.Cell("A4").Value = $"Генерирана на: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            ws.Cell("A4").Style.Font.FontColor = XLColor.Gray;

            int headerRow = 6;
            int colIndex = 1;

            // Заглавия на колоните
            ws.Cell(headerRow, colIndex++).Value = "Код";
            ws.Cell(headerRow, colIndex++).Value = "Наименование на артикул";

            int firstDateCol = colIndex;
            foreach (var date in report.Dates)
            {
                var cell = ws.Cell(headerRow, colIndex++);
                cell.Value = $"{date:dd.MM}\n({GetBgDayAbbr(date.DayOfWeek)})";
                cell.Style.Alignment.WrapText = true;
            }
            int totalCol = colIndex;
            ws.Cell(headerRow, totalCol).Value = "ОБЩО";

            // Стилизиране на заглавния ред
            var headerRange = ws.Range(headerRow, 1, headerRow, totalCol);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(headerRow).Height = 28;

            // 2. Редове с данни
            int currentRow = headerRow + 1;
            foreach (var item in report.Rows)
            {
                ws.Cell(currentRow, 1).Value = item.PluNumber;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(currentRow, 2).Value = item.ArticleName;
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                int dCol = firstDateCol;
                foreach (var date in report.Dates)
                {
                    decimal qty = item.GetQuantity(date);
                    var cell = ws.Cell(currentRow, dCol++);
                    if (qty != 0)
                    {
                        cell.Value = qty;
                        cell.Style.NumberFormat.Format = "#,##0.##";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                    }
                    else
                    {
                        cell.Value = "-";
                        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                        cell.Style.Font.FontColor = XLColor.LightGray;
                    }
                }

                var totCell = ws.Cell(currentRow, totalCol);
                totCell.Value = item.TotalQuantity;
                totCell.Style.Font.Bold = true;
                totCell.Style.NumberFormat.Format = "#,##0.##";
                totCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F4F7");
                totCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                currentRow++;
            }

            // 3. Обобщаващ ред (Footer Totals)
            ws.Cell(currentRow, 1).Value = "";
            ws.Cell(currentRow, 2).Value = "ОБЩО ЗА ДЕНЯ:";
            ws.Cell(currentRow, 2).Style.Font.Bold = true;
            ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int totDCol = firstDateCol;
            foreach (var date in report.Dates)
            {
                decimal dTotal = report.GetDailyTotal(date);
                var cell = ws.Cell(currentRow, totDCol++);
                cell.Value = dTotal;
                cell.Style.Font.Bold = true;
                cell.Style.NumberFormat.Format = "#,##0.##";
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            }

            var grandCell = ws.Cell(currentRow, totalCol);
            grandCell.Value = report.GrandTotal;
            grandCell.Style.Font.Bold = true;
            grandCell.Style.NumberFormat.Format = "#,##0.##";
            grandCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
            grandCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            // Бордери на обобщаващия ред
            var footerRange = ws.Range(currentRow, 1, currentRow, totalCol);
            footerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            footerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9EDF4");

            // Бордери за цялата таблица
            var dataTableRange = ws.Range(headerRow, 1, currentRow, totalCol);
            dataTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataTableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E0E0E0");
            dataTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataTableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1F4E79");

            // Замразяване на заглавните колони (Код и Име) и ред
            ws.SheetView.FreezeRows(headerRow);
            ws.SheetView.FreezeColumns(2);

            // Автоматично оразмеряване на колоните
            ws.Columns(1, totalCol).AdjustToContents();
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 10);
            ws.Column(2).Width = Math.Min(Math.Max(ws.Column(2).Width, 30), 60);

            for (int c = firstDateCol; c <= totalCol; c++)
            {
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 12);
            }

            workbook.SaveAs(filePath);
        }, ct);
    }

    public async Task ExportToCsvAsync(PivotReportResult report, string filePath, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        // Заглавна част
        sb.AppendLine($"# Обобщена крос-таблична справка по дни");
        sb.AppendLine($"# Група:;{report.Filter.GroupName}");
        sb.AppendLine($"# Период:;{report.Filter.StartDate:dd.MM.yyyy};—;{report.Filter.EndDate:dd.MM.yyyy}");
        sb.AppendLine($"# Терминал:;{(report.Filter.TerminalId > 0 ? report.Filter.TerminalName : "Всички")}");
        sb.AppendLine($"# Генерирана:;{DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine();

        // Заглавен ред
        var headerCols = new List<string> { "Код", "Наименование на артикул" };
        foreach (var d in report.Dates)
        {
            headerCols.Add($"{d:dd.MM.yyyy} ({GetBgDayAbbr(d.DayOfWeek)})");
        }
        headerCols.Add("ОБЩО");
        sb.AppendLine(string.Join(";", headerCols));

        // Редове с артикули
        foreach (var item in report.Rows)
        {
            var rowCols = new List<string>
            {
                item.PluNumber.ToString(),
                $"\"{item.ArticleName.Replace("\"", "\"\"")}\""
            };

            foreach (var d in report.Dates)
            {
                decimal qty = item.GetQuantity(d);
                rowCols.Add(qty.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            }

            rowCols.Add(item.TotalQuantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine(string.Join(";", rowCols));
        }

        // Ред с общи суми
        var totalsRow = new List<string> { "", "ОБЩО ЗА ДЕНЯ:" };
        foreach (var d in report.Dates)
        {
            totalsRow.Add(report.GetDailyTotal(d).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        }
        totalsRow.Add(report.GrandTotal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        sb.AppendLine(string.Join(";", totalsRow));

        // Запис с UTF-8 BOM за коректно отваряне в Excel
        await Task.Run(() => File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true)), ct);
    }

    public string CopyToClipboardFormat(PivotReportResult report)
    {
        var sb = new StringBuilder();

        var headerCols = new List<string> { "Код", "Наименование на артикул" };
        foreach (var d in report.Dates)
        {
            headerCols.Add($"{d:dd.MM.yyyy}");
        }
        headerCols.Add("ОБЩО");
        sb.AppendLine(string.Join("\t", headerCols));

        foreach (var item in report.Rows)
        {
            var rowCols = new List<string>
            {
                item.PluNumber.ToString(),
                item.ArticleName
            };

            foreach (var d in report.Dates)
            {
                decimal qty = item.GetQuantity(d);
                rowCols.Add(qty != 0 ? qty.ToString("0.##") : "0");
            }

            rowCols.Add(item.TotalQuantity.ToString("0.##"));
            sb.AppendLine(string.Join("\t", rowCols));
        }

        var totalsRow = new List<string> { "", "ОБЩО ЗА ДЕНЯ:" };
        foreach (var d in report.Dates)
        {
            totalsRow.Add(report.GetDailyTotal(d).ToString("0.##"));
        }
        totalsRow.Add(report.GrandTotal.ToString("0.##"));
        sb.AppendLine(string.Join("\t", totalsRow));

        return sb.ToString();
    }

    private static string GetBgDayAbbr(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Пн",
        DayOfWeek.Tuesday => "Вт",
        DayOfWeek.Wednesday => "Ср",
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        DayOfWeek.Sunday => "Нд",
        _ => ""
    };
}
