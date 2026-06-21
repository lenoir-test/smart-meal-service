using System.Globalization;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Sms.Test;
using SmartMealService.ApiClient.Clients;
using SmartMealService.ConsoleApp.Data;
using SmartMealService.Domain.Entities;
using SmartMealService.Domain.Interfaces;

namespace SmartMealService.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        var logFileName = $"test-sms-console-app-{DateTime.Now:yyyyMMdd}.log";
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File(logFileName)
            .CreateLogger();

        try
        {
            var host = CreateHostBuilder(args).Build();

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;

            var repository = services.GetRequiredService<DapperRepository>();
            var smsClient = services.GetRequiredService<ISmsClient>();

            Log.Information("Инициализация базы данных...");
            await repository.InitializeDatabaseAsync();

            Log.Information("Получение меню от сервера...");
            var menuResult = await smsClient.GetMenuAsync();
            if (!menuResult.Success)
            {
                Log.Error("{ErrorMessage}", menuResult.ErrorMessage);
                return;
            }
            var menu = menuResult.Dishes!;

            Log.Information("Сохранение меню в БД...");
            await repository.InsertDishesAsync(menu);

            Log.Information("--- МЕНЮ ---");
            foreach (var dish in menu)
            {
                Log.Information("{Name} – {Article} – {Price}", dish.Name, dish.Article, dish.Price);
            }

            var order = new Domain.Entities.Order();

            bool isValidInput = false;
            while (!isValidInput)
            {
                Log.Information("Введите список блюд в формате Код1:Количество1;Код2:Количество2;...");
                // юзер вводит данные. ReadLine блочит поток, но для консольного приложения пойдет
                // сразу пишем в лог для удобного дебага
                Console.Write("> ");
                var input = Console.ReadLine();
                Log.Information($"Ввод пользователя: {input}");

                if (string.IsNullOrWhiteSpace(input))
                {
                    Log.Warning("Пустой ввод. Попробуйте снова.");
                    continue;
                }

                var items = input.Split(';', StringSplitOptions.RemoveEmptyEntries);
                var validItems = new List<Domain.Entities.OrderItem>();
                bool hasErrors = false;

                foreach (var itemStr in items)
                {
                    var parts = itemStr.Split(':');
                    if (parts.Length != 2)
                    {
                        Log.Warning($"Некорректный формат позиции: {itemStr}");
                        hasErrors = true;
                        break;
                    }

                    var article = parts[0].Trim();
                    var qtyStr = parts[1].Trim();

                    // фикс плавающей точки: меняем запятую на точку, иначе InvariantCulture может упасть на русской локали
                    qtyStr = qtyStr.Replace(',', '.');

                    if (!double.TryParse(qtyStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double quantity) || quantity <= 0)
                    {
                        Log.Warning($"Некорректное количество для кода {article}: {parts[1]}");
                        hasErrors = true;
                        break;
                    }

                    var dish = menu.FirstOrDefault(d => d.Article == article);
                    if (dish == null)
                    {
                        Log.Warning($"Блюдо с кодом (артикулом) {article} не найдено.");
                        hasErrors = true;
                        break;
                    }

                    validItems.Add(new Domain.Entities.OrderItem { Id = dish.Id, Quantity = quantity });
                }

                if (!hasErrors && validItems.Count > 0)
                {
                    order.OrderItems.AddRange(validItems);
                    isValidInput = true;
                }
            }

            Log.Information("Отправка заказа на сервер...");
            var orderResult = await smsClient.SendOrderAsync(order);

            if (orderResult.Success)
            {
                Log.Information("УСПЕХ");
            }
            else
            {
                Log.Error("{ErrorMessage}", orderResult.ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Произошла ошибка: {Message}", ex.Message);
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((hostContext, services) =>
            {
                var config = hostContext.Configuration;
                
                services.AddSingleton<DapperRepository>();
                
                var grpcUrl = config["ApiSettings:GrpcServerUrl"] ?? "http://localhost:5000";
                
                services.AddScoped<ISmsClient>(sp =>
                {
                    var httpHandler = new HttpClientHandler();
                    httpHandler.ServerCertificateCustomValidationCallback = 
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        
                    var channel = GrpcChannel.ForAddress(grpcUrl, new GrpcChannelOptions 
                    { 
                        HttpHandler = httpHandler 
                    });
                    var grpcClient = new SmsTestService.SmsTestServiceClient(channel);
                    return new SmsGrpcClient(grpcClient);
                });
            });
}
