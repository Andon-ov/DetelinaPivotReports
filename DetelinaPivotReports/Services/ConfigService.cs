using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using DetelinaPivotReports.Models;

namespace DetelinaPivotReports.Services;

public class ConfigService : IConfigService
{
    private readonly string _configFilePath;

    public DatabaseSettings DatabaseSettings { get; private set; } = new();
    public Dictionary<string, string> TerminalNames { get; private set; } = new();
    public bool HideEmptyDaysDefault { get; private set; } = false;
    public string DefaultPeriodPreset { get; private set; } = "ThisMonth";

    public ConfigService(string? configPath = null)
    {
        _configFilePath = configPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
        LoadConfiguration();
    }

    public void LoadConfiguration()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                SaveDefaultConfiguration();
                return;
            }

            string json = File.ReadAllText(_configFilePath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("Firebird", out var fbElement))
            {
                DatabaseSettings = new DatabaseSettings
                {
                    Host = fbElement.TryGetProperty("Host", out var h) ? h.GetString() ?? "localhost" : "localhost",
                    Port = fbElement.TryGetProperty("Port", out var p) && p.TryGetInt32(out var pVal) ? pVal : 3050,
                    Database = fbElement.TryGetProperty("Database", out var d) ? d.GetString() ?? @"C:\Users\a.andonov\АТМ\ELTRADEBACKOFFICE.GDB" : @"C:\Users\a.andonov\АТМ\ELTRADEBACKOFFICE.GDB",
                    User = fbElement.TryGetProperty("User", out var u) ? u.GetString() ?? "SYSDBA" : "SYSDBA",
                    Password = fbElement.TryGetProperty("Password", out var pwd) ? pwd.GetString() ?? "masterkey" : "masterkey",
                    Charset = fbElement.TryGetProperty("Charset", out var c) ? c.GetString() ?? "WIN1251" : "WIN1251",
                    ConnectionTimeout = fbElement.TryGetProperty("ConnectionTimeout", out var ct) && ct.TryGetInt32(out var ctVal) ? ctVal : 15
                };
            }

            if (root.TryGetProperty("ReportSettings", out var repElement))
            {
                DefaultPeriodPreset = repElement.TryGetProperty("DefaultPeriod", out var dp) ? dp.GetString() ?? "ThisMonth" : "ThisMonth";
                HideEmptyDaysDefault = repElement.TryGetProperty("HideEmptyDays", out var hed) && hed.GetBoolean();

                if (repElement.TryGetProperty("TerminalNames", out var tnElement) && tnElement.ValueKind == JsonValueKind.Object)
                {
                    TerminalNames.Clear();
                    foreach (var prop in tnElement.EnumerateObject())
                    {
                        TerminalNames[prop.Name] = prop.Value.GetString() ?? string.Empty;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Резервни стойности при проблем с четенето
            DatabaseSettings = new DatabaseSettings();
            DefaultPeriodPreset = "ThisMonth";
            HideEmptyDaysDefault = false;
        }
    }

    public void SaveDatabaseSettings(DatabaseSettings settings)
    {
        DatabaseSettings = settings;

        try
        {
            JsonObject root;
            if (File.Exists(_configFilePath))
            {
                string text = File.ReadAllText(_configFilePath);
                root = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
            }
            else
            {
                root = new JsonObject();
            }

            var fbNode = new JsonObject
            {
                ["Host"] = settings.Host,
                ["Port"] = settings.Port,
                ["Database"] = settings.Database,
                ["User"] = settings.User,
                ["Password"] = settings.Password,
                ["Charset"] = settings.Charset,
                ["ConnectionTimeout"] = settings.ConnectionTimeout
            };
            root["Firebird"] = fbNode;

            if (!root.ContainsKey("ReportSettings"))
            {
                var repNode = new JsonObject
                {
                    ["DefaultPeriod"] = DefaultPeriodPreset,
                    ["HideEmptyDays"] = HideEmptyDaysDefault,
                    ["TerminalNames"] = new JsonObject
                    {
                        ["1"] = "Слънчев бряг (Пос 1)",
                        ["2"] = "Цар Калоян (Пос 2)",
                        ["3"] = "Галерия (Пос 3)",
                        ["4"] = "Ивайло (Пос 4)"
                    }
                };
                root["ReportSettings"] = repNode;
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(_configFilePath, root.ToJsonString(options));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Грешка при запис на настройките в {_configFilePath}: {ex.Message}", ex);
        }
    }

    private void SaveDefaultConfiguration()
    {
        SaveDatabaseSettings(new DatabaseSettings());
    }
}
