using System.Collections.Generic;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Ред от матрицата за текстил и униформи, представляващ конкретен размер (напр. 116, 122, S, M, L...).
/// </summary>
public class PivotRowItem
{
    public string Size { get; set; } = string.Empty;
    public string SortKey { get; set; } = string.Empty;
    public Dictionary<string, decimal> ModelQuantities { get; set; } = new();
    public decimal TotalQuantity { get; set; }

    public decimal GetQuantity(string modelName)
    {
        return ModelQuantities.TryGetValue(modelName, out var qty) ? qty : 0m;
    }
}
