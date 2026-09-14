using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DetelinaPivotReports.Models;
using FirebirdSql.Data.FirebirdClient;

namespace DetelinaPivotReports.Services;

public class FirebirdService : IFirebirdService
{
    static FirebirdService()
    {
        // Регистрация на поддръжка за Windows-1251 кодировка в .NET Core/.NET 8
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task<(bool Success, string Message, string? ServerVersion)> TestConnectionAsync(DatabaseSettings settings, CancellationToken ct = default)
    {
        try
        {
            string connString = settings.BuildConnectionString();
            using var conn = new FbConnection(connString);
            await conn.OpenAsync(ct);

            string serverVersion = conn.ServerVersion;

            // Тестова проверка на таблици
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM RDB$DATABASE";
            await cmd.ExecuteScalarAsync(ct);

            return (true, "Връзката към Firebird базата данни е успешна!", serverVersion);
        }
        catch (FbException fbEx)
        {
            return (false, $"Firebird SQL грешка [{fbEx.ErrorCode}]: {fbEx.Message}", null);
        }
        catch (Exception ex)
        {
            return (false, $"Неуспешно свързване: {ex.Message}", null);
        }
    }

    public async Task<List<PlugroupItem>> GetPlugroupsAsync(DatabaseSettings settings, CancellationToken ct = default)
    {
        var rawList = new List<PlugroupItem>();

        try
        {
            string connString = settings.BuildConnectionString();
            using var conn = new FbConnection(connString);
            await conn.OpenAsync(ct);

            string sql = @"
                SELECT 
                    PGRP_ID, 
                    PGRP_NAME, 
                    PGRP_PARENT, 
                    PGRP_CODE 
                FROM N_PLUGROUPS 
                ORDER BY PGRP_NAME";

            using var cmd = new FbCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                int id = reader.GetInt32(0);
                string name = reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim();
                int parent = reader.IsDBNull(2) ? -1 : reader.GetInt32(2);
                int code = reader.IsDBNull(3) ? 0 : reader.GetInt32(3);

                rawList.Add(new PlugroupItem
                {
                    Id = id,
                    Name = name,
                    ParentId = parent,
                    Code = code
                });
            }
        }
        catch (Exception)
        {
            // При грешка връщаме празен списък
            return new List<PlugroupItem> { PlugroupItem.CreateAllGroupsOption() };
        }

        // Подреждане по йерархия
        var organizedList = OrganizeHierarchy(rawList);
        organizedList.Insert(0, PlugroupItem.CreateAllGroupsOption());
        return organizedList;
    }

    public async Task<List<TerminalItem>> GetTerminalsAsync(DatabaseSettings settings, Dictionary<string, string> terminalNames, CancellationToken ct = default)
    {
        var list = new List<TerminalItem> { TerminalItem.CreateAllTerminalsOption() };

        try
        {
            string connString = settings.BuildConnectionString();
            using var conn = new FbConnection(connString);
            await conn.OpenAsync(ct);

            string sql = "SELECT DISTINCT SELL_TERMINAL FROM SALES_BON WHERE SELL_TERMINAL IS NOT NULL ORDER BY SELL_TERMINAL";
            using var cmd = new FbCommand(sql, conn);
            using var reader = await cmd.ExecuteReaderAsync(ct);

            var foundTerminals = new HashSet<int>();
            while (await reader.ReadAsync(ct))
            {
                int termId = reader.GetInt32(0);
                foundTerminals.Add(termId);

                string termName = terminalNames.TryGetValue(termId.ToString(), out var name) ? name : string.Empty;
                list.Add(new TerminalItem { Id = termId, Name = termName });
            }

            // Ако няма намерени в продажбите, добавяме тези от настройките
            foreach (var kvp in terminalNames)
            {
                if (int.TryParse(kvp.Key, out int termId) && !foundTerminals.Contains(termId))
                {
                    list.Add(new TerminalItem { Id = termId, Name = kvp.Value });
                }
            }
        }
        catch (Exception)
        {
            // Резервни терминали от config
            foreach (var kvp in terminalNames)
            {
                if (int.TryParse(kvp.Key, out int termId))
                {
                    list.Add(new TerminalItem { Id = termId, Name = kvp.Value });
                }
            }
        }

        return list;
    }

