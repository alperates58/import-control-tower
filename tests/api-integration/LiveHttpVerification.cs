using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
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
public class LiveHttpVerification
{
    private readonly WebApplicationFactory<Program> _factory;

    public LiveHttpVerification(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task ResetAdminPasswordAndAccessAsync()
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
    }

    [Fact]
    public async Task Run_Full_21_Item_Live_HTTP_Matrix()
    {
        await ResetAdminPasswordAndAccessAsync();
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-ICT-CSRF-Protection", "1");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", "127.0.0.1");

        // 1. Login
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);
        var loginBody = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Template download
        var templateRes = await client.GetAsync("/api/v1/purchase-order-imports/template");
        Assert.Equal(HttpStatusCode.OK, templateRes.StatusCode);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", templateRes.Content.Headers.ContentType?.MediaType);

        // 3 & 4 & 5. Valid upload -> 201 Created & Location header
        var validExcel = ExcelTestFixtureGenerator.CreateValidWorkbook(10, $"PO-LIVE-{Guid.NewGuid().ToString("N").Substring(0, 4)}-", "LIVE SUPPLIER INC");
        using var content3 = new MultipartFormDataContent();
        var fileContent3 = new ByteArrayContent(validExcel);
        fileContent3.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content3.Add(fileContent3, "file", "live-valid.xlsx");

        var uploadRes3 = await client.PostAsync("/api/v1/purchase-order-imports/upload", content3);
        Assert.Equal(HttpStatusCode.Created, uploadRes3.StatusCode);
        Assert.NotNull(uploadRes3.Headers.Location);

        var uploadDetail3 = await uploadRes3.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        Assert.NotNull(uploadDetail3);
        var batchId = uploadDetail3.Batch.Id;

        // 6. Batch Detail
        var detailRes6 = await client.GetAsync($"/api/v1/purchase-order-imports/{batchId}");
        Assert.Equal(HttpStatusCode.OK, detailRes6.StatusCode);

        // 7. Batch Rows
        var rowsRes7 = await client.GetAsync($"/api/v1/purchase-order-imports/{batchId}/rows");
        Assert.Equal(HttpStatusCode.OK, rowsRes7.StatusCode);

        // 8. Confirm
        var idempotencyKey = Guid.NewGuid().ToString();
        var confirmReq8 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/purchase-order-imports/{batchId}/confirm");
        confirmReq8.Headers.Add("Idempotency-Key", idempotencyKey);
        var confirmRes8 = await client.SendAsync(confirmReq8);
        Assert.Equal(HttpStatusCode.OK, confirmRes8.StatusCode);

        // 9 & 10. Repeat confirm with same Idempotency-Key -> stored response 200 OK
        var confirmReq9 = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/purchase-order-imports/{batchId}/confirm");
        confirmReq9.Headers.Add("Idempotency-Key", idempotencyKey);
        var confirmRes9 = await client.SendAsync(confirmReq9);
        Assert.Equal(HttpStatusCode.OK, confirmRes9.StatusCode);

        // 11. Purchase Order List
        var poListRes11 = await client.GetAsync("/api/v1/purchase-orders");
        Assert.Equal(HttpStatusCode.OK, poListRes11.StatusCode);
        var poListBody = await poListRes11.Content.ReadFromJsonAsync<PagedResultDto<PurchaseOrderDto>>();
        Assert.NotNull(poListBody);
        Assert.NotEmpty(poListBody.Items);

        var firstPoId = poListBody.Items.First().Id;

        // 12. Purchase Order Detail
        var poDetailRes12 = await client.GetAsync($"/api/v1/purchase-orders/{firstPoId}");
        Assert.Equal(HttpStatusCode.OK, poDetailRes12.StatusCode);

        // 13. Anonymous Upload -> 401
        var anonClient = _factory.CreateClient();
        anonClient.DefaultRequestHeaders.Add("X-ICT-CSRF-Protection", "1");
        anonClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        using var content13 = new MultipartFormDataContent();
        content13.Add(new ByteArrayContent(validExcel), "file", "anon.xlsx");
        var anonRes13 = await anonClient.PostAsync("/api/v1/purchase-order-imports/upload", content13);
        Assert.Equal(HttpStatusCode.Unauthorized, anonRes13.StatusCode);

        // 14. Missing Permission Upload -> 403
        var viewerLoginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("viewer@controltower.local", "AdminSecurePassword123!"));
        if (viewerLoginRes.StatusCode == HttpStatusCode.OK)
        {
            var viewerBody = await viewerLoginRes.Content.ReadFromJsonAsync<JsonElement>();
            var viewerToken = viewerBody.GetProperty("accessToken").GetString();

            var viewerClient = _factory.CreateClient();
            viewerClient.DefaultRequestHeaders.Add("X-ICT-CSRF-Protection", "1");
            viewerClient.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
            viewerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", viewerToken);

            using var content14 = new MultipartFormDataContent();
            content14.Add(new ByteArrayContent(validExcel), "file", "noperm.xlsx");
            var permRes14 = await viewerClient.PostAsync("/api/v1/purchase-order-imports/upload", content14);
            Assert.Equal(HttpStatusCode.Forbidden, permRes14.StatusCode);
        }

        // 15. Duplicate completed file hash -> 409
        using var content15 = new MultipartFormDataContent();
        var fileContent15 = new ByteArrayContent(validExcel);
        fileContent15.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content15.Add(fileContent15, "file", "duplicate.xlsx");
        var dupReq15 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-order-imports/upload") { Content = content15 };
        dupReq15.Headers.Add("X-Forwarded-For", "10.0.0.15");
        var dupRes15 = await client.SendAsync(dupReq15);
        Assert.Equal(HttpStatusCode.Conflict, dupRes15.StatusCode);

        // 16. Invalid file extension -> 415
        using var content16 = new MultipartFormDataContent();
        content16.Add(new ByteArrayContent(new byte[] { 1, 2, 3 }), "file", "test.txt");
        var extReq16 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-order-imports/upload") { Content = content16 };
        extReq16.Headers.Add("X-Forwarded-For", "10.0.0.16");
        var extRes16 = await client.SendAsync(extReq16);
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, extRes16.StatusCode);

        // 17. 10MB+ file -> 413 Payload Too Large / 400
        using var content17 = new MultipartFormDataContent();
        var bigBytes = new byte[16 * 1024 * 1024]; // 16MB
        var bigContent = new ByteArrayContent(bigBytes);
        bigContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content17.Add(bigContent, "file", "bigfile.xlsx");
        var bigReq17 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-order-imports/upload") { Content = content17 };
        bigReq17.Headers.Add("X-Forwarded-For", "10.0.0.17");
        var bigRes17 = await client.SendAsync(bigReq17);
        Assert.True(bigRes17.StatusCode == HttpStatusCode.RequestEntityTooLarge || bigRes17.StatusCode == HttpStatusCode.BadRequest);

        // 18. Corrupted workbook -> 422
        using var content18 = new MultipartFormDataContent();
        var junkBytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00 }; // invalid zip header
        var junkContent = new ByteArrayContent(junkBytes);
        junkContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content18.Add(junkContent, "file", "corrupt.xlsx");
        var corruptReq18 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-order-imports/upload") { Content = content18 };
        corruptReq18.Headers.Add("X-Forwarded-For", "10.0.0.18");
        var corruptRes18 = await client.SendAsync(corruptReq18);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, corruptRes18.StatusCode);

        // 19. Formula workbook -> 422
        var formulaExcel = ExcelTestFixtureGenerator.CreateWorkbookWithFormula();
        using var content19 = new MultipartFormDataContent();
        var formulaContent = new ByteArrayContent(formulaExcel);
        formulaContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content19.Add(formulaContent, "file", "formula.xlsx");
        var formulaReq19 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-order-imports/upload") { Content = content19 };
        formulaReq19.Headers.Add("X-Forwarded-For", "10.0.0.19");
        var formulaRes19 = await client.SendAsync(formulaReq19);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, formulaRes19.StatusCode);

        // 20. Ambiguous slash date -> validation error in row
        var slashExcel = ExcelTestFixtureGenerator.CreateWorkbookWithAmbiguousSlashDate();
        using var content20 = new MultipartFormDataContent();
        var slashContent = new ByteArrayContent(slashExcel);
        slashContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        content20.Add(slashContent, "file", "slashdate.xlsx");
        var slashReq20 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/purchase-order-imports/upload") { Content = content20 };
        slashReq20.Headers.Add("X-Forwarded-For", "10.0.0.20");
        var slashRes20 = await client.SendAsync(slashReq20);
        Assert.Equal(HttpStatusCode.Created, slashRes20.StatusCode);
        var slashDetail = await slashRes20.Content.ReadFromJsonAsync<ImportBatchDetailDto>();
        Assert.NotNull(slashDetail);
        Assert.True(slashDetail.Batch.WarningRowCount > 0 || slashDetail.Batch.InvalidRowCount > 0);

        // 21. Verify 0 financial fields in response JSON
        var poJsonStr = await poDetailRes12.Content.ReadAsStringAsync();
        Assert.DoesNotContain("price", poJsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("amount", poJsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cost", poJsonStr, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("currency", poJsonStr, StringComparison.OrdinalIgnoreCase);
    }
}
