using ImportControlTower.Domain.Constants;
using ImportControlTower.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ImportControlTower.Infrastructure.Persistence;

public static class InitialAdminSeeder
{
    private const long AdvisoryLockId = 987654321;

    public static async Task SeedAsync(IServiceProvider serviceProvider, IConfiguration configuration, IHostEnvironment environment)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();
        var connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? "Host=db;Port=5432;Database=import_control_tower;Username=ict_user;Password=ict_password";

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        bool lockAcquired = false;
        var timeout = TimeSpan.FromSeconds(30);
        var startTime = DateTime.UtcNow;

        while (DateTime.UtcNow - startTime < timeout)
        {
            await using var lockCommand = new NpgsqlCommand($"SELECT pg_try_advisory_lock({AdvisoryLockId})", connection);
            var result = await lockCommand.ExecuteScalarAsync();
            if (result is bool acquired && acquired)
            {
                lockAcquired = true;
                break;
            }
            await Task.Delay(500);
        }

        if (!lockAcquired)
        {
            logger.LogError("Could not acquire PostgreSQL advisory lock for seeding within 30 seconds.");
            throw new InvalidOperationException("Failed to acquire advisory lock for startup seeding.");
        }

        try
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connection)
                .Options;

            await using var dbContext = new ApplicationDbContext(options);
            logger.LogInformation("Applying EF Core migrations...");
            await dbContext.Database.MigrateAsync();

            var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            // 1. Seed permission catalog
            logger.LogInformation("Seeding permission catalog ({Count} items)...", PermissionsCatalog.All.Count);
            var existingPermissions = await dbContext.Permissions.ToDictionaryAsync(p => p.Code);
            foreach (var item in PermissionsCatalog.All)
            {
                if (!existingPermissions.TryGetValue(item.Code, out var perm))
                {
                    perm = new Permission
                    {
                        Id = Guid.NewGuid(),
                        Code = item.Code,
                        GroupName = item.GroupName,
                        Description = item.Description
                    };
                    dbContext.Permissions.Add(perm);
                    existingPermissions[item.Code] = perm;
                }
            }
            await dbContext.SaveChangesAsync();

            // 2. Seed System Roles & Role-Permission Matrix
            logger.LogInformation("Seeding system roles and role-permission matrix...");
            var roleMatrix = GetRoleMatrix();
            foreach (var (roleName, description, permCodes) in roleMatrix)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                {
                    role = new ApplicationRole
                    {
                        Id = Guid.NewGuid(),
                        Name = roleName,
                        Description = description,
                        IsSystemRole = true
                    };
                    await roleManager.CreateAsync(role);
                }

                // Sync permissions for this role
                var currentRolePermIds = await dbContext.RolePermissions
                    .Where(rp => rp.RoleId == role.Id)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var targetPermIds = permCodes
                    .Where(code => existingPermissions.ContainsKey(code))
                    .Select(code => existingPermissions[code].Id)
                    .ToList();

                var toAdd = targetPermIds.Except(currentRolePermIds).ToList();
                foreach (var permId in toAdd)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permId
                    });
                }
            }
            await dbContext.SaveChangesAsync();

            // 3. Seed System Settings (FinancialModuleEnabled = false)
            if (!await dbContext.SystemSettings.AnyAsync(s => s.Key == "FinancialModuleEnabled"))
            {
                var defaultFinancialFlag = configuration["FINANCIAL_MODULE_ENABLED"] ?? "false";
                dbContext.SystemSettings.Add(new SystemSetting
                {
                    Key = "FinancialModuleEnabled",
                    Value = defaultFinancialFlag,
                    ValueType = "Boolean",
                    Description = "Controls global financial module visibility and API projections",
                    IsSensitive = false,
                    UpdatedAtUtc = DateTime.UtcNow,
                    UpdatedByUserId = null
                });
                await dbContext.SaveChangesAsync();
            }

            // 4. Seed Default Document Requirements
            await SeedDocumentRequirementsAsync(dbContext, logger);

            // 4. Seed Initial Admin User
            var adminEmail = configuration["SEED_ADMIN_EMAIL"] ?? "admin@controltower.local";
            var adminPassword = configuration["SEED_ADMIN_PASSWORD"] ?? "AdminSecurePassword123!";
            var adminFullName = configuration["SEED_ADMIN_FULL_NAME"] ?? "Initial System Administrator";

            if (environment.IsProduction())
            {
                if (adminEmail.Contains("example.com") || 
                    adminPassword == "CHANGE_ME" || 
                    adminPassword == "Password123")
                {
                    logger.LogCritical("Production environment detected with placeholder or weak SEED_ADMIN_PASSWORD! Refusing to start.");
                    throw new InvalidOperationException("Unsafe seed admin password in production.");
                }
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                logger.LogInformation("Seeding initial admin user ({Email})...", adminEmail);
                adminUser = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = adminFullName,
                    IsActive = true,
                    MustChangePassword = true,
                    AuthVersion = 1,
                    CreatedAtUtc = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "SystemAdmin");
                }
                else
                {
                    logger.LogError("Failed to seed admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
        }
        finally
        {
            await using var unlockCommand = new NpgsqlCommand($"SELECT pg_advisory_unlock({AdvisoryLockId})", connection);
            await unlockCommand.ExecuteScalarAsync();
            await connection.CloseAsync();
        }
    }

    private static List<(string RoleName, string Description, List<string> PermCodes)> GetRoleMatrix()
    {
        return new List<(string, string, List<string>)>
        {
            ("SystemAdmin", "Tam yetkili sistem yöneticisi", PermissionsCatalog.All.Select(p => p.Code).ToList()),
            ("Management", "Üst yönetim izleme rolü", new List<string>
            {
                PermissionsCatalog.DashboardView, PermissionsCatalog.PurchaseOrdersView,
                PermissionsCatalog.ImportCasesView, PermissionsCatalog.ShipmentsView,
                PermissionsCatalog.ContainersView, PermissionsCatalog.DocumentsView,
                PermissionsCatalog.DocumentsDownload,
                PermissionsCatalog.TasksViewAll, PermissionsCatalog.AuditView
            }),
            ("Purchasing", "Satın alma operasyon rolü", new List<string>
            {
                PermissionsCatalog.DashboardView, PermissionsCatalog.PurchaseOrdersView,
                PermissionsCatalog.PurchaseOrdersImport, PermissionsCatalog.PurchaseOrdersEdit,
                PermissionsCatalog.ImportCasesView, PermissionsCatalog.ImportCasesEdit,
                PermissionsCatalog.ImportCasesAssignOrders, PermissionsCatalog.ShipmentsView,
                PermissionsCatalog.ContainersView, PermissionsCatalog.DocumentsView,
                PermissionsCatalog.DocumentsUpload, PermissionsCatalog.DocumentsDownload,
                PermissionsCatalog.DocumentsVersion,
                PermissionsCatalog.TasksViewOwn, PermissionsCatalog.TasksComplete
            }),
            ("ImportOperations", "İthalat operasyon sorumlusu", new List<string>
            {
                PermissionsCatalog.DashboardView, PermissionsCatalog.PurchaseOrdersView,
                PermissionsCatalog.ImportCasesView, PermissionsCatalog.ImportCasesCreate,
                PermissionsCatalog.ImportCasesEdit, PermissionsCatalog.ImportCasesAssignOrders,
                PermissionsCatalog.ImportCasesClose, PermissionsCatalog.ImportCasesCancel,
                PermissionsCatalog.ShipmentsView, PermissionsCatalog.ShipmentsCreate,
                PermissionsCatalog.ShipmentsEdit, PermissionsCatalog.ShipmentsCancel,
                PermissionsCatalog.ContainersView, PermissionsCatalog.ContainersEdit,
                PermissionsCatalog.MilestonesEdit,
                PermissionsCatalog.DocumentsView, PermissionsCatalog.DocumentsUpload,
                PermissionsCatalog.DocumentsDownload, PermissionsCatalog.DocumentsVersion,
                PermissionsCatalog.DocumentsCancel, PermissionsCatalog.DocumentsDelete,
                PermissionsCatalog.TasksViewOwn, PermissionsCatalog.TasksViewAll,
                PermissionsCatalog.TasksAssign, PermissionsCatalog.TasksComplete
            }),
            ("Planning", "Üretim ve ihtiyaç planlama", new List<string>
            {
                PermissionsCatalog.DashboardView, PermissionsCatalog.PurchaseOrdersView,
                PermissionsCatalog.ImportCasesView, PermissionsCatalog.ShipmentsView,
                PermissionsCatalog.ContainersView, PermissionsCatalog.DocumentsView,
                PermissionsCatalog.DocumentsDownload,
                PermissionsCatalog.TasksViewOwn, PermissionsCatalog.TasksComplete
            }),
            ("Finance", "Finansal operasyon ve ödemeler", new List<string>
            {
                PermissionsCatalog.DashboardView, PermissionsCatalog.PurchaseOrdersView,
                PermissionsCatalog.ImportCasesView, PermissionsCatalog.ShipmentsView,
                PermissionsCatalog.DocumentsView, PermissionsCatalog.DocumentsDownload,
                PermissionsCatalog.TasksViewOwn, PermissionsCatalog.FinancialView,
                PermissionsCatalog.FinancialEdit
            }),
            ("Viewer", "Salt okunur izleyici rolü", new List<string>
            {
                PermissionsCatalog.DashboardView, PermissionsCatalog.PurchaseOrdersView,
                PermissionsCatalog.ImportCasesView, PermissionsCatalog.ShipmentsView,
                PermissionsCatalog.ContainersView, PermissionsCatalog.DocumentsView,
                PermissionsCatalog.DocumentsDownload,
                PermissionsCatalog.TasksViewOwn
            })
        };
    }

    private static async Task SeedDocumentRequirementsAsync(ApplicationDbContext dbContext, ILogger logger)
    {
        if (await dbContext.DocumentRequirements.AnyAsync()) return;

        logger.LogInformation("Seeding default DocumentRequirements...");
        var defaults = new List<DocumentRequirement>
        {
            new() { ScopeType = "ImportCase", TransportMode = null, DocumentType = "ProformaInvoice", IsRequired = true, Description = "Sipariş proforma faturası", SortOrder = 1 },
            new() { ScopeType = "ImportCase", TransportMode = null, DocumentType = "CommercialInvoice", IsRequired = true, Description = "Ticari fatura", SortOrder = 2 },
            new() { ScopeType = "Shipment", TransportMode = "Sea", DocumentType = "CommercialInvoice", IsRequired = true, Description = "Deniz sevkiyatı ticari faturası", SortOrder = 1 },
            new() { ScopeType = "Shipment", TransportMode = "Sea", DocumentType = "PackingList", IsRequired = true, Description = "Çeki listesi", SortOrder = 2 },
            new() { ScopeType = "Shipment", TransportMode = "Sea", DocumentType = "BillOfLading", IsRequired = true, Description = "Deniz konşimentosu (B/L)", SortOrder = 3 },
            new() { ScopeType = "Shipment", TransportMode = "Sea", DocumentType = "CertificateOfOrigin", IsRequired = true, Description = "Menşe şehadetnamesi", SortOrder = 4 },
            new() { ScopeType = "Shipment", TransportMode = "Air", DocumentType = "CommercialInvoice", IsRequired = true, Description = "Hava sevkiyatı ticari faturası", SortOrder = 1 },
            new() { ScopeType = "Shipment", TransportMode = "Air", DocumentType = "PackingList", IsRequired = true, Description = "Çeki listesi", SortOrder = 2 },
            new() { ScopeType = "Shipment", TransportMode = "Air", DocumentType = "AirWaybill", IsRequired = true, Description = "Hava taşıma senedi (AWB)", SortOrder = 3 },
            new() { ScopeType = "Shipment", TransportMode = "Road", DocumentType = "CommercialInvoice", IsRequired = true, Description = "Kara sevkiyatı ticari faturası", SortOrder = 1 },
            new() { ScopeType = "Shipment", TransportMode = "Road", DocumentType = "PackingList", IsRequired = true, Description = "Çeki listesi", SortOrder = 2 },
            new() { ScopeType = "Shipment", TransportMode = "Road", DocumentType = "CMR", IsRequired = true, Description = "Kara taşıma senedi (CMR)", SortOrder = 3 },
        };

        dbContext.DocumentRequirements.AddRange(defaults);
        await dbContext.SaveChangesAsync();
    }
}
