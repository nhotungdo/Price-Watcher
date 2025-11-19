using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PriceWatcher.Services.Interfaces;
using System.Security.Claims;

namespace PriceWatcher.Pages
{
    [Authorize]
    public class HistoryModel : PageModel
    {
        private readonly ISearchHistoryService _historyService;
        private readonly ILogger<HistoryModel> _logger;

        public List<Dtos.SearchHistoryDto> Items { get; private set; } = new();

        public HistoryModel(ISearchHistoryService historyService, ILogger<HistoryModel> logger)
        {
            _historyService = historyService;
            _logger = logger;
        }

        public async Task OnGet()
        {
            var userIdStr = User.FindFirst("uid")?.Value ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdStr, out var userId))
            {
                Items = (await _historyService.GetUserHistoryAsync(userId, 1, 50)).ToList();
            }
            else
            {
                _logger.LogWarning("Cannot resolve user id from claims");
            }
        }
    }
}