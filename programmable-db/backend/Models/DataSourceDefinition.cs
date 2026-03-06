namespace ProgrammableDb.Api.Models;

public sealed class DataSourceDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "sqlite";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class CreateDataSourceRequest
{
    public string Name { get; set; } = string.Empty;
    public string Provider { get; set; } = "sqlite";
    public string ConnectionString { get; set; } = string.Empty;
}
