using System.Windows;
using DetelinaPivotReports.ViewModels;

namespace DetelinaPivotReports.Views;

public partial class SettingsWindow : Window
{
    private bool _isUpdatingPassword;

    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += SettingsWindow_Loaded;
    }

    private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            // Hook за затваряне на прозореца при Save или Cancel
            var field = typeof(SettingsViewModel).GetField("_onClose", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(vm, new System.Action(() =>
            {
                DialogResult = vm.IsSaved;
                Close();
            }));

            // Първоначално зареждане на паролата в маскираното поле
            _isUpdatingPassword = true;
            DbPasswordBox.Password = vm.Password ?? string.Empty;
            _isUpdatingPassword = false;
        }
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingPassword) return;
        if (DataContext is SettingsViewModel vm)
        {
            vm.Password = DbPasswordBox.Password;
        }
    }
}
