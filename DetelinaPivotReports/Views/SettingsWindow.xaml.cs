using System.Windows;
using System.Windows.Controls;
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
            DbPasswordTextBox.Text = vm.Password ?? string.Empty;
            _isUpdatingPassword = false;
        }
    }

    private void DbPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingPassword) return;
        _isUpdatingPassword = true;
        if (DataContext is SettingsViewModel vm)
        {
            vm.Password = DbPasswordBox.Password;
            DbPasswordTextBox.Text = DbPasswordBox.Password;
        }
        _isUpdatingPassword = false;
    }

    private void DbPasswordTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingPassword) return;
        _isUpdatingPassword = true;
        if (DataContext is SettingsViewModel vm)
        {
            vm.Password = DbPasswordTextBox.Text;
            DbPasswordBox.Password = DbPasswordTextBox.Text;
        }
        _isUpdatingPassword = false;
    }

    private void TogglePasswordVisibility_Click(object sender, RoutedEventArgs e)
    {
        if (DbPasswordBox.Visibility == Visibility.Visible)
        {
            DbPasswordBox.Visibility = Visibility.Collapsed;
            DbPasswordTextBox.Visibility = Visibility.Visible;
            DbPasswordTextBox.Focus();
            DbPasswordTextBox.CaretIndex = DbPasswordTextBox.Text.Length;
            TogglePasswordIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOffOutline;
        }
        else
        {
            DbPasswordTextBox.Visibility = Visibility.Collapsed;
            DbPasswordBox.Visibility = Visibility.Visible;
            DbPasswordBox.Focus();
            TogglePasswordIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.EyeOutline;
        }
    }
}
