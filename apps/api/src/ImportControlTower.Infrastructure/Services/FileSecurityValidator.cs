using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ImportControlTower.Infrastructure.Services;

public class FileValidationResult
{
    public bool IsValid { get; set; } = true;
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string Sha256Hash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public static class FileSecurityValidator
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024; // 25 MB
    private const int MaxZipEntries = 10000;
    private const long MaxUncompressedSize = 100 * 1024 * 1024; // 100 MB
    private const double MaxCompressionRatio = 10.0;

    private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".xlsx", ".png", ".jpg", ".jpeg" };

    public static async Task<FileValidationResult> ValidateStreamAsync(Stream stream, string fileName, string contentType)
    {
        var result = new FileValidationResult();
        if (stream == null || stream.Length == 0)
        {
            result.IsValid = false;
            result.ErrorCode = "FILE_EMPTY";
            result.ErrorMessage = "Yüklenen dosya boş (0 byte) olamaz.";
            return result;
        }

        if (stream.Length > MaxFileSizeBytes)
        {
            result.IsValid = false;
            result.ErrorCode = "PAYLOAD_TOO_LARGE";
            result.ErrorMessage = "Dosya boyutu 25 MB sınırını aşamaz.";
            return result;
        }

        result.FileSizeBytes = stream.Length;

        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !AllowedExtensions.Contains(ext))
        {
            result.IsValid = false;
            result.ErrorCode = "UNSUPPORTED_FILE_TYPE";
            result.ErrorMessage = $"Yalnızca PDF, DOCX, XLSX, PNG, JPG/JPEG formatları desteklenmektedir ({ext} desteklenmez).";
            return result;
        }

        // Calculate SHA-256 Hash
        stream.Position = 0;
        using (var sha256 = SHA256.Create())
        {
            byte[] hashBytes = await sha256.ComputeHashAsync(stream);
            result.Sha256Hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
        stream.Position = 0;

        // Magic Bytes Verification
        byte[] header = new byte[8];
        int bytesRead = await stream.ReadAsync(header, 0, header.Length);
        stream.Position = 0;

        if (ext == ".pdf")
        {
            if (bytesRead < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46) // %PDF-
            {
                result.IsValid = false;
                result.ErrorCode = "INVALID_MAGIC_BYTES";
                result.ErrorMessage = "Dosya uzantısı PDF ancak dosya içeriği geçerli bir PDF başlığı içermiyor.";
                return result;
            }
        }
        else if (ext == ".png")
        {
            if (bytesRead < 4 || header[0] != 0x89 || header[1] != 0x50 || header[2] != 0x4E || header[3] != 0x47)
            {
                result.IsValid = false;
                result.ErrorCode = "INVALID_MAGIC_BYTES";
                result.ErrorMessage = "Geçersiz PNG dosya başlığı.";
                return result;
            }
        }
        else if (ext == ".jpg" || ext == ".jpeg")
        {
            if (bytesRead < 3 || header[0] != 0xFF || header[1] != 0xD8 || header[2] != 0xFF)
            {
                result.IsValid = false;
                result.ErrorCode = "INVALID_MAGIC_BYTES";
                result.ErrorMessage = "Geçersiz JPEG dosya başlığı.";
                return result;
            }
        }
        else if (ext == ".docx" || ext == ".xlsx")
        {
            if (bytesRead < 4 || header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04) // PK.. ZIP
            {
                result.IsValid = false;
                result.ErrorCode = "INVALID_MAGIC_BYTES";
                result.ErrorMessage = "Geçersiz Office ZIP dosya başlığı.";
                return result;
            }

            // Deep Office ZIP Security Inspection
            var zipResult = ValidateOfficeZipPackage(stream, ext);
            if (!zipResult.IsValid)
            {
                return zipResult;
            }
        }

        return result;
    }

    private static FileValidationResult ValidateOfficeZipPackage(Stream stream, string ext)
    {
        var result = new FileValidationResult { FileSizeBytes = stream.Length };
        stream.Position = 0;

        try
        {
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true))
            {
                if (archive.Entries.Count > MaxZipEntries)
                {
                    result.IsValid = false;
                    result.ErrorCode = "FILE_ARCHIVE_LIMIT_EXCEEDED";
                    result.ErrorMessage = "Office paketi maksimum arşiv eleman sayısını aşıyor.";
                    return result;
                }

                long totalUncompressedBytes = 0;
                bool hasContentTypes = false;
                bool hasDocumentXml = false;
                bool hasWorkbookXml = false;

                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.Contains("../") || entry.FullName.StartsWith("/") || entry.FullName.StartsWith("\\"))
                    {
                        result.IsValid = false;
                        result.ErrorCode = "OFFICE_PATH_TRAVERSAL_REJECTED";
                        result.ErrorMessage = "Paket içinde geçersiz yol yapısı tespit edildi.";
                        return result;
                    }

                    totalUncompressedBytes += entry.Length;

                    var lowerFullName = entry.FullName.ToLowerInvariant();

                    if (lowerFullName == "[content_types].xml") hasContentTypes = true;
                    if (lowerFullName == "word/document.xml") hasDocumentXml = true;
                    if (lowerFullName == "xl/workbook.xml") hasWorkbookXml = true;

                    // Macro Check
                    if (lowerFullName.EndsWith("vbaproject.bin") || lowerFullName.Contains("vba"))
                    {
                        result.IsValid = false;
                        result.ErrorCode = "OFFICE_MACRO_NOT_ALLOWED";
                        result.ErrorMessage = "Makro içeren Office belgeleri (.bin/vba) güvenlik kuralı gereği reddedilir.";
                        return result;
                    }

                    // OLE / Embeddings Check
                    if (lowerFullName.Contains("oleobject") || lowerFullName.Contains("embeddings/"))
                    {
                        result.IsValid = false;
                        result.ErrorCode = "OFFICE_EMBEDDED_OBJECT_NOT_ALLOWED";
                        result.ErrorMessage = "Gömülü nesne (OLE/Embeddings) barındıran Office belgeleri reddedilir.";
                        return result;
                    }

                    // Encrypted Package Check
                    if (lowerFullName.Contains("encryptedpackage"))
                    {
                        result.IsValid = false;
                        result.ErrorCode = "ENCRYPTED_OFFICE_DOCUMENT_NOT_ALLOWED";
                        result.ErrorMessage = "Şifreli Office belgeleri kabul edilmemektedir.";
                        return result;
                    }
                }

                if (totalUncompressedBytes > MaxUncompressedSize)
                {
                    result.IsValid = false;
                    result.ErrorCode = "FILE_ARCHIVE_LIMIT_EXCEEDED";
                    result.ErrorMessage = "Office paketi sıkıştırılmamış toplam boyut sınırını aşıyor.";
                    return result;
                }

                double ratio = stream.Length > 0 ? (double)totalUncompressedBytes / stream.Length : 0;
                if (ratio > MaxCompressionRatio)
                {
                    result.IsValid = false;
                    result.ErrorCode = "FILE_ARCHIVE_LIMIT_EXCEEDED";
                    result.ErrorMessage = "Şüpheli yüksek sıkıştırma oranı tespit edildi (ZIP bomb koruması).";
                    return result;
                }

                if (!hasContentTypes)
                {
                    result.IsValid = false;
                    result.ErrorCode = "OFFICE_DOCUMENT_TYPE_MISMATCH";
                    result.ErrorMessage = "Geçersiz Office paketi ([Content_Types].xml bulunamadı).";
                    return result;
                }

                if (ext == ".docx" && !hasDocumentXml)
                {
                    result.IsValid = false;
                    result.ErrorCode = "OFFICE_DOCUMENT_TYPE_MISMATCH";
                    result.ErrorMessage = "DOCX paketi içinde word/document.xml bulunamadı.";
                    return result;
                }

                if (ext == ".xlsx" && !hasWorkbookXml)
                {
                    result.IsValid = false;
                    result.ErrorCode = "OFFICE_DOCUMENT_TYPE_MISMATCH";
                    result.ErrorMessage = "XLSX paketi içinde xl/workbook.xml bulunamadı.";
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            result.IsValid = false;
            result.ErrorCode = "INVALID_OFFICE_PACKAGE";
            result.ErrorMessage = $"Office paketi ayrıştırılamadı: {ex.Message}";
            return result;
        }
        finally
        {
            stream.Position = 0;
        }

        return result;
    }
}
