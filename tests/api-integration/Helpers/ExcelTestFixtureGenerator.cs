using System;
using System.Collections.Generic;
using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ImportControlTower.Api.IntegrationTests.Helpers;

public static class ExcelTestFixtureGenerator
{
    public static byte[] CreateValidWorkbook(
        int rowCount = 5,
        string poNumberPrefix = "PO-2026-",
        string supplierName = "ABC Tedarik A.S.")
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheet = new Sheet()
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1"
            };
            sheets.Append(sheet);

            // Header
            var headerRow = new Row();
            string[] headers = { "Sipariş No", "Firma Adı", "Sipariş Tarihi", "Stok Kodu", "Stok İsmi", "Sipariş Miktarı", "Sipariş Kalan Miktarı", "SAS Tarihi" };
            foreach (var h in headers)
            {
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(h) });
            }
            sheetData.AppendChild(headerRow);

            // Data Rows
            for (int i = 1; i <= rowCount; i++)
            {
                var row = new Row();
                var poNo = $"{poNumberPrefix}{i:D4}";
                var stockCode = $"STK-{i:D3}";
                var stockName = $"Ürün Malzeme Kalemi {i}";

                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(poNo) });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(supplierName) });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("15.04.2026") });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(stockCode) });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(stockName) });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("100") });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("50") });
                row.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("20.04.2026") });

                sheetData.AppendChild(row);
            }

            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }

    public static byte[] CreateWorkbookWithFormula()
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
            sheets.Append(sheet);

            var headerRow = new Row();
            string[] headers = { "Sipariş No", "Firma Adı", "Sipariş Tarihi", "Stok Kodu", "Stok İsmi", "Sipariş Miktarı", "Sipariş Kalan Miktarı" };
            foreach (var h in headers)
            {
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(h) });
            }
            sheetData.AppendChild(headerRow);

            var dataRow = new Row();
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("PO-100") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("Tedarikci A.S.") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("15.04.2026") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("STK-001") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("Stok 1") });

            // Formula Cell
            var formulaCell = new Cell { CellFormula = new CellFormula("SUM(10,20)"), CellValue = new CellValue("30") };
            dataRow.AppendChild(formulaCell);
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("10") });

            sheetData.AppendChild(dataRow);
            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }

    public static byte[] CreateWorkbookWithAmbiguousSlashDate()
    {
        using var ms = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            var sheet = new Sheet() { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Sheet1" };
            sheets.Append(sheet);

            var headerRow = new Row();
            string[] headers = { "Sipariş No", "Firma Adı", "Sipariş Tarihi", "Stok Kodu", "Stok İsmi", "Sipariş Miktarı", "Sipariş Kalan Miktarı", "SAS Tarihi" };
            foreach (var h in headers)
            {
                headerRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue(h) });
            }
            sheetData.AppendChild(headerRow);

            var dataRow = new Row();
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("PO-SLASH-001") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("Ambiguous Date Ltd") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("01/02/2026") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("STK-999") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("Test Item") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("100") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("50") });
            dataRow.AppendChild(new Cell { DataType = CellValues.String, CellValue = new CellValue("03/04/2026") });

            sheetData.AppendChild(dataRow);
            workbookPart.Workbook.Save();
        }

        return ms.ToArray();
    }
}
