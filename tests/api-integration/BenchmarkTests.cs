using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ImportControlTower.Api.IntegrationTests.Helpers;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

[Collection("IntegrationTests")]
public class BenchmarkTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public BenchmarkTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
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
    public async Task Benchmark_20000_Rows_Parse_Upload_And_Confirm_Performance()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-ICT-CSRF-Protection", "1");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");

        await LoginAsAdminAsync(client);

        var swGen = Stopwatch.StartNew();
        var fileBytes = ExcelTestFixtureGenerator.CreateValidWorkbook(
            rowCount: 20000,
            poNumberPrefix: "PO-BENCH-",
            supplierName: "PERFORMANCE SUPPLIER LTD");
        swGen.Stop();

        var swUpload = Stopwatch.StartNew();
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content.Add(fileContent, "file", "benchmark-20k.xlsx");

        var uploadRes = await client.PostAsync("/api/v1/purchase-order-imports/upload", content);
        swUpload.Stop();

        if (uploadRes.StatusCode != HttpStatusCode.Created)
        {
            var errText = await uploadRes.Content.ReadAsStringAsync();
            throw new Exception($"Upload failed status {uploadRes.StatusCode}: {errText}");
        }
        var uploadDetail = await uploadRes.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        Assert.NotNull(uploadDetail);
        Assert.Equal(20000, uploadDetail.Batch.TotalRowCount);

        var batchId = uploadDetail.Batch.Id;

        var swConfirm = Stopwatch.StartNew();
        var confirmReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/purchase-order-imports/{batchId}/confirm");
        confirmReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var confirmRes = await client.SendAsync(confirmReq);
        swConfirm.Stop();

        Assert.Equal(HttpStatusCode.OK, confirmRes.StatusCode);
        var confirmResult = await confirmRes.Content.ReadFromJsonAsync<ConfirmImportResponseDto>();
        Assert.NotNull(confirmResult);

        // Output metrics for report
        Console.WriteLine($"=== BENCHMARK METRICS (20,000 ROWS) ===");
        Console.WriteLine($"File Generation Time: {swGen.ElapsedMilliseconds} ms");
        Console.WriteLine($"Upload & Parsing Time: {swUpload.ElapsedMilliseconds} ms");
        Console.WriteLine($"Confirm & DB Persistence Time: {swConfirm.ElapsedMilliseconds} ms");
        Console.WriteLine($"Total End-to-End Time: {swUpload.ElapsedMilliseconds + swConfirm.ElapsedMilliseconds} ms");
        Console.WriteLine($"Imported Orders Count: {confirmResult.ImportedOrderCount}");
        Console.WriteLine($"Imported Lines Count: {confirmResult.ImportedLineCount}");
    }
}
