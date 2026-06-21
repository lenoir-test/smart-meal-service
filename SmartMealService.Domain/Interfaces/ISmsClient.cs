using SmartMealService.Domain.Entities;

namespace SmartMealService.Domain.Interfaces;

public interface ISmsClient
{
    Task<(bool Success, List<Dish>? Dishes, string? ErrorMessage)> GetMenuAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string? ErrorMessage)> SendOrderAsync(Order order, CancellationToken cancellationToken = default);
}
