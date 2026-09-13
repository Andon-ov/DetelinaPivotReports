using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Резултат от крос-табличната справка (Pivot Matrix).
/// </summary>
public class PivotReportResult
{
    public ReportFilter Filter { get; set; } = new();
    public List<DateTime> Dates { get; set; } = new();
    public List<PivotRowItem> Rows { get; set; } = new();
    public Dictionary<DateTime, decimal> DailyTotals { get; set; } = new();
    public decimal GrandTotal { get; set; }

    public int TotalArticlesCount => Rows.Count;
    public int ActiveDaysCount => Dates.Count;

    public string TopArticleName { get; set; } = "-";
    public decimal TopArticleQuantity { get; set; }

    public DateTime? PeakDate { get; set; }
    public decimal PeakDateQuantity { get; set; }

    public TimeSpan ExecutionTime { get; set; }

    public decimal GetDailyTotal(DateTime date)
    {
        return DailyTotals.TryGetValue(date.Date, out var sum) ? sum : 0m;
    }

    /// <summary>
    /// Генерира DataTable с динамични колони за визуализация в WPF DataGrid.
    /// </summary>
    public DataTable ToDataTable(string? searchText = null)
    {
        var dt = new DataTable("PivotReport");

        // Основни колони
        dt.Columns.Add("PLU_CODE", typeof(int)).Caption = "Код";
        dt.Columns.Add("PLU_NAME", typeof(string)).Caption = "Артикул";

        // Динамични колони за всяка дата от периода
        foreach (var date in Dates)
        {
            string colName = $"D_{date:yyyyMMdd}";
            string dayAbbr = GetBgDayAbbr(date.DayOfWeek);
            var col = dt.Columns.Add(colName, typeof(decimal));
            col.Caption = $"{date:dd.MM}\n({dayAbbr})";
        }

        // Общо за реда
        dt.Columns.Add("TOTAL", typeof(decimal)).Caption = "ОБЩО";

        // Филтриране по име или код при въведено търсене
        IEnumerable<PivotRowItem> filteredRows = Rows;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string term = searchText.Trim().ToLowerInvariant();
            filteredRows = Rows.Where(r => 
                r.PluNumber.ToString().Contains(term) || 
                r.ArticleName.ToLowerInvariant().Contains(term));
        }

        // Попълване на редовете
        foreach (var item in filteredRows)
        {
            var row = dt.NewRow();
            row["PLU_CODE"] = item.PluNumber;
            row["PLU_NAME"] = item.ArticleName;

            foreach (var date in Dates)
            {
                row[$"D_{date:yyyyMMdd}"] = item.GetQuantity(date);
            }

            row["TOTAL"] = item.TotalQuantity;
            dt.Rows.Add(row);
        }

        return dt;
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
