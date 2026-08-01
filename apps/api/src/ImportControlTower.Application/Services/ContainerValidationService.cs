using System;
using System.Text.RegularExpressions;

namespace ImportControlTower.Application.Services;

public interface IContainerValidationService
{
    string NormalizeContainerNumber(string raw);
    bool IsValidFormat(string normalized);
    bool VerifyCheckDigit(string normalized);
}

public class ContainerValidationService : IContainerValidationService
{
    private static readonly Regex IsoRegex = new(@"^[A-Z]{3}[UJZ]\d{7}$", RegexOptions.Compiled);

    public string NormalizeContainerNumber(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        return raw.Replace("-", "").Replace(" ", "").Trim().ToUpperInvariant();
    }

    public bool IsValidFormat(string normalized)
    {
        if (string.IsNullOrWhiteSpace(normalized)) return false;
        return IsoRegex.IsMatch(normalized);
    }

    public bool VerifyCheckDigit(string normalized)
    {
        if (!IsValidFormat(normalized)) return false;

        // ISO 6346 Check Digit Algorithm
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char ch = normalized[i];
            int charValue = GetCharacterValue(ch);
            int weight = (int)Math.Pow(2, i);
            sum += charValue * weight;
        }

        int expectedCheckDigit = sum % 11;
        if (expectedCheckDigit == 10) expectedCheckDigit = 0;

        int actualCheckDigit = normalized[10] - '0';
        return expectedCheckDigit == actualCheckDigit;
    }

    private static int GetCharacterValue(char ch)
    {
        if (char.IsDigit(ch)) return ch - '0';

        // Letters map to numbers (skipping multiples of 11: 11, 22, 33)
        // A=10, B=12... K=21, L=23... V=34, W=35, X=36, Y=37, Z=38
        return ch switch
        {
            'A' => 10, 'B' => 12, 'C' => 13, 'D' => 14, 'E' => 15,
            'F' => 16, 'G' => 17, 'H' => 18, 'I' => 19, 'J' => 20,
            'K' => 21, 'L' => 23, 'M' => 24, 'N' => 25, 'O' => 26,
            'P' => 27, 'Q' => 28, 'R' => 29, 'S' => 30, 'T' => 31,
            'U' => 32, 'V' => 34, 'W' => 35, 'X' => 36, 'Y' => 37,
            'Z' => 38, _ => 0
        };
    }
}
