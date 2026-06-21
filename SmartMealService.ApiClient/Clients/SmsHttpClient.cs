using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using SmartMealService.Domain.Entities;
using SmartMealService.Domain.Interfaces;

namespace SmartMealService.ApiClient.Clients;

public class SmsHttpClient : ISmsClient
{
    private readonly HttpClient _httpClient;

    public SmsHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        // хардкодим креды для basic auth в рамках тестового задания
        var authToken = System.Text.Encoding.ASCII.GetBytes("username:password");
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
    }

    public async Task<(bool Success, List<Dish>? Dishes, string? ErrorMessage)> GetMenuAsync(CancellationToken cancellationToken = default)
    {
        var request = new
        {
            Command = "GetMenu",
            CommandParameters = new { WithPrice = true }
        };

        var response = await _httpClient.PostAsJsonAsync("", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<SmsResponse<MenuData>>(cancellationToken: cancellationToken);
        
        if (content is null)
            return (false, null, "Empty response from server.");

        if (!content.Success)
            return (false, null, content.ErrorMessage);

        return (true, content.Data?.MenuItems ?? new List<Dish>(), null);
    }

    public async Task<(bool Success, string? ErrorMessage)> SendOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            Command = "SendOrder",
            CommandParameters = new
            {
                OrderId = order.Id,
                MenuItems = order.OrderItems.Select(item => new
                {
                    Id = item.Id,
                    Quantity = item.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)
                }).ToList()
            }
        };

        var response = await _httpClient.PostAsJsonAsync("", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadFromJsonAsync<SmsResponse<object>>(cancellationToken: cancellationToken);
        
        if (content is null)
            return (false, "Empty response from server.");

        if (!content.Success)
            return (false, content.ErrorMessage);

        return (true, null);
    }

    // DTO-шки для маппинга JSON
    private class SmsResponse<T>
    {
        public string Command { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public T? Data { get; set; }
    }

    private class MenuData
    {
        public List<Dish> MenuItems { get; set; } = new();
    }
}
