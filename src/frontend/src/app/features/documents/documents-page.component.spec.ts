import '@angular/compiler';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';

import {
  DocumentCatalogDetail,
  DocumentCatalogItem,
  DocumentCompleteness,
  DocumentSummary
} from '../../core/models/document-catalog.models';
import { AuthService } from '../../core/services/auth.service';
import { DocumentCatalogService } from '../../core/services/document-catalog.service';
import { DocumentsCatalogTableComponent } from './documents-catalog-table.component';
import { DocumentsPageComponent } from './documents-page.component';

const summary: DocumentSummary = {
  totalDocuments: 1,
  activeDocuments: 1,
  archivedDocuments: 0,
  integrityIssuesCount: 0,
  incompleteEntitiesCount: 1,
  reviewDueCount: 0,
  expiredRetentionCount: 0,
  administrativeHoldCount: 0,
  modules: [
    {
      moduleCode: 'MARKETS',
      moduleName: 'Mercados',
      totalDocuments: 1,
      activeDocuments: 1,
      archivedDocuments: 0,
      integrityIssuesCount: 0,
      incompleteEntitiesCount: 1,
      reviewDueCount: 0,
      expiredRetentionCount: 0,
      administrativeHoldCount: 0
    }
  ],
  documentClasses: [
    {
      documentClassCode: 'CERTIFICATE',
      totalDocuments: 1,
      activeDocuments: 1,
      archivedDocuments: 0
    }
  ],
  operationalStatuses: [],
  workQueueCategories: []
};

const catalogItem: DocumentCatalogItem = {
  id: 'doc-1',
  moduleCode: 'MARKETS',
  moduleName: 'Mercados',
  documentAreaCode: 'MARKETS_TENANT_CERTIFICATES',
  entityType: 'MARKET_TENANT',
  entityId: 'tenant-1',
  originContext: {
    moduleCode: 'MARKETS',
    moduleName: 'Mercados',
    entityType: 'MARKET_TENANT',
    entityId: 'tenant-1',
    displayName: 'Local 1',
    summary: 'Locatario activo',
    routeHint: '/markets/tenants/tenant-1'
  },
  originalFileName: 'cedula.pdf',
  contentType: 'application/pdf',
  sizeBytes: 1200,
  createdUtc: '2026-01-01T00:00:00Z',
  documentClassCode: 'CERTIFICATE',
  businessPurpose: null,
  isPrimaryDocument: true,
  classificationNotes: null,
  retentionPolicyCode: 'CERTIFICATE_REVIEW',
  retentionUntilUtc: '2027-01-01T00:00:00Z',
  retentionStatusCode: 'ACTIVE_RETENTION',
  retentionBaselinePolicyCode: 'CERTIFICATE_REVIEW',
  retentionBaselineUntilUtc: '2027-01-01T00:00:00Z',
  retentionEffectivePolicyCode: 'CERTIFICATE_REVIEW',
  retentionEffectiveUntilUtc: '2027-01-01T00:00:00Z',
  hasRetentionOverride: false,
  retentionOverridePolicyCode: null,
  retentionOverrideUntilUtc: null,
  retentionOverrideReason: null,
  retentionReviewStatusCode: 'REVIEW_PENDING',
  lastRetentionReviewUtc: null,
  nextRetentionReviewUtc: '2026-06-01T00:00:00Z',
  retentionReviewNotes: null,
  isAdministrativeHold: false,
  holdReason: null,
  holdPlacedUtc: null,
  holdReleasedUtc: null,
  holdPlacedBy: null,
  documentOperationalStatusCode: 'ACTIVE_OK',
  documentOperationalSeverityCode: 'NONE',
  statusCode: 'ACTIVE',
  archivedUtc: null,
  archiveReason: null,
  isSuperseded: false,
  supersededByDocumentId: null,
  replacedDocumentId: null,
  replacementGroupKey: 'tenant-1:CERTIFICATE',
  integrityState: 'VALID',
  actualSizeBytes: 1200,
  hasChecksum: true,
  isLegacyBackfill: false,
  detailUrl: '/api/documents/doc-1',
  downloadUrl: '/api/documents/doc-1/download'
};

const catalogDetail: DocumentCatalogDetail = {
  ...catalogItem,
  sha256Hex: 'abc123'
};

