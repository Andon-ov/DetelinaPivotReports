using System;
using System.Collections.Generic;
using System.Linq;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

/// <summary>
/// Сервиз за построяване на крос-таблична матрица за текстил и униформи (Размери × Модели).
/// </summary>
public class PivotReportService : IPivotReportService
{
    public PivotReportResult BuildPivotReport(List<ArticleSaleRecord> records, ReportFilter filter)
    {
        var result = new PivotReportResult
        {
            Filter = filter
        };

        if (records == null || records.Count == 0)
        {
            return result;
        }

        // 1. Парсване на модел и размер за всеки извлечен запис
        foreach (var rec in records)
        {
            var (modelName, size) = UniformItemParser.Parse(rec.ArticleName);
            rec.ModelName = modelName;
            rec.Size = size;
        }

        // При въведено търсене, предварително филтрираме записите по име на модел или размер
        IEnumerable<ArticleSaleRecord> effectiveRecords = records;
        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            string term = filter.SearchText.Trim().ToLowerInvariant();
            effectiveRecords = records.Where(r => 
                r.ModelName.ToLowerInvariant().Contains(term) ||
                r.Size.ToLowerInvariant().Contains(term) ||
                r.ArticleName.ToLowerInvariant().Contains(term) ||
                r.PluNumber.ToString().Contains(term));
        }

        var effectiveList = effectiveRecords.ToList();
        if (effectiveList.Count == 0)
        {
            return result;
        }

        // 2. Извличане и сортиране на уникалните Модели (Колони)
        var modelTotals = effectiveList
            .GroupBy(r => r.ModelName)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.SoldQuantity));

        var models = modelTotals.Keys.OrderBy(m => m).ToList();
        result.Models = models;
        result.ModelTotals = modelTotals;
        result.GrandTotal = modelTotals.Values.Sum();

        // 3. Групиране по Размер (Редове)
        var sizeGroups = effectiveList.GroupBy(r => r.Size);
        var rows = new List<PivotRowItem>();

        foreach (var sGrp in sizeGroups)
        {
            string size = sGrp.Key;
            var modelMap = new Dictionary<string, decimal>();
            decimal sizeTotal = 0m;

            foreach (var rec in sGrp)
            {
                string m = rec.ModelName;
                if (!modelMap.ContainsKey(m))
                {
                    modelMap[m] = 0m;
                }
                modelMap[m] += rec.SoldQuantity;
                sizeTotal += rec.SoldQuantity;
            }

            // Опция "Скриване на празни": скрива размери с 0 общи продажби
            if (filter.HideEmptyDays && sizeTotal == 0)
                continue;

            rows.Add(new PivotRowItem
            {
                Size = size,
                SortKey = UniformItemParser.GetSortOrderKey(size),
                ModelQuantities = modelMap,
                TotalQuantity = sizeTotal
            });
        }

        // 4. Логическо сортиране на размерите: детски ръстове -> буквени XS-3XL -> универсален -> без размер
        result.Rows = rows.OrderBy(r => r.SortKey).ToList();

        // 5. Изчисляване на KPIs
        if (modelTotals.Count > 0)
        {
            var topModel = modelTotals.OrderByDescending(kv => kv.Value).First();
            if (topModel.Value > 0)
            {
                result.TopModelName = topModel.Key;
                result.TopModelQuantity = topModel.Value;
            }
        }

        if (result.Rows.Count > 0)
        {
            var topSize = result.Rows.OrderByDescending(r => r.TotalQuantity).First();
            if (topSize.TotalQuantity > 0)
            {
                result.TopSizeName = topSize.Size;
                result.TopSizeQuantity = topSize.TotalQuantity;
            }
        }

        var salesByDate = effectiveList
            .GroupBy(r => r.SaleDate)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.SoldQuantity));

        if (salesByDate.Count > 0)
        {
            var peak = salesByDate.OrderByDescending(kv => kv.Value).First();
            if (peak.Value > 0)
            {
                result.PeakDate = peak.Key;
                result.PeakDateQuantity = peak.Value;
            }
        }

        return result;
    }
}
