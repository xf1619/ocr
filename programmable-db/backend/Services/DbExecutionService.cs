using Microsoft.Data.Sqlite;
using ProgrammableDb.Api.Models;

namespace ProgrammableDb.Api.Services;

public sealed class DbExecutionService
{
    public async Task TestConnectionAsync(DataSourceDefinition source, CancellationToken ct)
    {
        if (!string.Equals(source.Provider, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Provider '{source.Provider}' is planned but not yet implemented in this MVP.");
        }

        await using var conn = new SqliteConnection(source.ConnectionString);
        await conn.OpenAsync(ct);
    }

    public async Task<long> InsertAsync(DataSourceDefinition source, string table, Dictionary<string, object?> values, CancellationToken ct)
    {
        await using var conn = CreateSqlite(source);
        await conn.OpenAsync(ct);

        var columns = values.Keys.ToArray();
        var parameters = columns.Select((c, i) => $"@p{i}").ToArray();
        var sql = $"INSERT INTO {Quote(table)} ({string.Join(",", columns.Select(Quote))}) VALUES ({string.Join(",", parameters)}); SELECT last_insert_rowid();";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        for (var i = 0; i < columns.Length; i++)
        {
            cmd.Parameters.AddWithValue(parameters[i], values[columns[i]] ?? DBNull.Value);
        }

        var result = await cmd.ExecuteScalarAsync(ct);
        return Convert.ToInt64(result);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> SelectAsync(DataSourceDefinition source, string table, int limit, int offset, CancellationToken ct)
    {
        await using var conn = CreateSqlite(source);
        await conn.OpenAsync(ct);

        var sql = $"SELECT * FROM {Quote(table)} LIMIT @limit OFFSET @offset";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@limit", limit);
        cmd.Parameters.AddWithValue("@offset", offset);

        return await ReadRowsAsync(cmd, ct);
    }

    public async Task<int> UpdateByIdAsync(DataSourceDefinition source, string table, string id, Dictionary<string, object?> values, CancellationToken ct)
    {
        await using var conn = CreateSqlite(source);
        await conn.OpenAsync(ct);

        var cols = values.Keys.ToArray();
        var setClause = string.Join(",", cols.Select((c, i) => $"{Quote(c)}=@p{i}"));
        var sql = $"UPDATE {Quote(table)} SET {setClause} WHERE id=@id";

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;

        for (var i = 0; i < cols.Length; i++)
        {
            cmd.Parameters.AddWithValue($"@p{i}", values[cols[i]] ?? DBNull.Value);
        }
        cmd.Parameters.AddWithValue("@id", id);

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<int> DeleteByIdAsync(DataSourceDefinition source, string table, string id, CancellationToken ct)
    {
        await using var conn = CreateSqlite(source);
        await conn.OpenAsync(ct);

        var sql = $"DELETE FROM {Quote(table)} WHERE id=@id";
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("@id", id);

        return await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<Dictionary<string, object?>>> RunReadOnlySqlAsync(DataSourceDefinition source, SqlQueryRequest request, CancellationToken ct)
    {
        EnsureReadOnlySql(request.Sql);

        await using var conn = CreateSqlite(source);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = request.Sql;
        foreach (var pair in request.Parameters)
        {
            cmd.Parameters.AddWithValue($"@{pair.Key}", pair.Value ?? DBNull.Value);
        }

        return await ReadRowsAsync(cmd, ct);
    }

    private static SqliteConnection CreateSqlite(DataSourceDefinition source)
    {
        if (!string.Equals(source.Provider, "sqlite", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Provider '{source.Provider}' is planned but not yet implemented in this MVP.");
        }

        return new SqliteConnection(source.ConnectionString);
    }

    private static string Quote(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || identifier.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_')))
        {
            throw new ArgumentException("Invalid identifier.");
        }

        return $"\"{identifier}\"";
    }

    private static void EnsureReadOnlySql(string sql)
    {
        var normalized = sql.Trim().ToUpperInvariant();
        if (!normalized.StartsWith("SELECT"))
        {
            throw new InvalidOperationException("Only SELECT queries are allowed in this endpoint.");
        }

        if (normalized.Contains(';'))
        {
            throw new InvalidOperationException("Multi-statement SQL is not allowed.");
        }
    }

    private static async Task<IReadOnlyList<Dictionary<string, object?>>> ReadRowsAsync(SqliteCommand cmd, CancellationToken ct)
    {
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, ct) ? null : reader.GetValue(i);
            }
            rows.Add(row);
        }
        return rows;
    }
}