const pendingItem: DocumentCompleteness = {
  moduleCode: 'MARKETS',
  moduleName: 'Mercados',
  entityType: 'MARKET_TENANT',
  entityId: 'tenant-1',
  originContext: catalogItem.originContext,
  ruleCode: 'MARKET_CERTIFICATE',
  statusCode: 'INCOMPLETE',
  isComplete: false,
  requiredDocumentCode: 'TENANT_CERTIFICATE',
  requiredDocumentDescription: 'Cedula vigente',
  requiredDocumentClassCodes: ['CERTIFICATE'],
  minimumRequiredCount: 1,
  missingReasonCode: 'MISSING_REQUIRED_DOCUMENT',
  missingReasonDescription: 'Falta cedula',
  remediationHint: 'Carga la cedula desde el origen.',
  relatedDocumentCount: 0,
  coveringDocumentIds: []
};

function createDocumentCatalogServiceMock() {
  return {
    getSummary: vi.fn(() => of(summary)),
    listDocuments: vi.fn(() => of({
      totalCount: 1,
      returnedCount: 1,
      skip: 0,
      take: 50,
      items: [catalogItem]
    })),
    listPendingCompleteness: vi.fn(() => of({
      totalCount: 1,
      returnedCount: 1,
      skip: 0,
      take: 20,
      items: [pendingItem]
    })),
    getDocument: vi.fn(() => of(catalogDetail)),
    getDocumentTimeline: vi.fn(() => of({ totalCount: 0, items: [] })),
    downloadDocument: vi.fn(() => Promise.resolve()),
    exportDocuments: vi.fn(() => Promise.resolve())
  };
}

async function renderDocumentsPage(canAdministerUsers = false) {
  const documentCatalogService = createDocumentCatalogServiceMock();

  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [DocumentsPageComponent],
    providers: [
      {
        provide: DocumentCatalogService,
        useValue: documentCatalogService
      },
      {
        provide: AuthService,
        useValue: {
          canAdministerUsers: signal(canAdministerUsers),
          canWriteMarkets: vi.fn(() => false),
          canWriteDonations: vi.fn(() => false),
          canWriteFederation: vi.fn(() => false)
        }
      }
    ]
  }).compileComponents();

  const fixture = TestBed.createComponent(DocumentsPageComponent);
  fixture.detectChanges();
  await fixture.whenStable();
  fixture.detectChanges();

  return {
    fixture,
    documentCatalogService
  };
}

describe('DocumentsPageComponent', () => {
  it('renders summary KPIs and secondary summary content', async () => {
    const { fixture } = await renderDocumentsPage();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.textContent).toContain('Total');
    expect(compiled.textContent).toContain('Incompletos');

    compiled.querySelectorAll<HTMLButtonElement>('.tabs button')[2]?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Por modulo');
    expect(compiled.textContent).toContain('Por clase');
  });

  it('renders pending documents and filters the catalog from the pending action', async () => {
    const { fixture, documentCatalogService } = await renderDocumentsPage();
    const compiled = fixture.nativeElement as HTMLElement;

    compiled.querySelectorAll<HTMLButtonElement>('.tabs button')[1]?.click();
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Falta cedula');

    compiled.querySelector<HTMLButtonElement>('app-documents-pending-table button')?.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(documentCatalogService.listDocuments).toHaveBeenLastCalledWith(expect.objectContaining({
      entityId: 'tenant-1',
      entityType: 'MARKET_TENANT',
      statusCode: null,
      includeArchived: true
    }));
  });

  it('loads document detail when the catalog table emits a selection', async () => {
    const { fixture, documentCatalogService } = await renderDocumentsPage();
    const compiled = fixture.nativeElement as HTMLElement;
    const catalogTable = fixture.debugElement.query(By.directive(DocumentsCatalogTableComponent))
      .componentInstance as DocumentsCatalogTableComponent;

    catalogTable.selectDocument.emit(catalogItem);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(documentCatalogService.getDocument).toHaveBeenCalledWith('doc-1');
    expect(compiled.textContent).toContain('SHA-256');
  });

  it('hides administrative actions for non-admin users', async () => {
    const { fixture } = await renderDocumentsPage(false);
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('form.metadata-form')).toBeNull();
    expect(compiled.textContent).not.toContain('Guardar datos');
  });
});
