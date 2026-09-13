using System;
using System.Collections.Generic;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Ред от крос-таблицата, представляващ конкретен артикул.
/// </summary>
public class PivotRowItem
{
    public int PluNumber { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public Dictionary<DateTime, decimal> DailyQuantities { get; set; } = new();
    public decimal TotalQuantity { get; set; }

    public decimal GetQuantity(DateTime date)
    {
        return DailyQuantities.TryGetValue(date.Date, out var qty) ? qty : 0m;
    }
}
