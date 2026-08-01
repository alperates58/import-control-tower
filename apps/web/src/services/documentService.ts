import { DocumentSummary, DocumentVersion, DocumentChecklist } from '../types/document';

type FetchFn = (input: any, init?: any) => Promise<Response>;

let customFetch: FetchFn = window.fetch.bind(window);

export const setDocumentServiceFetch = (fetchFn: FetchFn) => {
  customFetch = fetchFn;
};

const getHeaders = (idempotencyKey?: string, rowVersion?: number) => {
  const headers: Record<string, string> = {
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

export const documentService = {
  setFetch(fetchFn: FetchFn) {
    customFetch = fetchFn;
  },

  async getDocuments(params: Record<string, any> = {}, fetchFn?: FetchFn): Promise<DocumentSummary[]> {
    const doFetch = fetchFn || customFetch;
    const query = new URLSearchParams();
    Object.keys(params).forEach(key => {
      if (params[key] !== undefined && params[key] !== null && params[key] !== '') {
        query.append(key, params[key].toString());
      }
    });

    const res = await doFetch(`/api/v1/documents?${query.toString()}`, {
      headers: getHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Belgeler yüklenemedi.');
    }
    return await res.json();
  },

  async getDocumentById(id: string, fetchFn?: FetchFn): Promise<DocumentSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/documents/${id}`, {
      headers: getHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Belge detayı yüklenemedi.');
    }
    return await res.json();
  },

  async uploadCaseDocument(caseId: string, formData: FormData, idempotencyKey: string, fetchFn?: FetchFn): Promise<DocumentSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/import-cases/${caseId}/documents`, {
      method: 'POST',
      headers: getHeaders(idempotencyKey),
      body: formData
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Belge yüklenemedi.');
    }
    return await res.json();
  },

  async uploadShipmentDocument(shipmentId: string, formData: FormData, idempotencyKey: string, fetchFn?: FetchFn): Promise<DocumentSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/shipments/${shipmentId}/documents`, {
      method: 'POST',
      headers: getHeaders(idempotencyKey),
      body: formData
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Belge yüklenemedi.');
    }
    return await res.json();
  },

  async uploadContainerDocument(containerId: string, formData: FormData, idempotencyKey: string, fetchFn?: FetchFn): Promise<DocumentSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/containers/${containerId}/documents`, {
      method: 'POST',
      headers: getHeaders(idempotencyKey),
      body: formData
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Belge yüklenemedi.');
    }
    return await res.json();
  },

  async addVersion(documentId: string, formData: FormData, idempotencyKey: string, fetchFn?: FetchFn): Promise<DocumentSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/documents/${documentId}/versions`, {
      method: 'POST',
      headers: getHeaders(idempotencyKey),
      body: formData
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Versiyon eklenemedi.');
    }
    return await res.json();
  },

  async getDocumentVersions(documentId: string, fetchFn?: FetchFn): Promise<DocumentVersion[]> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/documents/${documentId}/versions`, {
      headers: getHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Versiyon geçmişi yüklenemedi.');
    }
    return await res.json();
  },

  async getDownloadUrl(documentId: string, versionId?: string, fetchFn?: FetchFn): Promise<{ downloadUrl: string; expiresMinutes: number }> {
    const doFetch = fetchFn || customFetch;
    const path = versionId 
      ? `/api/v1/documents/${documentId}/versions/${versionId}/download`
      : `/api/v1/documents/${documentId}/download`;

    const res = await doFetch(path, {
      headers: getHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Indirme bağlantısı üretilemedi.');
    }
    return await res.json();
  },

  async cancelDocument(documentId: string, rowVersion: number, fetchFn?: FetchFn): Promise<DocumentSummary> {
    const doFetch = fetchFn || customFetch;
    const res = await doFetch(`/api/v1/documents/${documentId}/cancel`, {
      method: 'POST',
      headers: getHeaders(undefined, rowVersion)
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || errData?.title || 'Belge iptal edilemedi.');
    }
    return await res.json();
  },

  async getChecklist(scopeType: 'ImportCase' | 'Shipment', scopeId: string, fetchFn?: FetchFn): Promise<DocumentChecklist> {
    const doFetch = fetchFn || customFetch;
    const path = scopeType === 'ImportCase' 
      ? `/api/v1/import-cases/${scopeId}/document-checklist`
      : `/api/v1/shipments/${scopeId}/document-checklist`;

    const res = await doFetch(path, {
      headers: getHeaders()
    });
    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.detail || 'Checklist yüklenemedi.');
    }
    return await res.json();
  }
};
