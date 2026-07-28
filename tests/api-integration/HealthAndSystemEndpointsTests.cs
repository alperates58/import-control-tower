using System.Net;
using System.Net.Http.Json;
using ImportControlTower.Application.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

public class HealthAndSystemEndpointsTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthAndSystemEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_ReturnsSuccessStatusCode()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    [Fact]
    public async Task SystemInfo_ReturnsValidSystemInfoDto()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/v1/system/info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var info = await response.Content.ReadFromJsonAsync<SystemInfoDto>();
        Assert.NotNull(info);
        Assert.Equal("Import Control Tower API", info.AppName);
        Assert.Equal("0.1.0-foundation", info.Version);
    }
}
