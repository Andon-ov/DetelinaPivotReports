using System.Collections.Generic;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public interface IPivotReportService
{
    PivotReportResult BuildPivotReport(List<ArticleSaleRecord> records, ReportFilter filter);
}
