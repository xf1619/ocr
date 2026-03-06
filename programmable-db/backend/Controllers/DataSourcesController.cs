using Microsoft.AspNetCore.Mvc;
using ProgrammableDb.Api.Models;
using ProgrammableDb.Api.Services;

namespace ProgrammableDb.Api.Controllers;

[ApiController]
[Route("api/admin/datasources")]
public sealed class DataSourcesController(DataSourceStore store, DbExecutionService db) : ControllerBase
{
    [HttpGet]
    public IActionResult List() => Ok(store.List());

    [HttpPost]
    public IActionResult Create([FromBody] CreateDataSourceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest("name and connectionString are required");
        }

        var source = store.Add(request);
        return Created($"/api/admin/datasources/{source.Id}", source);
    }

    [HttpPost("{id}/test")]
    public async Task<IActionResult> TestConnection(string id, CancellationToken ct)
    {
        if (!store.TryGet(id, out var source) || source is null)
        {
            return NotFound();
        }

        await db.TestConnectionAsync(source, ct);
        return Ok(new { source.Id, source.Name, status = "ok" });
    }
}