    public async Task<List<ArticleSaleRecord>> GetSalesRecordsAsync(DatabaseSettings settings, ReportFilter filter, CancellationToken ct = default)
    {
        var results = new List<ArticleSaleRecord>();

        string connString = settings.BuildConnectionString();
        using var conn = new FbConnection(connString);
        await conn.OpenAsync(ct);

        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine(@"
            SELECT 
                SP.SPLU_PLUNUMB,
                COALESCE(NULLIF(TRIM(SP.SPLU_NAME), ''), P.PLU_NAME) AS ARTICLE_NAME,
                CAST(SB.SELL_DATETIME AS DATE) AS SALE_DATE,
                SUM(SP.SPLU_SOLDQUANT) AS TOTAL_QUANTITY
            FROM SALES_PLUES SP
            JOIN SALES_BON SB ON SP.SPLU_SELL_ID = SB.SELL_ID
            LEFT JOIN PLUES P ON SP.SPLU_PLUNUMB = P.PLU_NUMB
            WHERE SB.SELL_REVOKED_ = 0 
              AND SP.SPLU_REVOKED_ = 0
              AND SB.SELL_DATETIME >= @StartDate 
              AND SB.SELL_DATETIME <= @EndDate");

        using var cmd = new FbCommand();
        cmd.Connection = conn;

        // Параметри за дати (начало: 00:00:00, край: 23:59:59)
        DateTime startDt = filter.StartDate.Date;
        DateTime endDt = filter.EndDate.Date.AddDays(1).AddSeconds(-1);

        cmd.Parameters.Add(new FbParameter("@StartDate", FbDbType.TimeStamp) { Value = startDt });
        cmd.Parameters.Add(new FbParameter("@EndDate", FbDbType.TimeStamp) { Value = endDt });

        // Филтър по терминал
        if (filter.TerminalId > 0)
        {
            sqlBuilder.AppendLine("  AND SB.SELL_TERMINAL = @SelectedTerminal");
            cmd.Parameters.Add(new FbParameter("@SelectedTerminal", FbDbType.Integer) { Value = filter.TerminalId });
        }

        // Филтър по група / училище
        if (filter.GroupId > 0)
        {
            if (filter.IncludeSubgroups && filter.GroupIds.Count > 0)
            {
                var paramNames = new List<string>();
                for (int i = 0; i < filter.GroupIds.Count; i++)
                {
                    string pName = $"@grp_{i}";
                    paramNames.Add(pName);
                    cmd.Parameters.Add(new FbParameter(pName, FbDbType.Integer) { Value = filter.GroupIds[i] });
                }
                sqlBuilder.AppendLine($"  AND P.PLU_GROUP_ID IN ({string.Join(", ", paramNames)})");
            }
            else
            {
                sqlBuilder.AppendLine("  AND P.PLU_GROUP_ID = @SelectedGroupId");
                cmd.Parameters.Add(new FbParameter("@SelectedGroupId", FbDbType.Integer) { Value = filter.GroupId });
            }
        }

        sqlBuilder.AppendLine(@"
            GROUP BY SP.SPLU_PLUNUMB, COALESCE(NULLIF(TRIM(SP.SPLU_NAME), ''), P.PLU_NAME), CAST(SB.SELL_DATETIME AS DATE)
            ORDER BY COALESCE(NULLIF(TRIM(SP.SPLU_NAME), ''), P.PLU_NAME), SP.SPLU_PLUNUMB, CAST(SB.SELL_DATETIME AS DATE)");

        cmd.CommandText = sqlBuilder.ToString();

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            int pluNumber = reader.GetInt32(0);
            string articleName = reader.IsDBNull(1) || string.IsNullOrWhiteSpace(reader.GetString(1)) 
                ? $"Артикул {pluNumber}" 
                : reader.GetString(1).Trim();
            DateTime saleDate = reader.GetDateTime(2);
            decimal quantity = reader.IsDBNull(3) ? 0m : Convert.ToDecimal(reader.GetValue(3));

            results.Add(new ArticleSaleRecord
            {
                PluNumber = pluNumber,
                ArticleName = articleName,
                SaleDateTime = saleDate,
                SoldQuantity = quantity,
                Terminal = filter.TerminalId
            });
        }

        return results;
    }

    public async Task<List<DetailedSaleRecord>> GetDetailedSalesRecordsAsync(DatabaseSettings settings, ReportFilter filter, CancellationToken ct = default)
    {
        var results = new List<DetailedSaleRecord>();

        string connString = settings.BuildConnectionString();
        using var conn = new FbConnection(connString);
        await conn.OpenAsync(ct);

        var sqlBuilder = new StringBuilder();
        sqlBuilder.AppendLine(@"
            SELECT 
                CASE SB.SELL_TERMINAL
                    WHEN 1 THEN 'Сл бряг - ПОС 1'
                    WHEN 2 THEN 'Калоян - ПОС 2'
                    WHEN 3 THEN 'Галерия - ПОС 3'
                    WHEN 4 THEN 'Ивайло - ПОС 4'
                    ELSE 'Терминал ' || CAST(COALESCE(SB.SELL_TERMINAL, 0) AS VARCHAR(10))
                END AS TERMINAL_NAME,
                COALESCE(SB.SELL_TERMINAL, 0) AS SELL_TERMINAL,
                COALESCE(SB.SELL_BONNUMB, 0) AS SELL_BONNUMB,
                SB.SELL_DATETIME,
                COALESCE(SB.SELL_SUM, 0) AS SELL_SUM,
                COALESCE(G.PGRP_NAME, 'Без група') AS PGRP_NAME,
                COALESCE(SP.SPLU_PLUNUMB, 0) AS SPLU_PLUNUMB,
                COALESCE(NULLIF(TRIM(SP.SPLU_NAME), ''), COALESCE(P.PLU_NAME, 'Артикул ' || CAST(COALESCE(SP.SPLU_PLUNUMB, 0) AS VARCHAR(10)))) AS SPLU_NAME,
                COALESCE(SP.SPLU_SOLDQUANT, 0) AS SPLU_SOLDQUANT,
                COALESCE(SP.SPLU_SELLPRICE, 0) AS SPLU_SELLPRICE,
                COALESCE(SP.SPLU_SELLDISCOUNT, 0) AS SPLU_SELLDISCOUNT,
                COALESCE(SP.SPLU_SELL_ID, 0) AS SELL_ID
            FROM SALES_PLUES SP
            JOIN SALES_BON SB ON SP.SPLU_SELL_ID = SB.SELL_ID
            LEFT JOIN PLUES P ON SP.SPLU_PLUNUMB = P.PLU_NUMB
            LEFT JOIN N_PLUGROUPS G ON P.PLU_GROUP_ID = G.PGRP_ID
            WHERE COALESCE(SB.SELL_REVOKED_, 0) = 0 
              AND COALESCE(SP.SPLU_REVOKED_, 0) = 0
              AND SB.SELL_DATETIME >= @StartDate 
              AND SB.SELL_DATETIME <= @EndDate");

        using var cmd = new FbCommand();
        cmd.Connection = conn;

        DateTime startDt = filter.StartDate.Date;
        DateTime endDt = filter.EndDate.Date.AddDays(1).AddSeconds(-1);

        cmd.Parameters.Add(new FbParameter("@StartDate", FbDbType.TimeStamp) { Value = startDt });
        cmd.Parameters.Add(new FbParameter("@EndDate", FbDbType.TimeStamp) { Value = endDt });

        // Филтър по терминал
        if (filter.TerminalId > 0)
        {
            sqlBuilder.AppendLine("  AND SB.SELL_TERMINAL = @SelectedTerminal");
            cmd.Parameters.Add(new FbParameter("@SelectedTerminal", FbDbType.Integer) { Value = filter.TerminalId });
        }

        // Филтър по група / училище
        if (filter.GroupId > 0)
        {
            if (filter.IncludeSubgroups && filter.GroupIds.Count > 0)
            {
                var paramNames = new List<string>();
                for (int i = 0; i < filter.GroupIds.Count; i++)
                {
                    string pName = $"@grp_{i}";
                    paramNames.Add(pName);
                    cmd.Parameters.Add(new FbParameter(pName, FbDbType.Integer) { Value = filter.GroupIds[i] });
                }
                sqlBuilder.AppendLine($"  AND P.PLU_GROUP_ID IN ({string.Join(", ", paramNames)})");
            }
            else
            {
                sqlBuilder.AppendLine("  AND P.PLU_GROUP_ID = @SelectedGroupId");
                cmd.Parameters.Add(new FbParameter("@SelectedGroupId", FbDbType.Integer) { Value = filter.GroupId });
            }
        }

        sqlBuilder.AppendLine("ORDER BY SB.SELL_DATETIME, SB.SELL_BONNUMB, SP.SPLU_PLUNUMB");
        cmd.CommandText = sqlBuilder.ToString();

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            string termName = reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim();
            int termId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);
            long bonNumber = reader.IsDBNull(2) ? 0 : Convert.ToInt64(reader.GetValue(2));
            DateTime saleDateTime = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
            decimal bonTotal = reader.IsDBNull(4) ? 0m : Convert.ToDecimal(reader.GetValue(4));
            string groupName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5).Trim();
            int pluNumber = reader.IsDBNull(6) ? 0 : reader.GetInt32(6);
            string articleName = reader.IsDBNull(7) ? string.Empty : reader.GetString(7).Trim();
            decimal quantity = reader.IsDBNull(8) ? 0m : Convert.ToDecimal(reader.GetValue(8));
            decimal unitPrice = reader.IsDBNull(9) ? 0m : Convert.ToDecimal(reader.GetValue(9));
            decimal discount = reader.IsDBNull(10) ? 0m : Convert.ToDecimal(reader.GetValue(10));
            long sellId = reader.IsDBNull(11) ? 0 : Convert.ToInt64(reader.GetValue(11));

            // Сума ред = Количество * (Ед.цена + (Ед.цена * Отстъпка / 100))
            decimal rowTotal = Math.Round(quantity * (unitPrice + (unitPrice * discount / 100m)), 2);

            results.Add(new DetailedSaleRecord
            {
                TerminalId = termId,
                TerminalName = termName,
                BonNumber = bonNumber,
                SaleDateTime = saleDateTime,
                BonTotal = bonTotal,
                GroupName = groupName,
                PluNumber = pluNumber,
                ArticleName = articleName,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Discount = discount,
                RowTotal = rowTotal,
                SellId = sellId
            });
        }

        return results;
    }

    private static List<PlugroupItem> OrganizeHierarchy(List<PlugroupItem> rawList)
    {
        var result = new List<PlugroupItem>();
        var lookup = rawList.GroupBy(g => g.ParentId).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());

        void AddChildren(int parentId, int level)
        {
            if (lookup.TryGetValue(parentId, out var children))
            {
                foreach (var child in children)
                {
                    child.Level = level;
                    result.Add(child);
                    AddChildren(child.Id, level + 1);
                }
            }
        }

        // Коренови групи (ParentId <= 0 или ParentId липсва в rawList)
        var allIds = new HashSet<int>(rawList.Select(x => x.Id));
        var rootGroups = rawList
            .Where(x => x.ParentId <= 0 || !allIds.Contains(x.ParentId))
            .OrderBy(x => x.Name)
            .ToList();

        foreach (var root in rootGroups)
        {
            root.Level = 0;
            result.Add(root);
            AddChildren(root.Id, 1);
        }

        // Ако има останали неотчетени групи
        var processedIds = new HashSet<int>(result.Select(x => x.Id));
        foreach (var item in rawList.Where(x => !processedIds.Contains(x.Id)))
        {
            result.Add(item);
        }

        return result;
    }
}
