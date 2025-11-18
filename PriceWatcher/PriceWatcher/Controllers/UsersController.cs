using Microsoft.AspNetCore.Mvc;
using PriceWatcher.Services.Interfaces;

namespace PriceWatcher.Controllers;

[ApiController]
[Route("users")]
public class UsersController : ControllerBase
{
    private readonly ISearchHistoryService _historyService;

    public UsersController(ISearchHistoryService historyService)
    {
        _historyService = historyService;
    }

    [HttpGet("{id:int}/history")]
    public async Task<IActionResult> GetHistory(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var history = await _historyService.GetUserHistoryAsync(id, page, pageSize, cancellationToken);
        return Ok(history);
    }

    [HttpDelete("{id:int}/history/{historyId:int}")]
    public async Task<IActionResult> DeleteHistory(int id, int historyId, CancellationToken cancellationToken = default)
    {
        var deleted = await _historyService.DeleteHistoryAsync(id, historyId, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}

