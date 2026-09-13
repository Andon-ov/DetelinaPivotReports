using System;
using System.Collections.Generic;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Параметри и филтри за генериране на крос-табличната справка.
/// </summary>
public class ReportFilter
{
    public int GroupId { get; set; } = 0;
    public string GroupName { get; set; } = "Всички училища и групи";
    public List<int> GroupIds { get; set; } = new();
    public bool IncludeSubgroups { get; set; } = true;

    public int TerminalId { get; set; } = 0;
    public string TerminalName { get; set; } = "Всички терминали";

    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;

    public bool HideEmptyDays { get; set; } = false;
    public string SearchText { get; set; } = string.Empty;
}
