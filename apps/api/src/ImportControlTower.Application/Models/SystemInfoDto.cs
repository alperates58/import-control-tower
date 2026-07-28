namespace ImportControlTower.Application.Models;

public record SystemInfoDto(
    string AppName,
    string Version,
    string Environment,
    DateTime ServerTimeUtc,
    string ServerTimeIstanbul,
    string DatabaseStatus
);
