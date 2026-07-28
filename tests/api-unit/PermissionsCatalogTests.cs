using ImportControlTower.Domain.Constants;
using Xunit;

namespace ImportControlTower.ApiUnitTests;

public class PermissionsCatalogTests
{
    [Fact]
    public void PermissionsCatalog_ShouldContainExactly32Permissions()
    {
        // Act
        var count = PermissionsCatalog.All.Count;

        // Assert
        Assert.Equal(32, count);
    }

    [Fact]
    public void PermissionsCatalog_AllCodesShouldBeUnique()
    {
        // Act
        var codes = PermissionsCatalog.All.Select(p => p.Code).ToList();
        var uniqueCodes = codes.Distinct().ToList();

        // Assert
        Assert.Equal(codes.Count, uniqueCodes.Count);
    }
}
