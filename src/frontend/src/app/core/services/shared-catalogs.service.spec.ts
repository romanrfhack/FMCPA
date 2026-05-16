import '@angular/compiler';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, expect, it } from 'vitest';

import { ContactIntervention, CreateContactInterventionRequest } from '../models/shared-catalogs.models';
import { SharedCatalogsService } from './shared-catalogs.service';

describe('SharedCatalogsService', () => {
  it('gets contact interventions by origin', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        SharedCatalogsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    const service = TestBed.inject(SharedCatalogsService);
    const httpTestingController = TestBed.inject(HttpTestingController);
    const response: ContactIntervention[] = [
      {
        id: 'intervention-1',
        contactId: 'contact-1',
        contactName: 'Juan Perez',
        contactTypeName: 'Externo',
        moduleKey: 'MARKETS',
        originType: 'MARKET_ISSUE',
        originId: 'issue-1',
        originDisplayName: 'Incidencia Mercado Juarez',
        subject: 'Apoyo para gestionar incidencia',
        helpType: 'UNBLOCKING',
        outcome: 'USEFUL',
        notes: 'Ayudo con el area X.',
        occurredUtc: '2026-05-16T18:30:00Z',
        createdByUserId: 'user-1',
        createdByUserName: 'Operadora Interna',
        createdUtc: '2026-05-16T18:45:00Z',
        updatedUtc: null,
        archivedUtc: null
      }
    ];

    service.getContactInterventionsByOrigin({
      moduleKey: 'MARKETS',
      originType: 'MARKET_ISSUE',
      originId: 'issue-1'
    }).subscribe((interventions) => {
      expect(interventions).toEqual(response);
    });

    const httpRequest = httpTestingController.expectOne((request) =>
      request.url === '/api/contact-interventions' &&
      request.params.get('moduleKey') === 'MARKETS' &&
      request.params.get('originType') === 'MARKET_ISSUE' &&
      request.params.get('originId') === 'issue-1');
    expect(httpRequest.request.method).toBe('GET');
    httpRequest.flush(response);
    httpTestingController.verify();
  });

  it('posts a contact intervention by contact id', () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        SharedCatalogsService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    const service = TestBed.inject(SharedCatalogsService);
    const httpTestingController = TestBed.inject(HttpTestingController);
    const request: CreateContactInterventionRequest = {
      moduleKey: 'MARKETS',
      originType: 'MARKET_ISSUE',
      originId: 'issue-1',
      originDisplayName: 'Incidencia Mercado Juarez',
      subject: 'Apoyo para gestionar incidencia',
      helpType: 'UNBLOCKING',
      outcome: 'USEFUL',
      notes: 'Ayudo con el area X.',
      occurredUtc: '2026-05-16T18:30:00.000Z'
    };
    const response: ContactIntervention = {
      id: 'intervention-1',
      contactId: 'contact-1',
      contactName: 'Juan Perez',
      contactTypeName: 'Externo',
      moduleKey: request.moduleKey,
      originType: request.originType,
      originId: request.originId,
      originDisplayName: request.originDisplayName,
      subject: request.subject,
      helpType: request.helpType,
      outcome: request.outcome,
      notes: request.notes,
      occurredUtc: request.occurredUtc,
      createdByUserId: 'user-1',
      createdByUserName: 'Operadora Interna',
      createdUtc: '2026-05-16T18:45:00Z',
      updatedUtc: null,
      archivedUtc: null
    };

    service.createContactIntervention('contact-1', request).subscribe((createdIntervention) => {
      expect(createdIntervention).toEqual(response);
    });

    const httpRequest = httpTestingController.expectOne('/api/contacts/contact-1/interventions');
    expect(httpRequest.request.method).toBe('POST');
    expect(httpRequest.request.body).toEqual(request);
    httpRequest.flush(response);
    httpTestingController.verify();
  });
});
