export interface DocumentSummary {
  id: string;
  importCaseId?: string;
  shipmentId?: string;
  shipmentContainerId?: string;
  documentType: string;
  title: string;
  documentNumber?: string;
  documentDate?: string;
  expiryDate?: string;
  status: string;
  notes?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  createdByUserId: string;
  createdByUserName?: string;
  rowVersion: number;
  currentVersion?: DocumentVersion;
}

export interface DocumentVersion {
  id: string;
  documentId: string;
  versionNumber: number;
  originalFileName: string;
  storedObjectKey: string;
  contentType: string;
  fileExtension: string;
  fileSizeBytes: number;
  sha256Hash: string;
  storageStatus: string;
  isCurrent: boolean;
  status: string;
  uploadedAtUtc: string;
  uploadedByUserId: string;
  uploadedByUserName?: string;
}

export interface DocumentChecklist {
  scopeType: string;
  scopeId: string;
  totalRequiredCount: number;
  completedCount: number;
  missingCount: number;
  status: string; // Complete, Missing, Expired
  items: DocumentChecklistItem[];
}

export interface DocumentChecklistItem {
  documentType: string;
  description: string;
  isRequired: boolean;
  status: string; // Complete, Missing, Expired
  linkedDocumentId?: string;
  documentTitle?: string;
  documentNumber?: string;
  expiryDate?: string;
}
