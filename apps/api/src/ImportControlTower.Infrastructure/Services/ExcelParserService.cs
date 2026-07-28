using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using ImportControlTower.Application.Common.Interfaces;

namespace ImportControlTower.Infrastructure.Services;

public class ExcelParserService : IExcelParserService
{
    private static readonly CultureInfo TrCulture = new CultureInfo("tr-TR");

    private static readonly Dictionary<string, List<string>> TargetColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["OrderNumber"] = new() { "siparis no", "sipariş no", "siparis numarasi", "sipariş numarası", "order no", "purchase order no", "po no" },
        ["SupplierName"] = new() { "firma adi", "firma adı", "firma", "tedarikci", "tedarikçi", "supplier", "supplier name" },
        ["OrderDate"] = new() { "siparis tarihi", "sipariş tarihi", "order date", "po date" },
        ["StockCode"] = new() { "stok kodu", "malzeme kodu", "urun kodu", "ürün kodu", "stock code", "material code", "item code" },
        ["StockName"] = new() { "stok ismi", "stok adi", "stok adı", "malzeme adı", "ürün adı", "stock name", "item name", "description" },
        ["OrderedQuantity"] = new() { "siparis miktari", "sipariş miktarı", "miktar", "order quantity", "ordered qty", "quantity" },
        ["RemainingQuantity"] = new() { "siparis kalan miktari", "sipariş kalan miktarı", "kalan miktar", "acik miktar", "açık miktar", "remaining quantity", "open quantity", "remaining qty" },
        ["SasDate"] = new() { "sas tarihi", "sas", "requested date", "required date", "need by date" }
    };

    private static readonly HashSet<string> RequiredTargets = new(StringComparer.OrdinalIgnoreCase)
    {
        "OrderNumber", "SupplierName", "OrderDate", "StockCode", "StockName", "OrderedQuantity", "RemainingQuantity"
    };

    public Task<ExcelParseResult> ParseAndValidateAsync(
        Stream stream,
        string fileName,
        Dictionary<string, string>? customMapping = null,
        CancellationToken cancellationToken = default)
    {
        var securityErrors = new List<string>();

        // 1. File extension validation
        var ext = Path.GetExtension(fileName);
        if (!string.Equals(ext, ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            securityErrors.Add("FILE_INVALID_EXTENSION");
            return Task.FromResult(new ExcelParseResult(false, securityErrors, new(), new(), new(), new()));
        }

        // 2. Stream length validation (10 MB max)
        if (stream.Length > 10 * 1024 * 1024)
        {
            securityErrors.Add("FILE_TOO_LARGE");
            return Task.FromResult(new ExcelParseResult(false, securityErrors, new(), new(), new(), new()));
        }

        // 3. OpenXML Package-wide Security Scan
        var openXmlErrors = PerformOpenXmlSecurityScan(stream);
        if (openXmlErrors.Count > 0)
        {
            return Task.FromResult(new ExcelParseResult(false, openXmlErrors, new(), new(), new(), new()));
        }

        // Reset stream position for ExcelDataReader
        stream.Position = 0;

        // System.Text.Encoding.RegisterProvider required for ExcelDataReader
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        // 4. Read Rows via Forward-Only ExcelDataReader (NO AsDataSet)
        using var reader = ExcelReaderFactory.CreateReader(stream);

        if (reader == null)
        {
            securityErrors.Add("WORKBOOK_CORRUPTED");
            return Task.FromResult(new ExcelParseResult(false, securityErrors, new(), new(), new(), new()));
        }

        // Read Sheet 1 header
        if (!reader.Read())
        {
            securityErrors.Add("WORKBOOK_CORRUPTED");
            return Task.FromResult(new ExcelParseResult(false, securityErrors, new(), new(), new(), new()));
        }

        var colCount = reader.FieldCount;
        if (colCount > 100)
        {
            securityErrors.Add("TOO_MANY_COLUMNS");
            return Task.FromResult(new ExcelParseResult(false, securityErrors, new(), new(), new(), new()));
        }

        var headerNames = new List<string>();
        for (int i = 0; i < colCount; i++)
        {
            var headerVal = reader.GetValue(i)?.ToString()?.Trim() ?? $"Column_{i + 1}";
            headerNames.Add(headerVal);
        }

        // Determine column mapping
        var autoMapping = new Dictionary<string, string>();
        var unmapped = new List<string>();
        var missingRequired = new List<string>();

        var mappingToUse = customMapping ?? ResolveAutoMapping(headerNames, autoMapping, unmapped, missingRequired);

        if (customMapping != null)
        {
            // Validate custom mapping
            missingRequired = RequiredTargets.Where(req => !mappingToUse.ContainsValue(req)).ToList();
        }

        var rows = new List<ParsedRowResult>();
        int rowNum = 1;

        while (reader.Read())
        {
            rowNum++;
            if (rowNum > 20001) // Header is row 1
            {
                securityErrors.Add("TOO_MANY_ROWS");
                return Task.FromResult(new ExcelParseResult(false, securityErrors, autoMapping, unmapped, missingRequired, rows));
            }

            var rawValues = new Dictionary<string, string>();
            bool isRowEmpty = true;

            for (int i = 0; i < colCount; i++)
            {
                var colName = headerNames[i];
                var rawObj = reader.GetValue(i);
                var rawStr = rawObj?.ToString() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(rawStr))
                {
                    isRowEmpty = false;
                }

                if (rawStr.Length > 2000)
                {
                    rawStr = rawStr.Substring(0, 2000);
                }

                rawValues[colName] = rawStr;
            }

            if (isRowEmpty)
            {
                rows.Add(new ParsedRowResult(rowNum, rawValues, new(), new(), new() { "EMPTY_TRAILING_ROW_IGNORED" }));
                continue;
            }

            var normalizedValues = new Dictionary<string, string>();
            var rowErrors = new List<string>();
            var rowWarnings = new List<string>();

            // Parse each target column
            foreach (var kvp in mappingToUse)
            {
                var colHeader = kvp.Key;
                var targetField = kvp.Value;

                if (string.Equals(targetField, "IGNORE", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                rawValues.TryGetValue(colHeader, out var rawVal);
                rawVal ??= string.Empty;

                ParseTargetField(targetField, rawVal, normalizedValues, rowErrors, rowWarnings);
            }

            // Check required fields
            foreach (var req in RequiredTargets)
            {
                if (!normalizedValues.TryGetValue(req, out var val) || string.IsNullOrWhiteSpace(val))
                {
                    var reqErrCode = $"{req.ToUpperInvariant()}_REQUIRED";
                    if (!rowErrors.Contains(reqErrCode) && !rowErrors.Contains($"{req}_REQUIRED"))
                    {
                        rowErrors.Add(reqErrCode);
                    }
                }
            }

            rows.Add(new ParsedRowResult(rowNum, rawValues, normalizedValues, rowErrors, rowWarnings));
        }

        bool hasGlobalErrors = securityErrors.Count > 0 || missingRequired.Count > 0;
        bool hasRowErrors = rows.Any(r => r.ErrorCodes.Count > 0);

        return Task.FromResult(new ExcelParseResult(
            !hasGlobalErrors && !hasRowErrors,
            securityErrors,
            autoMapping,
            unmapped,
            missingRequired,
            rows));
    }

    private static List<string> PerformOpenXmlSecurityScan(Stream stream)
    {
        var errors = new List<string>();

        try
        {
            // ZIP structure check
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);
            
            if (archive.Entries.Count > 100)
            {
                errors.Add("WORKBOOK_RESOURCE_LIMIT_EXCEEDED");
                return errors;
            }

            long totalUncompressed = 0;
            foreach (var entry in archive.Entries)
            {
                if (entry.FullName.Contains(".."))
                {
                    errors.Add("WORKBOOK_INVALID_PACKAGE_ENTRY");
                    return errors;
                }

                if (entry.Length > 15 * 1024 * 1024)
                {
                    errors.Add("WORKBOOK_RESOURCE_LIMIT_EXCEEDED");
                    return errors;
                }

                if (entry.CompressedLength > 0 && entry.Length > 500 * 1024)
                {
                    double ratio = (double)entry.Length / entry.CompressedLength;
                    if (ratio > 100.0)
                    {
                        errors.Add("WORKBOOK_RESOURCE_LIMIT_EXCEEDED");
                        return errors;
                    }
                }

                totalUncompressed += entry.Length;
                if (totalUncompressed > 50 * 1024 * 1024)
                {
                    errors.Add("WORKBOOK_RESOURCE_LIMIT_EXCEEDED");
                    return errors;
                }
            }

            // OpenXML package scan
            stream.Position = 0;
            using var doc = SpreadsheetDocument.Open(stream, false);

            if (doc.WorkbookPart == null)
            {
                errors.Add("WORKBOOK_CORRUPTED");
                return errors;
            }

            if (doc.WorkbookPart.Workbook.Sheets?.Elements<Sheet>().Count() > 5)
            {
                errors.Add("TOO_MANY_WORKSHEETS");
                return errors;
            }

            // Package-wide ExternalRelationship & EmbeddedPackage scan
            foreach (var part in doc.WorkbookPart.Parts)
            {
                if (part.OpenXmlPart.ExternalRelationships.Any())
                {
                    errors.Add("WORKBOOK_EXTERNAL_LINK_NOT_ALLOWED");
                    return errors;
                }
            }

            // Scan worksheet parts for formulas and OLE objects
            foreach (var wsPart in doc.WorkbookPart.WorksheetParts)
            {
                if (wsPart.ExternalRelationships.Any())
                {
                    errors.Add("WORKBOOK_EXTERNAL_LINK_NOT_ALLOWED");
                    return errors;
                }

                if (wsPart.EmbeddedPackageParts.Any())
                {
                    errors.Add("WORKBOOK_EMBEDDED_OBJECT_NOT_ALLOWED");
                    return errors;
                }

                using var xmlReader = OpenXmlReader.Create(wsPart);
                while (xmlReader.Read())
                {
                    if (xmlReader.ElementType == typeof(CellFormula))
                    {
                        errors.Add("FORMULA_NOT_ALLOWED");
                        return errors;
                    }
                }
            }
        }
        catch (InvalidDataException)
        {
            errors.Add("WORKBOOK_CORRUPTED");
        }
        catch (Exception ex) when (ex.Message.Contains("encrypted") || ex.Message.Contains("password"))
        {
            errors.Add("WORKBOOK_PASSWORD_PROTECTED");
        }
        catch (Exception)
        {
            errors.Add("WORKBOOK_CORRUPTED");
        }

        return errors;
    }

    private static Dictionary<string, string> ResolveAutoMapping(
        List<string> headers,
        Dictionary<string, string> autoMapping,
        List<string> unmapped,
        List<string> missingRequired)
    {
        var matchedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in headers)
        {
            var normHeader = NormalizeHeaderString(header);
            string? matchedTarget = null;

            foreach (var kvp in TargetColumnAliases)
            {
                var targetName = kvp.Key;
                var aliases = kvp.Value;

                if (aliases.Any(alias => string.Equals(normHeader, NormalizeHeaderString(alias), StringComparison.OrdinalIgnoreCase)))
                {
                    matchedTarget = targetName;
                    break;
                }
            }

            if (matchedTarget != null)
            {
                if (autoMapping.ContainsValue(matchedTarget))
                {
                    // Ambiguous mapping
                    autoMapping[header] = "AMBIGUOUS";
                }
                else
                {
                    autoMapping[header] = matchedTarget;
                    matchedTargets.Add(matchedTarget);
                }
            }
            else
            {
                unmapped.Add(header);
            }
        }

        foreach (var req in RequiredTargets)
        {
            if (!matchedTargets.Contains(req))
            {
                missingRequired.Add(req);
            }
        }

        return autoMapping;
    }

    private static string NormalizeHeaderString(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        var s = val.Trim().ToLower(TrCulture);
        s = s.Replace("i̇", "i");
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }

    private static void ParseTargetField(
        string targetField,
        string rawVal,
        Dictionary<string, string> normalizedValues,
        List<string> rowErrors,
        List<string> rowWarnings)
    {
        var trimmed = rawVal.Trim();

        switch (targetField)
        {
            case "OrderNumber":
            case "StockCode":
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    rowErrors.Add($"{targetField.ToUpperInvariant()}_REQUIRED");
                }
                else
                {
                    if (Regex.IsMatch(trimmed, @"^[0-9]+(\.[0-9]+)?E\+[0-9]+$", RegexOptions.IgnoreCase))
                    {
                        rowErrors.Add("IDENTIFIER_PRECISION_LOSS");
                    }
                    else if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out _) && !trimmed.StartsWith("0"))
                    {
                        rowWarnings.Add("IDENTIFIER_STORED_AS_NUMBER");
                    }

                    normalizedValues[targetField] = trimmed;
                    normalizedValues[$"Normalized{targetField}"] = trimmed.ToUpper(TrCulture);
                }
                break;

            case "SupplierName":
            case "StockName":
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    rowErrors.Add($"{targetField.ToUpperInvariant()}_REQUIRED");
                }
                else
                {
                    var collapsed = Regex.Replace(trimmed, @"\s+", " ");
                    normalizedValues[targetField] = collapsed;
                    normalizedValues[$"Normalized{targetField}"] = collapsed.ToUpper(TrCulture);
                }
                break;

            case "OrderDate":
            case "SasDate":
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    if (targetField == "OrderDate")
                    {
                        rowErrors.Add("ORDER_DATE_REQUIRED");
                    }
                }
                else
                {
                    ParseDateValue(targetField, trimmed, normalizedValues, rowErrors, rowWarnings);
                }
                break;

            case "OrderedQuantity":
            case "RemainingQuantity":
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    rowErrors.Add($"{targetField.ToUpperInvariant()}_REQUIRED");
                }
                else
                {
                    ParseQuantityValue(targetField, trimmed, normalizedValues, rowErrors, rowWarnings);
                }
                break;
        }

        // Post-validation quantity comparison if both parsed
        if (normalizedValues.TryGetValue("OrderedQuantity", out var ordStr) &&
            normalizedValues.TryGetValue("RemainingQuantity", out var remStr) &&
            decimal.TryParse(ordStr, CultureInfo.InvariantCulture, out var ord) &&
            decimal.TryParse(remStr, CultureInfo.InvariantCulture, out var rem))
        {
            if (rem > ord)
            {
                if (!rowErrors.Contains("REMAINING_EXCEEDS_ORDERED"))
                {
                    rowErrors.Add("REMAINING_EXCEEDS_ORDERED");
                }
            }
        }
    }

    private static void ParseDateValue(
        string targetField,
        string raw,
        Dictionary<string, string> normalizedValues,
        List<string> rowErrors,
        List<string> rowWarnings)
    {
        // 1. Ambiguous slash check (e.g., 03/04/2026)
        if (Regex.IsMatch(raw, @"^\d{1,2}/\d{1,2}/\d{4}$"))
        {
            var parts = raw.Split('/');
            if (int.TryParse(parts[0], out int d1) && int.TryParse(parts[1], out int d2))
            {
                if (d1 <= 12 && d2 <= 12)
                {
                    rowErrors.Add("AMBIGUOUS_DATE_FORMAT");
                    return;
                }
            }
        }

        // 2. Try OLE Serial Date
        if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out double serial) && serial > 10000 && serial < 100000)
        {
            try
            {
                var dt = DateTime.FromOADate(serial);
                normalizedValues[targetField] = dt.ToString("yyyy-MM-ddTHH:mm:ssZ");
                return;
            }
            catch { }
        }

        // 3. Exact Date formats
        string[] formats = { "yyyy-MM-dd", "dd.MM.yyyy", "yyyy-MM-ddTHH:mm:ssZ", "yyyy-MM-ddTHH:mm:ss" };
        if (DateTime.TryParseExact(raw, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var parsedDt))
        {
            normalizedValues[targetField] = parsedDt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return;
        }

        if (DateTime.TryParseExact(raw, "dd.MM.yyyy", TrCulture, DateTimeStyles.AssumeUniversal, out var trDt))
        {
            normalizedValues[targetField] = trDt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return;
        }

        if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var genDt))
        {
            normalizedValues[targetField] = genDt.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return;
        }

        rowErrors.Add($"{targetField.ToUpperInvariant()}_INVALID");
    }

    private static void ParseQuantityValue(
        string targetField,
        string raw,
        Dictionary<string, string> normalizedValues,
        List<string> rowErrors,
        List<string> rowWarnings)
    {
        var sanitized = raw.Replace(" ", "");

        // Support Turkish comma or English dot
        if (sanitized.Contains(",") && !sanitized.Contains("."))
        {
            sanitized = sanitized.Replace(",", ".");
        }
        else if (sanitized.Contains(".") && sanitized.Contains(","))
        {
            // e.g., 1.250,50 -> 1250.50
            sanitized = sanitized.Replace(".", "").Replace(",", ".");
        }

        if (decimal.TryParse(sanitized, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
        {
            if (targetField == "OrderedQuantity" && val <= 0)
            {
                rowErrors.Add("ORDERED_QUANTITY_INVALID");
                return;
            }

            if (targetField == "RemainingQuantity" && val < 0)
            {
                rowErrors.Add("REMAINING_QUANTITY_INVALID");
                return;
            }

            normalizedValues[targetField] = val.ToString(CultureInfo.InvariantCulture);
        }
        else
        {
            rowErrors.Add($"{targetField.ToUpperInvariant()}_INVALID");
        }
    }

    public byte[] GenerateTemplateWorkbook()
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, DocumentFormat.OpenXml.SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            // Sheet 1: Sipariş İçe Aktarma
            var worksheetPart1 = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData1 = new SheetData();
            worksheetPart1.Worksheet = new Worksheet(sheetData1);

            var sheet1 = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart1),
                SheetId = 1,
                Name = "Sipariş İçe Aktarma"
            };
            sheets.Append(sheet1);

            // Add Header Row
            var headerRow = new Row();
            string[] headers = { "Sipariş No", "Firma Adı", "Sipariş Tarihi", "Stok Kodu", "Stok İsmi", "Sipariş Miktarı", "Sipariş Kalan Miktarı", "SAS Tarihi" };
            foreach (var h in headers)
            {
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(h) });
            }
            sheetData1.AppendChild(headerRow);

            // Sheet 2: Açıklama
            var worksheetPart2 = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData2 = new SheetData();
            worksheetPart2.Worksheet = new Worksheet(sheetData2);

            var sheet2 = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart2),
                SheetId = 2,
                Name = "Açıklama"
            };
            sheets.Append(sheet2);

            var descRow1 = new Row();
            descRow1.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("Lütfen Sipariş No ve Stok Kodu kolonlarını Metin (@) formatında tutunuz.") });
            sheetData2.AppendChild(descRow1);

            var descRow2 = new Row();
            descRow2.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("Tarih formatı: dd.MM.yyyy veya yyyy-MM-dd olmalıdır.") });
            sheetData2.AppendChild(descRow2);

            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }
}
