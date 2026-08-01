using ImportControlTower.Application.Services;
using Xunit;

namespace ImportControlTower.ApiUnitTests;

public class ContainerValidationTests
{
    private readonly ContainerValidationService _validator = new();

    [Theory]
    [InlineData("CSQU3054383", true)] // Valid ISO 6346 container number with check digit 3
    [InlineData("MSCU1234567", false)] // Invalid check digit
    [InlineData("CSQU-305438-3", true)] // Formatted with hyphens
    [InlineData("INVALID123", false)] // Invalid format
    public void Test_Container_Validation(string containerNum, bool expectedValid)
    {
        var normalized = _validator.NormalizeContainerNumber(containerNum);
        bool isValidFormat = _validator.IsValidFormat(normalized);

        if (!isValidFormat)
        {
            Assert.False(expectedValid);
            return;
        }

        bool checkDigitResult = _validator.VerifyCheckDigit(normalized);
        Assert.Equal(expectedValid, checkDigitResult);
    }
}
