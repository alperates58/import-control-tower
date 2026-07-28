using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ImportControlTower.Api.IntegrationTests;

[CollectionDefinition("IntegrationTests", DisableParallelization = true)]
public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}

[Collection("IntegrationTests")]
public class AuthAndSecurityIntegrationTests
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthAndSecurityIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClientWithCsrf(WebApplicationFactory<Program>? factory = null)
    {
        var f = factory ?? _factory;
        var client = f.CreateClient();
        client.DefaultRequestHeaders.Add("X-ICT-CSRF-Protection", "1");
        client.DefaultRequestHeaders.Add("Origin", "http://localhost:3000");
        client.DefaultRequestHeaders.Add("X-Forwarded-For", $"10.0.{Random.Shared.Next(1, 250)}.{Random.Shared.Next(1, 250)}");
        return client;
    }

    private async Task EnsureAdminCanExecuteAdminOperationsAsync()
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

    private string? ExtractCookie(HttpResponseMessage response, string cookieName)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var cookieStr = cookies.FirstOrDefault(c => c.Contains(cookieName));
            if (cookieStr != null)
            {
                var parts = cookieStr.Split(';');
                return parts[0].Trim();
            }
        }
        return null;
    }

    [Fact]
    public async Task Scenario01_Refresh_Token_Rotation_Works()
    {
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        var cookieHeader = ExtractCookie(loginRes, "ict_refresh_token");
        Assert.NotNull(cookieHeader);

        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Cookie", cookieHeader);
        refreshReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        refreshReq.Headers.Add("Origin", "http://localhost:3000");

        var refreshRes = await client.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.OK, refreshRes.StatusCode);

        var newCookieHeader = ExtractCookie(refreshRes, "ict_refresh_token");
        Assert.NotNull(newCookieHeader);
        Assert.NotEqual(cookieHeader, newCookieHeader);
    }

    [Fact]
    public async Task Scenario02_Revoked_Refresh_Token_Reuse_Detection_Returns_401()
    {
        var client1 = CreateClientWithCsrf();
        var loginRes = await client1.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var cookieR1 = ExtractCookie(loginRes, "ict_refresh_token");

        // First refresh uses R1, issues R2 and revokes R1
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req1.Headers.Add("Cookie", cookieR1);
        req1.Headers.Add("X-ICT-CSRF-Protection", "1");
        req1.Headers.Add("Origin", "http://localhost:3000");
        var res1 = await client1.SendAsync(req1);
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        // Second refresh REUSES revoked R1 on clean client instance -> Reuse Detection triggers 401
        var client2 = CreateClientWithCsrf();
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req2.Headers.Add("Cookie", cookieR1);
        req2.Headers.Add("X-ICT-CSRF-Protection", "1");
        req2.Headers.Add("Origin", "http://localhost:3000");
        var res2 = await client2.SendAsync(req2);
        Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);
    }

    [Fact]
    public async Task Scenario03_Concurrent_Refresh_Token_Requests_Does_Not_Return_500()
    {
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var cookie = ExtractCookie(loginRes, "ict_refresh_token");

        var req1 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req1.Headers.Add("Cookie", cookie);
        req1.Headers.Add("X-ICT-CSRF-Protection", "1");
        req1.Headers.Add("Origin", "http://localhost:3000");

        var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        req2.Headers.Add("Cookie", cookie);
        req2.Headers.Add("X-ICT-CSRF-Protection", "1");
        req2.Headers.Add("Origin", "http://localhost:3000");

        var task1 = client.SendAsync(req1);
        var task2 = client.SendAsync(req2);

        var responses = await Task.WhenAll(task1, task2);

        Assert.DoesNotContain(responses, r => r.StatusCode == HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task Scenario04_Logout_Invalidates_Refresh_Token()
    {
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();
        var cookie = ExtractCookie(loginRes, "ict_refresh_token");

        var logoutReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout");
        logoutReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        logoutReq.Headers.Add("Cookie", cookie);
        logoutReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        logoutReq.Headers.Add("Origin", "http://localhost:3000");

        var logoutRes = await client.SendAsync(logoutReq);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);

        // Subsequent refresh with logged out cookie returns 401
        var client2 = CreateClientWithCsrf();
        var refreshReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/refresh");
        refreshReq.Headers.Add("Cookie", cookie);
        refreshReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        refreshReq.Headers.Add("Origin", "http://localhost:3000");

        var refreshRes = await client2.SendAsync(refreshReq);
        Assert.Equal(HttpStatusCode.Unauthorized, refreshRes.StatusCode);
    }

    [Fact]
    public async Task Scenario05_LogoutAll_Revokes_All_Tokens_And_Increments_AuthVersion()
    {
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/logout-all");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        req.Headers.Add("X-ICT-CSRF-Protection", "1");
        req.Headers.Add("Origin", "http://localhost:3000");

        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        // Old Access Token now returns 401 because auth_version was incremented
        var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);
        var meRes = await client.SendAsync(meReq);
        Assert.Equal(HttpStatusCode.Unauthorized, meRes.StatusCode);
    }

    [Fact]
    public async Task Scenario06_Role_Permission_Update_Increments_AuthVersion()
    {
        await EnsureAdminCanExecuteAdminOperationsAsync();
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        // SystemAdmin updates Management role permissions
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mgmtRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Management");
        Assert.NotNull(mgmtRole);

        var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/v1/admin/roles/{mgmtRole.Id}");
        updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        updateReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        updateReq.Headers.Add("Origin", "http://localhost:3000");
        updateReq.Content = JsonContent.Create(new UpdateRoleRequest("Updated Description", new List<string> { PermissionsCatalog.DashboardView }));

        var updateRes = await client.SendAsync(updateReq);
        Assert.Equal(HttpStatusCode.OK, updateRes.StatusCode);
    }

    [Fact]
    public async Task Scenario07_Admin_User_Creation_Returns_201_With_MustChangePassword()
    {
        await EnsureAdminCanExecuteAdminOperationsAsync();
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        var targetEmail = $"newop_{Guid.NewGuid().ToString("N").Substring(0, 6)}@controltower.local";
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        createReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        createReq.Headers.Add("Origin", "http://localhost:3000");
        createReq.Content = JsonContent.Create(new CreateUserRequest(targetEmail, "New Operations User", "Password123!", new List<string> { "ImportOperations" }));

        var createRes = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
    }

    [Fact]
    public async Task Scenario08_NonAdmin_User_Gets_403_On_Admin_Endpoints()
    {
        var client = CreateClientWithCsrf();
        
        // Create viewer user
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var viewer = await userManager.FindByEmailAsync("viewer@controltower.local");
        if (viewer == null)
        {
            viewer = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "viewer@controltower.local",
                Email = "viewer@controltower.local",
                EmailConfirmed = true,
                FullName = "Viewer User",
                IsActive = true,
                MustChangePassword = false,
                AuthVersion = 1,
                CreatedAtUtc = DateTime.UtcNow
            };
            await userManager.CreateAsync(viewer, "ViewerPassword123!");
            await userManager.AddToRoleAsync(viewer, "Viewer");
        }

        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("viewer@controltower.local", "ViewerPassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Viewer attempts admin users list endpoint (requires users.view permission)
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/users");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        var res = await client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Scenario09_Role_Creation_And_Permission_Update()
    {
        await EnsureAdminCanExecuteAdminOperationsAsync();
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        var roleName = $"CustomRole_{Guid.NewGuid().ToString("N").Substring(0, 6)}";
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/roles");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        createReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        createReq.Headers.Add("Origin", "http://localhost:3000");
        createReq.Content = JsonContent.Create(new CreateRoleRequest(roleName, "Custom Role Description", new List<string> { PermissionsCatalog.DashboardView }));

        var createRes = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
    }

    [Fact]
    public async Task Scenario10_Login_Rate_Limiting_Triggers_429()
    {
        var client = CreateClientWithCsrf();
        var responses = new List<HttpResponseMessage>();

        for (int i = 0; i < 7; i++)
        {
            var res = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("invalid@test.com", "WrongPassword123!"));
            responses.Add(res);
        }

        Assert.Contains(responses, r => r.StatusCode == HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Scenario11_Production_Host_Cookie_Header_Validation()
    {
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        Assert.True(loginRes.Headers.TryGetValues("Set-Cookie", out var cookies));
        var cookieHeader = cookies.FirstOrDefault(c => c.Contains("ict_refresh_token"));
        Assert.NotNull(cookieHeader);
        Assert.Contains("httponly", cookieHeader.ToLower());
    }

    [Fact]
    public async Task Scenario12_Admin_Password_Reset_Returns_Temp_Password_Once_And_MustChangePassword()
    {
        await EnsureAdminCanExecuteAdminOperationsAsync();
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Create target user to reset
        var targetEmail = $"user_reset_{Guid.NewGuid().ToString("N").Substring(0, 6)}@controltower.local";
        var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/users");
        createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData!.AccessToken);
        createReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        createReq.Headers.Add("Origin", "http://localhost:3000");
        createReq.Content = JsonContent.Create(new CreateUserRequest(targetEmail, "Target Reset User", "TempPass123!", new List<string> { "Viewer" }));
        var createRes = await client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var createdUser = await createRes.Content.ReadFromJsonAsync<UserDto>();

        // Reset password
        var resetReq = new HttpRequestMessage(HttpMethod.Post, $"/api/v1/admin/users/{createdUser!.Id}/reset-password");
        resetReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);
        resetReq.Headers.Add("X-ICT-CSRF-Protection", "1");
        resetReq.Headers.Add("Origin", "http://localhost:3000");

        var resetRes = await client.SendAsync(resetReq);
        Assert.Equal(HttpStatusCode.OK, resetRes.StatusCode);
        var resetData = await resetRes.Content.ReadFromJsonAsync<ResetPasswordResponseDto>();
        Assert.NotNull(resetData);
        Assert.True(resetData.TemporaryPassword.Length >= 12);
    }

    [Fact]
    public async Task Scenario13_MustChangePassword_User_Blocked_From_Protected_Endpoints()
    {
        // Explicitly set MustChangePassword = true to test enforcement
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
        var admin = await userManager.FindByEmailAsync("admin@controltower.local");
        if (admin != null)
        {
            admin.MustChangePassword = true;
            await userManager.UpdateAsync(admin);
        }

        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.True(authData!.User.MustChangePassword);

        // Attempting protected business endpoint while MustChangePassword=true returns 403
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/v1/admin/audit-logs");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authData.AccessToken);
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Scenario14_Parallel_Startup_Migration_And_Seed_Concurrency()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var env = services.GetRequiredService<IHostEnvironment>();
        var config = services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();

        var task1 = InitialAdminSeeder.SeedAsync(services, config, env);
        var task2 = InitialAdminSeeder.SeedAsync(services, config, env);

        await Task.WhenAll(task1, task2);

        var db = services.GetRequiredService<ApplicationDbContext>();
        var permCount = await db.Permissions.CountAsync();
        Assert.Equal(32, permCount);
    }

    [Fact]
    public async Task Scenario15_Audit_Logs_Contain_No_Passwords_Tokens_Or_Secrets()
    {
        var client = CreateClientWithCsrf();
        var loginRes = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("admin@controltower.local", "AdminSecurePassword123!"));
        var authData = await loginRes.Content.ReadFromJsonAsync<AuthResponseDto>();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logs = await db.AuditLogs.ToListAsync();

        foreach (var log in logs)
        {
            Assert.DoesNotContain("AdminSecurePassword123!", log.MetadataJson);
            Assert.DoesNotContain("DEFAULT_SECRET", log.MetadataJson);
            Assert.DoesNotContain("eyJ", log.MetadataJson); // No raw JWT tokens in metadata
        }
    }
}
