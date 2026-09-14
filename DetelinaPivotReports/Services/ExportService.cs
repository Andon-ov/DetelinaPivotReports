using System;
using System.Collections.Generic;
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
            var ws = workbook.Worksheets.Add("Матрица униформи");

            // 1. Заглавна част
            ws.Cell("A1").Value = "ОБОБЩЕНА МАТРИЧНА СПРАВКА ЗА ТЕКСТИЛ / УНИФОРМИ";
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

            // Заглавия на колоните: Колона 1 е "Размер", следват Моделите, накрая "ОБЩО"
            ws.Cell(headerRow, colIndex++).Value = "Размер";

            int firstModelCol = colIndex;
            foreach (var model in report.Models)
            {
                var cell = ws.Cell(headerRow, colIndex++);
                cell.Value = model;
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
            ws.Row(headerRow).Height = 32;

            // 2. Редове с данни (всеки ред е Размер)
            int currentRow = headerRow + 1;
            foreach (var item in report.Rows)
            {
                // Колона 1: Размер
                ws.Cell(currentRow, 1).Value = item.Size;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(currentRow, 1).Style.Font.Bold = true;
                ws.Cell(currentRow, 1).Style.Font.FontColor = XLColor.FromHtml("#1F4E79");

                // Стойности по модели
                int mCol = firstModelCol;
                foreach (var model in report.Models)
                {
                    decimal qty = item.GetQuantity(model);
                    var cell = ws.Cell(currentRow, mCol++);
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

                // Крайна колона ОБЩО за дадения размер
                var totCell = ws.Cell(currentRow, totalCol);
                totCell.Value = item.TotalQuantity;
                totCell.Style.Font.Bold = true;
                totCell.Style.NumberFormat.Format = "#,##0.##";
                totCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F2F4F7");
                totCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                currentRow++;
            }

            // 3. Обобщаващ ред най-долу (ОБЩО ЗА МОДЕЛА)
            ws.Cell(currentRow, 1).Value = "ОБЩО ЗА МОДЕЛА:";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;
            ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            int totMCol = firstModelCol;
            foreach (var model in report.Models)
            {
                decimal mTotal = report.GetModelTotal(model);
                var cell = ws.Cell(currentRow, totMCol++);
                cell.Value = mTotal;
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

            // Бордери и фонове на обобщаващия ред
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

            // Замразяване на заглавния ред и колоната с размери
            ws.SheetView.FreezeRows(headerRow);
            ws.SheetView.FreezeColumns(1);

            // Автоматично оразмеряване на колоните
            ws.Columns(1, totalCol).AdjustToContents();
            ws.Column(1).Width = Math.Max(ws.Column(1).Width, 14);

            for (int c = firstModelCol; c <= totalCol; c++)
            {
                ws.Column(c).Width = Math.Max(ws.Column(c).Width, 14);
            }

            workbook.SaveAs(filePath);
        }, ct);
    }

    public async Task ExportToCsvAsync(PivotReportResult report, string filePath, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        // Заглавна част
        sb.AppendLine($"# Обобщена матрична справка за текстил и униформи");
        sb.AppendLine($"# Група:;{report.Filter.GroupName}");
        sb.AppendLine($"# Период:;{report.Filter.StartDate:dd.MM.yyyy};—;{report.Filter.EndDate:dd.MM.yyyy}");
        sb.AppendLine($"# Терминал:;{(report.Filter.TerminalId > 0 ? report.Filter.TerminalName : "Всички")}");
        sb.AppendLine($"# Генерирана:;{DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine();

        // Заглавен ред
        var headerCols = new List<string> { "Размер" };
        foreach (var m in report.Models)
        {
            headerCols.Add($"\"{m.Replace("\"", "\"\"")}\"");
        }
        headerCols.Add("ОБЩО");
        sb.AppendLine(string.Join(";", headerCols));

        // Редове с размери
        foreach (var item in report.Rows)
        {
            var rowCols = new List<string>
            {
                $"\"{item.Size.Replace("\"", "\"\"")}\""
            };

            foreach (var m in report.Models)
            {
                decimal qty = item.GetQuantity(m);
                rowCols.Add(qty.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            }

            rowCols.Add(item.TotalQuantity.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
            sb.AppendLine(string.Join(";", rowCols));
        }

        // Ред с общи суми по модели най-долу
        var totalsRow = new List<string> { "ОБЩО ЗА МОДЕЛА:" };
        foreach (var m in report.Models)
        {
            totalsRow.Add(report.GetModelTotal(m).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        }
        totalsRow.Add(report.GrandTotal.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
        sb.AppendLine(string.Join(";", totalsRow));

        // Запис с UTF-8 BOM за коректно отваряне в Excel
        await Task.Run(() => File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true)), ct);
    }

    public string CopyToClipboardFormat(PivotReportResult report)
    {
        var sb = new StringBuilder();

        var headerCols = new List<string> { "Размер" };
        foreach (var m in report.Models)
        {
            headerCols.Add(m);
        }
        headerCols.Add("ОБЩО");
        sb.AppendLine(string.Join("\t", headerCols));

        foreach (var item in report.Rows)
        {
            var rowCols = new List<string>
            {
                item.Size
            };

            foreach (var m in report.Models)
            {
                decimal qty = item.GetQuantity(m);
                rowCols.Add(qty != 0 ? qty.ToString("0.##") : "0");
            }

            rowCols.Add(item.TotalQuantity.ToString("0.##"));
            sb.AppendLine(string.Join("\t", rowCols));
        }

        var totalsRow = new List<string> { "ОБЩО ЗА МОДЕЛА:" };
        foreach (var m in report.Models)
        {
            totalsRow.Add(report.GetModelTotal(m).ToString("0.##"));
        }
        totalsRow.Add(report.GrandTotal.ToString("0.##"));
        sb.AppendLine(string.Join("\t", totalsRow));

        return sb.ToString();
    }

    public async Task ExportDetailedToExcelAsync(List<DetailedSaleRecord> records, ReportFilter filter, string filePath, CancellationToken ct = default)
    {
        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Детайлни продажби");

            // 1. Заглавна част
            ws.Cell("A1").Value = "ДЕТАЙЛНА СПРАВКА ПРОДАЖБИ ПО ТЕРМИНАЛИ И БОНОВЕ";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 15;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromHtml("#1F4E79");

            ws.Cell("A2").Value = $"Училище / Група: {filter.GroupName}";
            ws.Cell("A2").Style.Font.Bold = true;

            string termInfo = filter.TerminalId > 0 
                ? $"Терминал: {filter.TerminalName}" 
                : "Всички терминали";
            ws.Cell("A3").Value = $"Период: {filter.StartDate:dd.MM.yyyy} — {filter.EndDate:dd.MM.yyyy}  |  {termInfo}";
            ws.Cell("A4").Value = $"Генерирана на: {DateTime.Now:dd.MM.yyyy HH:mm:ss}";
            ws.Cell("A4").Style.Font.FontColor = XLColor.Gray;

            int headerRow = 6;
            string[] headers = 
            {
                "Терминал", 
                "Бон No", 
                "Дата/Час", 
                "Тотал бон", 
                "Арт.група", 
                "Арт.No", 
                "Артикул", 
                "Количество", 
                "Ед.цена", 
                "Сума ред"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
            }

            var headerRange = ws.Range(headerRow, 1, headerRow, headers.Length);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#1F4E79");
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.Row(headerRow).Height = 28;

            int currentRow = headerRow + 1;
            decimal totalQuantity = 0m;
            decimal totalRowSum = 0m;

            foreach (var r in records)
            {
                totalQuantity += r.Quantity;
                totalRowSum += r.RowTotal;

                // 1. Терминал
                ws.Cell(currentRow, 1).Value = r.TerminalName;
                ws.Cell(currentRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 2. Бон No
                ws.Cell(currentRow, 2).Value = r.BonNumber;
                ws.Cell(currentRow, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 3. Дата/Час
                ws.Cell(currentRow, 3).Value = r.SaleDateTime.ToString("dd.MM.yyyy HH:mm:ss");
                ws.Cell(currentRow, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 4. Тотал бон
                var cellBonTot = ws.Cell(currentRow, 4);
                cellBonTot.Value = r.BonTotal;
                cellBonTot.Style.NumberFormat.Format = "#,##0.00";
                cellBonTot.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // 5. Арт.група
                ws.Cell(currentRow, 5).Value = r.GroupName;
                ws.Cell(currentRow, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // 6. Арт.No
                ws.Cell(currentRow, 6).Value = r.PluNumber;
                ws.Cell(currentRow, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // 7. Артикул
                ws.Cell(currentRow, 7).Value = r.ArticleName;
                ws.Cell(currentRow, 7).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                // 8. Количество
                var cellQty = ws.Cell(currentRow, 8);
                cellQty.Value = r.Quantity;
                cellQty.Style.NumberFormat.Format = "#,##0.000";
                cellQty.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // 9. Ед.цена
                var cellPrice = ws.Cell(currentRow, 9);
                cellPrice.Value = r.UnitPrice;
                cellPrice.Style.NumberFormat.Format = "#,##0.00";
                cellPrice.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                // 10. Сума ред
                var cellRowSum = ws.Cell(currentRow, 10);
                cellRowSum.Value = r.RowTotal;
                cellRowSum.Style.NumberFormat.Format = "#,##0.00";
                cellRowSum.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                currentRow++;
            }

            // Сумиращ ред най-долу
            ws.Cell(currentRow, 1).Value = "ОБЩО:";
            ws.Cell(currentRow, 1).Style.Font.Bold = true;

            var qtyTotalCell = ws.Cell(currentRow, 8);
            qtyTotalCell.Value = totalQuantity;
            qtyTotalCell.Style.Font.Bold = true;
            qtyTotalCell.Style.NumberFormat.Format = "#,##0.000";
            qtyTotalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var sumTotalCell = ws.Cell(currentRow, 10);
            sumTotalCell.Value = totalRowSum;
            sumTotalCell.Style.Font.Bold = true;
            sumTotalCell.Style.NumberFormat.Format = "#,##0.00";
            sumTotalCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var footerRange = ws.Range(currentRow, 1, currentRow, headers.Length);
            footerRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            footerRange.Style.Border.BottomBorder = XLBorderStyleValues.Double;
            footerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#E9EDF4");

            // Бордери за цялата таблица
            var dataTableRange = ws.Range(headerRow, 1, currentRow, headers.Length);
            dataTableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            dataTableRange.Style.Border.InsideBorderColor = XLColor.FromHtml("#E0E0E0");
            dataTableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            dataTableRange.Style.Border.OutsideBorderColor = XLColor.FromHtml("#1F4E79");

            ws.SheetView.FreezeRows(headerRow);
            ws.Columns(1, headers.Length).AdjustToContents();

            workbook.SaveAs(filePath);
        }, ct);
    }

    public async Task ExportDetailedToCsvAsync(List<DetailedSaleRecord> records, ReportFilter filter, string filePath, CancellationToken ct = default)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# Детайлна справка продажби по терминали и бонове");
        sb.AppendLine($"# Група:;{filter.GroupName}");
        sb.AppendLine($"# Период:;{filter.StartDate:dd.MM.yyyy};—;{filter.EndDate:dd.MM.yyyy}");
        sb.AppendLine($"# Терминал:;{(filter.TerminalId > 0 ? filter.TerminalName : "Всички")}");
        sb.AppendLine($"# Генерирана:;{DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine();

        sb.AppendLine("Терминал;Бон No;Дата/Час;Тотал бон;Арт.група;Арт.No;Артикул;Количество;Ед.цена;Сума ред");

        decimal totalQuantity = 0m;
        decimal totalRowSum = 0m;

        foreach (var r in records)
        {
            totalQuantity += r.Quantity;
            totalRowSum += r.RowTotal;

            var cols = new List<string>
            {
                $"\"{r.TerminalName.Replace("\"", "\"\"")}\"",
                r.BonNumber.ToString(),
                r.SaleDateTime.ToString("dd.MM.yyyy HH:mm:ss"),
                r.BonTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                $"\"{r.GroupName.Replace("\"", "\"\"")}\"",
                r.PluNumber.ToString(),
                $"\"{r.ArticleName.Replace("\"", "\"\"")}\"",
                r.Quantity.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                r.UnitPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                r.RowTotal.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
            };

            sb.AppendLine(string.Join(";", cols));
        }

        sb.AppendLine($"ОБЩО;;;;;;;{totalQuantity.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture)};;{totalRowSum.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}");

        await Task.Run(() => File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true)), ct);
    }

    public string CopyDetailedToClipboardFormat(List<DetailedSaleRecord> records)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Терминал\tБон No\tДата/Час\tТотал бон\tАрт.група\tАрт.No\tАртикул\tКоличество\tЕд.цена\tСума ред");

        decimal totalQuantity = 0m;
        decimal totalRowSum = 0m;

        foreach (var r in records)
        {
            totalQuantity += r.Quantity;
            totalRowSum += r.RowTotal;

            var cols = new List<string>
            {
                r.TerminalName,
                r.BonNumber.ToString(),
                r.SaleDateTime.ToString("dd.MM.yyyy HH:mm:ss"),
                r.BonTotal.ToString("0.00"),
                r.GroupName,
                r.PluNumber.ToString(),
                r.ArticleName,
                r.Quantity.ToString("0.000"),
                r.UnitPrice.ToString("0.00"),
                r.RowTotal.ToString("0.00")
            };

            sb.AppendLine(string.Join("\t", cols));
        }

        sb.AppendLine($"ОБЩО\t\t\t\t\t\t\t{totalQuantity:0.000}\t\t{totalRowSum:0.00}");

        return sb.ToString();
    }
}
