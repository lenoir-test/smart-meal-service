using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SmartMealService.Domain.Entities;

namespace SmartMealService.ConsoleApp.Data;

public class DapperRepository
{
    private readonly string _connectionString;

    public DapperRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new ArgumentNullException("Connection string not found");
    }

    private IDbConnection CreateConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }

    public async Task InitializeDatabaseAsync()
    {
        // в проде тут по-хорошему надо накатывать миграции (например через FluentMigrator или EF),
        // но здесь будем работать с IF NOT EXISTS.
        // предполагаем, что БД уже создана
        var createTableQuery = @"
            CREATE TABLE IF NOT EXISTS Dishes (
                Id VARCHAR(255) PRIMARY KEY,
                Article VARCHAR(255) NOT NULL,
                Name VARCHAR(500) NOT NULL,
                Price DOUBLE PRECISION NOT NULL,
                IsWeighted BOOLEAN NOT NULL,
                FullPath VARCHAR(1000)
            );
        ";

        using var connection = CreateConnection();
        await connection.ExecuteAsync(createTableQuery);
    }

    public async Task InsertDishesAsync(IEnumerable<Dish> dishes)
    {
        var insertQuery = @"
            INSERT INTO Dishes (Id, Article, Name, Price, IsWeighted, FullPath)
            VALUES (@Id, @Article, @Name, @Price, @IsWeighted, @FullPath)
            ON CONFLICT (Id) DO UPDATE 
            SET Article = EXCLUDED.Article,
                Name = EXCLUDED.Name,
                Price = EXCLUDED.Price,
                IsWeighted = EXCLUDED.IsWeighted,
                FullPath = EXCLUDED.FullPath;
        ";

        using var connection = CreateConnection();
        // Dapper сам разрулит IEnumerable под капотом, циклы не требуются
        await connection.ExecuteAsync(insertQuery, dishes);
    }
}
