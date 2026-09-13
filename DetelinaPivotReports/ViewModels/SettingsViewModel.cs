using System;
using System.Threading.Tasks;
using System.Windows.Input;
using DetelinaPivotReports.Models;
using DetelinaPivotReports.Services;
using Microsoft.Win32;

namespace DetelinaPivotReports.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly IConfigService _configService;
    private readonly IFirebirdService _firebirdService;
    private readonly Action? _onClose;

    private string _host = "localhost";
    private int _port = 3050;
    private string _database = @"C:\Users\a.andonov\АТМ\ELTRADEBACKOFFICE.GDB";
    private string _user = "SYSDBA";
    private string _password = "masterkey";
    private string _charset = "WIN1251";
    private int _connectionTimeout = 15;

    private bool _isTesting;
    private string _testStatusMessage = string.Empty;
    private bool? _testSuccess;

    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    public int Port
    {
        get => _port;
        set => SetProperty(ref _port, value);
    }

    public string Database
    {
        get => _database;
        set => SetProperty(ref _database, value);
    }

    public string User
    {
        get => _user;
        set => SetProperty(ref _user, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Charset
    {
        get => _charset;
        set => SetProperty(ref _charset, value);
    }

    public int ConnectionTimeout
    {
        get => _connectionTimeout;
        set => SetProperty(ref _connectionTimeout, value);
    }

    public bool IsTesting
    {
        get => _isTesting;
        set => SetProperty(ref _isTesting, value);
    }

    public string TestStatusMessage
    {
        get => _testStatusMessage;
        set => SetProperty(ref _testStatusMessage, value);
    }

    public bool? TestSuccess
    {
        get => _testSuccess;
        set => SetProperty(ref _testSuccess, value);
    }

    public ICommand BrowseDatabaseCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public bool IsSaved { get; private set; }

    public SettingsViewModel(IConfigService configService, IFirebirdService firebirdService, Action? onClose = null)
    {
        _configService = configService;
        _firebirdService = firebirdService;
        _onClose = onClose;

        var current = _configService.DatabaseSettings;
        _host = current.Host;
        _port = current.Port;
        _database = current.Database;
        _user = current.User;
        _password = current.Password;
        _charset = current.Charset;
        _connectionTimeout = current.ConnectionTimeout;

        BrowseDatabaseCommand = new RelayCommand(BrowseDatabase);
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync(), () => !IsTesting);
        SaveCommand = new RelayCommand(Save);
        CancelCommand = new RelayCommand(() => _onClose?.Invoke());
    }

    private void BrowseDatabase()
    {
        var ofd = new OpenFileDialog
        {
            Title = "Изберете Firebird база данни (.GDB / .FDB)",
            Filter = "Firebird бази данни (*.gdb;*.fdb)|*.gdb;*.fdb|Всички файлове (*.*)|*.*",
            CheckFileExists = true
        };

        if (ofd.ShowDialog() == true)
        {
            Database = ofd.FileName;
        }
    }

    private async Task TestConnectionAsync()
    {
        IsTesting = true;
        TestStatusMessage = "Тестване на връзката към Firebird...";
        TestSuccess = null;

        try
        {
            var testSettings = GetCurrentSettings();
            var result = await _firebirdService.TestConnectionAsync(testSettings);

            TestSuccess = result.Success;
            TestStatusMessage = result.Success 
                ? $"Успешно свързване! Firebird версия: {result.ServerVersion}" 
                : $"Грешка: {result.Message}";
        }
        catch (Exception ex)
        {
            TestSuccess = false;
            TestStatusMessage = $"Неочаквана грешка: {ex.Message}";
        }
        finally
        {
            IsTesting = false;
        }
    }

    private void Save()
    {
        var newSettings = GetCurrentSettings();
        _configService.SaveDatabaseSettings(newSettings);
        IsSaved = true;
        _onClose?.Invoke();
    }

    public DatabaseSettings GetCurrentSettings()
    {
        return new DatabaseSettings
        {
            Host = Host.Trim(),
            Port = Port,
            Database = Database.Trim(),
            User = User.Trim(),
            Password = Password,
            Charset = Charset.Trim(),
            ConnectionTimeout = ConnectionTimeout
        };
    }
}
