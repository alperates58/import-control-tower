using System;
using System.Threading.Tasks;
using ImportControlTower.Application.Services;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ImportControlTower.Infrastructure.Services;

public class DocumentNumberGenerator : IDocumentNumberGenerator
{
    public async Task<string> GenerateCaseNumberAsync(object dbContextObj, int year)
    {
        if (dbContextObj is not ApplicationDbContext dbContext)
        {
            throw new ArgumentException("Invalid DbContext type.");
        }

        var sql = @"
            INSERT INTO document_number_counters (""DocumentType"", ""Year"", ""LastNumber"", ""UpdatedAtUtc"")
            VALUES ('ImportCase', @p0, 1, NOW())
            ON CONFLICT (""DocumentType"", ""Year"")
            DO UPDATE SET 
                ""LastNumber"" = document_number_counters.""LastNumber"" + 1,
                ""UpdatedAtUtc"" = NOW()
            RETURNING ""LastNumber"";
        ";

        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        
        var currentTransaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        if (currentTransaction != null)
        {
            command.Transaction = currentTransaction;
        }

        var param = command.CreateParameter();
        param.ParameterName = "@p0";
        param.Value = year;
        command.Parameters.Add(param);

        var result = await command.ExecuteScalarAsync();
        long lastNumber = Convert.ToInt64(result);

        return $"IMP-{year}-{lastNumber:D6}";
    }
}
