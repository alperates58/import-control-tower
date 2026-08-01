using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ImportControlTower.Application.DTOs;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

public class Phase04ClosingVerificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Phase04ClosingVerificationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static string? _cachedToken;

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email = "admin@controltower.local", string password = "AdminSecurePassword123!")
    {
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("STORAGE_PROVIDER", "LocalTest");
        });
        var client = customFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "1");

        if (string.IsNullOrEmpty(_cachedToken))
        {
            var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
            loginRes.EnsureSuccessStatusCode();
            var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
            _cachedToken = authData!.AccessToken;
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken);
        return client;
    }

    private async Task<Guid> CreateSampleCaseAsync(HttpClient client)
    {
        var idKey = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/import-cases")
        {
            Content = JsonContent.Create(new CreateImportCaseDto("Phase04 Doc Test Case", "ABC Tedarik A.S.", "Sea", "CN", "FOB", null, null, null))
        };
        req.Headers.Add("Idempotency-Key", idKey);
        var res = await client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var caseObj = await res.Content.ReadFromJsonAsync<ImportCaseDetailDto>();
        return caseObj!.Id;
    }

    [Fact]
    public async Task Scenario01_UploadValidPdf_Returns201Created()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Fake PDF Content for Phase04 Verification");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Commercial Invoice - 2026 Test"), "Title");
        content.Add(new StringContent("CommercialInvoice"), "DocumentType");
        content.Add(new StringContent("INV-998877"), "DocumentNumber");
        
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "commercial_invoice.pdf");

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents")
        {
            Content = content
        };
        req.Headers.Add("Idempotency-Key", idKey);

        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var doc = await res.Content.ReadFromJsonAsync<DocumentDto>();
        Assert.NotNull(doc);
        Assert.Equal("Commercial Invoice - 2026 Test", doc.Title);
        Assert.Equal("Active", doc.Status);
        Assert.NotNull(doc.CurrentVersion);
        Assert.Equal("Active", doc.CurrentVersion.StorageStatus);
    }

    [Fact]
    public async Task Scenario02_UploadUnsupportedExtension_Returns422UnprocessableEntity()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var exeBytes = Encoding.UTF8.GetBytes("MZ Fake Executable Content");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Executable Test"), "Title");
        content.Add(new StringContent("Other"), "DocumentType");

        var fileContent = new ByteArrayContent(exeBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(fileContent, "file", "malicious_script.exe");

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents")
        {
            Content = content
        };
        req.Headers.Add("Idempotency-Key", idKey);

        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Scenario03_UploadMagicBytesMismatch_Returns422UnprocessableEntity()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var fakePdfBytes = Encoding.UTF8.GetBytes("NOT A REAL PDF HEADER JUST TEXT");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Fake PDF Title"), "Title");
        content.Add(new StringContent("CommercialInvoice"), "DocumentType");

        var fileContent = new ByteArrayContent(fakePdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "fake.pdf");

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents")
        {
            Content = content
        };
        req.Headers.Add("Idempotency-Key", idKey);

        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Scenario04_UploadEmptyFile_Returns422UnprocessableEntity()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Empty File Title"), "Title");
        content.Add(new StringContent("CommercialInvoice"), "DocumentType");

        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "empty.pdf");

        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents")
        {
            Content = content
        };
        req.Headers.Add("Idempotency-Key", idKey);

        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, res.StatusCode);
    }

    [Fact]
    public async Task Scenario05_IdempotencyReplay_SamePayload_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Idempotent Replay PDF Content");

        var createReq = () =>
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("Idempotent Invoice"), "Title");
            content.Add(new StringContent("CommercialInvoice"), "DocumentType");
            var fileContent = new ByteArrayContent(pdfBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "invoice.pdf");
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents") { Content = content };
            req.Headers.Add("Idempotency-Key", idKey);
            return req;
        };

        var res1 = await client.SendAsync(createReq());
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        var res2 = await client.SendAsync(createReq());
        Assert.Equal(HttpStatusCode.Created, res2.StatusCode);
    }

    [Fact]
    public async Task Scenario06_SameIdempotencyKey_DifferentFileHash_Returns409Conflict()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var pdfBytes1 = Encoding.UTF8.GetBytes("%PDF-1.4 Original File Content Hash 1");
        var pdfBytes2 = Encoding.UTF8.GetBytes("%PDF-1.4 DIFFERENT File Content Hash 2");

        var createReq = (byte[] bytes) =>
        {
            var content = new MultipartFormDataContent();
            content.Add(new StringContent("Conflicting Invoice"), "Title");
            content.Add(new StringContent("CommercialInvoice"), "DocumentType");
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            content.Add(fileContent, "file", "invoice.pdf");
            var req = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents") { Content = content };
            req.Headers.Add("Idempotency-Key", idKey);
            return req;
        };

        var res1 = await client.SendAsync(createReq(pdfBytes1));
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        var res2 = await client.SendAsync(createReq(pdfBytes2));
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task Scenario07_PatchDocument_MissingIfMatch_Returns428PreconditionRequired()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Test PDF for Patch");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Patch Test Title"), "Title");
        content.Add(new StringContent("CommercialInvoice"), "DocumentType");
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "patch_test.pdf");

        var uploadReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents") { Content = content };
        uploadReq.Headers.Add("Idempotency-Key", idKey);
        var uploadRes = await client.SendAsync(uploadReq);
        var doc = await uploadRes.Content.ReadFromJsonAsync<DocumentDto>();

        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/documents/{doc!.Id}")
        {
            Content = JsonContent.Create(new UpdateDocumentDto { Title = "New Patch Title" })
        };
        // NO If-Match Header added!

        var patchRes = await client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.PreconditionRequired, patchRes.StatusCode);
    }

    [Fact]
    public async Task Scenario08_GetDocumentChecklist_CalculatesMissingAndComplete()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);

        var checklistRes = await client.GetAsync($"/api/v1/import-cases/{caseId}/document-checklist");
        checklistRes.EnsureSuccessStatusCode();

        var checklist = await checklistRes.Content.ReadFromJsonAsync<DocumentChecklistDto>();
        Assert.NotNull(checklist);
        Assert.Equal("ImportCase", checklist.ScopeType);
        Assert.Equal(caseId, checklist.ScopeId);
        Assert.True(checklist.TotalRequiredCount > 0);
        Assert.True(checklist.MissingCount > 0);
    }

    [Fact]
    public async Task Scenario09_ExactOneScopeDbConstraint_EnforcedByDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var invalidDoc = new Document
        {
            Id = Guid.NewGuid(),
            ImportCaseId = Guid.NewGuid(),
            ShipmentId = Guid.NewGuid(), // BOTH set -> violates exact-one scope CHECK!
            DocumentType = "CommercialInvoice",
            Title = "Invalid Dual Scope Document",
            Status = "Active",
            CreatedByUserId = Guid.NewGuid()
        };

        db.Documents.Add(invalidDoc);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Scenario10_DownloadDocument_ReturnsPresignedUrlAndNoStoreCacheHeader()
    {
        var client = await CreateAuthenticatedClientAsync();
        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Download Test PDF");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Downloadable Invoice"), "Title");
        content.Add(new StringContent("CommercialInvoice"), "DocumentType");
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "downloadable.pdf");

        var uploadReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents") { Content = content };
        uploadReq.Headers.Add("Idempotency-Key", idKey);
        var uploadRes = await client.SendAsync(uploadReq);
        var doc = await uploadRes.Content.ReadFromJsonAsync<DocumentDto>();

        var dlRes = await client.GetAsync($"/api/v1/documents/{doc!.Id}/download");
        Assert.Equal(HttpStatusCode.OK, dlRes.StatusCode);
        Assert.Contains("no-store", dlRes.Headers.CacheControl?.ToString() ?? "");

        var dlData = await dlRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(dlData.TryGetProperty("downloadUrl", out var urlElement));
        Assert.NotEmpty(urlElement.GetString() ?? "");
    }

    [Fact]
    public void Scenario11_LocalTestStorageProvider_ForbiddenInProductionEnvironment()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["STORAGE_PROVIDER"] = "LocalTest",
                ["ASPNETCORE_ENVIRONMENT"] = "Production"
            })
            .Build();

        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ImportControlTower.Infrastructure.Services.S3StorageService>.Instance;

        Action action = () => new ImportControlTower.Infrastructure.Services.S3StorageService(config, logger);
        var ex = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("STORAGE_LOCAL_TEST_FORBIDDEN_IN_PRODUCTION", ex.Message);
    }

    [Fact]
    public async Task Scenario12_S3Unavailable_Returns503_ServiceUnavailable_AndNoActiveDocumentInDB()
    {
        var customFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("STORAGE_PROVIDER", "S3");
            builder.UseSetting("STORAGE_ENDPOINT", "http://localhost:59999"); // Unreachable port
        });

        var client = customFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "1");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _cachedToken);

        var caseId = await CreateSampleCaseAsync(client);
        var idKey = Guid.NewGuid().ToString();

        var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4 Unreachable S3 Test");
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent("Unreachable Invoice"), "Title");
        content.Add(new StringContent("CommercialInvoice"), "DocumentType");
        var fileContent = new ByteArrayContent(pdfBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(fileContent, "file", "unreachable.pdf");

        var uploadReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{caseId}/documents") { Content = content };
        uploadReq.Headers.Add("Idempotency-Key", idKey);
        var uploadRes = await client.SendAsync(uploadReq);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, uploadRes.StatusCode);
        var errBody = await uploadRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(errBody.TryGetProperty("title", out var titleEl));
        Assert.Equal("STORAGE_UNAVAILABLE", titleEl.GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var activeDoc = await db.Documents.FirstOrDefaultAsync(d => d.ImportCaseId == caseId && d.Title == "Unreachable Invoice" && d.Status == "Active");
        Assert.Null(activeDoc);
    }
}
