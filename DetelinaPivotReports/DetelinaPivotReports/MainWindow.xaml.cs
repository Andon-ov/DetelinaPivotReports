using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using DetelinaPivotReports.Converters;
using DetelinaPivotReports.Models;
using DetelinaPivotReports.Services;
using DetelinaPivotReports.ViewModels;

namespace DetelinaPivotReports;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var configService = new ConfigService();
        var firebirdService = new FirebirdService();
        var pivotService = new PivotReportService();
        var exportService = new ExportService();

        _viewModel = new MainViewModel(configService, firebirdService, pivotService, exportService);
        _viewModel.ReportColumnsGenerated += OnReportColumnsGenerated;

        DataContext = _viewModel;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.InitializeDatabaseAsync();
    }

    private void OnReportColumnsGenerated(PivotReportResult result)
    {
        // Премахване на предишните динамични колони (запазват се само първите 2: Код и Наименование)
        while (PivotGrid.Columns.Count > 2)
        {
            PivotGrid.Columns.RemoveAt(PivotGrid.Columns.Count - 1);
        }

        var zeroConverter = (ZeroToDashConverter)FindResource("ZeroToDashConverter");

        // 1. Динамични колони за всяка дата от периода
        foreach (var date in result.Dates)
        {
            string colProp = $"D_{date:yyyyMMdd}";
            string dayAbbr = GetBgDayAbbr(date.DayOfWeek);

            var cellStyle = new Style(typeof(TextBlock));
            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
            cellStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            cellStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 8, 0)));

            var col = new DataGridTextColumn
            {
                Header = $"{date:dd.MM}\n({dayAbbr})",
                Binding = new Binding($"[{colProp}]")
                {
                    Converter = zeroConverter
                },
                Width = new DataGridLength(72, DataGridLengthUnitType.Pixel),
                ElementStyle = cellStyle,
                SortMemberPath = colProp
            };

            PivotGrid.Columns.Add(col);
        }

        // 2. Колона ОБЩО
        var totalCellStyle = new Style(typeof(TextBlock));
        totalCellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
        totalCellStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        totalCellStyle.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
        totalCellStyle.Setters.Add(new Setter(TextBlock.ForegroundProperty, new SolidColorBrush(Color.FromRgb(0x1F, 0x4E, 0x79))));
        totalCellStyle.Setters.Add(new Setter(FrameworkElement.MarginProperty, new Thickness(0, 0, 10, 0)));

        var totalCol = new DataGridTextColumn
        {
            Header = "ОБЩО",
            Binding = new Binding("[TOTAL]")
            {
                StringFormat = "{0:#,##0.##}"
            },
            Width = new DataGridLength(90, DataGridLengthUnitType.Pixel),
            ElementStyle = totalCellStyle,
            SortMemberPath = "TOTAL"
        };

        PivotGrid.Columns.Add(totalCol);
    }

    private static string GetBgDayAbbr(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Пн",
        DayOfWeek.Tuesday => "Вт",
        DayOfWeek.Wednesday => "Ср",
        DayOfWeek.Thursday => "Чт",
        DayOfWeek.Friday => "Пт",
        DayOfWeek.Saturday => "Сб",
        DayOfWeek.Sunday => "Нд",
        _ => ""
    };
}
