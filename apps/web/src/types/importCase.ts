export interface ImportCaseSummary {
  id: string;
  caseNumber: string;
  title: string;
  status: string;
  derivedOperationalStatus: string;
  supplierName: string;
  defaultTransportMode?: string;
  incoterm?: string;
  productionStatus: string;
  responsibleUserName?: string;
  estimatedProductionCompletionDate?: string;
  readyForShipmentDate?: string;
  minEtd?: string;
  maxEta?: string;
  isDelayed: boolean;
  lineCount: number;
  shipmentCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ImportCaseDetail {
  id: string;
  caseNumber: string;
  title: string;
  status: string;
  derivedOperationalStatus: string;
  supplierName: string;
  originCountry?: string;
  defaultTransportMode?: string;
  incoterm?: string;
  responsibleUserId?: string;
  responsibleUserName?: string;
  purchasingOwnerUserId?: string;
  purchasingOwnerUserName?: string;
  operationsOwnerUserId?: string;
  operationsOwnerUserName?: string;
  productionStatus: string;
  estimatedProductionCompletionDate?: string;
  readyForShipmentDate?: string;
  notes?: string;
  closedAtUtc?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
  lines: ImportCaseLine[];
  shipments: ShipmentSummary[];
}

export interface ImportCaseLine {
  id: string;
  importCaseId: string;
  purchaseOrderLineId: string;
  orderNumber: string;
  lineNumber: number;
  stockCode: string;
  stockName: string;
  orderedQuantity: number;
  allocatedQuantity: number;
  releasedQuantity: number;
  effectiveAllocatedQuantity: number;
  shippedQuantity: number;
  receivedQuantity: number;
  status: string;
  plannedShipmentDate?: string;
  notes?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
}

export interface AvailablePurchaseOrderLine {
  purchaseOrderLineId: string;
  purchaseOrderId: string;
  orderNumber: string;
  lineNumber: number;
  stockCode: string;
  stockName: string;
  supplierName: string;
  orderDate: string;
  orderedQuantity: number;
  remainingQuantity: number;
  allocatedToOtherCases: number;
  effectiveAvailableQuantity: number;
}

export interface ShipmentSummary {
  id: string;
  importCaseId: string;
  shipmentSequence: number;
  shipmentNumber: string;
  transportMode: string;
  originLocation: string;
  destinationLocation: string;
  originTimezoneId: string;
  destinationTimezoneId: string;
  bookingNumber?: string;
  forwarderName?: string;
  carrierName?: string;
  transportReference?: string;
  vesselName?: string;
  voyageNumber?: string;
  etd?: string;
  eta?: string;
  atd?: string;
  ata?: string;
  status: string;
  containerCount: number;
  lineAllocationCount: number;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface ShipmentDetail extends ShipmentSummary {
  caseNumber: string;
  estimatedWarehouseArrival?: string;
  actualWarehouseArrival?: string;
  notes?: string;
  rowVersion: number;
  lineAllocations: ShipmentLineAllocation[];
  containers: ShipmentContainer[];
  milestones: ShipmentMilestone[];
}

export interface ShipmentLineAllocation {
  id: string;
  shipmentId: string;
  importCaseLineId: string;
  importCaseId: string;
  stockCode: string;
  stockName: string;
  caseAllocatedQuantity: number;
  allocatedQuantity: number;
  releasedQuantity: number;
  effectiveAllocatedQuantity: number;
  shippedQuantity: number;
  receivedQuantity: number;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
}

export interface ShipmentContainer {
  id: string;
  shipmentId: string;
  containerNumber: string;
  normalizedContainerNumber: string;
  containerType: string;
  sealNumber?: string;
  grossWeightKg?: number;
  netWeightKg?: number;
  packageCount?: number;
  status: string;
  notes?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
}

export interface ShipmentMilestone {
  id: string;
  shipmentId: string;
  sequenceNumber: number;
  milestoneType: string;
  locationName?: string;
  timezoneId: string;
  plannedAtUtc?: string;
  estimatedAtUtc?: string;
  actualAtUtc?: string;
  status: string;
  source: string;
  notes?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
}

export interface SupplierLookup {
  supplierName: string;
  normalizedSupplierName: string;
  activeOrderCount: number;
}

export interface ImportCaseOperationalSummary {
  activeCaseCount: number;
  productionDelayedCount: number;
  readyForShipmentCount: number;
  bookingPendingCount: number;
  inTransitShipmentCount: number;
  delayedShipmentCount: number;
  etaThisWeekCount: number;
  unallocatedLineCount: number;
}
