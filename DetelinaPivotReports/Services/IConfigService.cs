using System.Collections.Generic;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public interface IConfigService
{
    DatabaseSettings DatabaseSettings { get; }
    Dictionary<string, string> TerminalNames { get; }
    bool HideEmptyDaysDefault { get; }
    bool IncludeSubgroupsDefault { get; }
    string DefaultPeriodPreset { get; }

    void LoadConfiguration();
    void SaveDatabaseSettings(DatabaseSettings settings);
}
