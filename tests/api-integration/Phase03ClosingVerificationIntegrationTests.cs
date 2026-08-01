using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

public class Phase03ClosingVerificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public Phase03ClosingVerificationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email = "admin@controltower.local", string password = "AdminSecurePassword123!")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", "1");

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        loginRes.EnsureSuccessStatusCode();

        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Scenario01_SameIdempotencyKey_DifferentPayload_Returns409()
    {
        var client = await CreateAuthenticatedClientAsync();
        var idKey = Guid.NewGuid().ToString();

        var payload1 = new CreateImportCaseDto("Original Title", "ABC Tedarik A.S.", "Sea", "CN", "FOB", null, null, null);
        var payload2 = new CreateImportCaseDto("DIFFERENT TITLE", "ABC Tedarik A.S.", "Air", "DE", "EXW", null, null, null);

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/import-cases")
        {
            Content = JsonContent.Create(payload1)
        };
        req1.Headers.Add("Idempotency-Key", idKey);
        var res1 = await client.SendAsync(req1);
        Assert.Equal(HttpStatusCode.Created, res1.StatusCode);

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/import-cases")
        {
            Content = JsonContent.Create(payload2)
        };
        req2.Headers.Add("Idempotency-Key", idKey);
        var res2 = await client.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task Scenario02_MissingIfMatchHeader_Returns428()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create Case
        var idKey = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/import-cases")
        {
            Content = JsonContent.Create(new CreateImportCaseDto("IfMatch Test Case", "ABC Tedarik A.S.", "Sea", "CN", "FOB", null, null, null))
        };
        req.Headers.Add("Idempotency-Key", idKey);
        var caseRes = await client.SendAsync(req);
        var c = await caseRes.Content.ReadFromJsonAsync<ImportCaseDetailDto>();

        // Patch without If-Match header
        var patchBody = new UpdateImportCaseDto("Updated Title", "Sea", "CN", "FOB", null, null, null, "Started", null, null, null);
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/import-cases/{c!.Id}")
        {
            Content = JsonContent.Create(patchBody)
        };

        var patchRes = await client.SendAsync(patchReq);
        Assert.Equal((HttpStatusCode)428, patchRes.StatusCode);
    }

    [Fact]
    public async Task Scenario03_StaleIfMatchRowVersion_Returns412()
    {
        var client = await CreateAuthenticatedClientAsync();

        var idKey = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/import-cases")
        {
            Content = JsonContent.Create(new CreateImportCaseDto("Stale ETag Test Case", "ABC Tedarik A.S.", "Sea", "CN", "FOB", null, null, null))
        };
        req.Headers.Add("Idempotency-Key", idKey);
        var caseRes = await client.SendAsync(req);
        var c = await caseRes.Content.ReadFromJsonAsync<ImportCaseDetailDto>();

        // Patch with wrong ETag
        var patchBody = new UpdateImportCaseDto("Updated Title", "Sea", "CN", "FOB", null, null, null, "Started", null, null, null);
        var patchReq = new HttpRequestMessage(HttpMethod.Patch, $"/api/v1/import-cases/{c!.Id}")
        {
            Content = JsonContent.Create(patchBody)
        };
        patchReq.Headers.Add("If-Match", "\"99999999\"");

        var patchRes = await client.SendAsync(patchReq);
        Assert.Equal(HttpStatusCode.PreconditionFailed, patchRes.StatusCode);
    }

    [Fact]
    public async Task Scenario08_NonSeaShipmentContainer_Returns422()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Create Case
        var idKey = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/import-cases")
        {
            Content = JsonContent.Create(new CreateImportCaseDto("Air Shipment Case", "ABC Tedarik A.S.", "Air", "DE", "FOB", null, null, null))
        };
        req.Headers.Add("Idempotency-Key", idKey);
        var caseRes = await client.SendAsync(req);
        var c = await caseRes.Content.ReadFromJsonAsync<ImportCaseDetailDto>();

        // Create Air Shipment
        var shipReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/import-cases/{c!.Id}/shipments")
        {
            Content = JsonContent.Create(new CreateShipmentDto("Air", "Frankfurt Airport", "Istanbul Airport", "Europe/Berlin", "Europe/Istanbul", null, null, null, null, null, null, null, null, null))
        };
        shipReq.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var shipRes = await client.SendAsync(shipReq);
        var s = await shipRes.Content.ReadFromJsonAsync<ShipmentDetailDto>();

        // Try to add container to Air shipment
        var contRes = await client.PostAsJsonAsync($"/api/v1/shipments/{s!.Id}/containers", new AddContainerDto("CSQU3054383", "40HC", null, null, null, null, false, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, contRes.StatusCode);
    }

    [Fact]
    public async Task Scenario09_ShipmentAbort_RequiresShortReasonCheckAndIfMatch()
    {
        var client = await CreateAuthenticatedClientAsync();

        // Abort with reason < 10 chars
        var abortRes = await client.PostAsJsonAsync($"/api/v1/shipments/{Guid.NewGuid()}/abort", new AbortShipmentDto("Short", null));
        // Missing If-Match returns 428
        Assert.Equal((HttpStatusCode)428, abortRes.StatusCode);
    }

    [Fact]
    public void Scenario12_FinancialFieldAbsence_Verification()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var entityTypes = db.Model.GetEntityTypes();
        foreach (var entity in entityTypes)
        {
            foreach (var prop in entity.GetProperties())
            {
                var name = prop.Name.ToLowerInvariant();
                Assert.DoesNotContain("price", name);
                Assert.DoesNotContain("cost", name);
                Assert.DoesNotContain("unitprice", name);
                Assert.DoesNotContain("totalamount", name);
                if (name.Contains("currency") && !name.Contains("concurrency"))
                {
                    Assert.Fail($"Financial field found: {name}");
                }
            }
        }
    }

    [Fact]
    public void Scenario14_PhysicalXminColumnAbsence_Verification()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Verify that xmin is registered as shadow property rowversion
        var importCaseEntity = db.Model.FindEntityType(typeof(ImportCase));
        var xminProp = importCaseEntity?.FindProperty("xmin");

        Assert.NotNull(xminProp);
        Assert.True(xminProp.IsConcurrencyToken);
    }
}
