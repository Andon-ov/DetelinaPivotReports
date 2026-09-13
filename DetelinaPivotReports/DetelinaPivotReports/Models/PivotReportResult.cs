using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Резултат от крос-табличната матрица за текстил и униформи (Размери × Модели).
/// </summary>
public class PivotReportResult
{
    public ReportFilter Filter { get; set; } = new();

    /// <summary>
    /// Списък с уникалните модели / артикули (Колони в матрицата).
    /// </summary>
    public List<string> Models { get; set; } = new();

    /// <summary>
    /// Списък с редовете на матрицата (всеки ред представлява един Размер).
    /// </summary>
    public List<PivotRowItem> Rows { get; set; } = new();

    /// <summary>
    /// Обобщени суми по модели (колони) за целия период.
    /// </summary>
    public Dictionary<string, decimal> ModelTotals { get; set; } = new();

    /// <summary>
    /// Общо продадени бройки за всички модели и размери.
    /// </summary>
    public decimal GrandTotal { get; set; }

    public int TotalModelsCount => Models.Count;
    public int TotalSizesCount => Rows.Count;

    public string TopModelName { get; set; } = "—";
    public decimal TopModelQuantity { get; set; }

    public string TopSizeName { get; set; } = "—";
    public decimal TopSizeQuantity { get; set; }

    public DateTime? PeakDate { get; set; }
    public decimal PeakDateQuantity { get; set; }

    public TimeSpan ExecutionTime { get; set; }

    public decimal GetModelTotal(string modelName)
    {
        return ModelTotals.TryGetValue(modelName, out var sum) ? sum : 0m;
    }

    /// <summary>
    /// Връща уникален технически идентификатор на колоната за DataGrid Binding (напр. COL_0, COL_1...).
    /// </summary>
    public static string GetModelColumnKey(int index) => $"COL_{index}";

    /// <summary>
    /// Генерира DataTable за визуализация в WPF DataGrid.
    /// </summary>
    public DataTable ToDataTable(string? searchText = null)
    {
        var dt = new DataTable("UniformPivotReport");

        // 1. Фиксирана колона за Размер
        dt.Columns.Add("SIZE", typeof(string)).Caption = "Размер";
        dt.Columns.Add("SORT_ORDER", typeof(string));
        dt.Columns.Add("IS_TOTAL_ROW", typeof(bool));

        // 2. Динамични колони за всеки модел
        for (int i = 0; i < Models.Count; i++)
        {
            string colKey = GetModelColumnKey(i);
            var col = dt.Columns.Add(colKey, typeof(decimal));
            col.Caption = Models[i];
        }

        // 3. Крайна колона ОБЩО
        dt.Columns.Add("TOTAL", typeof(decimal)).Caption = "ОБЩО";

        // Филтриране при въведено търсене
        IEnumerable<PivotRowItem> filteredRows = Rows;
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            string term = searchText.Trim().ToLowerInvariant();
            filteredRows = Rows.Where(r => 
                r.Size.ToLowerInvariant().Contains(term) ||
                Models.Any(m => m.ToLowerInvariant().Contains(term) && r.GetQuantity(m) > 0));
        }

        // 4. Попълване на редовете с размери
        foreach (var item in filteredRows)
        {
            var row = dt.NewRow();
            row["SIZE"] = item.Size;
            row["SORT_ORDER"] = item.SortKey;
            row["IS_TOTAL_ROW"] = false;

            for (int i = 0; i < Models.Count; i++)
            {
                row[GetModelColumnKey(i)] = item.GetQuantity(Models[i]);
            }

            row["TOTAL"] = item.TotalQuantity;
            dt.Rows.Add(row);
        }

        // 5. Сумиращ ред най-долу (ОБЩО ЗА МОДЕЛА)
        if (Rows.Count > 0)
        {
            var totalRow = dt.NewRow();
            totalRow["SIZE"] = "ОБЩО:";
            totalRow["SORT_ORDER"] = "9_99_ZZZ";
            totalRow["IS_TOTAL_ROW"] = true;

            for (int i = 0; i < Models.Count; i++)
            {
                totalRow[GetModelColumnKey(i)] = GetModelTotal(Models[i]);
            }

            totalRow["TOTAL"] = GrandTotal;
            dt.Rows.Add(totalRow);
        }

        return dt;
    }
}
