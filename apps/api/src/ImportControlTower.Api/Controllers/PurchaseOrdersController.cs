using System;
using System.Linq;
using System.Threading.Tasks;
using ImportControlTower.Application.Models;
using ImportControlTower.Domain.Constants;
using ImportControlTower.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImportControlTower.Api.Controllers;

[ApiController]
[Route("api/v1/purchase-orders")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public PurchaseOrdersController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersView)]
    public async Task<IActionResult> GetPurchaseOrders(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var query = _db.PurchaseOrders
            .Include(po => po.Lines)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(po => po.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normSearch = search.Trim().ToUpperInvariant();
            query = query.Where(po => po.NormalizedOrderNumber.Contains(normSearch) || po.NormalizedSupplierName.Contains(normSearch));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(po => po.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(po => new PurchaseOrderDto(
                po.Id,
                po.OrderNumber,
                po.SupplierName,
                po.OrderDate,
                po.Status,
                po.Source,
                po.Lines.Count,
                po.Lines.Sum(l => l.OrderedQuantity),
                po.Lines.Sum(l => l.RemainingQuantity),
                po.CreatedAtUtc,
                po.UpdatedAtUtc,
                po.Lines.Select(l => new PurchaseOrderLineDto(
                    l.Id,
                    l.PurchaseOrderId,
                    l.LineNumber,
                    l.StockCode,
                    l.StockName,
                    l.OrderedQuantity,
                    l.RemainingQuantity,
                    l.SasDate,
                    l.CreatedAtUtc,
                    l.UpdatedAtUtc
                )).ToList()
            ))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Ok(new PagedResultDto<PurchaseOrderDto>(items, totalCount, page, pageSize, totalPages));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = PermissionsCatalog.PurchaseOrdersView)]
    public async Task<IActionResult> GetPurchaseOrderById(Guid id)
    {
        var po = await _db.PurchaseOrders
            .Include(p => p.Lines)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (po == null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Sipariş Bulunamadı",
                Detail = $"ID'si {id} olan satın alma siparişi bulunamadı."
            });
        }

        var dto = new PurchaseOrderDto(
            po.Id,
            po.OrderNumber,
            po.SupplierName,
            po.OrderDate,
            po.Status,
            po.Source,
            po.Lines.Count,
            po.Lines.Sum(l => l.OrderedQuantity),
            po.Lines.Sum(l => l.RemainingQuantity),
            po.CreatedAtUtc,
            po.UpdatedAtUtc,
            po.Lines.OrderBy(l => l.LineNumber).Select(l => new PurchaseOrderLineDto(
                l.Id,
                l.PurchaseOrderId,
                l.LineNumber,
                l.StockCode,
                l.StockName,
                l.OrderedQuantity,
                l.RemainingQuantity,
                l.SasDate,
                l.CreatedAtUtc,
                l.UpdatedAtUtc
            )).ToList()
        );

        return Ok(dto);
    }
}
