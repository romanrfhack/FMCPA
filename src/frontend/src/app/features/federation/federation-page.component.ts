import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  CreateFederationActionParticipantRequest,
  CreateFederationActionRequest,
  CreateFederationDonationApplicationCommissionRequest,
  CreateFederationDonationApplicationRequest,
  CreateFederationDonationRequest,
  FederationActionAlert,
  FederationActionDetail,
  FederationActionParticipant,
  FederationActionSummary,
  FederationDonationAlert,
  FederationDonationApplication,
  FederationDonationApplicationEvidence,
  FederationDonationDetail,
  FederationDonationSummary
} from '../../core/models/federation.models';
import { CatalogItem, Contact, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { AuthService } from '../../core/services/auth.service';
import { FederationService } from '../../core/services/federation.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';
import { RelatedDocumentsPanelComponent } from '../documents/related-documents-panel.component';

type FederationTab = 'summary' | 'actions' | 'donations' | 'applications' | 'evidences';

interface FederationVisibleMetrics {
  actionCount: number;
  activeActionCount: number;
  followUpPendingActionCount: number;
  concludedActionCount: number;
  closedActionCount: number;
  donationCount: number;
  totalReceived: number;
  totalApplied: number;
  pendingBalance: number;
  appliedPercentage: number;
  pendingDonationCount: number;
  applicationCount: number;
  commissionCount: number;
  evidenceCount: number;
  alertCount: number;
}

@Component({
  selector: 'app-federation-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule, RelatedDocumentsPanelComponent],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">Operación de Federación</p>
        <h2>Federación</h2>
        <p>
          Módulo de gestiones con participantes internos y externos, donaciones maestras con múltiples
          aplicaciones, comisión por aplicación y evidencia acotada al contexto de Federación.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      @if (pageSuccess()) {
        <p class="alert success">{{ pageSuccess() }}</p>
      }

      <nav class="tab-nav" aria-label="Secciones de Federación">
        <button type="button" [class.is-active]="activeTab() === 'summary'" (click)="setActiveTab('summary')">
          Resumen
        </button>
        <button type="button" [class.is-active]="activeTab() === 'actions'" (click)="setActiveTab('actions')">
          Gestiones
        </button>
        <button type="button" [class.is-active]="activeTab() === 'donations'" (click)="setActiveTab('donations')">
          Donaciones
        </button>
        <button type="button" [class.is-active]="activeTab() === 'applications'" (click)="setActiveTab('applications')">
          Aplicaciones / comisiones
        </button>
        <button type="button" [class.is-active]="activeTab() === 'evidences'" (click)="setActiveTab('evidences')">
          Evidencias
        </button>
      </nav>

      @if (isActionModalOpen()) {
        <div class="modal" (click)="closeActionModal()" (document:keydown.escape)="closeActionModal()">
          <article
            class="form-card modal-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="federation-action-modal-title"
            (click)="$event.stopPropagation()">
            <div class="card-header">
              <div>
                <h3 id="federation-action-modal-title">Registrar gestión</h3>
                <p>Tipo, contraparte, fecha, objetivo y estatus base.</p>
              </div>
              <button type="button" class="ghost" (click)="closeActionModal()">Cerrar</button>
            </div>

            @if (actionFormError()) {
              <p class="alert error">{{ actionFormError() }}</p>
            }

            <form class="form-grid" [formGroup]="actionForm" (ngSubmit)="submitAction()">
              <label>
                <span>Tipo</span>
                <select formControlName="actionTypeCode">
                  <option value="">Selecciona un tipo</option>
                  @for (type of actionTypes; track type.value) {
                    <option [value]="type.value">{{ type.label }}</option>
                  }
                </select>
              </label>

              <label>
                <span>Fecha</span>
                <input type="date" formControlName="actionDate" />
              </label>

              <label class="full-width">
                <span>Contraparte o institucion</span>
                <input type="text" formControlName="counterpartyOrInstitution" placeholder="Institucion, dependencia o contraparte" />
              </label>

              <label>
                <span>Estatus</span>
                <select formControlName="statusCatalogEntryId">
                  <option [value]="0">Selecciona un estatus</option>
                  @for (status of actionStatuses(); track status.id) {
                    <option [value]="status.id">{{ status.statusName }}</option>
                  }
                </select>
              </label>

              <label class="full-width">
                <span>Objetivo</span>
                <textarea formControlName="objective" rows="4" placeholder="Objetivo operativo de la gestión"></textarea>
              </label>

              <label class="full-width">
                <span>Observaciones</span>
                <textarea formControlName="notes" rows="3" placeholder="Observaciones de la gestión"></textarea>
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingAction() || !canWrite()">Guardar gestión</button>
                <button type="button" class="ghost" (click)="resetActionForm()" [disabled]="isSubmittingAction()">Limpiar</button>
                <button type="button" class="ghost" (click)="closeActionModal()" [disabled]="isSubmittingAction()">Cancelar</button>
              </div>
            </form>
          </article>
        </div>
      }

      @if (isParticipantModalOpen()) {
        <div class="modal" (click)="closeParticipantModal()" (document:keydown.escape)="closeParticipantModal()">
          @if (selectedAction(); as actionDetail) {
            <article
              class="form-card modal-panel"
              role="dialog"
              aria-modal="true"
              aria-labelledby="federation-participant-modal-title"
              (click)="$event.stopPropagation()">
              <div class="card-header">
                <div>
                  <h3 id="federation-participant-modal-title">Agregar participante</h3>
                  <p>Relaciona personas internas y externas reutilizando el catalogo compartido.</p>
                </div>
                <button type="button" class="ghost" (click)="closeParticipantModal()">Cerrar</button>
              </div>

              <p class="inline-note">
                Gestión: {{ actionDetail.actionTypeName }} · {{ actionDetail.counterpartyOrInstitution }}.
              </p>

              @if (participantFormError()) {
                <p class="alert error">{{ participantFormError() }}</p>
              }

              <form class="form-grid" [formGroup]="participantForm" (ngSubmit)="submitParticipant()">
                <label>
                  <span>Contacto</span>
                  <select formControlName="contactId" (change)="syncParticipantSideFromContact()">
                    <option value="">Selecciona un contacto</option>
                    @for (contact of contacts(); track contact.id) {
                      <option [value]="contact.id">{{ contact.name }} · {{ contact.contactTypeName }}</option>
                    }
                  </select>
                </label>

                <label>
                  <span>Lado</span>
                  <select formControlName="participantSide">
                    <option value="">Selecciona un lado</option>
                    @for (side of participantSides; track side.value) {
                      <option [value]="side.value">{{ side.label }}</option>
                    }
                  </select>
                </label>

                <label class="full-width">
                  <span>Observaciones</span>
                  <textarea formControlName="notes" rows="3" placeholder="Nota breve del participante en esta gestión"></textarea>
                </label>

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingParticipant() || !canAddParticipant()">Agregar participante</button>
                  <button type="button" class="ghost" (click)="resetParticipantForm()" [disabled]="isSubmittingParticipant()">Limpiar</button>
                  <button type="button" class="ghost" (click)="closeParticipantModal()" [disabled]="isSubmittingParticipant()">Cancelar</button>
                </div>
              </form>
            </article>
          }
        </div>
      }

      @if (isDonationModalOpen()) {
        <div class="modal" (click)="closeDonationModal()" (document:keydown.escape)="closeDonationModal()">
          <article
            class="form-card modal-panel"
            role="dialog"
            aria-modal="true"
            aria-labelledby="federation-donation-modal-title"
            (click)="$event.stopPropagation()">
            <div class="card-header">
              <div>
                <h3 id="federation-donation-modal-title">Registrar donación</h3>
                <p>Registro maestro de Federación con referencia y estatus inicial controlado.</p>
              </div>
              <button type="button" class="ghost" (click)="closeDonationModal()">Cerrar</button>
            </div>

            @if (donationFormError()) {
              <p class="alert error">{{ donationFormError() }}</p>
            }

            <form class="form-grid" [formGroup]="donationForm" (ngSubmit)="submitDonation()">
              <label class="full-width">
                <span>Donante</span>
                <input type="text" formControlName="donorName" placeholder="Donante o entidad donante" />
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
                <span>Monto o valor base</span>
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
                <textarea formControlName="notes" rows="3" placeholder="Observaciones de la donación"></textarea>
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingDonation() || !canWrite()">Guardar donación</button>
                <button type="button" class="ghost" (click)="resetDonationForm()" [disabled]="isSubmittingDonation()">Limpiar</button>
                <button type="button" class="ghost" (click)="closeDonationModal()" [disabled]="isSubmittingDonation()">Cancelar</button>
              </div>
            </form>
          </article>
        </div>
      }

      @if (isApplicationModalOpen()) {
        <div class="modal" (click)="closeApplicationModal()" (document:keydown.escape)="closeApplicationModal()">
          @if (selectedDonation(); as donationDetail) {
            <article
              class="form-card modal-panel"
              role="dialog"
              aria-modal="true"
              aria-labelledby="federation-application-modal-title"
              (click)="$event.stopPropagation()">
              <div class="card-header">
                <div>
                  <h3 id="federation-application-modal-title">Registrar aplicación</h3>
                  <p>Beneficiario o destino, monto aplicado, comprobacion y datos de cierre.</p>
                </div>
                <button type="button" class="ghost" (click)="closeApplicationModal()">Cerrar</button>
              </div>

              <p class="inline-note">
                Donante: {{ donationDetail.donorName }}
                · Total recibido {{ donationDetail.baseAmount | number: '1.2-2' }}
                · Total aplicado {{ donationDetail.appliedAmountTotal | number: '1.2-2' }}
                · Saldo pendiente {{ donationDetail.remainingAmount | number: '1.2-2' }}.
              </p>

              @if (applicationFormError()) {
                <p class="alert error">{{ applicationFormError() }}</p>
              }

              <form class="form-grid" [formGroup]="applicationForm" (ngSubmit)="submitApplication()">
                <label class="full-width">
                  <span>Beneficiario o destino</span>
                  <input type="text" formControlName="beneficiaryOrDestinationName" placeholder="Beneficiario o destino" />
                </label>

                <label>
                  <span>Fecha de aplicación</span>
                  <input type="date" formControlName="applicationDate" />
                </label>

                <label>
                  <span>Monto aplicado</span>
                  <input type="number" min="0.01" step="0.01" formControlName="appliedAmount" />
                </label>

                <label>
                  <span>Estatus</span>
                  <select formControlName="statusCatalogEntryId">
                    <option [value]="0">Selecciona un estatus</option>
                    @for (status of applicationStatuses(); track status.id) {
                      <option [value]="status.id">{{ status.statusName }}</option>
                    }
                  </select>
                </label>

                <label class="full-width">
                  <span>Comprobacion / detalle</span>
                  <textarea formControlName="verificationDetails" rows="4" placeholder="Detalle de comprobacion"></textarea>
                </label>

                <label class="full-width">
                  <span>Datos de cierre</span>
                  <textarea formControlName="closingDetails" rows="3" placeholder="Si aplica"></textarea>
                </label>

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingApplication() || !canRegisterApplication()">Guardar aplicación</button>
                  <button type="button" class="ghost" (click)="resetApplicationForm()" [disabled]="isSubmittingApplication()">Limpiar</button>
                  <button type="button" class="ghost" (click)="closeApplicationModal()" [disabled]="isSubmittingApplication()">Cancelar</button>
                </div>
              </form>
            </article>
          }
        </div>
      }

      @if (isCommissionModalOpen()) {
        <div class="modal" (click)="closeCommissionModal()" (document:keydown.escape)="closeCommissionModal()">
          @if (selectedApplication(); as selectedApplicationDetail) {
            <article
              class="form-card modal-panel"
              role="dialog"
              aria-modal="true"
              aria-labelledby="federation-commission-modal-title"
              (click)="$event.stopPropagation()">
              <div class="card-header">
                <div>
                  <h3 id="federation-commission-modal-title">Registrar comisión</h3>
                  <p>La comisión queda asociada a la aplicación seleccionada.</p>
                </div>
                <button type="button" class="ghost" (click)="closeCommissionModal()">Cerrar</button>
              </div>

              <p class="inline-note">
                Aplicación: {{ selectedApplicationDetail.beneficiaryOrDestinationName }}
                · Monto {{ selectedApplicationDetail.appliedAmount | number: '1.2-2' }}.
              </p>

              @if (commissionFormError()) {
                <p class="alert error">{{ commissionFormError() }}</p>
              }

              <form class="form-grid" [formGroup]="commissionForm" (ngSubmit)="submitCommission()">
                <label>
                  <span>Tipo de comisión</span>
                  <select formControlName="commissionTypeId">
                    <option [value]="0">Selecciona un tipo</option>
                    @for (type of commissionTypes(); track type.id) {
                      <option [value]="type.id">{{ type.name }}</option>
                    }
                  </select>
                </label>

                <label>
                  <span>Categoria destinatario</span>
                  <select formControlName="recipientCategory">
                    <option value="">Selecciona una categoria</option>
                    @for (category of recipientCategories; track category.value) {
                      <option [value]="category.value">{{ category.label }}</option>
                    }
                  </select>
                </label>

                <label>
                  <span>Contacto destinatario</span>
                  <select formControlName="recipientContactId" (change)="syncRecipientFromContact()">
                    <option value="">Sin vincular</option>
                    @for (contact of contacts(); track contact.id) {
                      <option [value]="contact.id">{{ contact.name }}</option>
                    }
                  </select>
                </label>

                <label>
                  <span>Destinatario</span>
                  <input type="text" formControlName="recipientName" placeholder="Nombre del destinatario" />
                </label>

                <label>
                  <span>Monto base</span>
                  <input type="number" min="0.01" step="0.01" formControlName="baseAmount" />
                </label>

                <label>
                  <span>Monto de comisión</span>
                  <input type="number" min="0.01" step="0.01" formControlName="commissionAmount" />
                </label>

                <label class="full-width">
                  <span>Observaciones</span>
                  <textarea formControlName="notes" rows="3" placeholder="Observaciones de la comisión"></textarea>
                </label>

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingCommission() || !canRegisterCommission()">Guardar comisión</button>
                  <button type="button" class="ghost" (click)="resetCommissionForm()" [disabled]="isSubmittingCommission()">Limpiar</button>
                  <button type="button" class="ghost" (click)="closeCommissionModal()" [disabled]="isSubmittingCommission()">Cancelar</button>
                </div>
              </form>
            </article>
          }
        </div>
      }

      @if (isEvidenceModalOpen()) {
        <div class="modal" (click)="closeEvidenceModal()" (document:keydown.escape)="closeEvidenceModal()">
          @if (selectedApplication(); as selectedApplicationDetail) {
            <article
              class="form-card modal-panel"
              role="dialog"
              aria-modal="true"
              aria-labelledby="federation-evidence-modal-title"
              (click)="$event.stopPropagation()">
              <div class="card-header">
                <div>
                  <h3 id="federation-evidence-modal-title">Cargar evidencia</h3>
                  <p>La evidencia se asocia a la aplicación seleccionada, no al maestro.</p>
                </div>
                <button type="button" class="ghost" (click)="closeEvidenceModal()">Cerrar</button>
              </div>

              <p class="inline-note">
                Aplicación: {{ selectedApplicationDetail.beneficiaryOrDestinationName }}
                · Monto {{ selectedApplicationDetail.appliedAmount | number: '1.2-2' }}
                · Estatus documental {{ selectedApplicationDetail.evidenceCount > 0 ? 'con evidencia' : 'sin evidencia' }}.
              </p>
              <p class="inline-note">
                La evidencia registrada acredita presencia documental mínima; no sustituye revisión legal, fiscal o contable.
              </p>

              @if (evidenceFormError()) {
                <p class="alert error">{{ evidenceFormError() }}</p>
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
                  <button type="submit" [disabled]="isSubmittingEvidence() || !canUploadEvidence()">Guardar evidencia</button>
                  <button type="button" class="ghost" (click)="resetEvidenceForm()" [disabled]="isSubmittingEvidence()">Limpiar</button>
                  <button type="button" class="ghost" (click)="closeEvidenceModal()" [disabled]="isSubmittingEvidence()">Cancelar</button>
                </div>
              </form>
            </article>
          }
        </div>
      }

      @if (activeTab() === 'summary') {
        @if (federationMetrics(); as metrics) {
          <section class="tab-panel">
            <article class="detail-card executive-summary">
              <div class="detail-header">
                <div>
                  <p class="page-kicker">Resumen operativo</p>
                  <h3>Federación</h3>
                  <p>
                    Lectura ejecutiva con gestiones, donaciones, aplicaciones, comisiones y evidencias visibles
                    con los filtros actuales.
                  </p>
                </div>
                <button type="button" class="ghost" (click)="reloadPage()">Actualizar módulo</button>
              </div>

              <div class="summary-grid executive-grid">
                <article>
                  <h4>Gestiones visibles</h4>
                  <p>{{ metrics.actionCount }}</p>
                </article>
                <article>
                  <h4>En proceso</h4>
                  <p>{{ metrics.activeActionCount }}</p>
                </article>
                <article>
                  <h4>Seguimiento pendiente</h4>
                  <p>{{ metrics.followUpPendingActionCount }}</p>
                </article>
                <article>
                  <h4>Concluidas</h4>
                  <p>{{ metrics.concludedActionCount }}</p>
                </article>
                <article>
                  <h4>Cerradas</h4>
                  <p>{{ metrics.closedActionCount }}</p>
                </article>
                <article>
                  <h4>Donaciones visibles</h4>
                  <p>{{ metrics.donationCount }}</p>
                </article>
                <article>
                  <h4>Total recibido</h4>
                  <p>{{ metrics.totalReceived | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Total aplicado</h4>
                  <p>{{ metrics.totalApplied | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Saldo pendiente</h4>
                  <p>{{ metrics.pendingBalance | number: '1.2-2' }}</p>
                </article>
                <article>
                  <h4>Porcentaje aplicado</h4>
                  <p>{{ metrics.appliedPercentage | number: '1.2-2' }}%</p>
                </article>
                <article>
                  <h4>No/partialmente aplicadas</h4>
                  <p>{{ metrics.pendingDonationCount }}</p>
                </article>
                <article>
                  <h4>Aplicaciones</h4>
                  <p>{{ metrics.applicationCount }}</p>
                </article>
                <article>
                  <h4>Comisiones</h4>
                  <p>{{ metrics.commissionCount }}</p>
                </article>
                <article>
                  <h4>Evidencias</h4>
                  <p>{{ metrics.evidenceCount }}</p>
                </article>
                <article>
                  <h4>Alertas</h4>
                  <p>{{ metrics.alertCount }}</p>
                </article>
              </div>
            </article>

            <div class="summary-domain-grid">
              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Alertas principales de gestiones</h3>
                    <p>Gestiones en proceso o con seguimiento pendiente.</p>
                  </div>
                  <button type="button" class="ghost" (click)="setActiveTab('actions')">Ver gestiones</button>
                </div>

                @if (actionAlerts().length === 0) {
                  <p class="empty-state">No hay alertas activas de gestiones.</p>
                } @else {
                  <div class="entity-list compact-list">
                    @for (alert of actionAlerts(); track alert.actionId) {
                      <article class="entity-row">
                        <div class="row-top">
                          <div>
                            <h4>{{ alert.actionTypeName }}</h4>
                            <p class="meta">{{ alert.counterpartyOrInstitution }} · {{ alert.actionDate }}</p>
                          </div>
                          <span class="status-pill" [class]="actionAlertClass(alert.alertState)">
                            {{ actionAlertLabel(alert.alertState) }}
                          </span>
                        </div>
                      </article>
                    }
                  </div>
                }
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Alertas principales de donaciones</h3>
                    <p>Donaciones no aplicadas o con aplicación parcial.</p>
                  </div>
                  <button type="button" class="ghost" (click)="setActiveTab('donations')">Ver donaciones</button>
                </div>

                @if (donationAlerts().length === 0) {
                  <p class="empty-state">No hay alertas activas de donaciones.</p>
                } @else {
                  <div class="entity-list compact-list">
                    @for (alert of donationAlerts(); track alert.donationId) {
                      <article class="entity-row">
                        <div class="row-top">
                          <div>
                            <h4>{{ alert.donorName }}</h4>
                            <p class="meta">{{ alert.donationType }}</p>
                          </div>
                          <span class="status-pill" [class]="donationAlertClass(alert.alertState)">
                            {{ donationAlertLabel(alert.alertState) }}
                          </span>
                        </div>
                      </article>
                    }
                  </div>
                }
              </article>
            </div>
          </section>
        }
      }

      @if (activeTab() === 'actions') {
      <section class="module-section">
        <div class="section-heading">
          <div>
            <p class="page-kicker">Gestiones</p>
            <h3>Gestiones de Federación</h3>
            <p>Convenios, reuniones, entrevistas y gestiones con gobierno con alertas operativas.</p>
          </div>
          <div class="section-actions">
            @if (canWrite()) {
              <button type="button" (click)="openActionModal()">Registrar gestión</button>
            }
            @if (canAddParticipant()) {
              <button type="button" class="ghost" (click)="openParticipantModal()">Agregar participante</button>
            }
            <button type="button" class="ghost" (click)="reloadPage()">Actualizar modulo</button>
          </div>
        </div>

        <div class="page-grid">
          <aside class="sidebar">
            <article class="filter-card">
              <div class="card-header">
                <div>
                  <h3>Filtro de gestiones</h3>
                  <p>Reduce la lista por estatus o solo a las que siguen activas.</p>
                </div>
              </div>

              <form class="form-grid" [formGroup]="actionFiltersForm" (ngSubmit)="applyActionFilters()">
                <label>
                  <span>Estatus</span>
                  <select formControlName="statusCode">
                    <option value="">Todos</option>
                    @for (status of actionStatuses(); track status.id) {
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
                  <button type="button" class="ghost" (click)="clearActionFilters()">Limpiar</button>
                </div>
              </form>
            </article>

            <article class="list-card">
              <div class="card-header">
                <div>
                  <h3>Gestiones</h3>
                  <p>Lista operativa con participantes y seguimiento.</p>
                </div>
              </div>

              @if (isBootstrapping()) {
                <p class="empty-state">Cargando gestiones de Federación...</p>
              } @else if (actions().length === 0) {
                <p class="empty-state">No hay gestiones registradas con el filtro actual.</p>
              } @else {
                <div class="entity-list">
                  @for (action of actions(); track action.id) {
                    <button
                      type="button"
                      class="entity-button"
                      [class.is-selected]="action.id === selectedActionId()"
                      (click)="selectAction(action.id)">
                      <div class="row-top">
                        <div>
                          <h4>{{ action.actionTypeName }}</h4>
                          <p class="meta">{{ action.counterpartyOrInstitution }} · {{ action.actionDate }}</p>
                        </div>
                        <span class="status-pill" [class]="actionStatusClass(action.statusCode)">
                          {{ action.statusName }}
                        </span>
                      </div>

                      <p class="meta">{{ action.objective }}</p>

                      <div class="entity-stats">
                        <span>Participantes {{ action.participantCount }}</span>
                        <span [class]="actionAlertClass(action.alertState)">{{ actionAlertLabel(action.alertState) }}</span>
                      </div>
                    </button>
                  }
                </div>
              }
            </article>

            <article class="list-card">
              <div class="card-header">
                <div>
                  <h3>Alertas de gestiones</h3>
                  <p>Gestiones en proceso o con seguimiento pendiente.</p>
                </div>
              </div>

              @if (actionAlerts().length === 0) {
                <p class="empty-state">No hay alertas activas de gestiones.</p>
              } @else {
                <div class="alert-list">
                  @for (alert of actionAlerts(); track alert.actionId) {
                    <article class="alert-row">
                      <div class="row-top">
                        <div>
                          <h4>{{ alert.actionTypeName }}</h4>
                          <p class="meta">{{ alert.counterpartyOrInstitution }} · {{ alert.actionDate }}</p>
                        </div>
                        <span class="status-pill" [class]="actionAlertClass(alert.alertState)">
                          {{ actionAlertLabel(alert.alertState) }}
                        </span>
                      </div>
                    </article>
                  }
                </div>
              }
            </article>
          </aside>

          <div class="detail-column">
            @if (selectedAction(); as actionDetail) {
              <article class="detail-card">
                <div class="detail-header">
                  <div>
                    <p class="page-kicker">Gestión seleccionada</p>
                    <h3>{{ actionDetail.actionTypeName }}</h3>
                    <p class="meta">{{ actionDetail.counterpartyOrInstitution }} · {{ actionDetail.actionDate }}</p>
                  </div>
                <div class="detail-badges">
                  <span class="status-pill" [class]="actionStatusClass(actionDetail.statusCode)">
                    {{ actionDetail.statusName }}
                  </span>
                  <span class="status-pill" [class]="actionAlertClass(actionDetail.alertState)">
                    {{ actionAlertLabel(actionDetail.alertState) }}
                  </span>
                  <button
                    type="button"
                    class="ghost"
                    (click)="closeSelectedAction()"
                    [disabled]="!canAdminister() || actionDetail.statusIsClosed"
                    [attr.title]="!canAdminister()
                      ? 'Solo ADMIN puede registrar cierre formal.'
                      : actionDetail.statusIsClosed
                        ? 'La gestión ya se encuentra en estado terminal.'
                        : 'Registrar cierre formal.'">
                    {{ actionDetail.statusIsClosed ? 'Ya terminal' : 'Cerrar formalmente' }}
                  </button>
                </div>
              </div>

                <p class="detail-notes">{{ actionDetail.objective }}</p>

                @if (actionDetail.notes) {
                  <p class="detail-notes">{{ actionDetail.notes }}</p>
                }

                <div class="summary-grid">
                  <article>
                    <h4>Participantes</h4>
                    <p>{{ actionDetail.participants.length }}</p>
                  </article>
                  <article>
                    <h4>Internos</h4>
                    <p>{{ selectedActionInternalCount() }}</p>
                  </article>
                  <article>
                    <h4>Externos</h4>
                    <p>{{ selectedActionExternalCount() }}</p>
                  </article>
                </div>
              </article>

              <div class="detail-grid single-column">
                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Participantes</h3>
                      <p>Vista visible de personas internas y externas asociadas a la gestión.</p>
                    </div>
                  </div>

                  @if (actionDetail.participants.length === 0) {
                    <p class="empty-state">Aun no hay participantes registrados.</p>
                  } @else {
                    <div class="entity-list">
                      @for (participant of actionDetail.participants; track participant.id) {
                        <article class="entity-row">
                          <div class="row-top">
                            <div>
                              <h4>{{ participant.participantName }}</h4>
                              <p class="meta">{{ participant.organizationOrDependency || 'Sin organizacion' }}</p>
                            </div>
                            <span class="status-pill neutral">{{ participantSideLabel(participant.participantSide) }}</span>
                          </div>

                          <div class="entity-stats">
                            <span>{{ participant.contactTypeName }}</span>
                            <span>{{ participant.roleTitle || 'Sin cargo' }}</span>
                          </div>

                          @if (participant.notes) {
                            <p class="meta">{{ participant.notes }}</p>
                          }
                        </article>
                      }
                    </div>
                  }
                </article>
              </div>
            } @else {
              <article class="empty-card">
                <h3>Selecciona una gestión</h3>
                <p>
                  Cuando exista al menos una gestión, su detalle quedará disponible aquí para agregar
                  participantes internos y externos reutilizando el catalogo compartido.
                </p>
              </article>
            }
          </div>
        </div>
      </section>
      }

      @if (activeTab() === 'donations' || activeTab() === 'applications' || activeTab() === 'evidences') {
      <section class="module-section">
        <div class="section-heading">
          <div>
            <p class="page-kicker">Donaciones de Federación</p>
            @if (activeTab() === 'donations') {
              <h3>Donaciones de Federación</h3>
              <p>Registro maestro, estatus financiero, saldo pendiente y porcentaje aplicado.</p>
            } @else if (activeTab() === 'applications') {
              <h3>Aplicaciones / comisiones</h3>
              <p>Aplicaciones de la donación seleccionada y comisión asociada a cada aplicación.</p>
            } @else {
              <h3>Evidencias</h3>
              <p>Evidencias documentales por aplicación y acceso a descarga.</p>
            }
          </div>
          <div class="section-actions">
            @if (activeTab() === 'donations' && canWrite()) {
              <button type="button" (click)="openDonationModal()">Registrar donación</button>
            }
            @if (activeTab() === 'applications' && canRegisterApplication()) {
              <button type="button" (click)="openApplicationModal()">Registrar aplicación</button>
            }
            @if (activeTab() === 'applications' && canRegisterCommission()) {
              <button type="button" class="ghost" (click)="openCommissionModal()">Registrar comisión</button>
            }
            @if (activeTab() === 'evidences' && canUploadEvidence()) {
              <button type="button" (click)="openEvidenceModal()">Cargar evidencia</button>
            }
          </div>
        </div>

        <div class="page-grid">
          <aside class="sidebar">
            <article class="filter-card">
              <div class="card-header">
                <div>
                  <h3>Filtro de donaciones</h3>
                  <p>Reduce la lista por estatus o solo a las que siguen activas.</p>
                </div>
              </div>

              <form class="form-grid" [formGroup]="donationFiltersForm" (ngSubmit)="applyDonationFilters()">
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
                  <button type="button" class="ghost" (click)="clearDonationFilters()">Limpiar</button>
                </div>
              </form>
            </article>

            <article class="list-card">
              <div class="card-header">
                <div>
                  <h3>Donaciones</h3>
                  <p>Vista maestra con progreso, comisiones y evidencias.</p>
                </div>
              </div>

              @if (isBootstrapping()) {
                <p class="empty-state">Cargando donaciones de Federación...</p>
              } @else if (donations().length === 0) {
                <p class="empty-state">No hay donaciones registradas con el filtro actual.</p>
              } @else {
                <div class="entity-list">
                  @for (donation of donations(); track donation.id) {
                    <button
                      type="button"
                      class="entity-button"
                      [class.is-selected]="donation.id === selectedDonationId()"
                      (click)="selectDonation(donation.id)">
                      <div class="row-top">
                        <div>
                          <h4>{{ donation.donorName }}</h4>
                          <p class="meta">{{ donation.donationType }} · Ref {{ donation.reference }}</p>
                        </div>
                        <span class="status-pill" [class]="donationStatusClass(donation.statusCode)">
                          {{ donation.statusName }}
                        </span>
                      </div>

                      <div class="entity-stats">
                        <span>Aplicado {{ donation.appliedAmountTotal | number: '1.2-2' }}</span>
                        <span>{{ donation.appliedPercentage | number: '1.2-2' }}%</span>
                        <span>Comisiones {{ donation.commissionCount }}</span>
                      </div>

                      <span class="status-pill" [class]="donationAlertClass(donation.alertState)">
                        {{ donationAlertLabel(donation.alertState) }}
                      </span>
                    </button>
                  }
                </div>
              }
            </article>

            @if (activeTab() === 'donations') {
            <article class="list-card">
              <div class="card-header">
                <div>
                  <h3>Alertas de donaciones</h3>
                  <p>Donaciones no aplicadas o con aplicación parcial.</p>
                </div>
              </div>

              @if (donationAlerts().length === 0) {
                <p class="empty-state">No hay alertas activas de donaciones.</p>
              } @else {
                <div class="alert-list">
                  @for (alert of donationAlerts(); track alert.donationId) {
                    <article class="alert-row">
                      <div class="row-top">
                        <div>
                          <h4>{{ alert.donorName }}</h4>
                          <p class="meta">{{ alert.donationType }}</p>
                        </div>
                        <span class="status-pill" [class]="donationAlertClass(alert.alertState)">
                          {{ donationAlertLabel(alert.alertState) }}
                        </span>
                      </div>

                      <p class="meta">
                        Base {{ alert.baseAmount | number: '1.2-2' }}
                        · Aplicado {{ alert.appliedAmountTotal | number: '1.2-2' }}
                        · {{ alert.appliedPercentage | number: '1.2-2' }}%
                      </p>
                    </article>
                  }
                </div>
              }
            </article>
            }
          </aside>

          <div class="detail-column">
            @if (selectedDonation(); as donationDetail) {
              <article class="detail-card">
                <div class="detail-header">
                  <div>
                    <p class="page-kicker">Donación seleccionada</p>
                    <h3>{{ donationDetail.donorName }}</h3>
                    <p class="meta">{{ donationDetail.donationType }} · {{ donationDetail.donationDate }} · Ref {{ donationDetail.reference }}</p>
                  </div>
                <div class="detail-badges">
                  <span class="status-pill" [class]="donationStatusClass(donationDetail.statusCode)">
                    {{ donationDetail.statusName }}
                  </span>
                  <span class="status-pill" [class]="donationAlertClass(donationDetail.alertState)">
                    {{ donationAlertLabel(donationDetail.alertState) }}
                  </span>
                  <button
                    type="button"
                    class="ghost"
                    (click)="closeSelectedDonation()"
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
                    <h4>Comisiones</h4>
                    <p>{{ donationDetail.commissionCount }}</p>
                  </article>
                  <article>
                    <h4>Evidencias</h4>
                    <p>{{ donationDetail.evidenceCount }}</p>
                  </article>
                </div>
              </article>

              @if (activeTab() === 'applications') {
              <div class="detail-grid single-column">
                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Aplicaciones</h3>
                      <p>Cada aplicación concentra su comisión y su evidencia.</p>
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
                              <h4>{{ application.beneficiaryOrDestinationName }}</h4>
                              <p class="meta">{{ application.applicationDate }}</p>
                            </div>
                            <span class="status-pill" [class]="applicationStatusClass(application.statusCode)">
                              {{ application.statusName }}
                            </span>
                          </div>

                          <div class="entity-stats">
                            <span>Monto {{ application.appliedAmount | number: '1.2-2' }}</span>
                            <span>Comisiones {{ application.commissionCount }}</span>
                            <span>Evidencias {{ application.evidenceCount }}</span>
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

              <div class="detail-grid single-column">
                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Comisiones de la aplicación</h3>
                      <p>Vista operativa de las comisiones registradas dentro del módulo.</p>
                    </div>
                  </div>

                  @if (selectedApplication(); as selectedApplicationDetail) {
                    @if (selectedApplicationDetail.commissions.length === 0) {
                      <p class="empty-state">La aplicación seleccionada aun no tiene comisiones.</p>
                    } @else {
                      <div class="entity-list">
                        @for (commission of selectedApplicationDetail.commissions; track commission.id) {
                          <article class="entity-row">
                            <div class="row-top">
                              <div>
                                <h4>{{ commission.commissionTypeName }}</h4>
                                <p class="meta">{{ recipientCategoryLabel(commission.recipientCategory) }} · {{ commission.recipientName }}</p>
                              </div>
                              <span class="status-pill neutral">
                                {{ commission.commissionAmount | number: '1.2-2' }}
                              </span>
                            </div>

                            <div class="entity-stats">
                              <span>Base {{ commission.baseAmount | number: '1.2-2' }}</span>
                              <span>{{ commission.commissionTypeCode }}</span>
                            </div>

                            @if (commission.notes) {
                              <p class="meta">{{ commission.notes }}</p>
                            }
                          </article>
                        }
                      </div>
                    }
                  } @else {
                    <p class="empty-state">Selecciona una aplicación para consultar sus comisiones.</p>
                  }
                </article>
              </div>
              }

              @if (activeTab() === 'evidences') {
              <div class="detail-grid single-column">
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
                      moduleCode="FEDERATION"
                      entityType="FEDERATION_DONATION_APPLICATION"
                      [entityId]="selectedApplicationDetail.id"
                      [canRemediate]="canWrite()"
                      [remediationEvidenceTypes]="evidenceTypes()"
                      title="Documentos de la aplicación"
                      subtitle="Evidencias federales vistas desde el catalogo documental transversal."
                      emptyMessage="No hay documentos transversales asociados a esta aplicación."
                      (remediated)="reloadPage()">
                    </app-related-documents-panel>
                  } @else {
                    <p class="empty-state">Selecciona una aplicación para consultar sus evidencias.</p>
                  }
                </article>
              </div>
              }
            } @else {
              <article class="empty-card">
                <h3>Selecciona una donación</h3>
                <p>
                  Cuando exista al menos una donación, su detalle quedará disponible aquí para registrar
                  aplicaciones, consultar porcentaje aplicado, capturar comisiones y cargar evidencias.
                </p>
              </article>
            }
          </div>
        </div>
      </section>
      }
    </section>
  `,
  styles: [
    `
      .page-shell,
      .module-section,
      .tab-panel,
      .sidebar,
      .detail-column,
      .detail-grid,
      .entity-list,
      .alert-list,
      .summary-domain-grid {
        display: grid;
        gap: 1.25rem;
      }

      .page-grid {
        display: grid;
        grid-template-columns: minmax(22rem, 25rem) minmax(0, 1fr);
        gap: 1.25rem;
        align-items: start;
      }

      .detail-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .detail-grid.single-column {
        grid-template-columns: 1fr;
      }

      .modal {
        position: fixed;
        inset: 0;
        z-index: 30;
        display: grid;
        place-items: center;
        padding: 1rem;
        background: #1d2d2a6b;
      }

      .modal-panel {
        width: min(44rem, 100%);
        max-height: 90vh;
        overflow: auto;
      }

      .hero-card,
      .filter-card,
      .form-card,
      .list-card,
      .detail-card,
      .empty-card {
        min-width: 0;
        padding: 1.5rem;
        border-radius: 0.9rem;
        background: rgba(255, 255, 255, 0.82);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 12px 24px rgba(32, 44, 41, 0.05);
      }

      .tab-nav {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
        padding: 0.35rem;
        border-radius: 0.9rem;
        background: rgba(18, 63, 59, 0.06);
      }

      .tab-nav button {
        border-radius: 999px;
        padding: 0.65rem 0.85rem;
        background: transparent;
        color: #17423d;
      }

      .tab-nav button.is-active {
        background: #123f3b;
        color: #f6f6f2;
      }

      .summary-domain-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
      }

      .executive-summary {
        border-color: rgba(15, 118, 110, 0.18);
      }

      .section-heading,
      .card-header,
      .row-top,
      .detail-header {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        align-items: flex-start;
      }

      .section-actions {
        display: flex;
        flex-wrap: wrap;
        justify-content: flex-end;
        gap: 0.6rem;
      }

      .card-header {
        margin-bottom: 1rem;
      }

      .card-header > div,
      .row-top > div,
      .detail-header > div {
        min-width: 0;
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
      p {
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

      .detail-badges,
      .entity-stats,
      .row-actions {
        display: flex;
        flex-wrap: wrap;
        gap: 0.55rem;
      }

      .summary-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 10rem), 1fr));
        gap: 0.6rem;
        margin-top: 0.9rem;
      }

      .executive-grid {
        grid-template-columns: repeat(auto-fit, minmax(min(100%, 9.5rem), 1fr));
      }

      .summary-grid article {
        min-width: 0;
        padding: 0.75rem;
        border-radius: 0.75rem;
        background: #f6f5ef;
      }

      .summary-grid h4 {
        overflow-wrap: anywhere;
        font-size: 0.74rem;
        letter-spacing: 0.03em;
        line-height: 1.25;
        text-transform: uppercase;
        color: #5b6b68;
      }

      .summary-grid p {
        overflow-wrap: anywhere;
        margin-top: 0.4rem;
        font-size: 1rem;
        line-height: 1.2;
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
        min-width: 0;
        gap: 0.4rem;
        font-size: 0.92rem;
        font-weight: 600;
        color: #29403b;
      }

      input,
      select,
      textarea {
        width: 100%;
        min-width: 0;
        padding: 0.7rem 0.8rem;
        border-radius: 0.75rem;
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
        padding: 0.7rem 0.8rem;
        border-radius: 0.75rem;
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
        gap: 0.6rem;
      }

      .filter-card {
        padding: 0.85rem;
      }

      .filter-card .card-header {
        margin-bottom: 0.6rem;
      }

      button,
      .entity-button {
        border: none;
        border-radius: 0.75rem;
        padding: 0.7rem 0.9rem;
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

      .entity-row,
      .entity-button,
      .alert-row {
        display: grid;
        min-width: 0;
        gap: 0.55rem;
        padding: 0.85rem;
        border-radius: 0.8rem;
        background: #f6f5ef;
      }

      .entity-list,
      .alert-list,
      .compact-list {
        max-height: 42rem;
        overflow: auto;
      }

      .entity-button {
        text-align: left;
        cursor: pointer;
      }

      .entity-button.is-selected {
        outline: 2px solid rgba(15, 118, 110, 0.35);
      }

      .entity-stats span,
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
        max-width: 100%;
        padding: 0.42rem 0.62rem;
        border-radius: 999px;
        font-size: 0.82rem;
        line-height: 1.2;
        font-weight: 700;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        overflow-wrap: anywhere;
        text-align: center;
      }

      .status-pill.action-open,
      .status-pill.donation-open,
      .status-pill.application-open,
      .status-pill.alert-partial,
      .status-pill.alert-action-open {
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.action-closed,
      .status-pill.donation-closed,
      .status-pill.application-closed {
        background: rgba(70, 85, 82, 0.14);
        color: #41514e;
      }

      .status-pill.action-concluded {
        background: rgba(59, 130, 246, 0.12);
        color: #1d4ed8;
      }

      .status-pill.alert-pending {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .status-pill.neutral {
        background: rgba(79, 70, 229, 0.1);
        color: #4338ca;
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
        .summary-domain-grid,
        .summary-grid,
        .form-grid {
          grid-template-columns: 1fr;
        }
      }

      @media (max-width: 720px) {
        .hero-card,
        .section-heading,
        .card-header,
        .row-top,
        .detail-header {
          flex-direction: column;
        }

        .tab-nav {
          display: grid;
          grid-template-columns: 1fr;
        }

        .form-actions,
        .section-actions,
        .detail-badges {
          width: 100%;
        }

        .form-actions button,
        .section-actions button,
        .detail-badges button {
          width: 100%;
          white-space: normal;
        }
      }
    `
  ]
})
export class FederationPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly federationService = inject(FederationService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly canWrite = this.authService.canWriteFederation;
  protected readonly canAdminister = this.authService.canAdministerFormalClose;
  protected readonly actionTypes = [
    { value: 'AGREEMENT', label: 'Convenio' },
    { value: 'MEETING', label: 'Reunion' },
    { value: 'INTERVIEW', label: 'Entrevista' },
    { value: 'GOVERNMENT_MANAGEMENT', label: 'Gestion con gobierno' }
  ];

  protected readonly participantSides = [
    { value: 'INTERNAL', label: 'Interno' },
    { value: 'EXTERNAL', label: 'Externo' }
  ];

  protected readonly recipientCategories = [
    { value: 'COMPANY', label: 'Empresa' },
    { value: 'THIRD_PARTY', label: 'Tercero' },
    { value: 'OTHER_PARTICIPANT', label: 'Otro participante' }
  ];

  protected readonly isBootstrapping = signal(true);
  protected readonly pageError = signal<string | null>(null);
  protected readonly pageSuccess = signal<string | null>(null);

  protected readonly actionStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly donationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly applicationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly evidenceTypes = signal<CatalogItem[]>([]);
  protected readonly commissionTypes = signal<CatalogItem[]>([]);
  protected readonly activeTab = signal<FederationTab>('summary');

  protected readonly actions = signal<FederationActionSummary[]>([]);
  protected readonly actionAlerts = signal<FederationActionAlert[]>([]);
  protected readonly selectedActionId = signal<string | null>(null);
  protected readonly selectedAction = signal<FederationActionDetail | null>(null);

  protected readonly donations = signal<FederationDonationSummary[]>([]);
  protected readonly donationAlerts = signal<FederationDonationAlert[]>([]);
  protected readonly selectedDonationId = signal<string | null>(null);
  protected readonly selectedDonation = signal<FederationDonationDetail | null>(null);
  protected readonly selectedApplicationId = signal<string | null>(null);

  protected readonly isSubmittingAction = signal(false);
  protected readonly isSubmittingParticipant = signal(false);
  protected readonly isSubmittingDonation = signal(false);
  protected readonly isSubmittingApplication = signal(false);
  protected readonly isSubmittingCommission = signal(false);
  protected readonly isSubmittingEvidence = signal(false);
  protected readonly isActionModalOpen = signal(false);
  protected readonly isParticipantModalOpen = signal(false);
  protected readonly isDonationModalOpen = signal(false);
  protected readonly isApplicationModalOpen = signal(false);
  protected readonly isCommissionModalOpen = signal(false);
  protected readonly isEvidenceModalOpen = signal(false);

  protected readonly actionFormError = signal<string | null>(null);
  protected readonly actionFormSuccess = signal<string | null>(null);
  protected readonly participantFormError = signal<string | null>(null);
  protected readonly participantFormSuccess = signal<string | null>(null);
  protected readonly donationFormError = signal<string | null>(null);
  protected readonly donationFormSuccess = signal<string | null>(null);
  protected readonly applicationFormError = signal<string | null>(null);
  protected readonly applicationFormSuccess = signal<string | null>(null);
  protected readonly commissionFormError = signal<string | null>(null);
  protected readonly commissionFormSuccess = signal<string | null>(null);
  protected readonly evidenceFormError = signal<string | null>(null);
  protected readonly evidenceFormSuccess = signal<string | null>(null);
  protected readonly selectedEvidenceFile = signal<File | null>(null);
  protected readonly selectedEvidenceFileName = signal<string | null>(null);

  protected readonly creatableDonationStatuses = computed(() =>
    this.donationStatuses().filter((status) => status.statusCode === 'NOT_APPLIED' || status.statusCode === 'CLOSED'));

  protected readonly selectedApplication = computed<FederationDonationApplication | null>(() => {
    const selectedApplicationId = this.selectedApplicationId();
    const selectedDonation = this.selectedDonation();

    if (!selectedDonation || !selectedApplicationId) {
      return null;
    }

    return selectedDonation.applications.find((application) => application.id === selectedApplicationId) ?? null;
  });

  protected readonly canAddParticipant = computed(() => {
    const action = this.selectedAction();
    return this.canWrite() && !!action && !action.statusIsClosed;
  });

  protected readonly participantRegistrationUnavailableMessage = computed(() => {
    const action = this.selectedAction();

    if (!action) {
      return 'Selecciona una gestión abierta para agregar participantes.';
    }

    if (!this.canWrite()) {
      return 'No tienes permiso de escritura para agregar participantes.';
    }

    if (action.statusIsClosed) {
      return 'La gestión ya se encuentra en estado terminal y no admite nuevos participantes.';
    }

    return null;
  });

  protected readonly canRegisterApplication = computed(() => {
    const donation = this.selectedDonation();
    return this.canWrite() && !!donation && !donation.statusIsClosed;
  });

  protected readonly applicationRegistrationUnavailableMessage = computed(() => {
    const donation = this.selectedDonation();

    if (!donation) {
      return 'Selecciona una donación abierta para registrar aplicaciones.';
    }

    if (!this.canWrite()) {
      return 'No tienes permiso de escritura para registrar aplicaciones.';
    }

    if (donation.statusIsClosed) {
      return 'La donación ya se encuentra en estado terminal y no admite nuevas aplicaciones.';
    }

    return null;
  });

  protected readonly canRegisterCommission = computed(() => {
    const donation = this.selectedDonation();
    const application = this.selectedApplication();
    return this.canWrite() && !!donation && !donation.statusIsClosed && !!application && !application.statusIsClosed;
  });

  protected readonly commissionRegistrationUnavailableMessage = computed(() => {
    const donation = this.selectedDonation();
    const application = this.selectedApplication();

    if (!donation) {
      return 'Selecciona una donación abierta antes de registrar comisiones.';
    }

    if (!this.canWrite()) {
      return 'No tienes permiso de escritura para registrar comisiones.';
    }

    if (donation.statusIsClosed) {
      return 'La donación ya se encuentra en estado terminal y no admite nuevas comisiones.';
    }

    if (!application) {
      return 'Selecciona una aplicación para registrar la comisión.';
    }

    if (application.statusIsClosed) {
      return 'La aplicación ya se encuentra en estado terminal y no admite nuevas comisiones.';
    }

    return null;
  });

  protected readonly canUploadEvidence = computed(() => {
    const donation = this.selectedDonation();
    const application = this.selectedApplication();
    return this.canWrite() && !!donation && !donation.statusIsClosed && !!application && !application.statusIsClosed;
  });

  protected readonly evidenceUploadUnavailableMessage = computed(() => {
    const donation = this.selectedDonation();
    const application = this.selectedApplication();

    if (!donation) {
      return 'Selecciona una donación abierta antes de cargar evidencia.';
    }

    if (!this.canWrite()) {
      return 'No tienes permiso de escritura para cargar evidencia.';
    }

    if (donation.statusIsClosed) {
      return 'La donación ya se encuentra en estado terminal y no admite nueva evidencia.';
    }

    if (!application) {
      return 'Selecciona una aplicación para cargar evidencia.';
    }

    if (application.statusIsClosed) {
      return 'La aplicación ya se encuentra en estado terminal y no admite nueva evidencia.';
    }

    return null;
  });

  protected readonly selectedActionInternalCount = computed(() =>
    this.selectedAction()?.participants.filter((participant) => participant.participantSide === 'INTERNAL').length ?? 0);

  protected readonly selectedActionExternalCount = computed(() =>
    this.selectedAction()?.participants.filter((participant) => participant.participantSide === 'EXTERNAL').length ?? 0);

  protected readonly federationMetrics = computed<FederationVisibleMetrics>(() => {
    const actions = this.actions();
    const donations = this.donations();
    const totalReceived = donations.reduce((total, donation) => total + donation.baseAmount, 0);
    const totalApplied = donations.reduce((total, donation) => total + donation.appliedAmountTotal, 0);
    const pendingBalance = donations.reduce((total, donation) => total + donation.remainingAmount, 0);

    return {
      actionCount: actions.length,
      activeActionCount: actions.filter((action) => action.alertState === 'IN_PROCESS' || action.statusCode === 'IN_PROCESS').length,
      followUpPendingActionCount: actions.filter((action) => action.alertState === 'FOLLOW_UP_PENDING').length,
      concludedActionCount: actions.filter((action) => action.statusCode === 'CONCLUDED').length,
      closedActionCount: actions.filter((action) => action.statusIsClosed || action.statusCode === 'CLOSED').length,
      donationCount: donations.length,
      totalReceived,
      totalApplied,
      pendingBalance,
      appliedPercentage: totalReceived > 0 ? (totalApplied / totalReceived) * 100 : 0,
      pendingDonationCount: donations.filter((donation) =>
        donation.alertState === 'NOT_APPLIED'
        || donation.alertState === 'PARTIALLY_APPLIED'
        || donation.remainingAmount > 0).length,
      applicationCount: donations.reduce((total, donation) => total + donation.applicationCount, 0),
      commissionCount: donations.reduce((total, donation) => total + donation.commissionCount, 0),
      evidenceCount: donations.reduce((total, donation) => total + donation.evidenceCount, 0),
      alertCount: this.actionAlerts().length + this.donationAlerts().length
    };
  });

  protected readonly actionFiltersForm = this.formBuilder.nonNullable.group({
    statusCode: [''],
    alertsOnly: [false]
  });

  protected readonly donationFiltersForm = this.formBuilder.nonNullable.group({
    statusCode: [''],
    alertsOnly: [false]
  });

  protected readonly actionForm = this.formBuilder.nonNullable.group({
    actionTypeCode: ['', Validators.required],
    counterpartyOrInstitution: ['', [Validators.required, Validators.maxLength(200)]],
    actionDate: [this.todayIso(), Validators.required],
    objective: ['', [Validators.required, Validators.maxLength(1500)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    notes: ['']
  });

  protected readonly participantForm = this.formBuilder.nonNullable.group({
    contactId: ['', Validators.required],
    participantSide: ['', Validators.required],
    notes: ['']
  });

  protected readonly donationForm = this.formBuilder.nonNullable.group({
    donorName: ['', [Validators.required, Validators.maxLength(200)]],
    donationDate: [this.todayIso(), Validators.required],
    donationType: ['', [Validators.required, Validators.maxLength(120)]],
    baseAmount: [0, [Validators.required, Validators.min(0.01)]],
    reference: ['', [Validators.required, Validators.maxLength(120)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    notes: ['']
  });

  protected readonly applicationForm = this.formBuilder.nonNullable.group({
    beneficiaryOrDestinationName: ['', [Validators.required, Validators.maxLength(200)]],
    applicationDate: [this.todayIso(), Validators.required],
    appliedAmount: [0, [Validators.required, Validators.min(0.01)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    verificationDetails: [''],
    closingDetails: ['']
  });

  protected readonly commissionForm = this.formBuilder.nonNullable.group({
    commissionTypeId: [0, [Validators.required, Validators.min(1)]],
    recipientCategory: ['', Validators.required],
    recipientContactId: [''],
    recipientName: ['', [Validators.required, Validators.maxLength(200)]],
    baseAmount: [0, [Validators.required, Validators.min(0.01)]],
    commissionAmount: [0, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  protected readonly evidenceForm = this.formBuilder.nonNullable.group({
    evidenceTypeId: [0, [Validators.required, Validators.min(1)]],
    description: ['']
  });

  constructor() {
    void this.bootstrap();
  }

  protected setActiveTab(tab: FederationTab): void {
    this.activeTab.set(tab);
  }

  protected async applyActionFilters(): Promise<void> {
    await this.reloadActions();
  }

  protected async clearActionFilters(): Promise<void> {
    this.actionFiltersForm.setValue({
      statusCode: '',
      alertsOnly: false
    });

    await this.reloadActions();
  }

  protected async applyDonationFilters(): Promise<void> {
    await this.reloadDonations();
  }

  protected async clearDonationFilters(): Promise<void> {
    this.donationFiltersForm.setValue({
      statusCode: '',
      alertsOnly: false
    });

    await this.reloadDonations();
  }

  protected async reloadPage(): Promise<void> {
    this.pageSuccess.set(null);
    await this.bootstrap();
  }

  protected openActionModal(): void {
    if (!this.canWrite()) {
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.actionFormError.set(null);
    this.actionFormSuccess.set(null);
    this.isActionModalOpen.set(true);
  }

  protected closeActionModal(): void {
    if (this.isSubmittingAction()) {
      return;
    }

    this.isActionModalOpen.set(false);
    this.actionFormError.set(null);
    this.actionFormSuccess.set(null);
    this.resetActionForm();
  }

  protected openParticipantModal(): void {
    const unavailableMessage = this.participantRegistrationUnavailableMessage();

    if (unavailableMessage) {
      this.pageError.set(unavailableMessage);
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.participantFormError.set(null);
    this.participantFormSuccess.set(null);
    this.isParticipantModalOpen.set(true);
  }

  protected closeParticipantModal(): void {
    if (this.isSubmittingParticipant()) {
      return;
    }

    this.isParticipantModalOpen.set(false);
    this.participantFormError.set(null);
    this.participantFormSuccess.set(null);
    this.resetParticipantForm();
  }

  protected openDonationModal(): void {
    if (!this.canWrite()) {
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.donationFormError.set(null);
    this.donationFormSuccess.set(null);
    this.isDonationModalOpen.set(true);
  }

  protected closeDonationModal(): void {
    if (this.isSubmittingDonation()) {
      return;
    }

    this.isDonationModalOpen.set(false);
    this.donationFormError.set(null);
    this.donationFormSuccess.set(null);
    this.resetDonationForm();
  }

  protected openApplicationModal(): void {
    const unavailableMessage = this.applicationRegistrationUnavailableMessage();

    if (unavailableMessage) {
      this.pageError.set(unavailableMessage);
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.applicationFormError.set(null);
    this.applicationFormSuccess.set(null);
    this.isApplicationModalOpen.set(true);
  }

  protected closeApplicationModal(): void {
    if (this.isSubmittingApplication()) {
      return;
    }

    this.isApplicationModalOpen.set(false);
    this.applicationFormError.set(null);
    this.applicationFormSuccess.set(null);
    this.resetApplicationForm();
  }

  protected openCommissionModal(applicationId?: string): void {
    if (applicationId) {
      this.selectApplication(applicationId);
    }

    const unavailableMessage = this.commissionRegistrationUnavailableMessage();

    if (unavailableMessage) {
      this.pageError.set(unavailableMessage);
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.commissionFormError.set(null);
    this.commissionFormSuccess.set(null);
    this.syncCommissionBaseFromSelectedApplication();
    this.isCommissionModalOpen.set(true);
  }

  protected closeCommissionModal(): void {
    if (this.isSubmittingCommission()) {
      return;
    }

    this.isCommissionModalOpen.set(false);
    this.commissionFormError.set(null);
    this.commissionFormSuccess.set(null);
    this.resetCommissionForm();
  }

  protected openEvidenceModal(applicationId?: string): void {
    if (applicationId) {
      this.selectApplication(applicationId);
    }

    const unavailableMessage = this.evidenceUploadUnavailableMessage();

    if (unavailableMessage) {
      this.pageError.set(unavailableMessage);
      return;
    }

    this.pageError.set(null);
    this.pageSuccess.set(null);
    this.evidenceFormError.set(null);
    this.evidenceFormSuccess.set(null);
    this.isEvidenceModalOpen.set(true);
  }

  protected closeEvidenceModal(): void {
    if (this.isSubmittingEvidence()) {
      return;
    }

    this.isEvidenceModalOpen.set(false);
    this.evidenceFormError.set(null);
    this.evidenceFormSuccess.set(null);
    this.resetEvidenceForm();
  }

  protected async selectAction(actionId: string): Promise<void> {
    this.pageSuccess.set(null);
    this.selectedActionId.set(actionId);
    await this.loadActionDetail(actionId);
  }

  protected async closeSelectedAction(): Promise<void> {
    const action = this.selectedAction();
    if (!action) {
      return;
    }

    if (action.statusIsClosed) {
      this.pageError.set('La gestión ya se encuentra en estado terminal y no admite un nuevo cierre formal.');
      return;
    }

    const reason = globalThis.prompt('Motivo breve de cierre formal de la gestión. Deja vacío si no aplica.', '');
    if (reason === null) {
      return;
    }

    this.pageError.set(null);

    try {
      await firstValueFrom(this.federationService.closeAction(action.id, { reason: this.normalizeOptional(reason) }));
      await this.reloadActions(action.id);
      await this.loadActionDetail(action.id);
      await this.reloadAlerts();
      globalThis.alert('Cierre formal registrado en bitácora.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible registrar el cierre formal de la gestión.'));
    }
  }

  protected async selectDonation(donationId: string): Promise<void> {
    this.pageSuccess.set(null);
    this.selectedDonationId.set(donationId);
    this.selectedApplicationId.set(null);
    await this.loadDonationDetail(donationId);
  }

  protected async closeSelectedDonation(): Promise<void> {
    const donation = this.selectedDonation();
    if (!donation) {
      return;
    }

    if (donation.statusIsClosed) {
      this.pageError.set('La donación ya se encuentra en estado terminal y no admite un nuevo cierre formal.');
      return;
    }

    const reason = globalThis.prompt('Motivo breve de cierre formal de la donación. Deja vacío si no aplica.', '');
    if (reason === null) {
      return;
    }

    this.pageError.set(null);

    try {
      await firstValueFrom(this.federationService.closeDonation(donation.id, { reason: this.normalizeOptional(reason) }));
      await this.reloadDonations(donation.id);
      await this.loadDonationDetail(donation.id, this.selectedApplicationId() ?? undefined);
      await this.reloadAlerts();
      globalThis.alert('Cierre formal registrado en bitácora.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible registrar el cierre formal de la donación.'));
    }
  }

  protected selectApplication(applicationId: string): void {
    this.selectedApplicationId.set(applicationId);
    this.syncCommissionBaseFromSelectedApplication();
    this.commissionFormError.set(null);
    this.commissionFormSuccess.set(null);
    this.evidenceFormError.set(null);
    this.evidenceFormSuccess.set(null);
  }

  protected async submitAction(): Promise<void> {
    this.actionFormError.set(null);
    this.actionFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!this.canWrite()) {
      this.actionFormError.set('No tienes permiso de escritura para registrar gestiones.');
      return;
    }

    if (this.actionForm.invalid) {
      this.actionForm.markAllAsTouched();
      this.actionFormError.set('Completa los datos obligatorios de la gestion.');
      return;
    }

    this.isSubmittingAction.set(true);

    try {
      const rawValue = this.actionForm.getRawValue();
      const request: CreateFederationActionRequest = {
        actionTypeCode: rawValue.actionTypeCode,
        counterpartyOrInstitution: rawValue.counterpartyOrInstitution.trim(),
        actionDate: rawValue.actionDate,
        objective: rawValue.objective.trim(),
        statusCatalogEntryId: Number(rawValue.statusCatalogEntryId),
        notes: this.normalizeOptional(rawValue.notes)
      };

      const action = await firstValueFrom(this.federationService.createAction(request));
      this.resetActionForm();
      this.isActionModalOpen.set(false);
      this.pageSuccess.set('Gestión registrada.');
      await this.reloadActions(action.id);
      await this.reloadAlerts();
    } catch (error) {
      this.actionFormError.set(getApiErrorMessage(error, 'No fue posible registrar la gestion.'));
    } finally {
      this.isSubmittingAction.set(false);
    }
  }

  protected async submitParticipant(): Promise<void> {
    const selectedActionId = this.selectedActionId();

    this.participantFormError.set(null);
    this.participantFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!selectedActionId) {
      this.participantFormError.set('Selecciona una gestion antes de agregar participantes.');
      return;
    }

    const unavailableMessage = this.participantRegistrationUnavailableMessage();
    if (unavailableMessage) {
      this.participantFormError.set(unavailableMessage);
      return;
    }

    if (this.participantForm.invalid) {
      this.participantForm.markAllAsTouched();
      this.participantFormError.set('Completa los datos obligatorios del participante.');
      return;
    }

    this.isSubmittingParticipant.set(true);

    try {
      const rawValue = this.participantForm.getRawValue();
      const request: CreateFederationActionParticipantRequest = {
        contactId: rawValue.contactId,
        participantSide: rawValue.participantSide,
        notes: this.normalizeOptional(rawValue.notes)
      };

      await firstValueFrom(this.federationService.addActionParticipant(selectedActionId, request));
      this.resetParticipantForm();
      this.isParticipantModalOpen.set(false);
      this.pageSuccess.set('Participante agregado.');
      await this.loadActionDetail(selectedActionId);
      await this.reloadActions(selectedActionId);
      await this.reloadAlerts();
    } catch (error) {
      this.participantFormError.set(getApiErrorMessage(error, 'No fue posible agregar el participante.'));
    } finally {
      this.isSubmittingParticipant.set(false);
    }
  }

  protected async submitDonation(): Promise<void> {
    this.donationFormError.set(null);
    this.donationFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!this.canWrite()) {
      this.donationFormError.set('No tienes permiso de escritura para registrar donaciones.');
      return;
    }

    if (this.donationForm.invalid) {
      this.donationForm.markAllAsTouched();
      this.donationFormError.set('Completa los datos obligatorios de la donacion.');
      return;
    }

    this.isSubmittingDonation.set(true);

    try {
      const rawValue = this.donationForm.getRawValue();
      const request: CreateFederationDonationRequest = {
        donorName: rawValue.donorName.trim(),
        donationDate: rawValue.donationDate,
        donationType: rawValue.donationType.trim(),
        baseAmount: Number(rawValue.baseAmount),
        reference: rawValue.reference.trim(),
        notes: this.normalizeOptional(rawValue.notes),
        statusCatalogEntryId: Number(rawValue.statusCatalogEntryId)
      };

      const donation = await firstValueFrom(this.federationService.createDonation(request));
      this.resetDonationForm();
      this.isDonationModalOpen.set(false);
      this.pageSuccess.set('Donación registrada.');
      await this.reloadDonations(donation.id);
      await this.reloadAlerts();
    } catch (error) {
      this.donationFormError.set(getApiErrorMessage(error, 'No fue posible registrar la donacion.'));
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
      this.applicationFormError.set('Selecciona una donacion antes de registrar una aplicacion.');
      return;
    }

    const unavailableMessage = this.applicationRegistrationUnavailableMessage();
    if (unavailableMessage) {
      this.applicationFormError.set(unavailableMessage);
      return;
    }

    if (this.applicationForm.invalid) {
      this.applicationForm.markAllAsTouched();
      this.applicationFormError.set('Completa los datos obligatorios de la aplicacion.');
      return;
    }

    this.isSubmittingApplication.set(true);

    try {
      const rawValue = this.applicationForm.getRawValue();
      const request: CreateFederationDonationApplicationRequest = {
        beneficiaryOrDestinationName: rawValue.beneficiaryOrDestinationName.trim(),
        applicationDate: rawValue.applicationDate,
        appliedAmount: Number(rawValue.appliedAmount),
        statusCatalogEntryId: Number(rawValue.statusCatalogEntryId),
        verificationDetails: this.normalizeOptional(rawValue.verificationDetails),
        closingDetails: this.normalizeOptional(rawValue.closingDetails)
      };

      const application = await firstValueFrom(this.federationService.createDonationApplication(selectedDonationId, request));
      this.resetApplicationForm();
      this.isApplicationModalOpen.set(false);
      this.pageSuccess.set('Aplicación registrada.');
      await this.reloadDonations(selectedDonationId);
      await this.loadDonationDetail(selectedDonationId, application.id);
      await this.reloadAlerts();
    } catch (error) {
      this.applicationFormError.set(getApiErrorMessage(error, 'No fue posible registrar la aplicacion.'));
    } finally {
      this.isSubmittingApplication.set(false);
    }
  }

  protected async submitCommission(): Promise<void> {
    const selectedApplication = this.selectedApplication();

    this.commissionFormError.set(null);
    this.commissionFormSuccess.set(null);
    this.pageError.set(null);
    this.pageSuccess.set(null);

    if (!selectedApplication) {
      this.commissionFormError.set('Selecciona una aplicacion antes de registrar la comision.');
      return;
    }

    const unavailableMessage = this.commissionRegistrationUnavailableMessage();
    if (unavailableMessage) {
      this.commissionFormError.set(unavailableMessage);
      return;
    }

    if (this.commissionForm.invalid) {
      this.commissionForm.markAllAsTouched();
      this.commissionFormError.set('Completa los datos obligatorios de la comision.');
      return;
    }

    this.isSubmittingCommission.set(true);

    try {
      const rawValue = this.commissionForm.getRawValue();
      const request: CreateFederationDonationApplicationCommissionRequest = {
        commissionTypeId: Number(rawValue.commissionTypeId),
        recipientCategory: rawValue.recipientCategory,
        recipientContactId: rawValue.recipientContactId || null,
        recipientName: rawValue.recipientName.trim(),
        baseAmount: Number(rawValue.baseAmount),
        commissionAmount: Number(rawValue.commissionAmount),
        notes: this.normalizeOptional(rawValue.notes)
      };

      await firstValueFrom(this.federationService.createApplicationCommission(selectedApplication.id, request));
      this.resetCommissionForm();
      this.isCommissionModalOpen.set(false);
      this.pageSuccess.set('Comisión registrada.');

      const selectedDonationId = this.selectedDonationId();
      if (selectedDonationId) {
        await this.loadDonationDetail(selectedDonationId, selectedApplication.id);
        await this.reloadDonations(selectedDonationId);
      }
    } catch (error) {
      this.commissionFormError.set(getApiErrorMessage(error, 'No fue posible registrar la comision.'));
    } finally {
      this.isSubmittingCommission.set(false);
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
      this.evidenceFormError.set('Selecciona una aplicacion antes de cargar evidencia.');
      return;
    }

    const unavailableMessage = this.evidenceUploadUnavailableMessage();
    if (unavailableMessage) {
      this.evidenceFormError.set(unavailableMessage);
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
      await firstValueFrom(this.federationService.createApplicationEvidence(selectedApplication.id, {
        evidenceTypeId: Number(rawValue.evidenceTypeId),
        description: this.normalizeOptional(rawValue.description),
        file: evidenceFile
      }));

      this.resetEvidenceForm();
      this.isEvidenceModalOpen.set(false);
      this.pageSuccess.set('Evidencia cargada.');

      const selectedDonationId = this.selectedDonationId();
      if (selectedDonationId) {
        await this.loadDonationDetail(selectedDonationId, selectedApplication.id);
        await this.reloadDonations(selectedDonationId);
      }
    } catch (error) {
      this.evidenceFormError.set(getApiErrorMessage(error, 'No fue posible cargar la evidencia.'));
    } finally {
      this.isSubmittingEvidence.set(false);
    }
  }

  protected syncParticipantSideFromContact(): void {
    const contactId = this.participantForm.controls.contactId.getRawValue();
    if (!contactId) {
      return;
    }

    const contact = this.contacts().find((item) => item.id === contactId);
    if (!contact) {
      return;
    }

    this.participantForm.patchValue({
      participantSide: contact.contactTypeCode === 'INTERNAL' ? 'INTERNAL' : 'EXTERNAL'
    });
  }

  protected syncRecipientFromContact(): void {
    const contactId = this.commissionForm.controls.recipientContactId.getRawValue();
    if (!contactId) {
      return;
    }

    const contact = this.contacts().find((item) => item.id === contactId);
    if (!contact) {
      return;
    }

    this.commissionForm.patchValue({
      recipientName: contact.name
    });
  }

  protected onEvidenceSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    const file = input?.files?.item(0) ?? null;

    this.selectedEvidenceFile.set(file);
    this.selectedEvidenceFileName.set(file?.name ?? null);
  }

  protected resetActionForm(): void {
    this.actionForm.reset({
      actionTypeCode: '',
      counterpartyOrInstitution: '',
      actionDate: this.todayIso(),
      objective: '',
      statusCatalogEntryId: this.defaultActionStatusId(),
      notes: ''
    });
  }

  protected resetParticipantForm(): void {
    this.participantForm.reset({
      contactId: '',
      participantSide: '',
      notes: ''
    });
  }

  protected resetDonationForm(): void {
    this.donationForm.reset({
      donorName: '',
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
      beneficiaryOrDestinationName: '',
      applicationDate: this.todayIso(),
      appliedAmount: 0,
      statusCatalogEntryId: this.defaultApplicationStatusId(),
      verificationDetails: '',
      closingDetails: ''
    });
  }

  protected resetCommissionForm(): void {
    this.commissionForm.reset({
      commissionTypeId: 0,
      recipientCategory: '',
      recipientContactId: '',
      recipientName: '',
      baseAmount: this.selectedApplication()?.appliedAmount ?? 0,
      commissionAmount: 0,
      notes: ''
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

  protected actionStatusClass(statusCode: string): string {
    if (statusCode === 'CLOSED') {
      return 'action-closed';
    }

    if (statusCode === 'CONCLUDED') {
      return 'action-concluded';
    }

    return 'action-open';
  }

  protected donationStatusClass(statusCode: string): string {
    return statusCode === 'CLOSED' ? 'donation-closed' : 'donation-open';
  }

  protected applicationStatusClass(statusCode: string): string {
    return statusCode === 'CLOSED' ? 'application-closed' : 'application-open';
  }

  protected actionAlertClass(alertState: string): string {
    switch (alertState) {
      case 'FOLLOW_UP_PENDING':
        return 'alert-pending';
      case 'IN_PROCESS':
        return 'alert-action-open';
      default:
        return 'neutral';
    }
  }

  protected actionAlertLabel(alertState: string): string {
    switch (alertState) {
      case 'FOLLOW_UP_PENDING':
        return 'Seguimiento pendiente';
      case 'IN_PROCESS':
        return 'En proceso';
      default:
        return 'Sin alerta';
    }
  }

  protected donationAlertClass(alertState: string): string {
    switch (alertState) {
      case 'NOT_APPLIED':
        return 'alert-pending';
      case 'PARTIALLY_APPLIED':
        return 'alert-partial';
      default:
        return 'neutral';
    }
  }

  protected donationAlertLabel(alertState: string): string {
    switch (alertState) {
      case 'NOT_APPLIED':
        return 'No aplicada';
      case 'PARTIALLY_APPLIED':
        return 'Aplicacion parcial';
      default:
        return 'Sin alerta';
    }
  }

  protected participantSideLabel(participantSide: string): string {
    return participantSide === 'INTERNAL' ? 'Interno' : 'Externo';
  }

  protected recipientCategoryLabel(recipientCategory: string): string {
    switch (recipientCategory) {
      case 'COMPANY':
        return 'Empresa';
      case 'THIRD_PARTY':
        return 'Tercero';
      case 'OTHER_PARTICIPANT':
        return 'Otro participante';
      default:
        return recipientCategory;
    }
  }

  protected async downloadEvidence(evidence: FederationDonationApplicationEvidence): Promise<void> {
    this.pageError.set(null);

    try {
      await this.federationService.downloadEvidence(evidence.id, evidence.originalFileName);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible descargar la evidencia.'));
    }
  }

  private async bootstrap(): Promise<void> {
    this.isBootstrapping.set(true);
    this.pageError.set(null);

    try {
      await Promise.all([
        this.loadSharedData(),
        this.reloadActions(this.selectedActionId() ?? undefined),
        this.reloadDonations(this.selectedDonationId() ?? undefined),
        this.reloadAlerts()
      ]);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar el modulo de Federacion.'));
    } finally {
      this.isBootstrapping.set(false);
    }
  }

  private async loadSharedData(): Promise<void> {
    const [actionStatuses, donationStatuses, applicationStatuses, contacts, evidenceTypes, commissionTypes] = await Promise.all([
      firstValueFrom(this.sharedCatalogsService.getModuleStatuses('FEDERATION', 'FEDERATION_ACTION')),
      firstValueFrom(this.sharedCatalogsService.getModuleStatuses('FEDERATION', 'FEDERATION_DONATION')),
      firstValueFrom(this.sharedCatalogsService.getModuleStatuses('FEDERATION', 'FEDERATION_DONATION_APPLICATION')),
      firstValueFrom(this.sharedCatalogsService.getContacts()),
      firstValueFrom(this.sharedCatalogsService.getEvidenceTypes()),
      firstValueFrom(this.sharedCatalogsService.getCommissionTypes())
    ]);

    this.actionStatuses.set(actionStatuses);
    this.donationStatuses.set(donationStatuses);
    this.applicationStatuses.set(applicationStatuses);
    this.contacts.set(contacts);
    this.evidenceTypes.set(evidenceTypes);
    this.commissionTypes.set(commissionTypes);

    if (!this.actionForm.controls.statusCatalogEntryId.getRawValue()) {
      this.actionForm.patchValue({
        statusCatalogEntryId: this.defaultActionStatusId()
      });
    }

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

  private async reloadActions(preferredSelectionId?: string): Promise<void> {
    const rawFilters = this.actionFiltersForm.getRawValue();
    const actions = await firstValueFrom(
      this.federationService.listActions({
        statusCode: rawFilters.statusCode || null,
        alertsOnly: rawFilters.alertsOnly
      }));

    this.actions.set(actions);

    const nextSelectedActionId = preferredSelectionId
      ?? this.selectedActionId()
      ?? actions[0]?.id
      ?? null;

    if (nextSelectedActionId && actions.some((item) => item.id === nextSelectedActionId)) {
      this.selectedActionId.set(nextSelectedActionId);
      await this.loadActionDetail(nextSelectedActionId);
      return;
    }

    this.selectedActionId.set(null);
    this.selectedAction.set(null);
  }

  private async loadActionDetail(actionId: string): Promise<void> {
    const action = await firstValueFrom(this.federationService.getAction(actionId));
    this.selectedAction.set(action);
  }

  private async reloadDonations(preferredSelectionId?: string): Promise<void> {
    const rawFilters = this.donationFiltersForm.getRawValue();
    const donations = await firstValueFrom(
      this.federationService.listDonations({
        statusCode: rawFilters.statusCode || null,
        alertsOnly: rawFilters.alertsOnly
      }));

    this.donations.set(donations);

    const nextSelectedDonationId = preferredSelectionId
      ?? this.selectedDonationId()
      ?? donations[0]?.id
      ?? null;

    if (nextSelectedDonationId && donations.some((item) => item.id === nextSelectedDonationId)) {
      this.selectedDonationId.set(nextSelectedDonationId);
      await this.loadDonationDetail(nextSelectedDonationId, this.selectedApplicationId() ?? undefined);
      return;
    }

    this.selectedDonationId.set(null);
    this.selectedDonation.set(null);
    this.selectedApplicationId.set(null);
  }

  private async loadDonationDetail(donationId: string, preferredApplicationId?: string): Promise<void> {
    const donation = await firstValueFrom(this.federationService.getDonation(donationId));
    this.selectedDonation.set(donation);

    const nextSelectedApplicationId = preferredApplicationId
      ?? this.selectedApplicationId()
      ?? donation.applications[0]?.id
      ?? null;

    if (nextSelectedApplicationId && donation.applications.some((item) => item.id === nextSelectedApplicationId)) {
      this.selectedApplicationId.set(nextSelectedApplicationId);
      this.syncCommissionBaseFromSelectedApplication();
      return;
    }

    this.selectedApplicationId.set(null);
    this.resetCommissionForm();
  }

  private async reloadAlerts(): Promise<void> {
    const alerts = await firstValueFrom(this.federationService.getAlerts());
    this.actionAlerts.set(alerts.actions);
    this.donationAlerts.set(alerts.donations);
  }

  private syncCommissionBaseFromSelectedApplication(): void {
    const selectedApplication = this.selectedApplication();
    if (!selectedApplication) {
      return;
    }

    this.commissionForm.patchValue({
      baseAmount: selectedApplication.appliedAmount
    });
  }

  private defaultActionStatusId(): number {
    return this.actionStatuses().find((status) => status.statusCode === 'IN_PROCESS')?.id ?? 0;
  }

  private defaultCreatableDonationStatusId(): number {
    return this.donationStatuses().find((status) => status.statusCode === 'NOT_APPLIED')?.id ?? 0;
  }

  private defaultApplicationStatusId(): number {
    return this.applicationStatuses().find((status) => status.statusCode === 'PARTIALLY_APPLIED')?.id ?? 0;
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private normalizeOptional(value: string | null | undefined): string | null {
    const normalizedValue = value?.trim();
    return normalizedValue ? normalizedValue : null;
  }
}
