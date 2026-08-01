using System;
using System.Threading.Tasks;
using ImportControlTower.Application.Common.Interfaces;
using ImportControlTower.Application.Models;
using ImportControlTower.Application.Services;
using ImportControlTower.Domain.Entities;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace ImportControlTower.Tests;

public class ImportCaseTests
{
    private ApplicationDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateCase_WithValidSupplier_Succeeds()
    {
        using var db = GetInMemoryDbContext();
        var numGenMock = new Mock<IDocumentNumberGenerator>();
        numGenMock.Setup(g => g.GenerateCaseNumberAsync(It.IsAny<ApplicationDbContext>(), It.IsAny<int>()))
            .ReturnsAsync("IMP-2026-000001");

        var auditMock = new Mock<IAuditLogService>();

        // Seed PO
        db.PurchaseOrders.Add(new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            OrderNumber = "PO-100",
            NormalizedOrderNumber = "PO-100",
            SupplierName = "Shanghai Trading Ltd",
            NormalizedSupplierName = "SHANGHAI TRADING LTD",
            Status = "Open",
            Source = "Manual",
            CreatedByUserId = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var service = new ImportCaseService(db, numGenMock.Object, auditMock.Object);

        var dto = new CreateImportCaseDto(
            Title: "Test Case",
            SupplierName: "Shanghai Trading Ltd",
            DefaultTransportMode: "Sea",
            OriginCountry: "China",
            Incoterm: "FOB",
            ResponsibleUserId: null,
            Notes: null,
            EstimatedProductionCompletionDate: null
        );

        var created = await service.CreateCaseAsync(dto, Guid.NewGuid());

        Assert.NotNull(created);
        Assert.Equal("IMP-2026-000001", created.caseNumber);
        Assert.Equal("SHANGHAI TRADING LTD", db.ImportCases.First().NormalizedSupplierName);
    }
}
