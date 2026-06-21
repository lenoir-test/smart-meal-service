using Grpc.Core;
using Sms.Test;

namespace SmartMealService.MockServer.Services;

public class SmsTestServiceImpl : SmsTestService.SmsTestServiceBase
{
    public override Task<GetMenuResponse> GetMenu(Google.Protobuf.WellKnownTypes.BoolValue request, ServerCallContext context)
    {
        var response = new GetMenuResponse
        {
            Success = true,
            ErrorMessage = ""
        };

        response.MenuItems.Add(new MenuItem
        {
            Id = "5979224",
            Article = "A1004292",
            Name = "Каша гречневая",
            Price = 50,
            IsWeighted = false,
            FullPath = "ПРОИЗВОДСТВО\\Гарниры",
            Barcodes = { "57890975627974236429" }
        });

        response.MenuItems.Add(new MenuItem
        {
            Id = "9084246",
            Article = "A1004293",
            Name = "Конфеты Коровка",
            Price = 300,
            IsWeighted = true,
            FullPath = "ДЕСЕРТЫ\\Развес"
        });

        return Task.FromResult(response);
    }

    public override Task<SendOrderResponse> SendOrder(Order request, ServerCallContext context)
    {
        // Simple mock: always success
        return Task.FromResult(new SendOrderResponse
        {
            Success = true,
            ErrorMessage = ""
        });
    }
}
