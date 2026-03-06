using Microsoft.AspNetCore.Mvc;
using ProgrammableDb.Api.Models;
using ProgrammableDb.Api.Services;

namespace ProgrammableDb.Api.Controllers;

[ApiController]
[Route("api/data/{datasource}/{table}")]
public sealed class DataController(DataSourceStore store, DbExecutionService db) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Insert(string datasource, string table, [FromBody] CreateRowRequest request, CancellationToken ct)
    {
        var source = GetSource(datasource);
        if (source is null) return NotFound("Datasource not found");

        var id = await db.InsertAsync(source, table, request.Values, ct);
        return Ok(new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Select(string datasource, string table, [FromQuery] int limit = 50, [FromQuery] int offset = 0, CancellationToken ct = default)
    {
        var source = GetSource(datasource);
        if (source is null) return NotFound("Datasource not found");

        limit = Math.Clamp(limit, 1, 200);
        offset = Math.Max(0, offset);

        var rows = await db.SelectAsync(source, table, limit, offset, ct);
        return Ok(rows);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string datasource, string table, string id, [FromBody] UpdateRowRequest request, CancellationToken ct)
    {
        var source = GetSource(datasource);
        if (source is null) return NotFound("Datasource not found");

        var count = await db.UpdateByIdAsync(source, table, id, request.Values, ct);
        return Ok(new { affected = count });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string datasource, string table, string id, CancellationToken ct)
    {
        var source = GetSource(datasource);
        if (source is null) return NotFound("Datasource not found");

        var count = await db.DeleteByIdAsync(source, table, id, ct);
        return Ok(new { affected = count });
    }

    private DataSourceDefinition? GetSource(string id)
    {
        return store.TryGet(id, out var source) ? source : null;
    }
}
