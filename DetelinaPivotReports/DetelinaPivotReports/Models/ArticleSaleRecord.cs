using System;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Извлечен запис за продаден артикул от SALES_PLUES и SALES_BON.
/// </summary>
public class ArticleSaleRecord
{
    public int PluNumber { get; set; }
    public string ArticleName { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public decimal SoldQuantity { get; set; }
    public DateTime SaleDateTime { get; set; }
    public DateTime SaleDate => SaleDateTime.Date;
    public int Terminal { get; set; }
}
