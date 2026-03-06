using System.Collections.Concurrent;
using ProgrammableDb.Api.Models;

namespace ProgrammableDb.Api.Services;

public sealed class DataSourceStore
{
    private readonly ConcurrentDictionary<string, DataSourceDefinition> _sources = new(StringComparer.OrdinalIgnoreCase);

    public DataSourceStore()
    {
        var defaultSqlite = new DataSourceDefinition
        {
            Id = "default-sqlite",
            Name = "Default SQLite",
            Provider = "sqlite",
            ConnectionString = "Data Source=app-data.db"
        };
        _sources[defaultSqlite.Id] = defaultSqlite;
    }

    public IReadOnlyCollection<DataSourceDefinition> List() => _sources.Values.ToArray();

    public DataSourceDefinition Add(CreateDataSourceRequest request)
    {
        var source = new DataSourceDefinition
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = request.Name,
            Provider = request.Provider.ToLowerInvariant(),
            ConnectionString = request.ConnectionString
        };

        _sources[source.Id] = source;
        return source;
    }

    public bool TryGet(string id, out DataSourceDefinition? source) => _sources.TryGetValue(id, out source);
}
