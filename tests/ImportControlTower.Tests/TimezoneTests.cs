using System;
using ImportControlTower.Application.Services;
using Xunit;

namespace ImportControlTower.Tests;

public class TimezoneTests
{
    private readonly TimezoneService _timezoneService = new();

    [Theory]
    [InlineData("Europe/Istanbul", true)]
    [InlineData("Asia/Shanghai", true)]
    [InlineData("UTC", true)]
    [InlineData("Invalid/Timezone_Name", false)]
    public void Test_Timezone_Validation(string tzId, bool expected)
    {
        bool result = _timezoneService.IsValidTimezoneId(tzId);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Test_Local_To_Utc_Conversion()
    {
        var localTime = new DateTime(2026, 7, 30, 14, 0, 0); // 14:00 Istanbul (+03:00)
        var utcTime = _timezoneService.ConvertLocalToUtc(localTime, "Europe/Istanbul", out string? errorCode);

        Assert.Null(errorCode);
        Assert.Equal(new DateTime(2026, 7, 30, 11, 0, 0, DateTimeKind.Utc), utcTime);
    }
}
