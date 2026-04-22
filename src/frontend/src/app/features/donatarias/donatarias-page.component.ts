import { DatePipe, DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  CreateDonationApplicationRequest,
  CreateDonationRequest,
  DonationAlert,
  DonationApplication,
  DonationApplicationEvidence,
  DonationDetail,
  DonationSummary
} from '../../core/models/donations.models';
import { Contact, CatalogItem, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { DonationsService } from '../../core/services/donations.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-donatarias-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-04</p>
        <h2>Donatarias</h2>
        <p>
          Modulo de donaciones maestras con multiples aplicaciones, porcentaje aplicado calculado
          y evidencia acotada por aplicacion.
        </p>
      </article>

      @if (pageError()) {
        <p class="alert error">{{ pageError() }}</p>
      }

      <div class="page-grid">
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
                <h3>Alta de donación</h3>
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
                <textarea formControlName="notes" rows="4" placeholder="Observaciones de la donación"></textarea>
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingDonation()">Registrar donación</button>
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
                      <span>Base {{ donation.baseAmount | number: '1.2-2' }}</span>
                      <span>Aplicado {{ donation.appliedAmountTotal | number: '1.2-2' }}</span>
                      <span>{{ donation.appliedPercentage | number: '1.2-2' }}%</span>
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
                      Base {{ alert.baseAmount | number: '1.2-2' }}
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
                    <h3>Alta de aplicación</h3>
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
                    <span>Comprobación / detalle</span>
                    <textarea formControlName="verificationDetails" rows="4" placeholder="Detalle de comprobación"></textarea>
                  </label>

                  <label class="full-width">
                    <span>Datos de cierre</span>
                    <textarea formControlName="closingDetails" rows="3" placeholder="Si aplica"></textarea>
                  </label>

                  <div class="form-actions full-width">
                    <button type="submit" [disabled]="isSubmittingApplication()">Registrar aplicación</button>
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
                    <h3>Alta de evidencia</h3>
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
                    <button type="submit" [disabled]="isSubmittingEvidence() || !selectedApplication()">Cargar evidencia</button>
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
                            <a [href]="evidenceDownloadUrl(evidence.id)" target="_blank" rel="noopener noreferrer">
                              Descargar evidencia
                            </a>
                          </div>
                        </article>
                      }
                    </div>
                  }
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
export class DonatariasPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly donationsService = inject(DonationsService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly isBootstrapping = signal(true);
  protected readonly pageError = signal<string | null>(null);

  protected readonly donationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly applicationStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly evidenceTypes = signal<CatalogItem[]>([]);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly donations = signal<DonationSummary[]>([]);
  protected readonly donationAlerts = signal<DonationAlert[]>([]);
  protected readonly selectedDonationId = signal<string | null>(null);
  protected readonly selectedDonation = signal<DonationDetail | null>(null);
  protected readonly selectedApplicationId = signal<string | null>(null);

  protected readonly isSubmittingDonation = signal(false);
  protected readonly isSubmittingApplication = signal(false);
  protected readonly isSubmittingEvidence = signal(false);

  protected readonly donationFormError = signal<string | null>(null);
  protected readonly donationFormSuccess = signal<string | null>(null);
  protected readonly applicationFormError = signal<string | null>(null);
  protected readonly applicationFormSuccess = signal<string | null>(null);
  protected readonly evidenceFormError = signal<string | null>(null);
  protected readonly evidenceFormSuccess = signal<string | null>(null);
  protected readonly selectedEvidenceFile = signal<File | null>(null);
  protected readonly selectedEvidenceFileName = signal<string | null>(null);

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

  protected readonly selectedDonationEvidenceCount = computed(() =>
    this.selectedDonation()?.applications.reduce((total, application) => total + application.evidenceCount, 0) ?? 0);

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
    await this.bootstrap();
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
      await firstValueFrom(this.donationsService.closeDonation(donation.id, { reason: this.normalizeOptional(reason) }));
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
    this.evidenceFormError.set(null);
    this.evidenceFormSuccess.set(null);
  }

  protected async submitDonation(): Promise<void> {
    this.donationFormError.set(null);
    this.donationFormSuccess.set(null);
    this.pageError.set(null);

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

  protected evidenceDownloadUrl(evidenceId: string): string {
    return this.donationsService.getEvidenceDownloadUrl(evidenceId);
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

  private async reloadAlerts(): Promise<void> {
    const alerts = await firstValueFrom(this.donationsService.getDonationAlerts());
    this.donationAlerts.set(alerts);
  }

  private async loadDonationDetail(donationId: string, preferredApplicationId?: string): Promise<void> {
    const donationDetail = await firstValueFrom(this.donationsService.getDonation(donationId));
    this.selectedDonation.set(donationDetail);

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
