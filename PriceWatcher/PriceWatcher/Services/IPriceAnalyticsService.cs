using System.Threading.Tasks;
using PriceWatcher.Dtos;

namespace PriceWatcher.Services
{
    public interface IPriceAnalyticsService
    {
        Task<PriceHistoryResponseDto> GetPriceHistoryAsync(int productId, int days = 90, string granularity = "daily");
        Task<BuySignalDto> GetBuySignalAsync(int productId, int horizon = 7);
    }
}
