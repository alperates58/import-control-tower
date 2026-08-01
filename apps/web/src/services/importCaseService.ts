import {
  ImportCaseDetail,
  ImportCaseLine,
  AvailablePurchaseOrderLine,
  ShipmentDetail,
  ShipmentLineAllocation,
  ShipmentContainer,
  ShipmentMilestone,
  SupplierLookup,
  ImportCaseOperationalSummary
} from '../types/importCase';

type FetchFn = typeof fetch | ((url: string, init?: RequestInit) => Promise<Response>);

let customFetch: FetchFn = window.fetch.bind(window);

export const setImportCaseServiceFetch = (fetchFn: FetchFn) => {
  customFetch = fetchFn;
};

const getAuthHeaders = (idempotencyKey?: string, rowVersion?: number) => {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-ICT-CSRF-Protection': '1'
  };
  if (idempotencyKey) {
    headers['Idempotency-Key'] = idempotencyKey;
  }
  if (rowVersion !== undefined && rowVersion !== null) {
    headers['If-Match'] = `"${rowVersion}"`;
  }
  return headers;
};

export const importCaseService = {
  setFetch(fetchFn: typeof fetch) {
    customFetch = fetchFn;
  },

  async getCases(params: Record<string, any> = {}, fetchFn?: FetchFn) {
    const doFetch = fetchFn || customFetch;
    const query = new URLSearchParams();
    Object.keys(params).forEach(key => {
      if (params[key] !== undefined && params[key] !== null && params[key] !== '') {
        query.append(key, params[key].toString());
      }
    });

    const res = await doFetch(`/api/v1/import-cases?${query.toString()}`, {
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'İthalat dosyaları yüklenemedi.');
    }
    return await res.json();
  },

  async getCaseById(id: string, fetchFn?: FetchFn): Promise<ImportCaseDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${id}`, {
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'İthalat dosyası detayı yüklenemedi.');
    }
    return await res.json();
  },

  async createCase(dto: any, idempotencyKey: string, fetchFn?: FetchFn): Promise<ImportCaseDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch('/api/v1/import-cases', {
      method: 'POST',
      headers: getAuthHeaders(idempotencyKey),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'İthalat dosyası oluşturulamadı.');
    }
    return await res.json();
  },

  async updateCase(id: string, dto: any, rowVersion: number, fetchFn?: FetchFn): Promise<ImportCaseDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${id}`, {
      method: 'PATCH',
      headers: getAuthHeaders(undefined, rowVersion),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Dosya güncellenemedi.');
    }
    return await res.json();
  },

  async closeCase(id: string, fetchFn?: FetchFn): Promise<ImportCaseDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${id}/close`, {
      method: 'POST',
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Dosya kapatılamadı.');
    }
    return await res.json();
  },

  async cancelCase(id: string, fetchFn?: FetchFn): Promise<ImportCaseDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${id}/cancel`, {
      method: 'POST',
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Dosya iptal edilemedi.');
    }
    return await res.json();
  },

  async getAvailableSuppliers(search?: string, fetchFn?: FetchFn): Promise<SupplierLookup[]> {
    const doFetch = fetchFn || customFetch;
    const q = search ? `?search=${encodeURIComponent(search)}` : '';
    const res = await doFetch(`/api/v1/import-cases/available-suppliers${q}`, {
      headers: getAuthHeaders()
    });
    if (!res.ok) return [];
    return await res.json();
  },

  async getSummary(fetchFn?: FetchFn): Promise<ImportCaseOperationalSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch('/api/v1/import-cases/summary', {
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Özet yüklenemedi.');
    }
    return await res.json();
  },

  async getAvailablePurchaseOrders(caseId: string, search?: string, fetchFn?: FetchFn): Promise<AvailablePurchaseOrderLine[]> {
    const doFetch = fetchFn || customFetch;
    const q = search ? `?search=${encodeURIComponent(search)}` : '';
    const res = await doFetch(`/api/v1/import-cases/${caseId}/available-purchase-orders${q}`, {
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Atanabilir siparişler yüklenemedi.');
    }
    return await res.json();
  },

  async allocateOrderLine(caseId: string, dto: any, fetchFn?: FetchFn): Promise<ImportCaseLine> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${caseId}/lines`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sipariş kalemi eklenemedi.');
    }
    return await res.json();
  },

  async updateOrderLineAllocation(caseId: string, lineId: string, dto: any, rowVersion: number, fetchFn?: FetchFn): Promise<ImportCaseLine> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${caseId}/lines/${lineId}`, {
      method: 'PATCH',
      headers: getAuthHeaders(undefined, rowVersion),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Tahsis güncellenemedi.');
    }
    return await res.json();
  },

  async cancelOrderLineAllocation(caseId: string, lineId: string, fetchFn?: FetchFn): Promise<void> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${caseId}/lines/${lineId}/cancel`, {
      method: 'POST',
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Tahsis iptal edilemedi.');
    }
  },

  // Shipments
  async createShipment(caseId: string, dto: any, idempotencyKey: string, fetchFn?: FetchFn): Promise<ShipmentDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${caseId}/shipments`, {
      method: 'POST',
      headers: getAuthHeaders(idempotencyKey),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyat oluşturulamadı.');
    }
    return await res.json();
  },

  async getShipmentById(shipmentId: string, fetchFn?: FetchFn): Promise<ShipmentDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}`, {
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Sevkiyat detayı yüklenemedi.');
    }
    return await res.json();
  },

  async updateShipment(shipmentId: string, dto: any, rowVersion: number, fetchFn?: FetchFn): Promise<ShipmentDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}`, {
      method: 'PATCH',
      headers: getAuthHeaders(undefined, rowVersion),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyat güncellenemedi.');
    }
    return await res.json();
  },

  async cancelShipment(shipmentId: string, fetchFn?: FetchFn): Promise<ShipmentDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/cancel`, {
      method: 'POST',
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyat iptal edilemedi.');
    }
    return await res.json();
  },

  async abortShipment(shipmentId: string, reason: string, rowVersion: number, fetchFn?: FetchFn): Promise<ShipmentDetail> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/abort`, {
      method: 'POST',
      headers: getAuthHeaders(undefined, rowVersion),
      body: JSON.stringify({ reason })
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyat abort edilemedi.');
    }
    return await res.json();
  },

  // Shipment Line Allocations
  async allocateShipmentLine(shipmentId: string, dto: any, fetchFn?: FetchFn): Promise<ShipmentLineAllocation> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/lines`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyata kalem eklenemedi.');
    }
    return await res.json();
  },

  async updateShipmentLineAllocation(shipmentId: string, allocationId: string, dto: any, rowVersion: number, fetchFn?: FetchFn): Promise<ShipmentLineAllocation> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/lines/${allocationId}`, {
      method: 'PATCH',
      headers: getAuthHeaders(undefined, rowVersion),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyat kalemi güncellenemedi.');
    }
    return await res.json();
  },

  async cancelShipmentLineAllocation(shipmentId: string, allocationId: string, fetchFn?: FetchFn): Promise<void> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/lines/${allocationId}/cancel`, {
      method: 'POST',
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Sevkiyat kalemi iptal edilemedi.');
    }
  },

  // Containers
  async addContainer(shipmentId: string, dto: any, fetchFn?: typeof fetch): Promise<ShipmentContainer> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/containers`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Konteyner eklenemedi.');
    }
    return await res.json();
  },

  async cancelContainer(shipmentId: string, containerId: string, fetchFn?: typeof fetch): Promise<void> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/containers/${containerId}/cancel`, {
      method: 'POST',
      headers: getAuthHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Konteyner iptal edilemedi.');
    }
  },

  // Milestones
  async createMilestone(shipmentId: string, dto: any, fetchFn?: typeof fetch): Promise<ShipmentMilestone> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/milestones`, {
      method: 'POST',
      headers: getAuthHeaders(),
      body: JSON.stringify(dto)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Milestone eklenemedi.');
    }
    return await res.json();
  }
};
