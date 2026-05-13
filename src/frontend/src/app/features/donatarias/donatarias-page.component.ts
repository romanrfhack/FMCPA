import { DatePipe, DecimalPipe, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import {
  CreateDonationApplicationRequest,
  CreateDonationRequest,
  DonationAlert,
  DonationApplication,
  DonationApplicationDocumentaryStatus,
  DonationApplicationEvidence,
  DonationDetail,
  DonationDocumentaryStatus,
  DonationSummary,
  DonationTransparencyApplication,
  DonationTransparencyEvidence,
  DonationTransparencyReport
} from '../../core/models/donations.models';
import { Contact, CatalogItem, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { DonationsService } from '../../core/services/donations.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';
import { RelatedDocumentsPanelComponent } from '../documents/related-documents-panel.component';

type DonatariasTab = 'summary' | 'donations' | 'applications' | 'evidences' | 'report';
type ApplicationSortMode = 'dateAsc' | 'dateDesc' | 'amountDesc' | 'evidencePending';

interface VisibleDonationMetrics {
  donationCount: number;
  totalReceived: number;
  totalApplied: number;
  pendingBalance: number;
  appliedPercentage: number;
  openCount: number;
  closedCount: number;
  pendingBalanceCount: number;
}

interface PresentationReadiness {
  label: 'Si' | 'Parcial' | 'No';
  className: string;
  description: string;
}

@Component({
  selector: 'app-donatarias-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, RelatedDocumentsPanelComponent],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">Transparencia de donaciones</p>
        <h2>Donatarias</h2>
        <p>
          Seguimiento del recurso recibido, su distribucion, saldo pendiente y evidencia minima por aplicacion.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      @if (selectedDonation(); as donationDetail) {
        <article class="detail-card transparency-header">
          <div class="detail-header">
            <div>
              <p class="page-kicker">Donacion seleccionada</p>
              <h3>{{ donationDetail.donorEntityName }}</h3>
              <p class="meta">
                Ref {{ donationDetail.reference }}
                · {{ donationDetail.donationDate }}
                · {{ donationDetail.donationType }}
              </p>
            </div>
            <div class="detail-badges">
              <span class="status-pill" [class]="financialSignal().className">
                Financiero: {{ financialSignal().label }}
              </span>
              <span class="status-pill" [class]="documentarySignal().className">
                Documental: {{ documentarySignal().label }}
              </span>
              <span class="status-pill" [class]="operativeSignal().className">
                Operativo: {{ operativeSignal().label }}
              </span>
              <button
                type="button"
                class="ghost"
                (click)="openClosePanel()"
                [disabled]="!canAdminister() || donationDetail.statusIsClosed"
                [attr.title]="!canAdminister()
                  ? 'Solo ADMIN puede registrar cierre formal.'
                  : donationDetail.statusIsClosed
                    ? 'La donacion ya se encuentra en estado terminal.'
                    : 'Registrar cierre formal.'">
                {{ donationDetail.statusIsClosed ? 'Ya cerrada' : 'Cerrar formalmente' }}
              </button>
            </div>
          </div>

          <div class="summary-grid header-metrics">
            <article>
              <h4>Total recibido / valor recibido</h4>
              <p>{{ donationDetail.baseAmount | number: '1.2-2' }}</p>
            </article>
            <article>
              <h4>Total aplicado</h4>
              <p>{{ donationDetail.appliedAmountTotal | number: '1.2-2' }}</p>
            </article>
            <article>
              <h4>Saldo pendiente</h4>
              <p>{{ donationDetail.remainingAmount | number: '1.2-2' }}</p>
            </article>
            <article>
              <h4>Porcentaje aplicado</h4>
              <p>{{ donationDetail.appliedPercentage | number: '1.2-2' }}%</p>
            </article>
          </div>

          <p class="balance-callout" [class.warning]="donationDetail.remainingAmount > 0">
            {{ selectedDonationBalanceMessage() }}
          </p>
        </article>
      } @else {
        <article class="empty-card">
          <h3>Selecciona una donacion</h3>
          <p>
            La pantalla mostrara total recibido, aplicado, saldo, porcentaje, aplicaciones y evidencias
            cuando exista una donacion seleccionada.
          </p>
        </article>
      }

      @if (isClosePanelOpen()) {
        @if (selectedDonation(); as donationDetail) {
          <article class="detail-card close-panel">
            <div class="card-header">
              <div>
                <h3>Cierre formal operativo</h3>
                <p>
                  Este cierre operativo bloquea nuevas aplicaciones/evidencias sobre la donacion, pero no equivale
                  a comprobacion legal o contable completa.
                </p>
              </div>
            </div>

            <div class="warning-list">
              @if (donationDetail.remainingAmount > 0) {
                <p class="inline-note">Saldo pendiente: {{ donationDetail.remainingAmount | number: '1.2-2' }}.</p>
              }
              @if (applicationsWithoutEvidenceCount() > 0) {
                <p class="inline-note">
                  Aplicaciones con evidencia minima pendiente: {{ applicationsWithoutEvidenceCount() }}.
                </p>
              }
              <p class="inline-note">
                La evidencia registrada acredita presencia documental minima; no sustituye revision legal o contable.
              </p>
            </div>

            <label class="full-width">
              <span>Motivo breve de cierre formal</span>
              <textarea
                rows="3"
                [value]="closeReason()"
                (input)="setCloseReason($event)"
                placeholder="Opcional"></textarea>
            </label>

            <div class="form-actions">
              <button type="button" (click)="confirmCloseSelectedDonation()" [disabled]="isClosingDonation()">
                Confirmar cierre
              </button>
              <button type="button" class="ghost" (click)="cancelClosePanel()" [disabled]="isClosingDonation()">
                Cancelar
              </button>
            </div>
          </article>
        }
      }

      <nav class="tab-nav" aria-label="Secciones de Donatarias">
        <button type="button" [class.is-active]="activeTab() === 'summary'" (click)="setActiveTab('summary')">
          Resumen
        </button>
        <button type="button" [class.is-active]="activeTab() === 'donations'" (click)="setActiveTab('donations')">
          Donaciones
        </button>
        <button type="button" [class.is-active]="activeTab() === 'applications'" (click)="setActiveTab('applications')">
          Aplicaciones / distribucion
        </button>
        <button type="button" [class.is-active]="activeTab() === 'evidences'" (click)="setActiveTab('evidences')">
          Evidencias
        </button>
        <button type="button" [class.is-active]="activeTab() === 'report'" (click)="setActiveTab('report')">
          Reporte de transparencia
        </button>
      </nav>

      @if (visibleDonationMetrics(); as listMetrics) {
        <article class="detail-card list-summary-card">
          <div class="card-header">
            <div>
              <p class="page-kicker">Lista cargada / filtrada</p>
              <h3>Resumen financiero visible</h3>
              <p>
                Estos KPIs se calculan en frontend sobre las donaciones cargadas con el filtro actual;
                no representan el universo global si el backend no entrega un agregado dedicado.
              </p>
            </div>
          </div>

          <div class="summary-grid list-kpi-grid">
            <article>
              <h4>Donaciones visibles</h4>
              <p>{{ listMetrics.donationCount }}</p>
            </article>
            <article>
              <h4>Total recibido visible</h4>
              <p>{{ listMetrics.totalReceived | number: '1.2-2' }}</p>
            </article>
            <article>
              <h4>Total aplicado visible</h4>
              <p>{{ listMetrics.totalApplied | number: '1.2-2' }}</p>
            </article>
            <article>
              <h4>Saldo pendiente visible</h4>
              <p>{{ listMetrics.pendingBalance | number: '1.2-2' }}</p>
            </article>
            <article>
              <h4>% aplicado visible</h4>
              <p>{{ listMetrics.appliedPercentage | number: '1.2-2' }}%</p>
            </article>
            <article>
              <h4>Donaciones abiertas</h4>
              <p>{{ listMetrics.openCount }}</p>
            </article>
            <article>
              <h4>Donaciones cerradas</h4>
              <p>{{ listMetrics.closedCount }}</p>
            </article>
            <article>
              <h4>Con saldo pendiente</h4>
              <p>{{ listMetrics.pendingBalanceCount }}</p>
            </article>
          </div>
        </article>
      }

      @if (activeTab() === 'summary') {
        @if (selectedDonation(); as donationDetail) {
          <section class="tab-panel">
            <div class="summary-grid kpi-grid">
              <article>
                <h4>Total recibido</h4>
                <p>{{ donationDetail.baseAmount | number: '1.2-2' }}</p>
              </article>
              <article>
                <h4>Total aplicado</h4>
                <p>{{ donationDetail.appliedAmountTotal | number: '1.2-2' }}</p>
              </article>
              <article>
                <h4>Saldo pendiente</h4>
                <p>{{ donationDetail.remainingAmount | number: '1.2-2' }}</p>
              </article>
              <article>
                <h4>Porcentaje aplicado</h4>
                <p>{{ donationDetail.appliedPercentage | number: '1.2-2' }}%</p>
              </article>
              <article>
                <h4>Numero de aplicaciones</h4>
                <p>{{ donationDetail.applications.length }}</p>
              </article>
              <article>
                <h4>Evidencias registradas</h4>
                <p>{{ selectedDonationEvidenceCount() }}</p>
              </article>
              <article>
                <h4>Documentos activos</h4>
                <p>{{ selectedDonationActiveDocumentCount() }}</p>
              </article>
              <article>
                <h4>Aplicaciones con evidencia minima</h4>
                <p>{{ applicationsWithMinimumEvidenceCount() }}</p>
              </article>
              <article>
                <h4>Pendientes de evidencia</h4>
                <p>{{ applicationsWithoutEvidenceCount() }}</p>
              </article>
              <article>
                <h4>Estado operativo</h4>
                <p>{{ operativeSignal().label }}</p>
              </article>
            </div>

            <p class="balance-callout" [class.warning]="donationDetail.remainingAmount > 0">
              {{ selectedDonationBalanceMessage() }}
            </p>

            <div class="signal-grid">
              <article class="signal-card {{ financialSignal().className }}">
                <h4>Semaforo financiero</h4>
                <strong>{{ financialSignal().label }}</strong>
                <p>{{ financialSignal().description }}</p>
              </article>
              <article class="signal-card {{ documentarySignal().className }}">
                <h4>Semaforo documental</h4>
                <strong>{{ documentarySignal().label }}</strong>
                <p>{{ documentarySignal().description }}</p>
              </article>
              <article class="signal-card {{ operativeSignal().className }}">
                <h4>Semaforo operativo</h4>
                <strong>{{ operativeSignal().label }}</strong>
                <p>{{ operativeSignal().description }}</p>
              </article>
            </div>

            <article class="detail-card compact-card">
              <div class="card-header">
                <div>
                  <h4>Estado documental agregado</h4>
                  <p class="meta">
                    Aplicaciones con evidencia minima activa: {{ applicationsWithMinimumEvidenceCount() }}
                    · Pendientes: {{ applicationsWithoutEvidenceCount() }}
                    · Documentos activos: {{ selectedDonationActiveDocumentCount() }}
                  </p>
                </div>
                <span class="status-pill" [class]="documentarySignal().className">
                  {{ documentarySignal().label }}
                </span>
              </div>

              @if (applicationsWithoutEvidenceCount() > 0) {
                <div class="donation-stats">
                  @for (application of sortedApplications(); track application.id) {
                    @if (!applicationHasMinimumEvidence(application)) {
                      <span>{{ application.beneficiaryName }}: {{ applicationDocumentaryMissingLabel(application) }}</span>
                    }
                  }
                </div>
              }
            </article>

            <article class="detail-card readiness-card {{ presentationReadiness().className }}">
              <div>
                <p class="page-kicker">Criterio operativo preliminar</p>
                <h4>Lista para presentar: {{ presentationReadiness().label }}</h4>
                <p>{{ presentationReadiness().description }}</p>
                <p class="meta">
                  Este criterio usa saldo pendiente y evidencia minima por aplicacion; no sustituye revision legal o contable.
                </p>
              </div>
            </article>

            @if (selectedDonationAlert(); as alert) {
              <article class="alert-row">
                <div class="row-top">
                  <h4>Alerta activa</h4>
                  <span class="status-pill" [class]="alertStateClass(alert.alertState)">
                    {{ alertStateLabel(alert.alertState) }}
                  </span>
                </div>
                <p class="meta">
                  Total recibido {{ alert.baseAmount | number: '1.2-2' }}
                  · Aplicado {{ alert.appliedAmountTotal | number: '1.2-2' }}
                  · Saldo {{ alert.remainingAmount | number: '1.2-2' }}
                </p>
              </article>
            }

            @if (donationDetail.notes) {
              <article class="detail-card compact-card">
                <h4>Observaciones</h4>
                <p class="detail-notes">{{ donationDetail.notes }}</p>
              </article>
            }

            <div class="form-actions">
              <button type="button" class="ghost" (click)="setActiveTab('applications')">Ver distribucion</button>
              <button type="button" class="ghost" (click)="setActiveTab('evidences')">Ver evidencias</button>
              <button type="button" class="ghost" (click)="setActiveTab('report')">Ver vista preliminar</button>
            </div>
          </section>
        } @else {
          <p class="empty-state">Selecciona una donacion para ver el resumen de transparencia.</p>
        }
      }

      @if (activeTab() === 'applications') {
        @if (selectedDonation(); as donationDetail) {
          <section class="tab-panel detail-grid">
            <article class="list-card wide-card">
              <div class="card-header">
                <div>
                  <h3>Aplicaciones / distribucion</h3>
                  <p>Distribucion del recurso recibido y evidencia minima por aplicacion.</p>
                </div>
              </div>

              <div class="sort-toolbar">
                <span>Orden visual para saldo acumulado:</span>
                <button
                  type="button"
                  class="ghost"
                  [class.is-active]="applicationSort() === 'dateAsc'"
                  (click)="setApplicationSort('dateAsc')">
                  Fecha asc.
                </button>
                <button
                  type="button"
                  class="ghost"
                  [class.is-active]="applicationSort() === 'dateDesc'"
                  (click)="setApplicationSort('dateDesc')">
                  Fecha desc.
                </button>
                <button
                  type="button"
                  class="ghost"
                  [class.is-active]="applicationSort() === 'amountDesc'"
                  (click)="setApplicationSort('amountDesc')">
                  Monto mayor
                </button>
                <button
                  type="button"
                  class="ghost"
                  [class.is-active]="applicationSort() === 'evidencePending'"
                  (click)="setApplicationSort('evidencePending')">
                  Evidencia pendiente
                </button>
              </div>

              <p class="inline-note">
                El saldo restante despues de cada aplicacion se calcula sobre este orden visual con datos actuales.
              </p>

              @if (donationDetail.applications.length === 0) {
                <p class="empty-state">Aun no hay aplicaciones registradas.</p>
              } @else {
                <div class="application-table">
                  <div class="application-row table-head">
                    <span>Beneficiario</span>
                    <span>Fecha</span>
                    <span>Responsable</span>
                    <span>Monto aplicado</span>
                    <span>% del total recibido</span>
                    <span>Saldo restante</span>
                    <span>Peso relativo</span>
                    <span>Estatus de aplicacion</span>
                    <span>Evidencias</span>
                    <span>Estado documental simple</span>
                    <span>Detalle de comprobacion</span>
                  </div>
                  @for (application of sortedApplications(); track application.id) {
                    <button
                      type="button"
                      class="application-row"
                      [class.is-selected]="application.id === selectedApplicationId()"
                      (click)="selectApplication(application.id)">
                      <span>{{ application.beneficiaryName }}</span>
                      <span>{{ application.applicationDate }}</span>
                      <span>{{ application.responsibleName }}</span>
                      <span>{{ application.appliedAmount | number: '1.2-2' }}</span>
                      <span>{{ applicationSharePercentage(application, donationDetail) | number: '1.2-2' }}%</span>
                      <span>{{ applicationRemainingAfter(application, donationDetail) | number: '1.2-2' }}</span>
                      <span class="status-pill" [class]="applicationSignificanceClass(application, donationDetail)">
                        {{ applicationSignificanceLabel(application, donationDetail) }}
                      </span>
                      <span class="status-pill" [class]="applicationStatusClass(application.statusCode)">
                        {{ application.statusName }}
                      </span>
                      <span>{{ applicationEvidenceSummary(application) }}</span>
                      <span class="status-pill" [class]="applicationDocumentaryClass(application)">
                        {{ applicationDocumentaryLabel(application) }}
                      </span>
                      <span>{{ application.verificationDetails || applicationDocumentaryMissingLabel(application) }}</span>
                    </button>
                  }
                </div>
              }
            </article>

            <article class="form-card">
              <div class="card-header">
                <div>
                  <h3>Registrar aplicacion</h3>
                  <p>Beneficiario, responsable, monto aplicado y detalle de comprobacion.</p>
                </div>
              </div>

              @if (applicationFormError()) {
                <p class="alert error">{{ applicationFormError() }}</p>
              }

              @if (applicationFormSuccess()) {
                <p class="alert success">{{ applicationFormSuccess() }}</p>
              }

              <form class="form-grid" [formGroup]="applicationForm" (ngSubmit)="submitApplication()">
                <label>
                  <span>Beneficiario</span>
                  <input type="text" formControlName="beneficiaryName" placeholder="Beneficiario" />
                </label>

                <label>
                  <span>Fecha de aplicacion</span>
                  <input type="date" formControlName="applicationDate" />
                </label>

                <label>
                  <span>Contacto responsable</span>
                  <select formControlName="responsibleContactId" (change)="syncResponsibleFromContact()">
                    <option value="">Sin vincular</option>
                    @for (contact of contacts(); track contact.id) {
                      <option [value]="contact.id">{{ contact.name }}</option>
                    }
                  </select>
                </label>

                <label>
                  <span>Responsable o creador</span>
                  <input type="text" formControlName="responsibleName" placeholder="Nombre del responsable" />
                </label>

                <label>
                  <span>Monto aplicado</span>
                  <input type="number" min="0.01" step="0.01" formControlName="appliedAmount" />
                </label>

                <label>
                  <span>Estatus de aplicacion</span>
                  <select formControlName="statusCatalogEntryId">
                    <option [value]="0">Selecciona un estatus</option>
                    @for (status of applicationStatuses(); track status.id) {
                      <option [value]="status.id">{{ status.statusName }}</option>
                    }
                  </select>
                </label>

                <label class="full-width">
                  <span>Detalle de comprobacion</span>
                  <textarea formControlName="verificationDetails" rows="4" placeholder="Detalle de comprobacion"></textarea>
                </label>

                <label class="full-width">
                  <span>Nota de cierre de la aplicacion</span>
                  <textarea formControlName="closingDetails" rows="3" placeholder="Si aplica"></textarea>
                </label>

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingApplication() || !canWrite()">Registrar aplicacion</button>
                  <button type="button" class="ghost" (click)="resetApplicationForm()">Limpiar</button>
                  <button
                    type="button"
                    class="ghost"
                    [disabled]="!selectedApplication()"
                    (click)="selectedApplication() && setActiveTab('evidences')">
                    Cargar evidencia
                  </button>
                </div>
              </form>
            </article>
          </section>
        } @else {
          <p class="empty-state">Selecciona una donacion para consultar o registrar aplicaciones.</p>
        }
      }

      @if (activeTab() === 'evidences') {
        @if (selectedDonation(); as donationDetail) {
          <section class="tab-panel detail-grid">
            <article class="detail-card wide-card">
              <div class="card-header">
                <div>
                  <p class="page-kicker">Estado documental agregado</p>
                  <h3>{{ documentarySignal().label }}</h3>
                  <p>{{ documentarySignal().description }}</p>
                </div>
                <span class="status-pill" [class]="documentarySignal().className">
                  {{ applicationsWithMinimumEvidenceCount() }} completas / {{ applicationsWithoutEvidenceCount() }} pendientes
                </span>
              </div>
              <p class="inline-note">
                El semaforo documental se calcula con documentos activos del catalogo transversal asociados a la evidencia de cada aplicacion.
              </p>
            </article>

            <article class="form-card">
              <div class="card-header">
                <div>
                  <h3>Cargar evidencia</h3>
                  <p>La evidencia se asocia a una aplicacion especifica.</p>
                </div>
              </div>

              <p class="inline-note">
                La evidencia registrada acredita presencia documental minima; no sustituye revision legal o contable.
              </p>

              @if (evidenceFormError()) {
                <p class="alert error">{{ evidenceFormError() }}</p>
              }

              @if (evidenceFormSuccess()) {
                <p class="alert success">{{ evidenceFormSuccess() }}</p>
              }

              @if (selectedApplication(); as selectedApplicationDetail) {
                <p class="inline-note">
                  Aplicacion seleccionada: {{ selectedApplicationDetail.beneficiaryName }}
                  · {{ selectedApplicationDetail.appliedAmount | number: '1.2-2' }}
                </p>
              } @else {
                <p class="empty-state">Selecciona una aplicacion para cargar evidencia.</p>
              }

              <form class="form-grid" [formGroup]="evidenceForm" (ngSubmit)="submitEvidence()">
                <label>
                  <span>Tipo de evidencia</span>
                  <select formControlName="evidenceTypeId">
                    <option [value]="0">Selecciona un tipo</option>
                    @for (type of evidenceTypes(); track type.id) {
                      <option [value]="type.id">{{ type.name }}</option>
                    }
                  </select>
                </label>

                <label class="full-width">
                  <span>Descripcion</span>
                  <textarea formControlName="description" rows="3" placeholder="Descripcion breve de la evidencia"></textarea>
                </label>

                <label class="full-width">
                  <span>Archivo</span>
                  <input type="file" accept=".pdf,.jpg,.jpeg,.png,.webp,.mp4,.mov,.avi" (change)="onEvidenceSelected($event)" />
                </label>

                @if (selectedEvidenceFileName()) {
                  <p class="inline-note full-width">Archivo seleccionado: {{ selectedEvidenceFileName() }}</p>
                }

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingEvidence() || !selectedApplication() || !canWrite()">Cargar evidencia</button>
                  <button type="button" class="ghost" (click)="resetEvidenceForm()">Limpiar</button>
                </div>
              </form>
            </article>

            <article class="list-card">
              <div class="card-header">
                <div>
                  <h3>Evidencias por aplicacion</h3>
                  <p>Archivos agrupados por la aplicacion que respaldan.</p>
                </div>
              </div>

              @if (donationDetail.applications.length === 0) {
                <p class="empty-state">Aun no hay aplicaciones que puedan recibir evidencia.</p>
              } @else {
                <div class="entity-list">
                  @for (application of sortedApplications(); track application.id) {
                    <article class="entity-row" [class.is-selected]="application.id === selectedApplicationId()">
                      <div class="row-top">
                        <div>
                          <h4>{{ application.beneficiaryName }}</h4>
                          <p class="meta">
                            {{ application.applicationDate }}
                            · {{ application.appliedAmount | number: '1.2-2' }}
                            · {{ applicationDocumentaryLabel(application) }}
                          </p>
                        </div>
                        <button type="button" class="ghost" (click)="selectApplicationAndOpenEvidence(application.id)">
                          Seleccionar
                        </button>
                      </div>

                      @if (application.evidences.length === 0) {
                        <p class="empty-state">Esta aplicacion no tiene evidencia registrada.</p>
                      } @else {
                        <div class="entity-list compact-list">
                          @for (evidence of application.evidences; track evidence.id) {
                            <article class="entity-row evidence-row">
                              <div class="row-top">
                                <div>
                                  <h4>{{ evidence.evidenceTypeName }}</h4>
                                  <p class="meta">{{ evidence.originalFileName }}</p>
                                </div>
                                <span class="status-pill neutral">
                                  {{ evidence.fileSizeBytes / 1024 | number: '1.0-0' }} KB
                                </span>
                              </div>

                              @if (evidence.description) {
                                <p class="meta">{{ evidence.description }}</p>
                              }

                              <div class="row-actions">
                                <span>{{ evidence.uploadedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</span>
                                <button type="button" class="ghost" (click)="downloadEvidence(evidence)">
                                  Descargar evidencia
                                </button>
                              </div>
                            </article>
                          }
                        </div>
                      }
                    </article>
                  }
                </div>
              }

              @if (selectedApplication(); as selectedApplicationDetail) {
                <app-related-documents-panel
                  moduleCode="DONATARIAS"
                  entityType="DONATION_APPLICATION"
                  [entityId]="selectedApplicationDetail.id"
                  [canRemediate]="canWrite()"
                  [remediationEvidenceTypes]="evidenceTypes()"
                  title="Documentos de la aplicacion seleccionada"
                  subtitle="Catalogo documental transversal para la misma evidencia operativa."
                  emptyMessage="No hay documentos transversales asociados a esta aplicacion."
                  (remediated)="reloadPage()">
                </app-related-documents-panel>
              }
            </article>
          </section>
        } @else {
          <p class="empty-state">Selecciona una donacion para consultar evidencias.</p>
        }
      }

      @if (activeTab() === 'report') {
        @if (selectedTransparencyReport(); as report) {
          <section class="tab-panel report-tab-panel">
            <article class="detail-card report-preview printable-report" aria-label="Reporte de transparencia de donacion">
              <div class="card-header report-header">
                <div>
                  <p class="page-kicker">Reporte de transparencia</p>
                  <h3>Reporte de transparencia de donación</h3>
                  <p class="report-subtitle">{{ report.donorEntityName }}</p>
                  <p>
                    Vista operativa de consulta con informacion financiera, documental y operativa registrada.
                  </p>
                </div>
                <div class="detail-badges report-actions">
                  <span class="status-pill neutral">
                    Corte {{ report.reportGeneratedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                  </span>
                  <button type="button" class="ghost print-hidden" (click)="printTransparencyReport()">
                    Imprimir reporte
                  </button>
                </div>
              </div>

              <p class="inline-note report-scope-note">{{ transparencyScopeNote }}</p>

              <div class="summary-grid">
                <article>
                  <h4>Donante</h4>
                  <p>{{ report.donorEntityName }}</p>
                </article>
                <article>
                  <h4>Fecha de donacion</h4>
                  <p>{{ report.donationDate }}</p>
                </article>
                <article>
                  <h4>Tipo de donacion</h4>
                  <p>{{ report.donationType }}</p>
                </article>
                <article>
                  <h4>Referencia</h4>
                  <p>{{ report.reference }}</p>
                </article>
                <article>
                  <h4>Fecha de corte / generación</h4>
                  <p>{{ report.reportGeneratedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</p>
                </article>
                <article>
                  <h4>Estado operativo</h4>
                  <p>{{ report.operationalStatus.donationStatusName }}</p>
                </article>
                <article>
                  <h4>Observaciones</h4>
                  <p>{{ report.notes || 'Sin observaciones registradas' }}</p>
                </article>
              </div>

              <div class="summary-grid">
                <article>
                  <h4>Total recibido / valor recibido</h4>
                  <p>{{ report.financialSummary.baseAmount | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Total aplicado</h4>
                  <p>{{ report.financialSummary.appliedAmountTotal | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Saldo pendiente</h4>
                  <p>{{ report.financialSummary.remainingAmount | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Porcentaje aplicado</h4>
                  <p>{{ report.financialSummary.appliedPercentage | number: '1.2-2' }}%</p>
                </article>
                <article>
                  <h4>Aplicaciones</h4>
                  <p>{{ report.financialSummary.applicationCount }}</p>
                </article>
                <article>
                  <h4>Estado financiero</h4>
                  <p>{{ report.operationalStatus.financialStatusLabel }}</p>
                </article>
              </div>

              <div class="signal-grid">
                <article class="signal-card {{ reportFinancialClass(report) }}">
                  <h4>Financiero</h4>
                  <strong>{{ report.operationalStatus.financialStatusLabel }}</strong>
                </article>
                <article class="signal-card {{ reportDocumentaryClass(report.documentarySummary.documentaryStatusCode) }}">
                  <h4>Documental</h4>
                  <strong>{{ report.documentarySummary.documentaryStatusLabel }}</strong>
                </article>
                <article class="signal-card {{ reportOperationalClass(report) }}">
                  <h4>Operativo</h4>
                  <strong>{{ report.operationalStatus.operationalStatusLabel }}</strong>
                </article>
              </div>

              <article class="readiness-card {{ reportReadinessClass(report.presentationReadiness.readinessCode) }}">
                <p class="page-kicker">Criterio operativo preliminar</p>
                <h4>
                  Readiness {{ report.presentationReadiness.readinessCode }} ·
                  {{ report.presentationReadiness.readinessLabel }}
                </h4>
                <p class="meta">{{ reportReadinessDescription(report.presentationReadiness.readinessCode) }}</p>
                @for (reason of report.presentationReadiness.reasons; track reason) {
                  <p class="meta">{{ reason }}</p>
                }
              </article>

              <div class="warning-list">
                <p class="inline-note">{{ transparencyScopeNote }}</p>
                @if (report.financialSummary.remainingAmount > 0) {
                  <p class="inline-note">Aún existe recurso pendiente de aplicar.</p>
                }
                @if (report.documentarySummary.applicationsMissingEvidence > 0) {
                  <p class="inline-note">Existen aplicaciones con evidencia pendiente.</p>
                }
                @if (report.operationalStatus.statusIsClosed && report.financialSummary.remainingAmount > 0) {
                  <p class="inline-note">La donación fue cerrada operativamente con saldo pendiente.</p>
                }
              </div>

              <div class="report-section">
                <h4>Estado documental</h4>
                <div class="summary-grid">
                  <article>
                    <h4>Aplicaciones completas</h4>
                    <p>{{ report.documentarySummary.applicationsWithEvidence }}</p>
                  </article>
                  <article>
                    <h4>Aplicaciones pendientes</h4>
                    <p>{{ report.documentarySummary.applicationsMissingEvidence }}</p>
                  </article>
                  <article>
                    <h4>Total de aplicaciones</h4>
                    <p>{{ report.documentarySummary.totalApplications }}</p>
                  </article>
                  <article>
                    <h4>Estado documental</h4>
                    <p>{{ report.documentarySummary.documentaryStatusLabel }}</p>
                  </article>
                </div>

                @if (report.documentarySummary.applicationsMissingEvidence > 0) {
                  <div class="entity-list compact-list">
                    @for (application of report.applications; track application.applicationId) {
                      @if (!reportApplicationHasMinimumEvidence(application)) {
                        <article class="entity-row">
                          <div class="row-top">
                            <div>
                              <h4>{{ application.beneficiaryName }}</h4>
                              <p class="meta">{{ reportApplicationMissingLabel(application) }}</p>
                            </div>
                            <span class="status-pill signal-warning">Evidencia pendiente</span>
                          </div>
                        </article>
                      }
                    }
                  </div>
                } @else {
                  <p class="meta">Todas las aplicaciones registran evidencia minima activa.</p>
                }
              </div>

              <div class="report-section">
                <h4>Distribucion financiera por aplicacion</h4>
                @if (report.applications.length === 0) {
                  <p class="empty-state">No hay aplicaciones registradas para reportar.</p>
                } @else {
                  <div class="application-table report-distribution-table">
                    <div class="application-row table-head">
                      <span>Beneficiario</span>
                      <span>Fecha</span>
                      <span>Responsable</span>
                      <span>Monto aplicado</span>
                      <span>% del total recibido</span>
                      <span>Estatus</span>
                      <span>Evidencia</span>
                      <span>Documentos activos</span>
                      <span>Faltantes documentales</span>
                    </div>
                    @for (application of report.applications; track application.applicationId) {
                      <div class="application-row">
                        <span>{{ application.beneficiaryName }}</span>
                        <span>{{ application.applicationDate }}</span>
                        <span>{{ application.responsibleName }}</span>
                        <span>{{ application.appliedAmount | number: '1.2-2' }}</span>
                        <span>{{ application.percentageOfDonation | number: '1.2-2' }}%</span>
                        <span class="status-pill" [class]="applicationStatusClass(application.statusCode)">
                          {{ application.statusName }}
                        </span>
                        <span class="status-pill" [class]="reportApplicationDocumentaryClass(application)">
                          {{ reportApplicationEvidenceSummary(application) }}
                        </span>
                        <span>{{ application.activeDocumentCount }}</span>
                        <span>{{ reportApplicationMissingLabel(application) }}</span>
                      </div>
                    }
                  </div>

                  <div class="entity-list">
                    @for (application of report.applications; track application.applicationId) {
                      <article class="entity-row">
                        <div class="row-top">
                          <div>
                            <h4>{{ application.beneficiaryName }}</h4>
                            <p class="meta">{{ application.verificationDetails || 'Sin detalle de comprobacion capturado.' }}</p>
                          </div>
                          <span class="status-pill" [class]="reportApplicationDocumentaryClass(application)">
                            {{ reportApplicationEvidenceSummary(application) }}
                          </span>
                        </div>

                        @if (application.closingDetails) {
                          <p class="meta">{{ application.closingDetails }}</p>
                        }

                        @if (!reportApplicationHasMinimumEvidence(application)) {
                          <p class="empty-state">Faltante documental: {{ reportApplicationMissingLabel(application) }}.</p>
                        }

                        @if (application.evidences.length === 0) {
                          <p class="empty-state">Sin evidencias registradas para esta aplicacion.</p>
                        } @else {
                          <div class="entity-list compact-list">
                            @for (evidence of application.evidences; track evidence.evidenceId) {
                              <article class="entity-row evidence-row">
                                <div class="row-top">
                                  <div>
                                    <h4>{{ evidence.evidenceTypeName }}</h4>
                                    <p class="meta">{{ evidence.originalFileName }}</p>
                                  </div>
                                  <span class="status-pill neutral">
                                    {{ evidence.uploadedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}
                                  </span>
                                </div>

                                @if (evidence.description) {
                                  <p class="meta">{{ evidence.description }}</p>
                                }

                                <div class="row-actions">
                                  <button type="button" class="ghost print-hidden" (click)="downloadTransparencyEvidence(evidence)">
                                    Descargar evidencia
                                  </button>
                                </div>
                              </article>
                            }
                          </div>
                        }
                      </article>
                    }
                  </div>
                }
              </div>

              <div class="report-section">
                <h4>Faltantes basicos</h4>
                @if (report.documentarySummary.applicationsMissingEvidence > 0) {
                  <p class="empty-state">
                    Aplicaciones con evidencia minima pendiente: {{ report.documentarySummary.applicationsMissingEvidence }}.
                  </p>
                } @else {
                  <p class="meta">No se detectan faltantes basicos de evidencia minima.</p>
                }
              </div>

              <div class="report-section">
                <h4>Notas de alcance</h4>
                <p class="inline-note">{{ transparencyScopeNote }}</p>
                @for (note of report.scopeNotes; track note) {
                  <p class="inline-note">{{ note }}</p>
                }
              </div>
            </article>
          </section>
        } @else {
          <article class="empty-card report-empty-state">
            <h3>Selecciona una donacion</h3>
            <p class="empty-state">El reporte imprimible solo esta disponible cuando hay una donacion seleccionada.</p>
            <div class="form-actions">
              <button
                type="button"
                class="ghost"
                disabled
                title="Selecciona una donacion antes de imprimir el reporte.">
                Imprimir reporte
              </button>
            </div>
          </article>
        }
      }

      @if (activeTab() === 'donations') {
        <div class="page-grid donations-tab">
        <aside class="sidebar">
          <article class="filter-card">
            <div class="card-header">
              <div>
                <h3>Filtro</h3>
                <p>Reduce la lista por estatus o por alertas activas.</p>
              </div>
            </div>

            <form class="form-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
              <label>
                <span>Estatus</span>
                <select formControlName="statusCode">
                  <option value="">Todos</option>
                  @for (status of donationStatuses(); track status.id) {
                    <option [value]="status.statusCode">{{ status.statusName }}</option>
                  }
                </select>
              </label>

              <label class="toggle">
                <input type="checkbox" formControlName="alertsOnly" />
                <span>Solo con alertas activas</span>
              </label>

              <div class="form-actions full-width">
                <button type="submit">Aplicar filtro</button>
                <button type="button" class="ghost" (click)="clearFilters()">Limpiar</button>
              </div>
            </form>
          </article>

          <article class="form-card">
            <div class="card-header">
              <div>
                <h3>Registrar donacion</h3>
                <p>Registro maestro con referencia y estatus inicial controlado.</p>
              </div>
            </div>

            @if (donationFormError()) {
              <p class="alert error">{{ donationFormError() }}</p>
            }

            @if (donationFormSuccess()) {
              <p class="alert success">{{ donationFormSuccess() }}</p>
            }

            <form class="form-grid" [formGroup]="donationForm" (ngSubmit)="submitDonation()">
              <label class="full-width">
                <span>Donante</span>
                <input type="text" formControlName="donorEntityName" placeholder="Empresa o entidad donante" />
              </label>

              <label>
                <span>Fecha</span>
                <input type="date" formControlName="donationDate" />
              </label>

              <label>
                <span>Tipo de donación</span>
                <input type="text" formControlName="donationType" placeholder="Efectivo, especie u otro" />
              </label>

              <label>
                <span>Total recibido / valor recibido</span>
                <input type="number" min="0.01" step="0.01" formControlName="baseAmount" />
              </label>

              <label>
                <span>Referencia</span>
                <input type="text" formControlName="reference" placeholder="Referencia interna o documental" />
              </label>

              <label>
                <span>Estatus inicial</span>
                <select formControlName="statusCatalogEntryId">
                  <option [value]="0">Selecciona un estatus</option>
                  @for (status of creatableDonationStatuses(); track status.id) {
                    <option [value]="status.id">{{ status.statusName }}</option>
                  }
                </select>
              </label>

              <label class="full-width">
                <span>Observaciones</span>
                <textarea formControlName="notes" rows="4" placeholder="Observaciones de la donación"></textarea>
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingDonation() || !canWrite()">Registrar donación</button>
                <button type="button" class="ghost" (click)="resetDonationForm()">Limpiar</button>
              </div>
            </form>
          </article>

          <article class="list-card">
            <div class="card-header">
              <div>
                <h3>Donaciones</h3>
                <p>Vista maestra con progreso, aplicaciones y evidencias.</p>
              </div>
              <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
            </div>

            @if (isBootstrapping()) {
              <p class="empty-state">Cargando modulo de Donatarias...</p>
            } @else if (donations().length === 0) {
              <p class="empty-state">No hay donaciones registradas con el filtro actual.</p>
            } @else {
              <div class="donation-list">
                @for (donation of donations(); track donation.id) {
                  <button
                    type="button"
                    class="donation-card"
                    [class.is-selected]="donation.id === selectedDonationId()"
                    (click)="selectDonation(donation.id)">
                    <div class="row-top">
                      <h4>{{ donation.donorEntityName }}</h4>
                      <span class="status-pill" [class]="donationStatusClass(donation.statusCode)">
                        {{ donation.statusName }}
                      </span>
                    </div>

                    <p class="meta">{{ donation.donationType }} · Ref {{ donation.reference }}</p>

                    <div class="donation-stats">
                      <span>Total recibido {{ donation.baseAmount | number: '1.2-2' }}</span>
                      <span>Aplicado {{ donation.appliedAmountTotal | number: '1.2-2' }}</span>
                      <span>Saldo pendiente {{ donation.remainingAmount | number: '1.2-2' }}</span>
                      <span>Porcentaje aplicado {{ donation.appliedPercentage | number: '1.2-2' }}%</span>
                    </div>

                    <span class="status-pill" [class]="alertStateClass(donation.alertState)">
                      {{ alertStateLabel(donation.alertState) }}
                    </span>
                  </button>
                }
              </div>
            }
          </article>

          <article class="list-card">
            <div class="card-header">
              <div>
                <h3>Alertas activas</h3>
                <p>Donaciones no aplicadas o con aplicación parcial.</p>
              </div>
            </div>

            @if (donationAlerts().length === 0) {
              <p class="empty-state">No hay alertas activas de Donatarias.</p>
            } @else {
              <div class="alert-list">
                @for (alert of donationAlerts(); track alert.donationId) {
                  <article class="alert-row">
                    <div class="row-top">
                      <h4>{{ alert.donorEntityName }}</h4>
                      <span class="status-pill" [class]="alertStateClass(alert.alertState)">
                        {{ alertStateLabel(alert.alertState) }}
                      </span>
                    </div>
                    <p class="meta">{{ alert.donationType }} · {{ alert.appliedPercentage | number: '1.2-2' }}%</p>
                    <p class="meta">
                      Total recibido {{ alert.baseAmount | number: '1.2-2' }}
                      · Aplicado {{ alert.appliedAmountTotal | number: '1.2-2' }}
                    </p>
                  </article>
                }
              </div>
            }
          </article>
        </aside>

        <div class="detail-column">
          @if (selectedDonation(); as donationDetail) {
            <article class="detail-card">
              <div class="detail-header">
                <div>
                  <p class="page-kicker">Donación seleccionada</p>
                  <h3>{{ donationDetail.donorEntityName }}</h3>
                  <p class="meta">
                    {{ donationDetail.donationType }} · {{ donationDetail.donationDate }}
                    · Ref {{ donationDetail.reference }}
                  </p>
                </div>
                <div class="detail-badges">
                  <span class="status-pill" [class]="donationStatusClass(donationDetail.statusCode)">
                    {{ donationDetail.statusName }}
                  </span>
                  <span class="status-pill" [class]="alertStateClass(donationDetail.alertState)">
                    {{ alertStateLabel(donationDetail.alertState) }}
                  </span>
                  <button
                    type="button"
                    class="ghost"
                    (click)="openClosePanel()"
                    [disabled]="!canAdminister() || donationDetail.statusIsClosed"
                    [attr.title]="!canAdminister()
                      ? 'Solo ADMIN puede registrar cierre formal.'
                      : donationDetail.statusIsClosed
                        ? 'La donación ya se encuentra en estado terminal.'
                        : 'Registrar cierre formal.'">
                    {{ donationDetail.statusIsClosed ? 'Ya terminal' : 'Cerrar formalmente' }}
                  </button>
                </div>
              </div>

              @if (donationDetail.notes) {
                <p class="detail-notes">{{ donationDetail.notes }}</p>
              }

              <div class="summary-grid">
                <article>
                  <h4>Total recibido / valor recibido</h4>
                  <p>{{ donationDetail.baseAmount | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Total aplicado</h4>
                  <p>{{ donationDetail.appliedAmountTotal | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Saldo pendiente</h4>
                  <p>{{ donationDetail.remainingAmount | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Porcentaje aplicado</h4>
                  <p>{{ donationDetail.appliedPercentage | number: '1.2-2' }}%</p>
                </article>
                <article>
                  <h4>Aplicaciones</h4>
                  <p>{{ donationDetail.applications.length }}</p>
                </article>
                <article>
                  <h4>Evidencias</h4>
                  <p>{{ selectedDonationEvidenceCount() }}</p>
                </article>
              </div>
            </article>

            <div class="detail-grid">
              <article class="form-card">
                <div class="card-header">
                  <div>
                    <h3>Registrar aplicacion</h3>
                    <p>Beneficiario, responsable, monto aplicado y detalle de comprobación.</p>
                  </div>
                </div>

                @if (applicationFormError()) {
                  <p class="alert error">{{ applicationFormError() }}</p>
                }

                @if (applicationFormSuccess()) {
                  <p class="alert success">{{ applicationFormSuccess() }}</p>
                }

                <form class="form-grid" [formGroup]="applicationForm" (ngSubmit)="submitApplication()">
                  <label>
                    <span>Beneficiario</span>
                    <input type="text" formControlName="beneficiaryName" placeholder="Beneficiario" />
                  </label>

                  <label>
                    <span>Fecha de aplicación</span>
                    <input type="date" formControlName="applicationDate" />
                  </label>

                  <label>
                    <span>Contacto responsable</span>
                    <select formControlName="responsibleContactId" (change)="syncResponsibleFromContact()">
                      <option value="">Sin vincular</option>
                      @for (contact of contacts(); track contact.id) {
                        <option [value]="contact.id">{{ contact.name }}</option>
                      }
                    </select>
                  </label>

                  <label>
                    <span>Responsable o creador</span>
                    <input type="text" formControlName="responsibleName" placeholder="Nombre del responsable" />
                  </label>

                  <label>
                    <span>Monto aplicado</span>
                    <input type="number" min="0.01" step="0.01" formControlName="appliedAmount" />
                  </label>

                  <label>
                    <span>Estatus de aplicación</span>
                    <select formControlName="statusCatalogEntryId">
                      <option [value]="0">Selecciona un estatus</option>
                      @for (status of applicationStatuses(); track status.id) {
                        <option [value]="status.id">{{ status.statusName }}</option>
                      }
                    </select>
                  </label>

                  <label class="full-width">
                    <span>Detalle de comprobacion</span>
                    <textarea formControlName="verificationDetails" rows="4" placeholder="Detalle de comprobacion"></textarea>
                  </label>

                  <label class="full-width">
                    <span>Nota de cierre de la aplicacion</span>
                    <textarea formControlName="closingDetails" rows="3" placeholder="Si aplica"></textarea>
                  </label>

                  <div class="form-actions full-width">
                    <button type="submit" [disabled]="isSubmittingApplication() || !canWrite()">Registrar aplicación</button>
                    <button type="button" class="ghost" (click)="resetApplicationForm()">Limpiar</button>
                  </div>
                </form>
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Aplicaciones de la donación</h3>
                    <p>Cada aplicación conserva su evidencia propia.</p>
                  </div>
                </div>

                @if (donationDetail.applications.length === 0) {
                  <p class="empty-state">Aun no hay aplicaciones registradas.</p>
                } @else {
                  <div class="entity-list">
                    @for (application of donationDetail.applications; track application.id) {
                      <button
                        type="button"
                        class="entity-button"
                        [class.is-selected]="application.id === selectedApplicationId()"
                        (click)="selectApplication(application.id)">
                        <div class="row-top">
                          <div>
                            <h4>{{ application.beneficiaryName }}</h4>
                            <p class="meta">{{ application.applicationDate }} · {{ application.responsibleName }}</p>
                          </div>
                          <span class="status-pill" [class]="applicationStatusClass(application.statusCode)">
                            {{ application.statusName }}
                          </span>
                        </div>

                        <div class="donation-stats">
                          <span>Monto {{ application.appliedAmount | number: '1.2-2' }}</span>
                          <span>{{ applicationEvidenceSummary(application) }}</span>
                        </div>

                        @if (application.verificationDetails) {
                          <p class="meta">{{ application.verificationDetails }}</p>
                        }
                      </button>
                    }
                  </div>
                }
              </article>
            </div>

            <div class="detail-grid">
              <article class="form-card">
                <div class="card-header">
                  <div>
                    <h3>Cargar evidencia</h3>
                    <p>La evidencia se asocia a una aplicación, no a la donación maestra.</p>
                  </div>
                </div>

                @if (evidenceFormError()) {
                  <p class="alert error">{{ evidenceFormError() }}</p>
                }

                @if (evidenceFormSuccess()) {
                  <p class="alert success">{{ evidenceFormSuccess() }}</p>
                }

                @if (selectedApplication(); as selectedApplicationDetail) {
                  <p class="inline-note">
                    Aplicación seleccionada: {{ selectedApplicationDetail.beneficiaryName }}
                    · {{ selectedApplicationDetail.appliedAmount | number: '1.2-2' }}
                  </p>
                } @else {
                  <p class="empty-state">Selecciona una aplicación para cargar evidencia.</p>
                }

                <form class="form-grid" [formGroup]="evidenceForm" (ngSubmit)="submitEvidence()">
                  <label>
                    <span>Tipo de evidencia</span>
                    <select formControlName="evidenceTypeId">
                      <option [value]="0">Selecciona un tipo</option>
                      @for (type of evidenceTypes(); track type.id) {
                        <option [value]="type.id">{{ type.name }}</option>
                      }
                    </select>
                  </label>

                  <label class="full-width">
                    <span>Descripción</span>
                    <textarea formControlName="description" rows="3" placeholder="Descripción breve de la evidencia"></textarea>
                  </label>

                  <label class="full-width">
                    <span>Archivo</span>
                    <input type="file" accept=".pdf,.jpg,.jpeg,.png,.webp,.mp4,.mov,.avi" (change)="onEvidenceSelected($event)" />
                  </label>

                  @if (selectedEvidenceFileName()) {
                    <p class="inline-note full-width">Archivo seleccionado: {{ selectedEvidenceFileName() }}</p>
                  }

                  <div class="form-actions full-width">
                    <button type="submit" [disabled]="isSubmittingEvidence() || !selectedApplication() || !canWrite()">Cargar evidencia</button>
                    <button type="button" class="ghost" (click)="resetEvidenceForm()">Limpiar</button>
                  </div>
                </form>
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Evidencias</h3>
                    <p>Metadatos descargables por aplicación.</p>
                  </div>
                </div>

                @if (selectedApplication(); as selectedApplicationDetail) {
                  @if (selectedApplicationDetail.evidences.length === 0) {
                    <p class="empty-state">La aplicación seleccionada aun no tiene evidencias.</p>
                  } @else {
                    <div class="entity-list">
                      @for (evidence of selectedApplicationDetail.evidences; track evidence.id) {
                        <article class="entity-row">
                          <div class="row-top">
                            <div>
                              <h4>{{ evidence.evidenceTypeName }}</h4>
                              <p class="meta">{{ evidence.originalFileName }}</p>
                            </div>
                            <span class="status-pill neutral">
                              {{ evidence.fileSizeBytes / 1024 | number: '1.0-0' }} KB
                            </span>
                          </div>

                          @if (evidence.description) {
                            <p class="meta">{{ evidence.description }}</p>
                          }

                          <div class="row-actions">
                            <span>{{ evidence.uploadedUtc | date: 'yyyy-MM-dd HH:mm':'UTC' }}</span>
                            <button
                              type="button"
                              class="ghost"
                              (click)="downloadEvidence(evidence)">
                              Descargar evidencia
                            </button>
                          </div>
                        </article>
                      }
                    </div>
                  }

                  <app-related-documents-panel
                    moduleCode="DONATARIAS"
                    entityType="DONATION_APPLICATION"
                    [entityId]="selectedApplicationDetail.id"
                    [canRemediate]="canWrite()"
                    [remediationEvidenceTypes]="evidenceTypes()"
                    title="Documentos de la aplicación"
                    subtitle="Evidencias vistas desde el catálogo documental transversal."
                    emptyMessage="No hay documentos transversales asociados a esta aplicación."
                    (remediated)="reloadPage()">
                  </app-related-documents-panel>
                } @else {
                  <p class="empty-state">Selecciona una aplicación para consultar sus evidencias.</p>
                }
              </article>
            </div>
          } @else {
            <article class="empty-card">
              <h3>Selecciona una donación</h3>
              <p>
                Cuando exista al menos una donación, su detalle quedará disponible aquí para registrar
                aplicaciones, consultar el porcentaje aplicado y cargar evidencias.
              </p>
            </article>
          }
        </div>
        </div>
      }
    </section>
  `,
  styles: [
    `
      .page-shell,
      .sidebar,
      .detail-column,
      .detail-grid,
      .entity-list,
      .donation-list,
      .alert-list {
        display: grid;
        gap: 1.25rem;
      }

      .page-grid {
        display: grid;
        grid-template-columns: minmax(22rem, 25rem) minmax(0, 1fr);
        gap: 1.25rem;
        align-items: start;
      }

      .page-grid.donations-tab {
        grid-template-columns: minmax(0, 1fr);
      }

      .page-grid.donations-tab .detail-column {
        display: none;
      }

      .detail-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .tab-panel,
      .signal-grid,
      .application-table,
      .report-section,
      .warning-list,
      .list-summary-card,
      .readiness-card,
      .compact-list {
        display: grid;
        gap: 1rem;
      }

      .tab-nav {
        display: flex;
        flex-wrap: wrap;
        gap: 0.6rem;
        padding: 0.35rem;
        border-radius: 1rem;
        background: rgba(18, 63, 59, 0.06);
      }

      .tab-nav button {
        border-radius: 999px;
        background: transparent;
        color: #17423d;
      }

      .tab-nav button.is-active {
        background: #123f3b;
        color: #f6f6f2;
      }

      .transparency-header {
        display: grid;
        gap: 1rem;
      }

      .header-metrics {
        grid-template-columns: repeat(4, minmax(0, 1fr));
      }

      .kpi-grid {
        grid-template-columns: repeat(4, minmax(0, 1fr));
      }

      .list-kpi-grid {
        grid-template-columns: repeat(4, minmax(0, 1fr));
      }

      .signal-grid {
        grid-template-columns: repeat(3, minmax(0, 1fr));
      }

      .signal-card {
        padding: 1rem;
        border-radius: 1rem;
        border: 1px solid rgba(29, 45, 42, 0.08);
        background: #fbfbf8;
      }

      .signal-card h4,
      .signal-card strong {
        display: block;
      }

      .signal-card strong {
        margin-top: 0.35rem;
        color: #203734;
      }

      .signal-card p {
        margin-top: 0.45rem;
        color: #4d615c;
        line-height: 1.5;
      }

      .wide-card {
        grid-column: 1 / -1;
      }

      .compact-card {
        padding: 1rem;
      }

      .balance-callout {
        margin-top: 1rem;
        padding: 0.85rem 1rem;
        border-radius: 1rem;
        background: rgba(22, 101, 52, 0.1);
        color: #166534;
        font-weight: 800;
      }

      .balance-callout.warning {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .sort-toolbar {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
        align-items: center;
      }

      .sort-toolbar span {
        color: #4d615c;
        font-size: 0.9rem;
        font-weight: 700;
      }

      .sort-toolbar button.is-active {
        background: #123f3b;
        color: #f6f6f2;
      }

      .application-table {
        overflow-x: auto;
      }

      .application-row {
        display: grid;
        grid-template-columns:
          minmax(9rem, 1.3fr)
          minmax(7rem, 0.8fr)
          minmax(9rem, 1fr)
          minmax(8rem, 0.8fr)
          minmax(7rem, 0.7fr)
          minmax(8rem, 0.8fr)
          minmax(9rem, 0.9fr)
          minmax(8rem, 0.8fr)
          minmax(6rem, 0.6fr)
          minmax(10rem, 1fr)
          minmax(12rem, 1.4fr);
        gap: 0.6rem;
        align-items: center;
        min-width: 92rem;
        padding: 0.85rem;
        border-radius: 0.85rem;
        background: #f6f5ef;
        color: #203734;
        text-align: left;
      }

      .application-row.table-head {
        background: rgba(18, 63, 59, 0.08);
        color: #29403b;
        font-weight: 800;
        cursor: default;
      }

      .report-distribution-table .application-row {
        grid-template-columns:
          minmax(10rem, 1.3fr)
          minmax(7rem, 0.7fr)
          minmax(9rem, 1fr)
          minmax(8rem, 0.8fr)
          minmax(8rem, 0.8fr)
          minmax(8rem, 0.8fr)
          minmax(8rem, 0.8fr)
          minmax(8rem, 0.8fr)
          minmax(12rem, 1.2fr);
        min-width: 84rem;
      }

      button.application-row {
        width: 100%;
        border: 0;
      }

      button.application-row.is-selected,
      .entity-row.is-selected {
        outline: 2px solid rgba(15, 118, 110, 0.35);
      }

      .report-preview {
        display: grid;
        gap: 1.2rem;
      }

      .report-subtitle {
        margin-top: 0.35rem;
        color: #203734;
        font-size: 1.15rem;
        font-weight: 800;
      }

      .report-actions {
        justify-content: flex-end;
      }

      .report-scope-note {
        border-left: 4px solid #0f766e;
        padding: 0.85rem 1rem;
        border-radius: 0.85rem;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-weight: 700;
      }

      .readiness-card {
        padding: 1rem;
        border-radius: 1rem;
      }

      .hero-card,
      .filter-card,
      .form-card,
      .list-card,
      .detail-card,
      .empty-card {
        padding: 1.5rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .page-kicker {
        margin: 0 0 0.5rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      h2,
      h3,
      h4,
      p,
      dd {
        margin: 0;
      }

      .hero-card p:last-child,
      .meta,
      .detail-notes,
      .empty-state,
      .inline-note {
        margin-top: 0.75rem;
        line-height: 1.6;
        color: #4d615c;
      }

      .card-header,
      .row-top,
      .detail-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .card-header {
        margin-bottom: 1rem;
      }

      .detail-badges,
      .donation-stats,
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
      }

      .summary-grid {
        display: grid;
        grid-template-columns: repeat(3, minmax(0, 1fr));
        gap: 0.75rem;
        margin-top: 1rem;
      }

      .summary-grid article {
        padding: 0.9rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .summary-grid h4 {
        font-size: 0.82rem;
        letter-spacing: 0.06em;
        text-transform: uppercase;
        color: #5b6b68;
      }

      .summary-grid p {
        margin-top: 0.4rem;
        font-size: 1.05rem;
        font-weight: 700;
        color: #203734;
      }

      .form-grid {
        display: grid;
        grid-template-columns: repeat(2, minmax(0, 1fr));
        gap: 0.9rem;
      }

      label {
        display: grid;
        gap: 0.4rem;
        font-size: 0.92rem;
        font-weight: 600;
        color: #29403b;
      }

      input,
      select,
      textarea {
        width: 100%;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        border: 1px solid rgba(29, 45, 42, 0.14);
        background: #fbfbf8;
        color: #1d2d2a;
        font: inherit;
      }

      textarea {
        resize: vertical;
      }

      .toggle {
        grid-template-columns: auto 1fr;
        align-items: center;
        gap: 0.7rem;
        padding: 0.8rem 0.9rem;
        border-radius: 0.9rem;
        background: #f6f5ef;
      }

      .toggle input {
        width: auto;
        margin: 0;
      }

      .full-width {
        grid-column: 1 / -1;
      }

      .form-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.75rem;
      }

      button,
      .donation-card,
      .entity-button {
        border: none;
        border-radius: 0.9rem;
        padding: 0.8rem 1rem;
        font: inherit;
      }

      button {
        cursor: pointer;
        font-weight: 700;
        background: #123f3b;
        color: #f6f6f2;
      }

      button.ghost {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      button:disabled {
        opacity: 0.75;
        cursor: wait;
      }

      .donation-card,
      .entity-row,
      .entity-button,
      .alert-row {
        display: grid;
        gap: 0.7rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .donation-card,
      .entity-button {
        text-align: left;
        cursor: pointer;
      }

      .donation-card.is-selected,
      .entity-button.is-selected {
        outline: 2px solid rgba(15, 118, 110, 0.35);
      }

      .donation-stats span,
      .row-actions span,
      .row-actions a {
        padding: 0.45rem 0.65rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.82rem;
        text-decoration: none;
      }

      .status-pill {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        padding: 0.5rem 0.72rem;
        border-radius: 999px;
        font-size: 0.82rem;
        font-weight: 700;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      .status-pill.donation-open,
      .status-pill.application-open,
      .status-pill.alert-partial {
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.donation-closed,
      .status-pill.application-closed {
        background: rgba(70, 85, 82, 0.14);
        color: #41514e;
      }

      .status-pill.alert-pending {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .status-pill.neutral {
        background: rgba(79, 70, 229, 0.1);
        color: #4338ca;
      }

      .status-pill.signal-warning,
      .signal-card.signal-warning,
      .readiness-card.signal-warning {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .status-pill.signal-partial,
      .signal-card.signal-partial,
      .readiness-card.signal-partial {
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.signal-complete,
      .signal-card.signal-complete,
      .readiness-card.signal-complete {
        background: rgba(22, 101, 52, 0.1);
        color: #166534;
      }

      .status-pill.signal-open,
      .signal-card.signal-open {
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
      }

      .status-pill.signal-closed,
      .status-pill.signal-neutral,
      .signal-card.signal-closed,
      .signal-card.signal-neutral {
        background: rgba(70, 85, 82, 0.14);
        color: #41514e;
      }

      .alert {
        padding: 0.9rem 1rem;
        border-radius: 0.9rem;
        font-weight: 600;
      }

      .alert.error {
        background: rgba(190, 24, 93, 0.1);
        color: #9d174d;
      }

      .alert.success {
        background: rgba(15, 118, 110, 0.1);
        color: #0f766e;
      }

      @media (max-width: 1080px) {
        .page-grid,
        .detail-grid,
        .summary-grid,
        .header-metrics,
        .kpi-grid,
        .list-kpi-grid,
        .signal-grid,
        .form-grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class DonatariasPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly document = inject(DOCUMENT);
  private readonly authService = inject(AuthService);
  private readonly donationsService = inject(DonationsService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);
  private readonly printBodyClassName = 'donatarias-print-active';
  private readonly initialDonationId = this.route.snapshot.queryParamMap.get('donationId');
  private readonly initialApplicationId = this.route.snapshot.queryParamMap.get('applicationId');
  private hasAppliedInitialQuerySelection = false;

  protected readonly canWrite = this.authService.canWriteDonations;
  protected readonly canAdminister = this.authService.canAdministerFormalClose;
  protected readonly isBootstrapping = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);
  protected readonly activeTab = signal<DonatariasTab>(this.initialApplicationId ? 'evidences' : 'summary');

  protected readonly donationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly applicationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly evidenceTypes = signal<CatalogItem[]>([]);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly donations = signal<DonationSummary[]>([]);
  protected readonly donationAlerts = signal<DonationAlert[]>([]);
  protected readonly selectedDonationId = signal<string | null>(null);
  protected readonly selectedDonation = signal<DonationDetail | null>(null);
  protected readonly selectedDocumentaryStatus = signal<DonationDocumentaryStatus | null>(null);
  protected readonly selectedTransparencyReport = signal<DonationTransparencyReport | null>(null);
  protected readonly selectedApplicationId = signal<string | null>(null);
  protected readonly applicationSort = signal<ApplicationSortMode>('dateAsc');

  protected readonly isSubmittingDonation = signal(false);
  protected readonly isSubmittingApplication = signal(false);
  protected readonly isSubmittingEvidence = signal(false);
  protected readonly isClosingDonation = signal(false);
  protected readonly isClosePanelOpen = signal(false);

  protected readonly donationFormError = signal<string | null>(null);
  protected readonly donationFormSuccess = signal<string | null>(null);
  protected readonly applicationFormError = signal<string | null>(null);
  protected readonly applicationFormSuccess = signal<string | null>(null);
  protected readonly evidenceFormError = signal<string | null>(null);
  protected readonly evidenceFormSuccess = signal<string | null>(null);
  protected readonly selectedEvidenceFile = signal<File | null>(null);
  protected readonly selectedEvidenceFileName = signal<string | null>(null);
  protected readonly closeReason = signal('');
  protected readonly transparencyScopeNote =
    'Este reporte es una vista operativa de transparencia basada en la información registrada en el sistema. La evidencia mínima registrada no sustituye revisión legal, fiscal o contable.';

  protected readonly creatableDonationStatuses = computed(() =>
    this.donationStatuses().filter((status) => status.statusCode === 'NOT_APPLIED' || status.statusCode === 'CLOSED'));

  protected readonly selectedApplication = computed<DonationApplication | null>(() => {
    const selectedApplicationId = this.selectedApplicationId();
    const selectedDonation = this.selectedDonation();

    if (!selectedDonation || !selectedApplicationId) {
      return null;
    }

    return selectedDonation.applications.find((application) => application.id === selectedApplicationId) ?? null;
  });

  protected readonly sortedApplications = computed<DonationApplication[]>(() => {
    const applications = [...(this.selectedDonation()?.applications ?? [])];
    const sortMode = this.applicationSort();

    return applications.sort((left, right) => {
      switch (sortMode) {
        case 'dateDesc':
          return this.compareApplicationsByDate(right, left);
        case 'amountDesc':
          return right.appliedAmount - left.appliedAmount || this.compareApplicationsByDate(left, right);
        case 'evidencePending':
          return this.compareApplicationsByEvidence(left, right) || this.compareApplicationsByDate(left, right);
        case 'dateAsc':
        default:
          return this.compareApplicationsByDate(left, right);
      }
    });
  });

  protected readonly visibleDonationMetrics = computed<VisibleDonationMetrics>(() => {
    const donations = this.donations();
    const totalReceived = donations.reduce((total, donation) => total + donation.baseAmount, 0);
    const totalApplied = donations.reduce((total, donation) => total + donation.appliedAmountTotal, 0);
    const pendingBalance = donations.reduce((total, donation) => total + donation.remainingAmount, 0);

    return {
      donationCount: donations.length,
      totalReceived,
      totalApplied,
      pendingBalance,
      appliedPercentage: totalReceived <= 0 ? 0 : (totalApplied / totalReceived) * 100,
      openCount: donations.filter((donation) => !donation.statusIsClosed).length,
      closedCount: donations.filter((donation) => donation.statusIsClosed).length,
      pendingBalanceCount: donations.filter((donation) => donation.remainingAmount > 0).length
    };
  });

  protected readonly selectedDonationEvidenceCount = computed(() => {
    const documentaryStatus = this.selectedDocumentaryStatus();
    if (documentaryStatus) {
      return documentaryStatus.applicationStatuses.reduce(
        (total, applicationStatus) => total + applicationStatus.evidenceCount,
        0);
    }

    return this.selectedDonation()?.applications.reduce((total, application) => total + application.evidenceCount, 0) ?? 0;
  });

  protected readonly selectedDonationActiveDocumentCount = computed(() =>
    this.selectedDocumentaryStatus()?.applicationStatuses.reduce(
      (total, applicationStatus) => total + applicationStatus.activeDocumentCount,
      0) ?? this.selectedDonationEvidenceCount());

  protected readonly applicationsWithMinimumEvidenceCount = computed(() =>
    this.selectedDocumentaryStatus()?.applicationsWithEvidence
      ?? this.selectedDonation()?.applications.filter((application) => application.evidenceCount > 0).length
      ?? 0);

  protected readonly applicationsWithoutEvidenceCount = computed(() =>
    this.selectedDocumentaryStatus()?.applicationsMissingEvidence
      ?? this.selectedDonation()?.applications.filter((application) => application.evidenceCount <= 0).length
      ?? 0);

  protected readonly presentationReadiness = computed<PresentationReadiness>(() => {
    const donation = this.selectedDonation();
    const hasApplications = (donation?.applications.length ?? 0) > 0;
    const hasEvidence = this.selectedDonationActiveDocumentCount() > 0;
    const hasPendingBalance = (donation?.remainingAmount ?? 0) > 0;
    const hasPendingEvidence = this.applicationsWithoutEvidenceCount() > 0;

    if (donation && hasApplications && !hasPendingBalance && !hasPendingEvidence) {
      return {
        label: 'Si',
        className: 'signal-complete',
        description: 'El recurso registrado esta aplicado financieramente y cada aplicacion tiene evidencia minima.'
      };
    }

    if (hasApplications && hasEvidence) {
      return {
        label: 'Parcial',
        className: 'signal-partial',
        description: 'Existe avance comprobable, pero hay saldo pendiente o faltantes basicos por aplicacion.'
      };
    }

    return {
      label: 'No',
      className: 'signal-warning',
      description: 'Faltan aplicaciones registradas o evidencia minima para presentar el uso del recurso.'
    };
  });

  protected readonly selectedDonationAlert = computed(() => {
    const donationId = this.selectedDonationId();
    return donationId
      ? this.donationAlerts().find((alert) => alert.donationId === donationId) ?? null
      : null;
  });

  protected readonly financialSignal = computed(() => {
    const donation = this.selectedDonation();
    if (!donation || donation.appliedAmountTotal <= 0) {
      return {
        label: 'Sin aplicar',
        className: 'signal-warning',
        description: 'Aun no hay recurso aplicado financieramente.'
      };
    }

    if (donation.appliedAmountTotal < donation.baseAmount) {
      return {
        label: 'Parcialmente aplicada',
        className: 'signal-partial',
        description: 'Existe recurso aplicado financieramente y saldo pendiente.'
      };
    }

    return {
      label: 'Aplicada financieramente',
      className: 'signal-complete',
      description: 'El recurso registrado ya fue aplicado financieramente; esto es independiente del cierre operativo.'
    };
  });

  protected readonly documentarySignal = computed(() => {
    const documentaryStatus = this.selectedDocumentaryStatus();
    if (documentaryStatus?.documentaryStatusCode === 'NO_APPLICATIONS') {
      return {
        label: 'Sin aplicaciones que comprobar',
        className: 'signal-neutral',
        description: 'Primero registra una aplicacion para asociar evidencia.'
      };
    }

    if (documentaryStatus?.documentaryStatusCode === 'EVIDENCE_PENDING') {
      return {
        label: 'Evidencia pendiente',
        className: 'signal-warning',
        description: `${documentaryStatus.applicationsMissingEvidence} aplicacion(es) tienen evidencia minima pendiente.`
      };
    }

    if (documentaryStatus?.documentaryStatusCode === 'MINIMUM_EVIDENCE_COMPLETE') {
      return {
        label: 'Comprobacion minima completa',
        className: 'signal-complete',
        description: 'Cada aplicacion tiene evidencia minima activa registrada.'
      };
    }

    const donation = this.selectedDonation();
    if (!donation || donation.applications.length === 0) {
      return {
        label: 'Sin aplicaciones que comprobar',
        className: 'signal-neutral',
        description: 'Primero registra una aplicacion para asociar evidencia.'
      };
    }

    if (this.applicationsWithoutEvidenceCount() > 0) {
      return {
        label: 'Evidencia pendiente',
        className: 'signal-warning',
        description: 'Hay aplicaciones sin evidencia minima registrada.'
      };
    }

    return {
      label: 'Comprobacion minima completa',
      className: 'signal-complete',
      description: 'Cada aplicacion tiene al menos una evidencia registrada.'
    };
  });

  protected readonly operativeSignal = computed(() => {
    const donation = this.selectedDonation();
    if (donation?.statusIsClosed) {
      return {
        label: 'Cerrada',
        className: 'signal-closed',
        description: 'La donacion ya tiene estado operativo terminal; no implica aplicacion financiera al 100%.'
      };
    }

    return {
      label: 'Abierta',
      className: 'signal-open',
      description: 'La donacion sigue disponible para operacion.'
    };
  });

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    statusCode: [''],
    alertsOnly: [false]
  });

  protected readonly donationForm = this.formBuilder.nonNullable.group({
    donorEntityName: ['', [Validators.required, Validators.maxLength(200)]],
    donationDate: [this.todayIso(), Validators.required],
    donationType: ['', [Validators.required, Validators.maxLength(120)]],
    baseAmount: [0, [Validators.required, Validators.min(0.01)]],
    reference: ['', [Validators.required, Validators.maxLength(120)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    notes: ['']
  });

  protected readonly applicationForm = this.formBuilder.nonNullable.group({
    beneficiaryName: ['', [Validators.required, Validators.maxLength(200)]],
    responsibleContactId: [''],
    responsibleName: ['', [Validators.required, Validators.maxLength(200)]],
    applicationDate: [this.todayIso(), Validators.required],
    appliedAmount: [0, [Validators.required, Validators.min(0.01)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    verificationDetails: [''],
    closingDetails: ['']
  });

  protected readonly evidenceForm = this.formBuilder.nonNullable.group({
    evidenceTypeId: [0, [Validators.required, Validators.min(1)]],
    description: ['']
  });

  constructor() {
    void this.bootstrap();
  }

  protected setActiveTab(tab: DonatariasTab): void {
    this.activeTab.set(tab);
  }

  protected printTransparencyReport(): void {
    if (!this.selectedTransparencyReport()) {
      this.pageError.set('Selecciona una donación antes de imprimir el reporte de transparencia.');
      return;
    }

    this.pageError.set(null);
    this.activeTab.set('report');
    this.document.body.classList.add(this.printBodyClassName);

    window.addEventListener(
      'afterprint',
      () => this.document.body.classList.remove(this.printBodyClassName),
      { once: true });
    window.print();
  }

  protected async applyFilters(): Promise<void> {
    await this.reloadDonations();
  }

  protected async clearFilters(): Promise<void> {
    this.filtersForm.setValue({
      statusCode: '',
      alertsOnly: false
    });

    await this.reloadDonations();
  }

  protected async reloadPage(): Promise<void> {
    this.pageSuccess.set(null);
    await this.bootstrap();
  }

  protected async selectDonation(donationId: string): Promise<void> {
    this.pageSuccess.set(null);
    this.selectedDonationId.set(donationId);
    this.selectedApplicationId.set(null);
    this.selectedDocumentaryStatus.set(null);
    this.selectedTransparencyReport.set(null);
    await this.loadDonationDetail(donationId);
  }

  protected openClosePanel(): void {
    const donation = this.selectedDonation();
    if (!donation) {
      return;
    }

    if (donation.statusIsClosed) {
      this.pageError.set('La donación ya se encuentra en estado terminal y no admite un nuevo cierre formal.');
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.closeReason.set('');
    this.isClosePanelOpen.set(true);
  }

  protected cancelClosePanel(): void {
    this.isClosePanelOpen.set(false);
    this.closeReason.set('');
  }

  protected setCloseReason(event: Event): void {
    const input = event.target as HTMLTextAreaElement;
    this.closeReason.set(input.value);
  }

  protected async confirmCloseSelectedDonation(): Promise<void> {
    const donation = this.selectedDonation();
    if (!donation) {
      return;
    }

    if (donation.statusIsClosed) {
      this.pageError.set('La donación ya se encuentra en estado terminal y no admite un nuevo cierre formal.');
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.isClosingDonation.set(true);

    try {
      await firstValueFrom(this.donationsService.closeDonation(donation.id, { reason: this.normalizeOptional(this.closeReason()) }));
      await this.reloadDonations(donation.id);
      await this.loadDonationDetail(donation.id, this.selectedApplicationId() ?? undefined);
      await this.reloadAlerts();
      this.isClosePanelOpen.set(false);
      this.closeReason.set('');
      this.pageSuccess.set('Cierre formal registrado en bitácora.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible registrar el cierre formal de la donación.'));
    } finally {
      this.isClosingDonation.set(false);
    }
  }

  protected selectApplication(applicationId: string): void {
    this.selectedApplicationId.set(applicationId);
    this.evidenceFormError.set(null);
    this.evidenceFormSuccess.set(null);
  }

  protected setApplicationSort(sortMode: ApplicationSortMode): void {
    this.applicationSort.set(sortMode);
  }

  protected selectApplicationAndOpenEvidence(applicationId: string): void {
    this.selectApplication(applicationId);
    this.activeTab.set('evidences');
  }

  protected selectedDonationBalanceMessage(): string {
    const donation = this.selectedDonation();

    if (!donation) {
      return '';
    }

    if (donation.statusIsClosed && donation.remainingAmount > 0) {
      return 'La donacion fue cerrada operativamente con saldo pendiente.';
    }

    if (donation.remainingAmount > 0) {
      return 'Aun existe recurso pendiente de aplicar.';
    }

    return 'El recurso registrado ya fue aplicado financieramente.';
  }

  protected applicationSharePercentage(application: DonationApplication, donation: DonationDetail): number {
    return donation.baseAmount <= 0
      ? 0
      : (application.appliedAmount / donation.baseAmount) * 100;
  }

  protected applicationRemainingAfter(application: DonationApplication, donation: DonationDetail): number {
    const sortedApplications = this.sortedApplications();
    const selectedIndex = sortedApplications.findIndex((item) => item.id === application.id);
    const appliedThroughApplication = sortedApplications
      .slice(0, selectedIndex + 1)
      .reduce((total, item) => total + item.appliedAmount, 0);

    return donation.baseAmount - appliedThroughApplication;
  }

  protected applicationEvidenceSummary(application: DonationApplication): string {
    const documentaryStatus = this.applicationDocumentaryStatus(application.id);
    if (documentaryStatus) {
      if (documentaryStatus.activeDocumentCount > 0) {
        return `${documentaryStatus.activeDocumentCount} documento(s) activo(s)`;
      }

      if (documentaryStatus.evidenceCount > 0) {
        return `${documentaryStatus.evidenceCount} evidencia(s), sin documento activo`;
      }

      return 'Sin evidencia activa';
    }

    return application.evidenceCount > 0
      ? `${application.evidenceCount} evidencia(s)`
      : 'Sin evidencia';
  }

  protected applicationSignificanceLabel(application: DonationApplication, donation: DonationDetail): string {
    return this.applicationSharePercentage(application, donation) >= 25
      ? 'Parte significativa'
      : 'Distribucion regular';
  }

  protected applicationSignificanceClass(application: DonationApplication, donation: DonationDetail): string {
    return this.applicationSharePercentage(application, donation) >= 25
      ? 'signal-partial'
      : 'neutral';
  }

  protected applicationMissingBasicsLabel(application: DonationApplication): string {
    const documentaryStatus = this.applicationDocumentaryStatus(application.id);
    const missingItems = [
      this.applicationHasMinimumEvidence(application) ? null : this.missingReasonLabel(documentaryStatus?.missingReasonCode),
      application.verificationDetails ? null : 'detalle de comprobacion'
    ].filter((item): item is string => item !== null);

    return missingItems.length > 0
      ? `Falta: ${missingItems.join(', ')}`
      : 'Sin faltantes basicos';
  }

  protected applicationDocumentaryLabel(application: DonationApplication): string {
    return this.applicationHasMinimumEvidence(application)
      ? 'Evidencia minima registrada'
      : 'Evidencia pendiente';
  }

  protected applicationDocumentaryClass(application: DonationApplication): string {
    return this.applicationHasMinimumEvidence(application)
      ? 'signal-complete'
      : 'signal-warning';
  }

  protected applicationDocumentaryStatus(applicationId: string): DonationApplicationDocumentaryStatus | null {
    return this.selectedDocumentaryStatus()?.applicationStatuses.find(
      (status) => status.applicationId === applicationId) ?? null;
  }

  protected applicationDocumentaryMissingLabel(application: DonationApplication): string {
    const documentaryStatus = this.applicationDocumentaryStatus(application.id);
    return this.applicationHasMinimumEvidence(application)
      ? 'Sin faltantes documentales minimos'
      : `Falta: ${this.missingReasonLabel(documentaryStatus?.missingReasonCode)}`;
  }

  protected applicationHasMinimumEvidence(application: DonationApplication): boolean {
    const documentaryStatus = this.applicationDocumentaryStatus(application.id);
    if (documentaryStatus) {
      return documentaryStatus.requirementStatus === 'MINIMUM_EVIDENCE_REGISTERED'
        || documentaryStatus.activeDocumentCount > 0;
    }

    return application.evidenceCount > 0;
  }

  protected missingReasonLabel(missingReasonCode: string | null | undefined): string {
    switch (missingReasonCode) {
      case 'MISSING_EVIDENCE':
        return 'evidencia minima activa';
      default:
        return 'evidencia minima activa';
    }
  }

  protected async submitDonation(): Promise<void> {
    this.donationFormError.set(null);
    this.donationFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (this.donationForm.invalid) {
      this.donationForm.markAllAsTouched();
      this.donationFormError.set('Completa los datos obligatorios de la donación.');
      return;
    }

    this.isSubmittingDonation.set(true);

    try {
      const rawValue = this.donationForm.getRawValue();
      const request: CreateDonationRequest = {
        donorEntityName: rawValue.donorEntityName.trim(),
        donationDate: rawValue.donationDate,
        donationType: rawValue.donationType.trim(),
        baseAmount: Number(rawValue.baseAmount),
        reference: rawValue.reference.trim(),
        notes: this.normalizeOptional(rawValue.notes),
        statusCatalogEntryId: Number(rawValue.statusCatalogEntryId)
      };

      const donation = await firstValueFrom(this.donationsService.createDonation(request));
      this.donationFormSuccess.set('Donación registrada.');
      this.resetDonationForm();
      await this.reloadDonations(donation.id);
      await this.reloadAlerts();
      this.activeTab.set('summary');
    } catch (error) {
      this.donationFormError.set(getApiErrorMessage(error, 'No fue posible registrar la donación.'));
    } finally {
      this.isSubmittingDonation.set(false);
    }
  }

  protected async submitApplication(): Promise<void> {
    const selectedDonationId = this.selectedDonationId();

    this.applicationFormError.set(null);
    this.applicationFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!selectedDonationId) {
      this.applicationFormError.set('Selecciona una donación antes de registrar una aplicación.');
      return;
    }

    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      this.applicationFormError.set('Completa los datos obligatorios de la aplicación.');
      return;
    }

    this.isSubmittingApplication.set(true);

    try {
      const rawValue = this.applicationForm.getRawValue();
      const request: CreateDonationApplicationRequest = {
        beneficiaryName: rawValue.beneficiaryName.trim(),
        responsibleContactId: rawValue.responsibleContactId || null,
        responsibleName: rawValue.responsibleName.trim(),
        applicationDate: rawValue.applicationDate,
        appliedAmount: Number(rawValue.appliedAmount),
        statusCatalogEntryId: Number(rawValue.statusCatalogEntryId),
        verificationDetails: this.normalizeOptional(rawValue.verificationDetails),
        closingDetails: this.normalizeOptional(rawValue.closingDetails)
      };

      const application = await firstValueFrom(
        this.donationsService.createDonationApplication(selectedDonationId, request));

      this.applicationFormSuccess.set('Aplicación registrada.');
      this.resetApplicationForm();
      await this.reloadDonations(selectedDonationId);
      await this.loadDonationDetail(selectedDonationId, application.id);
      await this.reloadAlerts();
      this.activeTab.set('applications');
    } catch (error) {
      this.applicationFormError.set(getApiErrorMessage(error, 'No fue posible registrar la aplicación.'));
    } finally {
      this.isSubmittingApplication.set(false);
    }
  }

  protected async submitEvidence(): Promise<void> {
    const selectedApplication = this.selectedApplication();
    const evidenceFile = this.selectedEvidenceFile();

    this.evidenceFormError.set(null);
    this.evidenceFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!selectedApplication) {
      this.evidenceFormError.set('Selecciona una aplicación antes de cargar evidencia.');
      return;
    }

    if (this.evidenceForm.invalid) {
      this.evidenceForm.markAllAsTouched();
      this.evidenceFormError.set('Completa los datos obligatorios de la evidencia.');
      return;
    }

    if (!evidenceFile) {
      this.evidenceFormError.set('Selecciona un archivo de evidencia.');
      return;
    }

    this.isSubmittingEvidence.set(true);

    try {
      const rawValue = this.evidenceForm.getRawValue();
      await firstValueFrom(
        this.donationsService.createApplicationEvidence(selectedApplication.id, {
          evidenceTypeId: Number(rawValue.evidenceTypeId),
          description: this.normalizeOptional(rawValue.description),
          file: evidenceFile
        }));

      this.evidenceFormSuccess.set('Evidencia cargada.');
      this.resetEvidenceForm();

      const selectedDonationId = this.selectedDonationId();
      if (selectedDonationId) {
        await this.loadDonationDetail(selectedDonationId, selectedApplication.id);
        await this.reloadDonations(selectedDonationId);
      }
      this.activeTab.set('evidences');
    } catch (error) {
      this.evidenceFormError.set(getApiErrorMessage(error, 'No fue posible cargar la evidencia.'));
    } finally {
      this.isSubmittingEvidence.set(false);
    }
  }

  protected syncResponsibleFromContact(): void {
    const contactId = this.applicationForm.controls.responsibleContactId.getRawValue();
    if (!contactId) {
      return;
    }

    const contact = this.contacts().find((item) => item.id === contactId);
    if (!contact) {
      return;
    }

    this.applicationForm.patchValue({
      responsibleName: contact.name
    });
  }

  protected onEvidenceSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.item(0) ?? null;

    this.selectedEvidenceFile.set(file);
    this.selectedEvidenceFileName.set(file?.name ?? null);
  }

  protected resetDonationForm(): void {
    this.donationForm.reset({
      donorEntityName: '',
      donationDate: this.todayIso(),
      donationType: '',
      baseAmount: 0,
      reference: '',
      statusCatalogEntryId: this.defaultCreatableDonationStatusId(),
      notes: ''
    });
  }

  protected resetApplicationForm(): void {
    this.applicationForm.reset({
      beneficiaryName: '',
      responsibleContactId: '',
      responsibleName: '',
      applicationDate: this.todayIso(),
      appliedAmount: 0,
      statusCatalogEntryId: this.defaultApplicationStatusId(),
      verificationDetails: '',
      closingDetails: ''
    });
  }

  protected resetEvidenceForm(): void {
    this.evidenceForm.reset({
      evidenceTypeId: 0,
      description: ''
    });

    this.selectedEvidenceFile.set(null);
    this.selectedEvidenceFileName.set(null);
  }

  protected donationStatusClass(statusCode: string): string {
    return statusCode === 'CLOSED' ? 'donation-closed' : 'donation-open';
  }

  protected applicationStatusClass(statusCode: string): string {
    return statusCode === 'CLOSED' ? 'application-closed' : 'application-open';
  }

  protected alertStateClass(alertState: string): string {
    switch (alertState) {
      case 'NOT_APPLIED':
        return 'alert-pending';
      case 'PARTIALLY_APPLIED':
        return 'alert-partial';
      default:
        return 'neutral';
    }
  }

  protected alertStateLabel(alertState: string): string {
    switch (alertState) {
      case 'NOT_APPLIED':
        return 'No aplicada';
      case 'PARTIALLY_APPLIED':
        return 'Aplicación parcial';
      default:
        return 'Sin alerta';
    }
  }

  protected async downloadEvidence(evidence: DonationApplicationEvidence): Promise<void> {
    this.pageError.set(null);

    try {
      await this.donationsService.downloadEvidence(evidence.id, evidence.originalFileName);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible descargar la evidencia.'));
    }
  }

  protected async downloadTransparencyEvidence(evidence: DonationTransparencyEvidence): Promise<void> {
    this.pageError.set(null);

    try {
      await this.donationsService.downloadEvidence(evidence.evidenceId, evidence.originalFileName);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible descargar la evidencia.'));
    }
  }

  protected reportFinancialClass(report: DonationTransparencyReport): string {
    if (report.financialSummary.appliedAmountTotal <= 0) {
      return 'signal-warning';
    }

    return report.financialSummary.remainingAmount > 0 ? 'signal-partial' : 'signal-complete';
  }

  protected reportDocumentaryClass(documentaryStatusCode: string): string {
    switch (documentaryStatusCode) {
      case 'MINIMUM_EVIDENCE_COMPLETE':
        return 'signal-complete';
      case 'EVIDENCE_PENDING':
        return 'signal-warning';
      default:
        return 'signal-neutral';
    }
  }

  protected reportOperationalClass(report: DonationTransparencyReport): string {
    if (report.operationalStatus.statusIsClosed && report.financialSummary.remainingAmount > 0) {
      return 'signal-warning';
    }

    return report.operationalStatus.statusIsClosed ? 'signal-closed' : 'signal-open';
  }

  protected reportReadinessClass(readinessCode: string): string {
    switch (readinessCode) {
      case 'READY':
        return 'signal-complete';
      case 'PARTIAL':
        return 'signal-partial';
      default:
        return 'signal-warning';
    }
  }

  protected reportReadinessDescription(readinessCode: string): string {
    switch (readinessCode) {
      case 'READY':
        return 'Listo operativamente para presentarse como vista de transparencia.';
      case 'PARTIAL':
        return 'Presenta avance operativo, pero conserva saldo pendiente o evidencia minima pendiente.';
      case 'NOT_READY':
        return 'No esta listo para presentarse porque faltan aplicaciones registradas.';
      default:
        return 'Criterio operativo preliminar calculado con los datos registrados.';
    }
  }

  protected reportApplicationEvidenceSummary(application: DonationTransparencyApplication): string {
    if (application.activeDocumentCount > 0) {
      return `${application.activeDocumentCount} documento(s) activo(s)`;
    }

    if (application.evidenceCount > 0) {
      return `${application.evidenceCount} evidencia(s), sin documento activo`;
    }

    return 'Sin evidencia activa';
  }

  protected reportApplicationHasMinimumEvidence(application: DonationTransparencyApplication): boolean {
    return application.requirementStatus === 'MINIMUM_EVIDENCE_REGISTERED'
      || application.activeDocumentCount > 0;
  }

  protected reportApplicationDocumentaryClass(application: DonationTransparencyApplication): string {
    return this.reportApplicationHasMinimumEvidence(application) ? 'signal-complete' : 'signal-warning';
  }

  protected reportApplicationMissingLabel(application: DonationTransparencyApplication): string {
    return this.reportApplicationHasMinimumEvidence(application)
      ? 'Sin faltantes documentales minimos'
      : `Falta: ${this.missingReasonLabel(application.missingReasonCode)}`;
  }

  private async bootstrap(): Promise<void> {
    this.isBootstrapping.set(true);
    this.pageError.set(null);

    try {
      await Promise.all([
        this.loadSharedData(),
        this.reloadDonations(this.selectedDonationId() ?? undefined),
        this.reloadAlerts()
      ]);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar el módulo de Donatarias.'));
    } finally {
      this.isBootstrapping.set(false);
    }
  }

  private async loadSharedData(): Promise<void> {
    const [donationStatuses, applicationStatuses, evidenceTypes, contacts] = await Promise.all([
      firstValueFrom(this.sharedCatalogsService.getModuleStatuses('DONATARIAS', 'DONATION')),
      firstValueFrom(this.sharedCatalogsService.getModuleStatuses('DONATARIAS', 'DONATION_APPLICATION')),
      firstValueFrom(this.sharedCatalogsService.getEvidenceTypes()),
      firstValueFrom(this.sharedCatalogsService.getContacts())
    ]);

    this.donationStatuses.set(donationStatuses);
    this.applicationStatuses.set(applicationStatuses);
    this.evidenceTypes.set(evidenceTypes);
    this.contacts.set(contacts);

    if (!this.donationForm.controls.statusCatalogEntryId.getRawValue()) {
      this.donationForm.patchValue({
        statusCatalogEntryId: this.defaultCreatableDonationStatusId()
      });
    }

    if (!this.applicationForm.controls.statusCatalogEntryId.getRawValue()) {
      this.applicationForm.patchValue({
        statusCatalogEntryId: this.defaultApplicationStatusId()
      });
    }
  }

  private async reloadDonations(preferredSelectionId?: string): Promise<void> {
    const rawFilters = this.filtersForm.getRawValue();
    const donations = await firstValueFrom(
      this.donationsService.listDonations({
        statusCode: rawFilters.statusCode || null,
        alertsOnly: rawFilters.alertsOnly
      }));

    this.donations.set(donations);

    const queryDonationId = this.hasAppliedInitialQuerySelection ? null : this.initialDonationId;
    const nextSelectedDonationId = [
      preferredSelectionId,
      queryDonationId,
      this.selectedDonationId(),
      donations[0]?.id
    ].find((candidate): candidate is string =>
      !!candidate && donations.some((item) => item.id === candidate)) ?? null;

    if (nextSelectedDonationId && donations.some((item) => item.id === nextSelectedDonationId)) {
      this.selectedDonationId.set(nextSelectedDonationId);
      await this.loadDonationDetail(
        nextSelectedDonationId,
        this.hasAppliedInitialQuerySelection
          ? this.selectedApplicationId() ?? undefined
          : this.initialApplicationId ?? this.selectedApplicationId() ?? undefined);
      this.hasAppliedInitialQuerySelection = true;
      return;
    }

    this.selectedDonationId.set(null);
    this.selectedDonation.set(null);
    this.selectedDocumentaryStatus.set(null);
    this.selectedTransparencyReport.set(null);
    this.selectedApplicationId.set(null);
    this.hasAppliedInitialQuerySelection = true;
  }

  private async reloadAlerts(): Promise<void> {
    const alerts = await firstValueFrom(this.donationsService.getDonationAlerts());
    this.donationAlerts.set(alerts);
  }

  private async loadDonationDetail(donationId: string, preferredApplicationId?: string): Promise<void> {
    const [donationDetail, documentaryStatus, transparencyReport] = await Promise.all([
      firstValueFrom(this.donationsService.getDonation(donationId)),
      firstValueFrom(this.donationsService.getDonationDocumentaryStatus(donationId)),
      firstValueFrom(this.donationsService.getDonationTransparencyReport(donationId))
    ]);
    this.selectedDonation.set(donationDetail);
    this.selectedDocumentaryStatus.set(documentaryStatus);
    this.selectedTransparencyReport.set(transparencyReport);

    const nextSelectedApplicationId = preferredApplicationId
      ?? this.selectedApplicationId()
      ?? donationDetail.applications[0]?.id
      ?? null;

    if (nextSelectedApplicationId && donationDetail.applications.some((item) => item.id === nextSelectedApplicationId)) {
      this.selectedApplicationId.set(nextSelectedApplicationId);
    } else {
      this.selectedApplicationId.set(donationDetail.applications[0]?.id ?? null);
    }
  }

  private compareApplicationsByDate(left: DonationApplication, right: DonationApplication): number {
    return left.applicationDate.localeCompare(right.applicationDate)
      || left.createdUtc.localeCompare(right.createdUtc)
      || left.beneficiaryName.localeCompare(right.beneficiaryName);
  }

  private compareApplicationsByEvidence(left: DonationApplication, right: DonationApplication): number {
    const leftHasEvidence = this.applicationHasMinimumEvidence(left) ? 1 : 0;
    const rightHasEvidence = this.applicationHasMinimumEvidence(right) ? 1 : 0;

    return leftHasEvidence - rightHasEvidence;
  }

  private defaultCreatableDonationStatusId(): number {
    return this.creatableDonationStatuses().find((status) => status.statusCode === 'NOT_APPLIED')?.id
      ?? this.creatableDonationStatuses()[0]?.id
      ?? 0;
  }

  private defaultApplicationStatusId(): number {
    return this.applicationStatuses().find((status) => status.statusCode === 'PARTIALLY_APPLIED')?.id
      ?? this.applicationStatuses()[0]?.id
      ?? 0;
  }

  private normalizeOptional(value: string | null | undefined): string | null {
    return value && value.trim().length > 0 ? value.trim() : null;
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }
}
