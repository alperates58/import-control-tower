using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ImportControlTower.Api.IntegrationTests.Helpers;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

[Collection("IntegrationTests")]
public class PurchaseOrderImportIntegrationTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public PurchaseOrderImportIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
    }

    private HttpClient CreateClientWithCsrf()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-ICT-CSRF-Protection", "1");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@controltower.local");
        if (admin != null)
        {
            admin.MustChangePassword = false;
            admin.AccessFailedCount = 0;
            admin.LockoutEnd = null;
            await userManager.UpdateAsync(admin);
        }

        var loginReq = new LoginRequest("admin@controltower.local", "AdminSecurePassword123!");
        var res = await client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return token!;
    }

    [Fact]
    public async Task Scenario01_Upload_WithoutAuthentication_Returns401()
    {
        var client = CreateClientWithCsrf();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.xlsx");

        var response = await client.PostAsync("/api/v1/purchase-order-imports/upload", content);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Scenario02_Upload_InvalidExtension_Returns415()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.csv");

        var response = await client.PostAsync("/api/v1/purchase-order-imports/upload", content);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
    }

    [Fact]
    public async Task Scenario03_Upload_ValidWorkbook_Returns201AndLocationHeader()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var fileBytes = ExcelTestFixtureGenerator.CreateValidWorkbook(rowCount: 3, poNumberPrefix: "PO-TEST-");
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "valid-orders.xlsx");

        var response = await client.PostAsync("/api/v1/purchase-order-imports/upload", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var dto = await response.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        Assert.NotNull(dto);
        Assert.Equal("valid-orders.xlsx", dto.Batch.OriginalFileName);
        Assert.Equal(3, dto.Batch.TotalRowCount);
        Assert.Equal("ReadyForConfirmation", dto.Batch.Status);
    }

    [Fact]
    public async Task Scenario04_Upload_FormulaCellWorkbook_Returns422()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var fileBytes = ExcelTestFixtureGenerator.CreateWorkbookWithFormula();
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(fileBytes), "file", "formula-test.xlsx");

        var response = await client.PostAsync("/api/v1/purchase-order-imports/upload", content);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Scenario05_Confirm_ValidBatch_CreatesOrdersAndLinesInSingleTransaction()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var poPrefix = $"PO-CONFIRM-{Random.Shared.Next(1000, 9999)}-";
        var fileBytes = ExcelTestFixtureGenerator.CreateValidWorkbook(rowCount: 2, poNumberPrefix: poPrefix);

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(fileBytes), "file", "confirm-test.xlsx");

        var uploadRes = await client.PostAsync("/api/v1/purchase-order-imports/upload", uploadContent);
        Assert.Equal(HttpStatusCode.Created, uploadRes.StatusCode);

        var uploadDetail = await uploadRes.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        var batchId = uploadDetail!.Batch.Id;

        // Confirm batch
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/purchase-order-imports/{batchId}/confirm");
        var idempotencyKey = Guid.NewGuid().ToString();
        confirmReq.Headers.Add("Idempotency-Key", idempotencyKey);

        var confirmRes = await client.SendAsync(confirmReq);
        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);

        var confirmResult = await confirmRes.Content.ReadFromJsonAsync<ConfirmImportResponseDto>();
        Assert.NotNull(confirmResult);
        Assert.Equal("Completed", confirmResult.Status);
        Assert.True(confirmResult.ImportedOrderCount > 0);

        // Verify purchase orders list endpoint returns created POs
        var listRes = await client.GetAsync("/api/v1/purchase-orders");
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        var pagedPos = await listRes.Content.ReadFromJsonAsync<PagedResultDto<PurchaseOrderDto>>();
        Assert.NotNull(pagedPos);
        Assert.True(pagedPos.TotalCount > 0);
    }

    [Fact]
    public async Task Scenario06_Confirm_SameBatchAndSameKey_ReturnsStoredPreviousResponse()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var poPrefix = $"PO-IDEM-{Random.Shared.Next(1000, 9999)}-";
        var fileBytes = ExcelTestFixtureGenerator.CreateValidWorkbook(rowCount: 1, poNumberPrefix: poPrefix);

        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(fileBytes), "file", "idem-test.xlsx");

        var uploadRes = await client.PostAsync("/api/v1/purchase-order-imports/upload", uploadContent);
        var uploadDetail = await uploadRes.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        var batchId = uploadDetail!.Batch.Id;

        var idempotencyKey = Guid.NewGuid().ToString();

        // First Confirm
        var req1 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/purchase-order-imports/{batchId}/confirm");
        req1.Headers.Add("Idempotency-Key", idempotencyKey);
        var res1 = await client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);
        var dto1 = await res1.Content.ReadFromJsonAsync<ConfirmImportResponseDto>();

        // Second Confirm with SAME Key
        var req2 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/purchase-order-imports/{batchId}/confirm");
        req2.Headers.Add("Idempotency-Key", idempotencyKey);
        var res2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
        var dto2 = await res2.Content.ReadFromJsonAsync<ConfirmImportResponseDto>();

        Assert.Equal(dto1!.CompletedAtUtc, dto2!.CompletedAtUtc);
    }

    [Fact]
    public async Task Scenario07_Cancel_AllowedState_Succeeds()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var fileBytes = ExcelTestFixtureGenerator.CreateValidWorkbook(rowCount: 1, poNumberPrefix: "PO-CANCEL-");
        using var uploadContent = new MultipartFormDataContent();
        uploadContent.Add(new ByteArrayContent(fileBytes), "file", "cancel-test.xlsx");

        var uploadRes = await client.PostAsync("/api/v1/purchase-order-imports/upload", uploadContent);
        var uploadDetail = await uploadRes.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        var batchId = uploadDetail!.Batch.Id;

        // Cancel
        var cancelRes = await client.PostAsync($"/api/v1/purchase-order-imports/{batchId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);

        var getRes = await client.GetAsync($"/api/v1/purchase-order-imports/{batchId}");
        var batchDetail = await getRes.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        Assert.Equal("Cancelled", batchDetail!.Batch.Status);
    }

    [Fact]
    public async Task Scenario08_Template_ReturnsValidFileStream()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var response = await client.GetAsync("/api/v1/purchase-order-imports/template");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);

        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.True(bytes.Length > 0);
    }

    [Fact]
    public async Task Scenario09_PurchaseOrdersList_ExposesZeroFinancialFields()
    {
        var client = CreateClientWithCsrf();
        await LoginAsAdminAsync(client);

        var response = await client.GetAsync("/api/v1/purchase-orders");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var jsonStr = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("price", jsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("unitPrice", jsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cost", jsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("currency", jsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("amount", jsonStr, StringComparison.OrdinalIgnoreCase);
    }
}
