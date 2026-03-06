using Microsoft.AspNetCore.Mvc;
using ProgrammableDb.Api.Models;
using ProgrammableDb.Api.Services;

namespace ProgrammableDb.Api.Controllers;

[ApiController]
[Route("api/query/{datasource}")]
public sealed class QueryController(DataSourceStore store, DbExecutionService db) : ControllerBase
{
    [HttpPost("sql")]
    public async Task<IActionResult> Sql(string datasource, [FromBody] SqlQueryRequest request, CancellationToken ct)
    {
        if (!store.TryGet(datasource, out var source) || source is null)
        {
            return NotFound("Datasource not found");
        }

        var rows = await db.RunReadOnlySqlAsync(source, request, ct);
        return Ok(rows);
    }
}
