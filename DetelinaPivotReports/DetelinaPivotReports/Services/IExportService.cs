using System.Threading;
using System.Threading.Tasks;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public interface IExportService
{
    Task ExportToExcelAsync(PivotReportResult report, string filePath, CancellationToken ct = default);
    Task ExportToCsvAsync(PivotReportResult report, string filePath, CancellationToken ct = default);
    string CopyToClipboardFormat(PivotReportResult report);
}
