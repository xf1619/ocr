namespace ProgrammableDb.Api.Models;

public sealed class CreateRowRequest
{
    public Dictionary<string, object?> Values { get; set; } = new();
}

public sealed class UpdateRowRequest
{
    public Dictionary<string, object?> Values { get; set; } = new();
}

public sealed class SqlQueryRequest
{
    public string Sql { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = new();
}
