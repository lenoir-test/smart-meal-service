namespace SmartMealService.Domain.Entities;

public class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public List<OrderItem> OrderItems { get; set; } = new();
}
