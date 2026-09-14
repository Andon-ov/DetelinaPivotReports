using System;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Запис за детайлна справка продажби по терминали и бонове.
/// </summary>
public class DetailedSaleRecord
{
    public int TerminalId { get; set; }
    public string TerminalName { get; set; } = string.Empty;
    public long BonNumber { get; set; }
    public DateTime SaleDateTime { get; set; }
    public decimal BonTotal { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int PluNumber { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal RowTotal { get; set; }
    public long SellId { get; set; }
}
