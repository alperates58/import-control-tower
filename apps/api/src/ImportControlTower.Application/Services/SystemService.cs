using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Common;

namespace ImportControlTower.Application.Services;

public interface ISystemService
{
    Task<SystemInfoDto> GetSystemInfoAsync(CancellationToken cancellationToken = default);
}

public interface IDatabaseHealthChecker
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}

public class SystemService : ISystemService
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IDatabaseHealthChecker _dbHealthChecker;

    public SystemService(IDateTimeProvider dateTimeProvider, IDatabaseHealthChecker dbHealthChecker)
    {
        _dateTimeProvider = dateTimeProvider;
        _dbHealthChecker = dbHealthChecker;
    }

    public async Task<SystemInfoDto> GetSystemInfoAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var istanbulTimeStr = utcNow.ToIstanbulTime().ToString("yyyy-MM-dd HH:mm:ss 'TRT (UTC+3)'");
        var canConnect = await _dbHealthChecker.CanConnectAsync(cancellationToken);
        var dbStatus = canConnect ? "Connected" : "Disconnected";
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

        return new SystemInfoDto(
            AppName: "Import Control Tower API",
            Version: "0.1.0-foundation",
            Environment: environmentName,
            ServerTimeUtc: utcNow,
            ServerTimeIstanbul: istanbulTimeStr,
            DatabaseStatus: dbStatus
        );
    }
}
