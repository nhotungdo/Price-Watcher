using System.Collections.Generic;
using System.Threading.Tasks;
using PriceWatcher.Dtos;

namespace PriceWatcher.Services
{
    public interface IProductComparisonService
    {
        Task<List<ProductComparisonDto>> GetProductComparisonsAsync(int mappingId, string currency = "VND", string sort = "price", bool onlyAvailable = false);
        Task<List<ProductComparisonDto>> GetProductComparisonsByProductIdAsync(int productId);
    }
}
