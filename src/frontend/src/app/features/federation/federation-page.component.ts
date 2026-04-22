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
import { FederationService } from '../../core/services/federation.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-federation-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-06</p>
        <h2>Federacion</h2>
        <p>
          Modulo de gestiones con participantes internos y externos, donaciones maestras con multiples
          aplicaciones, comision por aplicacion y evidencia acotada al contexto de Federacion.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      <section class="module-section">
        <div class="section-heading">
          <div>
            <p class="page-kicker">Gestiones</p>
            <h3>Gestiones de Federacion</h3>
            <p>Convenios, reuniones, entrevistas y gestiones con gobierno con alertas operativas.</p>
          </div>
          <button type="button" class="ghost" (click)="reloadPage()">Actualizar modulo</button>
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

            <article class="form-card">
              <div class="card-header">
                <div>
                  <h3>Alta de gestion</h3>
                  <p>Tipo, contraparte, fecha, objetivo y estatus base.</p>
                </div>
              </div>

              @if (actionFormError()) {
                <p class="alert error">{{ actionFormError() }}</p>
              }

              @if (actionFormSuccess()) {
                <p class="alert success">{{ actionFormSuccess() }}</p>
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
                  <textarea formControlName="objective" rows="4" placeholder="Objetivo operativo de la gestion"></textarea>
                </label>

                <label class="full-width">
                  <span>Observaciones</span>
                  <textarea formControlName="notes" rows="3" placeholder="Observaciones de la gestion"></textarea>
                </label>

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingAction()">Registrar gestion</button>
                  <button type="button" class="ghost" (click)="resetActionForm()">Limpiar</button>
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
                <p class="empty-state">Cargando gestiones de Federacion...</p>
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
                    <p class="page-kicker">Gestion seleccionada</p>
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
                    [disabled]="actionDetail.statusIsClosed"
                    [attr.title]="actionDetail.statusIsClosed ? 'La gestión ya se encuentra en estado terminal.' : 'Registrar cierre formal.'">
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

              <div class="detail-grid">
                <article class="form-card">
                  <div class="card-header">
                    <div>
                      <h3>Agregar participante</h3>
                      <p>Relaciona personas internas y externas reutilizando el catalogo compartido.</p>
                    </div>
                  </div>

                  @if (participantFormError()) {
                    <p class="alert error">{{ participantFormError() }}</p>
                  }

                  @if (participantFormSuccess()) {
                    <p class="alert success">{{ participantFormSuccess() }}</p>
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
                      <textarea formControlName="notes" rows="3" placeholder="Nota breve del participante en esta gestion"></textarea>
                    </label>

                    <div class="form-actions full-width">
                      <button type="submit" [disabled]="isSubmittingParticipant()">Agregar participante</button>
                      <button type="button" class="ghost" (click)="resetParticipantForm()">Limpiar</button>
                    </div>
                  </form>
                </article>

                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Participantes</h3>
                      <p>Vista visible de personas internas y externas asociadas a la gestion.</p>
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
                <h3>Selecciona una gestion</h3>
                <p>
                  Cuando exista al menos una gestion, su detalle quedara disponible aqui para agregar
                  participantes internos y externos reutilizando el catalogo compartido.
                </p>
              </article>
            }
          </div>
        </div>
      </section>

      <section class="module-section">
        <div class="section-heading">
          <div>
            <p class="page-kicker">Donaciones</p>
            <h3>Donaciones de Federacion</h3>
            <p>Registro maestro con multiples aplicaciones, comision por aplicacion y evidencia propia.</p>
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

            <article class="form-card">
              <div class="card-header">
                <div>
                  <h3>Alta de donacion</h3>
                  <p>Registro maestro de Federacion con referencia y estatus inicial controlado.</p>
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
                  <input type="text" formControlName="donorName" placeholder="Donante o entidad donante" />
                </label>

                <label>
                  <span>Fecha</span>
                  <input type="date" formControlName="donationDate" />
                </label>

                <label>
                  <span>Tipo de donacion</span>
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
                  <textarea formControlName="notes" rows="3" placeholder="Observaciones de la donacion"></textarea>
                </label>

                <div class="form-actions full-width">
                  <button type="submit" [disabled]="isSubmittingDonation()">Registrar donacion</button>
                  <button type="button" class="ghost" (click)="resetDonationForm()">Limpiar</button>
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
                <p class="empty-state">Cargando donaciones de Federacion...</p>
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

            <article class="list-card">
              <div class="card-header">
                <div>
                  <h3>Alertas de donaciones</h3>
                  <p>Donaciones no aplicadas o con aplicacion parcial.</p>
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
          </aside>

          <div class="detail-column">
            @if (selectedDonation(); as donationDetail) {
              <article class="detail-card">
                <div class="detail-header">
                  <div>
                    <p class="page-kicker">Donacion seleccionada</p>
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
                    [disabled]="donationDetail.statusIsClosed"
                    [attr.title]="donationDetail.statusIsClosed ? 'La donación ya se encuentra en estado terminal.' : 'Registrar cierre formal.'">
                    {{ donationDetail.statusIsClosed ? 'Ya terminal' : 'Cerrar formalmente' }}
                  </button>
                </div>
              </div>

                @if (donationDetail.notes) {
                  <p class="detail-notes">{{ donationDetail.notes }}</p>
                }

                <div class="summary-grid">
                  <article>
                    <h4>Monto base</h4>
                    <p>{{ donationDetail.baseAmount | number: '1.2-2' }}</p>
                  </article>
                  <article>
                    <h4>Monto aplicado</h4>
                    <p>{{ donationDetail.appliedAmountTotal | number: '1.2-2' }}</p>
                  </article>
                  <article>
                    <h4>Remanente</h4>
                    <p>{{ donationDetail.remainingAmount | number: '1.2-2' }}</p>
                  </article>
                  <article>
                    <h4>Porcentaje</h4>
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

              <div class="detail-grid">
                <article class="form-card">
                  <div class="card-header">
                    <div>
                      <h3>Alta de aplicacion</h3>
                      <p>Beneficiario o destino, monto aplicado, comprobacion y datos de cierre.</p>
                    </div>
                  </div>

                  @if (applicationFormError()) {
                    <p class="alert error">{{ applicationFormError() }}</p>
                  }

                  @if (applicationFormSuccess()) {
                    <p class="alert success">{{ applicationFormSuccess() }}</p>
                  }

                  <form class="form-grid" [formGroup]="applicationForm" (ngSubmit)="submitApplication()">
                    <label class="full-width">
                      <span>Beneficiario o destino</span>
                      <input type="text" formControlName="beneficiaryOrDestinationName" placeholder="Beneficiario o destino" />
                    </label>

                    <label>
                      <span>Fecha de aplicacion</span>
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
                      <button type="submit" [disabled]="isSubmittingApplication()">Registrar aplicacion</button>
                      <button type="button" class="ghost" (click)="resetApplicationForm()">Limpiar</button>
                    </div>
                  </form>
                </article>

                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Aplicaciones</h3>
                      <p>Cada aplicacion concentra su comision y su evidencia.</p>
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

              <div class="detail-grid">
                <article class="form-card">
                  <div class="card-header">
                    <div>
                      <h3>Registrar comision</h3>
                      <p>La comision queda asociada a la aplicacion seleccionada.</p>
                    </div>
                  </div>

                  @if (commissionFormError()) {
                    <p class="alert error">{{ commissionFormError() }}</p>
                  }

                  @if (commissionFormSuccess()) {
                    <p class="alert success">{{ commissionFormSuccess() }}</p>
                  }

                  @if (selectedApplication(); as selectedApplicationDetail) {
                    <p class="inline-note">
                      Aplicacion seleccionada: {{ selectedApplicationDetail.beneficiaryOrDestinationName }}
                      · {{ selectedApplicationDetail.appliedAmount | number: '1.2-2' }}
                    </p>
                  } @else {
                    <p class="empty-state">Selecciona una aplicacion para registrar la comision.</p>
                  }

                  <form class="form-grid" [formGroup]="commissionForm" (ngSubmit)="submitCommission()">
                    <label>
                      <span>Tipo de comision</span>
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
                      <span>Monto de comision</span>
                      <input type="number" min="0.01" step="0.01" formControlName="commissionAmount" />
                    </label>

                    <label class="full-width">
                      <span>Observaciones</span>
                      <textarea formControlName="notes" rows="3" placeholder="Observaciones de la comision"></textarea>
                    </label>

                    <div class="form-actions full-width">
                      <button type="submit" [disabled]="isSubmittingCommission() || !selectedApplication()">Registrar comision</button>
                      <button type="button" class="ghost" (click)="resetCommissionForm()">Limpiar</button>
                    </div>
                  </form>
                </article>

                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Comisiones de la aplicacion</h3>
                      <p>Vista operativa de las comisiones registradas dentro del modulo.</p>
                    </div>
                  </div>

                  @if (selectedApplication(); as selectedApplicationDetail) {
                    @if (selectedApplicationDetail.commissions.length === 0) {
                      <p class="empty-state">La aplicacion seleccionada aun no tiene comisiones.</p>
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
                    <p class="empty-state">Selecciona una aplicacion para consultar sus comisiones.</p>
                  }
                </article>
              </div>

              <div class="detail-grid">
                <article class="form-card">
                  <div class="card-header">
                    <div>
                      <h3>Alta de evidencia</h3>
                      <p>La evidencia se asocia a la aplicacion seleccionada, no al maestro.</p>
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
                      Aplicacion seleccionada: {{ selectedApplicationDetail.beneficiaryOrDestinationName }}
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
                      <button type="submit" [disabled]="isSubmittingEvidence() || !selectedApplication()">Cargar evidencia</button>
                      <button type="button" class="ghost" (click)="resetEvidenceForm()">Limpiar</button>
                    </div>
                  </form>
                </article>

                <article class="list-card">
                  <div class="card-header">
                    <div>
                      <h3>Evidencias</h3>
                      <p>Metadatos descargables por aplicacion.</p>
                    </div>
                  </div>

                  @if (selectedApplication(); as selectedApplicationDetail) {
                    @if (selectedApplicationDetail.evidences.length === 0) {
                      <p class="empty-state">La aplicacion seleccionada aun no tiene evidencias.</p>
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
                              <a [href]="evidenceDownloadUrl(evidence.id)" target="_blank" rel="noopener noreferrer">
                                Descargar evidencia
                              </a>
                            </div>
                          </article>
                        }
                      </div>
                    }
                  } @else {
                    <p class="empty-state">Selecciona una aplicacion para consultar sus evidencias.</p>
                  }
                </article>
              </div>
            } @else {
              <article class="empty-card">
                <h3>Selecciona una donacion</h3>
                <p>
                  Cuando exista al menos una donacion, su detalle quedara disponible aqui para registrar
                  aplicaciones, consultar porcentaje aplicado, capturar comisiones y cargar evidencias.
                </p>
              </article>
            }
          </div>
        </div>
      </section>
    </section>
  `,
  styles: [
    `
      .page-shell,
      .module-section,
      .sidebar,
      .detail-column,
      .detail-grid,
      .entity-list,
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

      .detail-grid {
        grid-template-columns: repeat(2, minmax(0, 1fr));
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

      .section-heading,
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

      .entity-row,
      .entity-button,
      .alert-row {
        display: grid;
        gap: 0.7rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
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
        padding: 0.5rem 0.72rem;
        border-radius: 999px;
        font-size: 0.82rem;
        font-weight: 700;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
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
        .summary-grid,
        .form-grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class FederationPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly federationService = inject(FederationService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

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

  protected readonly actionStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly donationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly applicationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly evidenceTypes = signal<CatalogItem[]>([]);
  protected readonly commissionTypes = signal<CatalogItem[]>([]);

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

  protected readonly selectedActionInternalCount = computed(() =>
    this.selectedAction()?.participants.filter((participant) => participant.participantSide === 'INTERNAL').length ?? 0);

  protected readonly selectedActionExternalCount = computed(() =>
    this.selectedAction()?.participants.filter((participant) => participant.participantSide === 'EXTERNAL').length ?? 0);

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
    await this.bootstrap();
  }

  protected async selectAction(actionId: string): Promise<void> {
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
      this.actionFormSuccess.set('Gestion registrada.');
      this.resetActionForm();
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

    if (!selectedActionId) {
      this.participantFormError.set('Selecciona una gestion antes de agregar participantes.');
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
      this.participantFormSuccess.set('Participante agregado.');
      this.resetParticipantForm();
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
      this.donationFormSuccess.set('Donacion registrada.');
      this.resetDonationForm();
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

    if (!selectedDonationId) {
      this.applicationFormError.set('Selecciona una donacion antes de registrar una aplicacion.');
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
      this.applicationFormSuccess.set('Aplicacion registrada.');
      this.resetApplicationForm();
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

    if (!selectedApplication) {
      this.commissionFormError.set('Selecciona una aplicacion antes de registrar la comision.');
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
      this.commissionFormSuccess.set('Comision registrada.');
      this.resetCommissionForm();

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

    if (!selectedApplication) {
      this.evidenceFormError.set('Selecciona una aplicacion antes de cargar evidencia.');
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

      this.evidenceFormSuccess.set('Evidencia cargada.');
      this.resetEvidenceForm();

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

  protected evidenceDownloadUrl(evidenceId: string): string {
    return this.federationService.getEvidenceDownloadUrl(evidenceId);
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
