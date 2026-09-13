using System;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace DetelinaPivotReports;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Задължителна регистрация на Windows-1251 кодировка за Firebird 3.0 в .NET 8
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // Прихващане на глобални грешки за плавна работа
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        base.OnStartup(e);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Възникна неочаквана грешка:\n\n{e.Exception.Message}\n\n{e.Exception.StackTrace}",
                        "Системна грешка", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            MessageBox.Show($"Фатална грешка:\n\n{ex.Message}", "Фатална грешка", MessageBoxButton.OK, MessageBoxImage.Stop);
        }
    }
}
