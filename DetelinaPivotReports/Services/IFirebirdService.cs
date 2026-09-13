using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public interface IFirebirdService
{
    Task<(bool Success, string Message, string? ServerVersion)> TestConnectionAsync(DatabaseSettings settings, CancellationToken ct = default);
    Task<List<PlugroupItem>> GetPlugroupsAsync(DatabaseSettings settings, CancellationToken ct = default);
    Task<List<TerminalItem>> GetTerminalsAsync(DatabaseSettings settings, Dictionary<string, string> terminalNames, CancellationToken ct = default);
    Task<List<ArticleSaleRecord>> GetSalesRecordsAsync(DatabaseSettings settings, ReportFilter filter, CancellationToken ct = default);
}
