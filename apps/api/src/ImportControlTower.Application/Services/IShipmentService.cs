using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImportControlTower.Application.Models;

namespace ImportControlTower.Application.Services;

public interface IShipmentService
{
    Task<ShipmentDetailDto> CreateShipmentAsync(Guid caseId, CreateShipmentDto dto, Guid userId, string? correlationId = null);
    Task<List<ShipmentSummaryDto>> GetShipmentsByCaseIdAsync(Guid caseId);
    Task<ShipmentDetailDto?> GetShipmentByIdAsync(Guid shipmentId);
    Task<ShipmentDetailDto> UpdateShipmentAsync(Guid shipmentId, UpdateShipmentDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null);
    Task<ShipmentDetailDto> CancelShipmentAsync(Guid shipmentId, Guid userId, string? correlationId = null);
    Task<ShipmentDetailDto> AbortShipmentAsync(Guid shipmentId, AbortShipmentDto dto, Guid userId, uint expectedRowVersion);

    Task<List<ShipmentLineAllocationDto>> GetShipmentLinesAsync(Guid shipmentId);
    Task<ShipmentLineAllocationDto> AllocateShipmentLineAsync(Guid shipmentId, AllocateShipmentLineDto dto, Guid userId, string? correlationId = null);
    Task<ShipmentLineAllocationDto> UpdateShipmentLineAllocationAsync(Guid shipmentId, Guid allocationId, UpdateShipmentLineAllocationDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null);
    Task CancelShipmentLineAllocationAsync(Guid shipmentId, Guid allocationId, Guid userId, string? correlationId = null);

    Task<ShipmentContainerDto> AddContainerAsync(Guid shipmentId, AddContainerDto dto, Guid userId, string? correlationId = null);
    Task<ShipmentContainerDto> UpdateContainerAsync(Guid shipmentId, Guid containerId, UpdateContainerDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null);
    Task CancelContainerAsync(Guid shipmentId, Guid containerId, Guid userId, string? correlationId = null);

    Task<List<ShipmentMilestoneDto>> GetMilestonesAsync(Guid shipmentId);
    Task<ShipmentMilestoneDto> CreateMilestoneAsync(Guid shipmentId, CreateMilestoneDto dto, Guid userId, string? correlationId = null);
    Task<ShipmentMilestoneDto> UpdateMilestoneAsync(Guid shipmentId, Guid milestoneId, UpdateMilestoneDto dto, Guid userId, uint expectedRowVersion, string? correlationId = null);
}
