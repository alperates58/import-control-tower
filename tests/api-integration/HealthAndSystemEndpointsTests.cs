using System.Net;
using System.Net.Http.Json;
using ImportControlTower.Application.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

[Collection("IntegrationTests")]
public class HealthAndSystemEndpointsTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthAndSystemEndpointsTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthLive_ReturnsSuccessStatusCode()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/health/live");

        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SystemInfo_ReturnsValidSystemInfoDto()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/system/info");

        response.EnsureSuccessStatusCode();
        var systemInfo = await response.Content.ReadFromJsonAsync<SystemInfoDto>();

        Assert.NotNull(systemInfo);
        Assert.Equal("Import Control Tower API", systemInfo.AppName);
        Assert.False(string.IsNullOrWhiteSpace(systemInfo.Environment));
    }
}
