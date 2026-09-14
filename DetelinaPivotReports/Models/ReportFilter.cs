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
    public TimeSpan StartTime { get; set; } = new TimeSpan(0, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(23, 59, 59);

    public DateTime EffectiveStartDateTime => StartDate.Date + StartTime;
    public DateTime EffectiveEndDateTime => EndDate.Date + EndTime;

    public bool HideEmptyDays { get; set; } = false;
    public string SearchText { get; set; } = string.Empty;
}
