using System.Text.Json;
using SmartMealService.MockServer.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();

var app = builder.Build();

app.MapGrpcService<SmsTestServiceImpl>();

// Simple Basic Auth middleware for HTTP Endpoint
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/api/v1/sms")
    {
        if (!context.Request.Headers.ContainsKey("Authorization"))
        {
            context.Response.StatusCode = 401;
            return;
        }
    }
    await next(context);
});

app.MapPost("/api/v1/sms", async (HttpContext context) =>
{
    using var reader = new StreamReader(context.Request.Body);
    var body = await reader.ReadToEndAsync();
    
    using var jsonDoc = JsonDocument.Parse(body);
    var command = jsonDoc.RootElement.GetProperty("Command").GetString();

    if (command == "GetMenu")
    {
        return Results.Ok(new
        {
            Command = "GetMenu",
            Success = true,
            ErrorMessage = "",
            Data = new
            {
                MenuItems = new[]
                {
                    new
                    {
                        Id = "5979224",
                        Article = "A1004292",
                        Name = "Каша гречневая",
                        Price = 50,
                        IsWeighted = false,
                        FullPath = "ПРОИЗВОДСТВО\\Гарниры",
                        Barcodes = new[] { "57890975627974236429" }
                    },
                    new
                    {
                        Id = "9084246",
                        Article = "A1004293",
                        Name = "Конфеты Коровка",
                        Price = 300,
                        IsWeighted = true,
                        FullPath = "ДЕСЕРТЫ\\Развес",
                        Barcodes = Array.Empty<string>()
                    }
                }
            }
        });
    }

    if (command == "SendOrder")
    {
        return Results.Ok(new
        {
            Command = "SendOrder",
            Success = true,
            ErrorMessage = ""
        });
    }

    return Results.Ok(new { Success = false, ErrorMessage = "Unknown command" });
});

app.Run();
