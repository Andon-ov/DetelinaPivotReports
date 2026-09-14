using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using DetelinaPivotReports.Models;
using DetelinaPivotReports.Services;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;

namespace DetelinaPivotReports.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IFirebirdService _firebirdService;
    private readonly IPivotReportService _pivotReportService;
    private readonly IExportService _exportService;

    // Избор на текуща справка (0: Матрична, 1: Детайлна)
    private int _selectedReportIndex;

    // Списъци
    private ObservableCollection<PlugroupItem> _plugroups = new();
    private PlugroupItem? _selectedPlugroup;
    private ObservableCollection<TerminalItem> _terminals = new();
    private TerminalItem? _selectedTerminal;

    // Филтри
    private DateTime _startDate = DateTime.Today;
    private DateTime _endDate = DateTime.Today;
    private string _startTime = "00:00";
    private string _endTime = "23:59";
    private bool _hideEmptyDays = false;
    private string _searchText = string.Empty;

    // Състояние
    private bool _isLoading;
    private string _statusMessage = "Готов за работа";
    private bool _isConnected;
    private string _databasePathDisplay = string.Empty;
    private TimeSpan _lastQueryDuration;

    // Резултати - Справка 1: Матрица униформи
    private PivotReportResult? _pivotResult;
    private DataTable? _pivotDataTable;
    private DataView? _pivotDataView;
    private bool _hasReportData;

    // KPIs - Справка 1
    private decimal _kpiTotalQuantity;
    private int _kpiUniqueArticles;
    private int _kpiActiveDays;
    private string _kpiTopArticle = "—";
    private string _kpiPeakDate = "—";

    // Резултати - Справка 2: Детайлна справка продажби по терминали и бонове
    private ObservableCollection<DetailedSaleRecord> _detailedRecords = new();
    private ICollectionView? _detailedDataView;
    private bool _hasDetailedData;
    private string _detailedSearchText = string.Empty;

    // KPIs - Справка 2
    private decimal _detailedKpiTotalQuantity;
    private decimal _detailedKpiTotalRowSum;
    private int _detailedKpiUniqueBonsCount;
    private int _detailedKpiTotalRowsCount;
    private decimal _detailedKpiTotalBonSum;

    // Резултати и графики - Справка 3: Графичен анализ и визуализация (Dashboard)
    private SeriesCollection _donutSeries = new();
    private SeriesCollection _schoolRankingSeries = new();
    private string[] _schoolRankingLabels = Array.Empty<string>();
    private bool _hasChartData;
    private string _kpiTopSchoolName = "—";

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
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                OnPropertyChanged(nameof(FormattedPeriodDisplay));
            }
        }
    }

    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
            {
                OnPropertyChanged(nameof(FormattedPeriodDisplay));
            }
        }
    }

    public string StartTime
    {
        get => _startTime;
        set
        {
            if (SetProperty(ref _startTime, value))
            {
                OnPropertyChanged(nameof(FormattedPeriodDisplay));
            }
        }
    }

    public string EndTime
    {
        get => _endTime;
        set
        {
            if (SetProperty(ref _endTime, value))
            {
                OnPropertyChanged(nameof(FormattedPeriodDisplay));
            }
        }
    }

    /// <summary>
    /// Текстово представяне на избрания времеви интервал за показване в UI и експорт.
    /// </summary>
    public string FormattedPeriodDisplay
    {
        get
        {
            bool isFullDay = (string.IsNullOrWhiteSpace(StartTime) || StartTime == "00:00" || StartTime == "00:00:00") &&
                             (string.IsNullOrWhiteSpace(EndTime) || EndTime == "23:59" || EndTime == "23:59:59");
            if (isFullDay)
            {
                return $"{StartDate:dd.MM.yyyy} — {EndDate:dd.MM.yyyy}";
            }
            return $"{StartDate:dd.MM.yyyy} {StartTime} — {EndDate:dd.MM.yyyy} {EndTime}";
        }
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

    // Свойства за Справка 2: Детайлни продажби
    public int SelectedReportIndex
    {
        get => _selectedReportIndex;
        set
        {
            if (SetProperty(ref _selectedReportIndex, value))
            {
                if (value == 2)
                {
                    if (_detailedRecords.Count > 0 && !_hasChartData)
                    {
                        UpdateChartData(_detailedRecords.ToList());
                    }
                    else if (_detailedRecords.Count == 0 && IsConnected && !IsLoading)
                    {
                        _ = LoadChartsReportAsync();
                    }
                }
            }
        }
    }

    public ObservableCollection<DetailedSaleRecord> DetailedRecords
    {
        get => _detailedRecords;
        set => SetProperty(ref _detailedRecords, value);
    }

    public ICollectionView? DetailedDataView
    {
        get => _detailedDataView;
        set => SetProperty(ref _detailedDataView, value);
    }

    public bool HasDetailedData
    {
        get => _hasDetailedData;
        set => SetProperty(ref _hasDetailedData, value);
    }

    public string DetailedSearchText
    {
        get => _detailedSearchText;
        set
        {
            if (SetProperty(ref _detailedSearchText, value))
            {
                _detailedDataView?.Refresh();
                var visible = _detailedDataView?.Cast<DetailedSaleRecord>().ToList() ?? new List<DetailedSaleRecord>();
                ProcessReceiptGrouping(visible);
                UpdateDetailedKpis();
            }
        }
    }

    public decimal DetailedKpiTotalQuantity
    {
        get => _detailedKpiTotalQuantity;
        set => SetProperty(ref _detailedKpiTotalQuantity, value);
    }

    public decimal DetailedKpiTotalRowSum
    {
        get => _detailedKpiTotalRowSum;
        set => SetProperty(ref _detailedKpiTotalRowSum, value);
    }

    public int DetailedKpiUniqueBonsCount
    {
        get => _detailedKpiUniqueBonsCount;
        set => SetProperty(ref _detailedKpiUniqueBonsCount, value);
    }

    public int DetailedKpiTotalRowsCount
    {
        get => _detailedKpiTotalRowsCount;
        set => SetProperty(ref _detailedKpiTotalRowsCount, value);
    }

    public decimal DetailedKpiTotalBonSum
    {
        get => _detailedKpiTotalBonSum;
        set => SetProperty(ref _detailedKpiTotalBonSum, value);
    }

    // Свойства за Справка 3: Графичен анализ и визуализация (Dashboard)
    public SeriesCollection DonutSeries
    {
        get => _donutSeries;
        set => SetProperty(ref _donutSeries, value);
    }

    public SeriesCollection SchoolRankingSeries
    {
        get => _schoolRankingSeries;
        set => SetProperty(ref _schoolRankingSeries, value);
    }

    public string[] SchoolRankingLabels
    {
        get => _schoolRankingLabels;
        set => SetProperty(ref _schoolRankingLabels, value);
    }

    public bool HasChartData
    {
        get => _hasChartData;
        set => SetProperty(ref _hasChartData, value);
    }

    public string KpiTopSchoolName
    {
        get => _kpiTopSchoolName;
        set => SetProperty(ref _kpiTopSchoolName, value);
    }

    public decimal DetailedKpiAverageBonSum => DetailedKpiUniqueBonsCount > 0 
        ? Math.Round(DetailedKpiTotalRowSum / DetailedKpiUniqueBonsCount, 2) 
        : 0m;

    public Func<double, string> CurrencyFormatter { get; } = val => $"{val:N0} €";
    public Func<double, string> QuantityFormatter { get; } = val => $"{val:N0} бр.";

    #endregion

    #region Commands

    // Обща команда за зареждане на текущата активна справка от горния панел
    public ICommand LoadActiveReportCommand { get; }

    public ICommand LoadReportCommand { get; }
    public ICommand QuickPeriodCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand CopyClipboardCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand RefreshMetadataCommand { get; }
    public ICommand ClearSearchCommand { get; }

    // Команди за Справка 2: Детайлни продажби
    public ICommand LoadDetailedReportCommand { get; }
    public ICommand ExportDetailedExcelCommand { get; }
    public ICommand ExportDetailedCsvCommand { get; }
    public ICommand CopyDetailedClipboardCommand { get; }
    public ICommand ClearDetailedSearchCommand { get; }

    // Команди за Справка 3: Графичен анализ
    public ICommand LoadChartsReportCommand { get; }

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

        _hideEmptyDays = _configService.HideEmptyDaysDefault;

        // Начален период
        ApplyPeriodPreset(_configService.DefaultPeriodPreset);

        // Обща команда за извличане
        LoadActiveReportCommand = new RelayCommand(async () => await LoadActiveReportAsync(), () => !IsLoading);

        // Инициализация на команди - Справка 1
        LoadReportCommand = new RelayCommand(async () => await LoadReportAsync(), () => !IsLoading);
        QuickPeriodCommand = new RelayCommand(param => ApplyPeriodPreset(param?.ToString() ?? "Today"));
        ExportExcelCommand = new RelayCommand(async () => await ExportToExcelAsync(), () => HasReportData && !IsLoading);
        ExportCsvCommand = new RelayCommand(async () => await ExportToCsvAsync(), () => HasReportData && !IsLoading);
        CopyClipboardCommand = new RelayCommand(CopyClipboard, () => HasReportData && !IsLoading);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        RefreshMetadataCommand = new RelayCommand(async () => await InitializeDatabaseAsync(), () => !IsLoading);
        ClearSearchCommand = new RelayCommand(() => SearchText = string.Empty);

        // Инициализация на команди - Справка 2
        LoadDetailedReportCommand = new RelayCommand(async () => await LoadDetailedReportAsync(), () => !IsLoading);
        ExportDetailedExcelCommand = new RelayCommand(async () => await ExportDetailedToExcelAsync(), () => HasDetailedData && !IsLoading);
        ExportDetailedCsvCommand = new RelayCommand(async () => await ExportDetailedToCsvAsync(), () => HasDetailedData && !IsLoading);
        CopyDetailedClipboardCommand = new RelayCommand(CopyDetailedClipboard, () => HasDetailedData && !IsLoading);
        ClearDetailedSearchCommand = new RelayCommand(() => DetailedSearchText = string.Empty);

        // Инициализация на команди - Справка 3
        LoadChartsReportCommand = new RelayCommand(async () => await LoadChartsReportAsync(), () => !IsLoading);
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

    /// <summary>
    /// Извлича справката за текущо избрания таб (0: Матрична, 1: Детайлна, 2: Графики)
    /// </summary>
    public async Task LoadActiveReportAsync()
    {
        if (SelectedReportIndex == 2)
        {
            await LoadChartsReportAsync();
        }
        else if (SelectedReportIndex == 1)
        {
            await LoadDetailedReportAsync();
        }
        else
        {
            await LoadReportAsync();
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
                GroupName = SelectedPlugroup?.Name ?? "[Всички училища]",
                TerminalId = SelectedTerminal?.Id ?? 0,
                TerminalName = SelectedTerminal?.DisplayName ?? "Всички терминали",
                StartDate = StartDate,
                EndDate = EndDate,
                StartTime = new TimeSpan(0, 0, 0),
                EndTime = new TimeSpan(23, 59, 59),
                HideEmptyDays = HideEmptyDays,
                SearchText = SearchText
            };

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
            KpiUniqueArticles = _pivotResult.TotalModelsCount;
            KpiActiveDays = _pivotResult.TotalSizesCount;
            KpiTopArticle = _pivotResult.TopModelQuantity > 0 
                ? $"{_pivotResult.TopModelName} ({_pivotResult.TopModelQuantity:#,##0.##} бр.)" 
                : "—";
            KpiPeakDate = _pivotResult.TopSizeQuantity > 0 
                ? $"{_pivotResult.TopSizeName} ({_pivotResult.TopSizeQuantity:#,##0.##} бр.)" 
                : "—";

            // Уведомяване на View за пренареждане на динамичните колони в DataGrid
            ReportColumnsGenerated?.Invoke(_pivotResult);

            StatusMessage = $"Матрицата е генерирана за {sw.Elapsed.TotalSeconds:F2} сек. Намерени {_pivotResult.TotalModelsCount} модела в {_pivotResult.TotalSizesCount} размера, общо {_pivotResult.GrandTotal:#,##0.##} бр.";
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
        StartTime = "00:00";
        EndTime = "23:59";

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

            _pivotDataView.RowFilter = $"SIZE LIKE '%{term}%' OR IS_TOTAL_ROW = true";
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
            FileName = $"Матрица_униформи_{_pivotResult.Filter.GroupName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
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
            FileName = $"Матрица_униформи_{_pivotResult.Filter.GroupName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
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

    public async Task LoadDetailedReportAsync()
    {
        if (StartDate > EndDate)
        {
            MessageBox.Show("Началната дата не може да бъде след крайната дата!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        StatusMessage = "Извличане на детайлни продажби от базата данни...";
        var sw = Stopwatch.StartNew();

        try
        {
            var filter = new ReportFilter
            {
                GroupId = SelectedPlugroup?.Id ?? 0,
                GroupName = SelectedPlugroup?.Name ?? "[Всички училища]",
                TerminalId = SelectedTerminal?.Id ?? 0,
                TerminalName = SelectedTerminal?.DisplayName ?? "Всички терминали",
                StartDate = StartDate,
                EndDate = EndDate,
                StartTime = ParseTime(StartTime, new TimeSpan(0, 0, 0), isEndOfDay: false),
                EndTime = ParseTime(EndTime, new TimeSpan(23, 59, 59), isEndOfDay: true)
            };

            var records = await _firebirdService.GetDetailedSalesRecordsAsync(_configService.DatabaseSettings, filter);
            ProcessReceiptGrouping(records);
            DetailedRecords = new ObservableCollection<DetailedSaleRecord>(records);

            var view = CollectionViewSource.GetDefaultView(DetailedRecords);
            view.Filter = FilterDetailedRecord;
            DetailedDataView = view;

            HasDetailedData = records.Count > 0;
            UpdateDetailedKpis();
            UpdateChartData(records);
            sw.Stop();

            StatusMessage = $"Детайлната справка е генерирана за {sw.Elapsed.TotalSeconds:F2} сек. Намерени {records.Count:N0} записа, {DetailedKpiUniqueBonsCount:N0} бона, общо {DetailedKpiTotalQuantity:#,##0.000} бр., сума {DetailedKpiTotalRowSum:N2} €";
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

    private bool FilterDetailedRecord(object item)
    {
        if (item is not DetailedSaleRecord record) return false;
        if (string.IsNullOrWhiteSpace(DetailedSearchText)) return true;

        string term = DetailedSearchText.Trim();
        return (record.ArticleName != null && record.ArticleName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
               record.PluNumber.ToString().Contains(term) ||
               record.BonNumber.ToString().Contains(term) ||
               (record.GroupName != null && record.GroupName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
               (record.TerminalName != null && record.TerminalName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private void UpdateDetailedKpis()
    {
        if (DetailedDataView == null)
        {
            DetailedKpiTotalQuantity = 0m;
            DetailedKpiTotalRowSum = 0m;
            DetailedKpiUniqueBonsCount = 0;
            DetailedKpiTotalRowsCount = 0;
            DetailedKpiTotalBonSum = 0m;
            OnPropertyChanged(nameof(DetailedKpiAverageBonSum));
            return;
        }

        var visible = DetailedDataView.Cast<DetailedSaleRecord>().ToList();
        DetailedKpiTotalQuantity = visible.Sum(r => r.Quantity);
        DetailedKpiTotalRowSum = visible.Sum(r => r.RowTotal);
        DetailedKpiTotalRowsCount = visible.Count;
        DetailedKpiUniqueBonsCount = visible.Select(r => (r.TerminalId, r.BonNumber)).Distinct().Count();
        DetailedKpiTotalBonSum = visible.GroupBy(r => (r.TerminalId, r.BonNumber, r.SaleDateTime.Date)).Sum(g => g.First().BonTotal);
        OnPropertyChanged(nameof(DetailedKpiAverageBonSum));
    }

    /// <summary>
    /// Зарежда данни за третия таб (Графичен анализ и визуализация)
    /// </summary>
    public async Task LoadChartsReportAsync()
    {
        if (StartDate > EndDate)
        {
            MessageBox.Show("Началната дата не може да бъде след крайната дата!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        StatusMessage = "Извличане на данни за графичен анализ...";
        var sw = Stopwatch.StartNew();

        try
        {
            var filter = new ReportFilter
            {
                GroupId = SelectedPlugroup?.Id ?? 0,
                GroupName = SelectedPlugroup?.Name ?? "[Всички училища]",
                TerminalId = SelectedTerminal?.Id ?? 0,
                TerminalName = SelectedTerminal?.DisplayName ?? "Всички терминали",
                StartDate = StartDate,
                EndDate = EndDate,
                StartTime = ParseTime(StartTime, new TimeSpan(0, 0, 0), isEndOfDay: false),
                EndTime = ParseTime(EndTime, new TimeSpan(23, 59, 59), isEndOfDay: true)
            };

            var records = await _firebirdService.GetDetailedSalesRecordsAsync(_configService.DatabaseSettings, filter);
            ProcessReceiptGrouping(records);
            DetailedRecords = new ObservableCollection<DetailedSaleRecord>(records);

            var view = CollectionViewSource.GetDefaultView(DetailedRecords);
            view.Filter = FilterDetailedRecord;
            DetailedDataView = view;

            HasDetailedData = records.Count > 0;
            UpdateDetailedKpis();
            UpdateChartData(records);
            sw.Stop();

            StatusMessage = $"Графичният анализ е генериран за {sw.Elapsed.TotalSeconds:F2} сек. {records.Count:N0} записа, {DetailedKpiUniqueBonsCount:N0} бона, общ оборот {DetailedKpiTotalRowSum:N2} €";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Грешка при графичния анализ: {ex.Message}";
            MessageBox.Show($"Възникна грешка при извличане на данните за графиките:\n\n{ex.Message}", "Грешка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Преизчислява сериите за Donut Chart (Графика 1) и Dual-Axis Cartesian Chart (Графика 2).
    /// </summary>
    public void UpdateChartData(IList<DetailedSaleRecord>? records)
    {
        if (Application.Current != null && Application.Current.Dispatcher.CheckAccess() == false)
        {
            Application.Current.Dispatcher.Invoke(() => UpdateChartData(records));
            return;
        }

        if (records == null || records.Count == 0)
        {
            DonutSeries = new SeriesCollection();
            SchoolRankingSeries = new SeriesCollection();
            SchoolRankingLabels = Array.Empty<string>();
            HasChartData = false;
            KpiTopSchoolName = "—";
            return;
        }

        // 1. Графика 1: Donut Chart - Съотношение Оборот (€) към Брой поръчки / бонове (PushOut = 12 за Оборот)
        decimal totalTurnover = records.Sum(r => r.RowTotal);
        int totalBons = records.Select(r => r.ReceiptKey).Distinct().Count();

        var donut = new SeriesCollection
        {
            new PieSeries
            {
                Title = "Оборот (€)",
                Values = new ChartValues<double> { (double)totalTurnover },
                DataLabels = true,
                LabelPoint = point => $"{point.Y:N2} €",
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F4E79")),
                PushOut = 12
            },
            new PieSeries
            {
                Title = "Брой поръчки",
                Values = new ChartValues<double> { (double)totalBons },
                DataLabels = true,
                LabelPoint = point => $"{point.Y:N0} бр.",
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C41")),
                PushOut = 0
            }
        };

        // 2. Графика 2: Бизнес класиране по училища (Dual-Axis: Стълбове + Линия)
        // X-ос: Имената на училищата / групите, подредени по най-голям оборот в низходящ ред (ТОП училищата напред)
        var schoolGroups = records
            .GroupBy(r => string.IsNullOrWhiteSpace(r.GroupName) ? "[Без училище/група]" : r.GroupName.Trim())
            .Select(g => new
            {
                SchoolName = g.Key,
                TotalTurnover = g.Sum(r => r.RowTotal),
                TotalQuantity = g.Sum(r => r.Quantity)
            })
            .OrderByDescending(x => x.TotalTurnover)
            .ToList();

        var labels = schoolGroups.Select(x => x.SchoolName).ToArray();
        var turnoverValues = new ChartValues<double>(schoolGroups.Select(x => (double)x.TotalTurnover));
        var quantityValues = new ChartValues<double>(schoolGroups.Select(x => (double)x.TotalQuantity));

        var rankingSeries = new SeriesCollection
        {
            new ColumnSeries
            {
                Title = "Оборот",
                Values = turnoverValues,
                ScalesYAt = 0,
                Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1F4E79")),
                MaxColumnWidth = 40,
                LabelPoint = point => $"{point.Y:N2} €"
            },
            new LineSeries
            {
                Title = "Продадени",
                Values = quantityValues,
                ScalesYAt = 1,
                Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C41")),
                Fill = new SolidColorBrush(Color.FromArgb(20, 16, 124, 65)),
                PointGeometry = DefaultGeometries.Circle,
                PointGeometrySize = 10,
                PointForeground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#107C41")),
                LineSmoothness = 0.35,
                LabelPoint = point => $"{point.Y:#,##0.##} бр."
            }
        };

        DonutSeries = donut;
        SchoolRankingSeries = rankingSeries;
        SchoolRankingLabels = labels;
        HasChartData = true;

        if (schoolGroups.Count > 0)
        {
            KpiTopSchoolName = $"{schoolGroups[0].SchoolName} ({schoolGroups[0].TotalTurnover:N2} €)";
        }
        else
        {
            KpiTopSchoolName = "—";
        }
    }

    /// <summary>
    /// Изчислява флаговете за скриване на повторения и зебра оцветяване по цели бонове.
    /// </summary>
    public static void ProcessReceiptGrouping(IList<DetailedSaleRecord> records)
    {
        if (records == null || records.Count == 0) return;

        string? currentReceiptKey = null;
        bool isAlternate = false;

        for (int i = 0; i < records.Count; i++)
        {
            var rec = records[i];
            string key = rec.ReceiptKey;

            if (key != currentReceiptKey)
            {
                // Нов бон - превключваме фона
                currentReceiptKey = key;
                isAlternate = !isAlternate;
                rec.IsFirstInReceipt = true;
            }
            else
            {
                // Пореден ред от същия бон - скриваме повтарящите се полета
                rec.IsFirstInReceipt = false;
            }

            rec.IsAlternateReceiptGroup = isAlternate;

            // Тънка разделителна линия само на последния ред от бона
            bool isLast = (i == records.Count - 1) || (records[i + 1].ReceiptKey != key);
            rec.IsLastInReceipt = isLast;
        }
    }

    public static TimeSpan ParseTime(string? timeStr, TimeSpan fallback, bool isEndOfDay = false)
    {
        if (string.IsNullOrWhiteSpace(timeStr))
            return fallback;

        timeStr = timeStr!.Trim().Replace('.', ':');

        // Ако е въведен само час (напр. "8" или "14"), го тълкуваме като час, а не като дни
        if (int.TryParse(timeStr, out int hour) && hour >= 0 && hour <= 23)
        {
            return isEndOfDay ? new TimeSpan(hour, 59, 59) : new TimeSpan(hour, 0, 0);
        }

        if (TimeSpan.TryParse(timeStr, System.Globalization.CultureInfo.InvariantCulture, out var ts))
        {
            if (isEndOfDay && ts.Seconds == 0 && ts.TotalHours < 24)
            {
                return new TimeSpan(ts.Hours, ts.Minutes, 59);
            }
            return ts;
        }

        return fallback;
    }

    private async Task ExportDetailedToExcelAsync()
    {
        if (!HasDetailedData || DetailedDataView == null) return;
        var records = DetailedDataView.Cast<DetailedSaleRecord>().ToList();
        if (records.Count == 0) return;

        var filter = new ReportFilter
        {
            GroupId = SelectedPlugroup?.Id ?? 0,
            GroupName = SelectedPlugroup?.Name ?? "[Всички училища]",
            TerminalId = SelectedTerminal?.Id ?? 0,
            TerminalName = SelectedTerminal?.DisplayName ?? "Всички терминали",
            StartDate = StartDate,
            EndDate = EndDate,
            StartTime = ParseTime(StartTime, new TimeSpan(0, 0, 0), isEndOfDay: false),
            EndTime = ParseTime(EndTime, new TimeSpan(23, 59, 59), isEndOfDay: true)
        };

        var sfd = new SaveFileDialog
        {
            Title = "Експорт на детайлна справка в Microsoft Excel",
            Filter = "Excel таблица (*.xlsx)|*.xlsx",
            FileName = $"Детайлни_продажби_{filter.GroupName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Експортиране на детайлната справка в Excel...";
                await _exportService.ExportDetailedToExcelAsync(records, filter, sfd.FileName);
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

    private async Task ExportDetailedToCsvAsync()
    {
        if (!HasDetailedData || DetailedDataView == null) return;
        var records = DetailedDataView.Cast<DetailedSaleRecord>().ToList();
        if (records.Count == 0) return;

        var filter = new ReportFilter
        {
            GroupId = SelectedPlugroup?.Id ?? 0,
            GroupName = SelectedPlugroup?.Name ?? "[Всички училища]",
            TerminalId = SelectedTerminal?.Id ?? 0,
            TerminalName = SelectedTerminal?.DisplayName ?? "Всички терминали",
            StartDate = StartDate,
            EndDate = EndDate,
            StartTime = ParseTime(StartTime, new TimeSpan(0, 0, 0), isEndOfDay: false),
            EndTime = ParseTime(EndTime, new TimeSpan(23, 59, 59), isEndOfDay: true)
        };

        var sfd = new SaveFileDialog
        {
            Title = "Експорт на детайлна справка в CSV файл",
            Filter = "CSV файл (*.csv)|*.csv",
            FileName = $"Детайлни_продажби_{filter.GroupName.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmm}.csv"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Експортиране на детайлната справка в CSV...";
                await _exportService.ExportDetailedToCsvAsync(records, filter, sfd.FileName);
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

    private void CopyDetailedClipboard()
    {
        if (!HasDetailedData || DetailedDataView == null) return;
        var records = DetailedDataView.Cast<DetailedSaleRecord>().ToList();
        if (records.Count == 0) return;

        try
        {
            string tsv = _exportService.CopyDetailedToClipboardFormat(records);
            Clipboard.SetText(tsv);
            StatusMessage = "Детайлните данни са копирани в клипборда! Можете да ги поставите в Excel.";
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
