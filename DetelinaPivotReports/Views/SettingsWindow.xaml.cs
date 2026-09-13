using System.Windows;
using DetelinaPivotReports.ViewModels;

namespace DetelinaPivotReports.Views;

public partial class SettingsWindow : Window
{
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
        }
    }
}
