namespace DetelinaPivotReports.Models;

/// <summary>
/// Артикулна група / Училище от таблица N_PLUGROUPS.
/// </summary>
public class PlugroupItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ParentId { get; set; }
    public int Code { get; set; }
    public int Level { get; set; } = 0;

    public bool IsAllGroups => Id == 0;

    public string DisplayName
    {
        get
        {
            if (IsAllGroups) return "— Всички училища и групи —";
            string indent = Level > 0 ? new string(' ', Level * 3) + "↳ " : string.Empty;
            return Code > 0 ? $"{indent}[{Code}] {Name}" : $"{indent}{Name}";
        }
    }

    public override string ToString() => DisplayName;

    public static PlugroupItem CreateAllGroupsOption()
    {
        return new PlugroupItem
        {
            Id = 0,
            Name = "— Всички училища и групи —",
            ParentId = -1,
            Code = 0,
            Level = 0
        };
    }
}
