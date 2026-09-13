using FirebirdSql.Data.FirebirdClient;

namespace DetelinaPivotReports.Models;

/// <summary>
/// Настройки за свързване към Firebird 3.0 база данни.
/// </summary>
public class DatabaseSettings
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 3050;
    public string Database { get; set; } = @"C:\Users\a.andonov\АТМ\ELTRADEBACKOFFICE.GDB";
    public string User { get; set; } = "SYSDBA";
    public string Password { get; set; } = "masterkey";
    public string Charset { get; set; } = "WIN1251";
    public int ConnectionTimeout { get; set; } = 15;

    public string BuildConnectionString()
    {
        var csb = new FbConnectionStringBuilder
        {
            ServerType = FbServerType.Default,
            DataSource = Host,
            Port = Port,
            Database = Database,
            UserID = User,
            Password = Password,
            Charset = string.IsNullOrWhiteSpace(Charset) ? "WIN1251" : Charset,
            ConnectionTimeout = ConnectionTimeout > 0 ? ConnectionTimeout : 15,
            Pooling = true,
            MinPoolSize = 0,
            MaxPoolSize = 50
        };
        return csb.ToString();
    }
}
