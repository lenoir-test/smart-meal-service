using Grpc.Net.Client;
using Sms.Test;
using SmartMealService.Domain.Entities;
using SmartMealService.Domain.Interfaces;

namespace SmartMealService.ApiClient.Clients;

public class SmsGrpcClient : ISmsClient
{
    private readonly SmsTestService.SmsTestServiceClient _client;

    public SmsGrpcClient(SmsTestService.SmsTestServiceClient client)
    {
        _client = client;
    }

    public async Task<(bool Success, List<Dish>? Dishes, string? ErrorMessage)> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var response = await _client.GetMenuAsync(new Google.Protobuf.WellKnownTypes.BoolValue { Value = true }, cancellationToken: cancellationToken);

        if (!response.Success)
        {
            return (false, null, response.ErrorMessage);
        }

        var dishes = response.MenuItems.Select(m => new Dish
        {
            Id = m.Id,
            Article = m.Article,
            Name = m.Name,
            Price = m.Price,
            IsWeighted = m.IsWeighted,
            FullPath = m.FullPath,
            Barcodes = m.Barcodes.ToList()
        }).ToList();
        
        return (true, dishes, null);
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOrderAsync(Domain.Entities.Order order, CancellationToken cancellationToken = default)
    {
        var grpcOrder = new Sms.Test.Order
        {
            Id = order.Id
        };
        grpcOrder.OrderItems.AddRange(order.OrderItems.Select(item => new Sms.Test.OrderItem
        {
            Id = item.Id,
            Quantity = item.Quantity
        }));

        var response = await _client.SendOrderAsync(grpcOrder, cancellationToken: cancellationToken);

        if (!response.Success)
        {
            return (false, response.ErrorMessage);
        }

        return (true, null);
    }
}
