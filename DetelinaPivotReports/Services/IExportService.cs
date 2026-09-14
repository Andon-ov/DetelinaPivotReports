using System.Threading;
using System.Threading.Tasks;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public interface IExportService
{
    Task ExportToExcelAsync(PivotReportResult report, string filePath, CancellationToken ct = default);
    Task ExportToCsvAsync(PivotReportResult report, string filePath, CancellationToken ct = default);
    string CopyToClipboardFormat(PivotReportResult report);

    Task ExportDetailedToExcelAsync(List<DetailedSaleRecord> records, ReportFilter filter, string filePath, CancellationToken ct = default);
    Task ExportDetailedToCsvAsync(List<DetailedSaleRecord> records, ReportFilter filter, string filePath, CancellationToken ct = default);
    string CopyDetailedToClipboardFormat(List<DetailedSaleRecord> records);
}
