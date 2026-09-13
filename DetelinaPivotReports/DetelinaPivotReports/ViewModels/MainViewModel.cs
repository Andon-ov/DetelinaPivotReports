using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DetelinaPivotReports.Models;
using DetelinaPivotReports.Services;
using Microsoft.Win32;

namespace DetelinaPivotReports.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IFirebirdService _firebirdService;
    private readonly IPivotReportService _pivotReportService;
    private readonly IExportService _exportService;

    // Списъци
    private ObservableCollection<PlugroupItem> _plugroups = new();
    private PlugroupItem? _selectedPlugroup;
    private ObservableCollection<TerminalItem> _terminals = new();
    private TerminalItem? _selectedTerminal;

    // Филтри
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Today;
    private bool _includeSubgroups = true;
    private bool _hideEmptyDays = false;
    private string _searchText = string.Empty;

    // Състояние
    private bool _isLoading;
    private string _statusMessage = "Готов за работа";
    private bool _isConnected;
    private string _databasePathDisplay = string.Empty;
    private TimeSpan _lastQueryDuration;

    // Резултати
    private PivotReportResult? _pivotResult;
    private DataTable? _pivotDataTable;
    private DataView? _pivotDataView;
    private bool _hasReportData;

    // KPIs
    private decimal _kpiTotalQuantity;
    private int _kpiUniqueArticles;
    private int _kpiActiveDays;
    private string _kpiTopArticle = "—";
    private string _kpiPeakDate = "—";

    // Събитие за уведомяване на View-то за обновяване на колоните в DataGrid
    public event Action<PivotReportResult>? ReportColumnsGenerated;

    #region Properties

    public ObservableCollection<PlugroupItem> Plugroups
    {
        get => _plugroups;
        set => SetProperty(ref _plugroups, value);
    }

    public PlugroupItem? SelectedPlugroup
    {
        get => _selectedPlugroup;
        set => SetProperty(ref _selectedPlugroup, value);
    }

    public ObservableCollection<TerminalItem> Terminals
    {
        get => _terminals;
        set => SetProperty(ref _terminals, value);
    }

    public TerminalItem? SelectedTerminal
    {
        get => _selectedTerminal;
        set => SetProperty(ref _selectedTerminal, value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public bool IncludeSubgroups
    {
        get => _includeSubgroups;
        set => SetProperty(ref _includeSubgroups, value);
    }

    public bool HideEmptyDays
    {
        get => _hideEmptyDays;
        set => SetProperty(ref _hideEmptyDays, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplySearchFilter();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => SetProperty(ref _isConnected, value);
    }

    public string DatabasePathDisplay
    {
        get => _databasePathDisplay;
        set => SetProperty(ref _databasePathDisplay, value);
    }

    public DataView? PivotDataView
    {
        get => _pivotDataView;
        set => SetProperty(ref _pivotDataView, value);
    }

    public bool HasReportData
    {
        get => _hasReportData;
        set => SetProperty(ref _hasReportData, value);
    }

    public decimal KpiTotalQuantity
    {
        get => _kpiTotalQuantity;
        set => SetProperty(ref _kpiTotalQuantity, value);
    }

    public int KpiUniqueArticles
    {
        get => _kpiUniqueArticles;
        set => SetProperty(ref _kpiUniqueArticles, value);
    }

    public int KpiActiveDays
    {
        get => _kpiActiveDays;
        set => SetProperty(ref _kpiActiveDays, value);
    }

    public string KpiTopArticle
    {
        get => _kpiTopArticle;
        set => SetProperty(ref _kpiTopArticle, value);
    }

    public string KpiPeakDate
    {
        get => _kpiPeakDate;
        set => SetProperty(ref _kpiPeakDate, value);
    }

    #endregion

    #region Commands

    public ICommand LoadReportCommand { get; }
    public ICommand QuickPeriodCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand CopyClipboardCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand RefreshMetadataCommand { get; }
    public ICommand ClearSearchCommand { get; }

    #endregion

    public MainViewModel(
        IConfigService configService,
        IFirebirdService firebirdService,
        IPivotReportService pivotReportService,
        IExportService exportService)
    {
        _configService = configService;
        _firebirdService = firebirdService;
        _pivotReportService = pivotReportService;
        _exportService = exportService;

        _includeSubgroups = _configService.IncludeSubgroupsDefault;
        _hideEmptyDays = _configService.HideEmptyDaysDefault;

        // Начален период
        ApplyPeriodPreset(_configService.DefaultPeriodPreset);

        // Инициализация на команди
        LoadReportCommand = new RelayCommand(async () => await LoadReportAsync(), () => !IsLoading);
        QuickPeriodCommand = new RelayCommand(param => ApplyPeriodPreset(param?.ToString() ?? "Today"));
        ExportExcelCommand = new RelayCommand(async () => await ExportToExcelAsync(), () => HasReportData && !IsLoading);
        ExportCsvCommand = new RelayCommand(async () => await ExportToCsvAsync(), () => HasReportData && !IsLoading);
        CopyClipboardCommand = new RelayCommand(CopyClipboard, () => HasReportData && !IsLoading);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        RefreshMetadataCommand = new RelayCommand(async () => await InitializeDatabaseAsync(), () => !IsLoading);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);
    }

    public async Task InitializeDatabaseAsync()
    {
        IsLoading = true;
        StatusMessage = "Свързване към Firebird база данни...";
        DatabasePathDisplay = $"{_configService.DatabaseSettings.Host}:{_configService.DatabaseSettings.Port} [{_configService.DatabaseSettings.Database}]";

        try
        {
            var test = await _firebirdService.TestConnectionAsync(_configService.DatabaseSettings);
            IsConnected = test.Success;

            if (!test.Success)
            {
                StatusMessage = $"Няма връзка: {test.Message}";
                return;
            }

            StatusMessage = "Зареждане на номенклатури...";

            // Зареждане на артикулни групи (училища)
            var groups = await _firebirdService.GetPlugroupsAsync(_configService.DatabaseSettings);
            Plugroups = new ObservableCollection<PlugroupItem>(groups);
            SelectedPlugroup = Plugroups.FirstOrDefault();

            // Зареждане на терминали
            var terms = await _firebirdService.GetTerminalsAsync(_configService.DatabaseSettings, _configService.TerminalNames);
            Terminals = new ObservableCollection<TerminalItem>(terms);
            SelectedTerminal = Terminals.FirstOrDefault();

            StatusMessage = $"Свързан към Firebird ({test.ServerVersion}). Намерени {groups.Count - 1} групи и {terms.Count - 1} терминала.";
        }
        catch (Exception ex)
        {
            IsConnected = false;
            StatusMessage = $"Грешка при инициализация: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadReportAsync()
    {
        if (StartDate > EndDate)
        {
            MessageBox.Show("Началната дата не може да бъде след крайната дата!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        StatusMessage = "Извличане на продажби от базата данни...";
        var sw = Stopwatch.StartNew();

        try
        {
            // Подготовка на филтъра
            var filter = new ReportFilter
            {
                GroupId = SelectedPlugroup?.Id ?? 0,
                GroupName = SelectedPlugroup?.Name ?? "Всички групи",
                IncludeSubgroups = IncludeSubgroups,
                TerminalId = SelectedTerminal?.Id ?? 0,
                TerminalName = SelectedTerminal?.DisplayName ?? "Всички терминали",
                StartDate = StartDate,
                EndDate = EndDate,
                HideEmptyDays = HideEmptyDays,
                SearchText = SearchText
            };

            // При избор на конкретна група и включени подгрупи, извличаме всички потомствени ID-та
            if (filter.GroupId > 0 && filter.IncludeSubgroups)
            {
                filter.GroupIds = _pivotReportService.GetGroupAndDescendantIds(filter.GroupId, Plugroups);
            }

            // Извличане на записите от Firebird
            var records = await _firebirdService.GetSalesRecordsAsync(_configService.DatabaseSettings, filter);

            // Построяване на крос-таблицата
            _pivotResult = _pivotReportService.BuildPivotReport(records, filter);
            sw.Stop();
            _lastQueryDuration = sw.Elapsed;
            _pivotResult.ExecutionTime = _lastQueryDuration;

            // Генериране на DataTable
            _pivotDataTable = _pivotResult.ToDataTable(SearchText);
            PivotDataView = _pivotDataTable.DefaultView;
            HasReportData = _pivotResult.Rows.Count > 0;

            // Актуализиране на KPIs
            KpiTotalQuantity = _pivotResult.GrandTotal;
            KpiUniqueArticles = _pivotResult.TotalArticlesCount;
            KpiActiveDays = _pivotResult.ActiveDaysCount;
            KpiTopArticle = _pivotResult.TopArticleQuantity > 0 
                ? $"{_pivotResult.TopArticleName} ({_pivotResult.TopArticleQuantity:#,##0.##} бр.)" 
                : "—";
            KpiPeakDate = _pivotResult.PeakDate.HasValue 
                ? $"{_pivotResult.PeakDate.Value:dd.MM.yyyy} ({_pivotResult.PeakDateQuantity:#,##0.##} бр.)" 
                : "—";

            // Уведомяване на View за пренареждане на динамичните колони в DataGrid
            ReportColumnsGenerated?.Invoke(_pivotResult);

            StatusMessage = $"Справката е генерирана за {sw.Elapsed.TotalSeconds:F2} сек. Намерени {_pivotResult.Rows.Count} артикула, общо {_pivotResult.GrandTotal:#,##0.##} бр.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Грешка при справката: {ex.Message}";
            MessageBox.Show($"Възникна грешка при извличане на данните:\n\n{ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void ApplyPeriodPreset(string preset)
    {
        DateTime today = DateTime.Today;

        switch (preset)
        {
            case "Today":
                StartDate = today;
                EndDate = today;
                break;
            case "Yesterday":
                StartDate = today.AddDays(-1);
                EndDate = today.AddDays(-1);
                break;
            case "10Days":
                StartDate = today.AddDays(-9);
                EndDate = today;
                break;
            case "30Days":
                StartDate = today.AddDays(-29);
                EndDate = today;
                break;
            case "50Days":
                StartDate = today.AddDays(-49);
                EndDate = today;
                break;
            case "ThisMonth":
                StartDate = new DateTime(today.Year, today.Month, 1);
                EndDate = today;
                break;
        }
    }

    private void ApplySearchFilter()
    {
        if (_pivotDataTable == null || _pivotDataView == null) return;

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            _pivotDataView.RowFilter = string.Empty;
            return;
        }

        try
        {
            string term = SearchText.Trim()
                .Replace("'", "''")
                .Replace("[", "[[]")
                .Replace("%", "[%]")
                .Replace("*", "[*]");

            _pivotDataView.RowFilter = $"Convert(PLU_CODE, 'System.String') LIKE '%{term}%' OR PLU_NAME LIKE '%{term}%'";
        }
        catch
        {
            _pivotDataView.RowFilter = string.Empty;
        }
    }

    private async Task ExportToExcelAsync()
    {
        if (_pivotResult == null) return;

        var sfd = new SaveFileDialog
        {
            Title = "Експорт в Microsoft Excel",
            Filter = "Excel таблица (*.xlsx)|*.xlsx",
            FileName = $"Справка_{_pivotResult.Filter.GroupName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Експортиране в Excel...";
                await _exportService.ExportToExcelAsync(_pivotResult, sfd.FileName);
                StatusMessage = $"Успешен експорт в {Path.GetFileName(sfd.FileName)}";

                var res = MessageBox.Show($"Справката беше записана успешно:\n{sfd.FileName}\n\nЖелаете ли да я отворите веднага?",
                    "Успешен експорт", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при експорт в Excel:\n{ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusMessage = "Грешка при експорт в Excel";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private async Task ExportToCsvAsync()
    {
        if (_pivotResult == null) return;

        var sfd = new SaveFileDialog
        {
            Title = "Експорт в CSV файл",
            Filter = "CSV файл (*.csv)|*.csv",
            FileName = $"Справка_{_pivotResult.Filter.GroupName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Експортиране в CSV...";
                await _exportService.ExportToCsvAsync(_pivotResult, sfd.FileName);
                StatusMessage = $"Успешен експорт в {Path.GetFileName(sfd.FileName)}";
                MessageBox.Show($"CSV файлът е записан успешно:\n{sfd.FileName}", "Успешен експорт", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Грешка при експорт в CSV:\n{ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    private void CopyClipboard()
    {
        if (_pivotResult == null) return;

        try
        {
            string tsv = _exportService.CopyToClipboardFormat(_pivotResult);
            Clipboard.SetText(tsv);
            StatusMessage = "Таблицата е копирана в клипборда! Можете да я поставите директно в Excel.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Грешка при копиране: {ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenSettings()
    {
        var settingsVm = new SettingsViewModel(_configService, _firebirdService);
        var win = new Views.SettingsWindow { DataContext = settingsVm, Owner = Application.Current.MainWindow };

        if (win.ShowDialog() == true || settingsVm.IsSaved)
        {
            // Обновяваме базата при промяна на настройките
            _ = InitializeDatabaseAsync();
        }
    }
}
