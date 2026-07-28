using ImportControlTower.Application.Services;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Infrastructure.Persistence;

public class DatabaseHealthChecker : IDatabaseHealthChecker
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseHealthChecker(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dbContext.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }
}
