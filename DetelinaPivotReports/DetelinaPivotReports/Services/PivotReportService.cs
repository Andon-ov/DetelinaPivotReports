using System;
using System.Collections.Generic;
using System.Linq;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public class PivotReportService : IPivotReportService
{
    public PivotReportResult BuildPivotReport(List<ArticleSaleRecord> records, ReportFilter filter)
    {
        var result = new PivotReportResult
        {
            Filter = filter
        };

        // Определяне на списъка с дати за колоните
        var dateList = new List<DateTime>();
        if (filter.HideEmptyDays)
        {
            dateList = records
                .Select(r => r.SaleDate)
                .Distinct()
                .OrderBy(d => d)
                .ToList();
        }
        else
        {
            DateTime cur = filter.StartDate.Date;
            DateTime end = filter.EndDate.Date;
            while (cur <= end)
            {
                dateList.Add(cur);
                cur = cur.AddDays(1);
            }
        }

        result.Dates = dateList;

        // Групиране по артикул (PluNumber)
        var articleGroups = records
            .GroupBy(r => r.PluNumber)
            .OrderBy(g => g.First().ArticleName)
            .ToList();

        var rows = new List<PivotRowItem>();
        var dailyTotals = dateList.ToDictionary(d => d, _ => 0m);

        foreach (var grp in articleGroups)
        {
            int pluNumber = grp.Key;
            string articleName = grp.First().ArticleName;

            var dailyMap = new Dictionary<DateTime, decimal>();
            decimal rowTotal = 0m;

            foreach (var rec in grp)
            {
                DateTime d = rec.SaleDate;
                if (!dailyMap.ContainsKey(d))
                {
                    dailyMap[d] = 0m;
                }
                dailyMap[d] += rec.SoldQuantity;
                rowTotal += rec.SoldQuantity;

                if (dailyTotals.ContainsKey(d))
                {
                    dailyTotals[d] += rec.SoldQuantity;
                }
            }

            rows.Add(new PivotRowItem
            {
                PluNumber = pluNumber,
                ArticleName = articleName,
                DailyQuantities = dailyMap,
                TotalQuantity = rowTotal
            });
        }

        // Подреждане на редовете по общо продадено количество низходящо (или по име)
        result.Rows = rows.OrderByDescending(r => r.TotalQuantity).ThenBy(r => r.ArticleName).ToList();
        result.DailyTotals = dailyTotals;
        result.GrandTotal = dailyTotals.Values.Sum();

        // Изчисляване на KPIs
        if (result.Rows.Count > 0)
        {
            var topArticle = result.Rows.First();
            result.TopArticleName = $"{topArticle.ArticleName} (код {topArticle.PluNumber})";
            result.TopArticleQuantity = topArticle.TotalQuantity;
        }

        if (dailyTotals.Count > 0)
        {
            var peak = dailyTotals.OrderByDescending(kv => kv.Value).First();
            if (peak.Value > 0)
            {
                result.PeakDate = peak.Key;
                result.PeakDateQuantity = peak.Value;
            }
        }

        return result;
    }

    public List<int> GetGroupAndDescendantIds(int rootGroupId, IEnumerable<PlugroupItem> allGroups)
    {
        var result = new HashSet<int> { rootGroupId };
        var queue = new Queue<int>();
        queue.Enqueue(rootGroupId);

        var lookup = allGroups
            .Where(g => !g.IsAllGroups)
            .GroupBy(g => g.ParentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (lookup.TryGetValue(current, out var children))
            {
                foreach (var child in children)
                {
                    if (result.Add(child.Id))
                    {
                        queue.Enqueue(child.Id);
                    }
                }
            }
        }

        return result.ToList();
    }
}
