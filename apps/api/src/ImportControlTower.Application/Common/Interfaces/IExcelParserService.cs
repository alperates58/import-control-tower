using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ImportControlTower.Application.Common.Interfaces;

public record ParsedRowResult(
    int RowNumber,
    Dictionary<string, string> RawValues,
    Dictionary<string, string> NormalizedValues,
    List<string> ErrorCodes,
    List<string> WarningCodes
);

public record ExcelParseResult(
    bool IsValid,
    List<string> SecurityErrors,
    Dictionary<string, string> AutoColumnMapping,
    List<string> UnmappedColumns,
    List<string> MissingRequiredColumns,
    List<ParsedRowResult> Rows
);

public interface IExcelParserService
{
    Task<ExcelParseResult> ParseAndValidateAsync(
        Stream stream,
        string fileName,
        Dictionary<string, string>? customMapping = null,
        CancellationToken cancellationToken = default);

    byte[] GenerateTemplateWorkbook();
}
