namespace DetelinaPivotReports.Models;

/// <summary>
/// POS терминал / обект от поле SELL_TERMINAL в SALES_BON.
/// </summary>
public class TerminalItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public bool IsAllTerminals => Id == 0;

    public string DisplayName => IsAllTerminals 
        ? "— Всички терминали / обекти —" 
        : (!string.IsNullOrWhiteSpace(Name) ? $"Терминал {Id} ({Name})" : $"Терминал {Id}");

    public override string ToString() => DisplayName;

    public static TerminalItem CreateAllTerminalsOption()
    {
        return new TerminalItem
        {
            Id = 0,
            Name = "Всички"
        };
    }
}
