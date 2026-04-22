import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

import {
  CreateFinancialCreditCommissionRequest,
  CreateFinancialCreditRequest,
  CreateFinancialPermitRequest,
  FinancialCredit,
  FinancialPermitAlert,
  FinancialPermitDetail,
  FinancialPermitSummary
} from '../../core/models/financials.models';
import { CatalogItem, Contact, ModuleStatusCatalogEntry } from '../../core/models/shared-catalogs.models';
import { FinancialsService } from '../../core/services/financials.service';
import { SharedCatalogsService } from '../../core/services/shared-catalogs.service';
import { getApiErrorMessage } from '../../core/utils/api-error-message';

@Component({
  selector: 'app-financials-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, ReactiveFormsModule],
  template: `
    <section class="page-shell">
      <article class="hero-card">
        <p class="page-kicker">STAGE-05</p>
        <h2>Financieras</h2>
        <p>
          Modulo de oficios y autorizaciones con vigencia, creditos individuales y comisiones por credito,
          sin abrir aun la vista transversal global de comisiones.
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
                <p>Reduce la lista por estatus o por oficios con alerta activa.</p>
              </div>
            </div>

            <form class="form-grid" [formGroup]="filtersForm" (ngSubmit)="applyFilters()">
              <label>
                <span>Estatus</span>
                <select formControlName="statusCode">
                  <option value="">Todos</option>
                  @for (status of permitStatuses(); track status.id) {
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
                <h3>Alta de oficio o autorización</h3>
                <p>Registro base de vigencia, stand y términos negociados.</p>
              </div>
            </div>

            @if (permitFormError()) {
              <p class="alert error">{{ permitFormError() }}</p>
            }

            @if (permitFormSuccess()) {
              <p class="alert success">{{ permitFormSuccess() }}</p>
            }

            <form class="form-grid" [formGroup]="permitForm" (ngSubmit)="submitPermit()">
              <label>
                <span>Financiera</span>
                <input type="text" formControlName="financialName" placeholder="Nombre de la financiera" />
              </label>

              <label>
                <span>Dependencia o institución</span>
                <input type="text" formControlName="institutionOrDependency" placeholder="Institución o dependencia" />
              </label>

              <label>
                <span>Lugar / stand</span>
                <input type="text" formControlName="placeOrStand" placeholder="Stand o lugar de operación" />
              </label>

              <label>
                <span>Horario</span>
                <input type="text" formControlName="schedule" placeholder="Horario autorizado" />
              </label>

              <label>
                <span>Inicio</span>
                <input type="date" formControlName="validFrom" />
              </label>

              <label>
                <span>Fin / vigencia</span>
                <input type="date" formControlName="validTo" />
              </label>

              <label>
                <span>Estatus</span>
                <select formControlName="statusCatalogEntryId">
                  <option [value]="0">Selecciona un estatus</option>
                  @for (status of permitStatuses(); track status.id) {
                    <option [value]="status.id">{{ status.statusName }}</option>
                  }
                </select>
              </label>

              <label class="full-width">
                <span>Términos del convenio negociado</span>
                <textarea formControlName="negotiatedTerms" rows="4" placeholder="Términos negociados"></textarea>
              </label>

              <label class="full-width">
                <span>Observaciones</span>
                <textarea formControlName="notes" rows="3" placeholder="Observaciones del oficio o autorización"></textarea>
              </label>

              <div class="form-actions full-width">
                <button type="submit" [disabled]="isSubmittingPermit()">Registrar oficio</button>
                <button type="button" class="ghost" (click)="resetPermitForm()">Limpiar</button>
              </div>
            </form>
          </article>

          <article class="list-card">
            <div class="card-header">
              <div>
                <h3>Oficios y autorizaciones</h3>
                <p>Lista operativa con vigencia, créditos y comisiones asociadas.</p>
              </div>
              <button type="button" class="ghost" (click)="reloadPage()">Actualizar</button>
            </div>

            @if (isBootstrapping()) {
              <p class="empty-state">Cargando modulo de Financieras...</p>
            } @else if (permits().length === 0) {
              <p class="empty-state">No hay oficios registrados con el filtro actual.</p>
            } @else {
              <div class="permit-list">
                @for (permit of permits(); track permit.id) {
                  <button
                    type="button"
                    class="permit-card"
                    [class.is-selected]="permit.id === selectedPermitId()"
                    (click)="selectPermit(permit.id)">
                    <div class="row-top">
                      <h4>{{ permit.financialName }}</h4>
                      <span class="status-pill" [class]="permitStatusClass(permit.statusCode)">
                        {{ permit.statusName }}
                      </span>
                    </div>

                    <p class="meta">{{ permit.institutionOrDependency }} · {{ permit.placeOrStand }}</p>
                    <p class="meta">
                      Vigencia {{ permit.validFrom }} a {{ permit.validTo }}
                      · {{ expirationLabel(permit.daysUntilExpiration) }}
                    </p>

                    <div class="permit-stats">
                      <span>Créditos {{ permit.creditCount }}</span>
                      <span>Comisiones {{ permit.commissionCount }}</span>
                      <span [class]="alertStateClass(permit.alertState)">{{ alertStateLabel(permit.alertState) }}</span>
                    </div>
                  </button>
                }
              </div>
            }
          </article>

          <article class="list-card">
            <div class="card-header">
              <div>
                <h3>Alertas activas</h3>
                <p>Oficios por vencer, vencidos o en renovación.</p>
              </div>
            </div>

            @if (permitAlerts().length === 0) {
              <p class="empty-state">No hay alertas activas de Financieras.</p>
            } @else {
              <div class="alert-list">
                @for (alert of permitAlerts(); track alert.permitId) {
                  <article class="alert-row">
                    <div class="row-top">
                      <h4>{{ alert.financialName }}</h4>
                      <span class="status-pill" [class]="alertStateClass(alert.alertState)">
                        {{ alertStateLabel(alert.alertState) }}
                      </span>
                    </div>
                    <p class="meta">{{ alert.institutionOrDependency }} · {{ alert.placeOrStand }}</p>
                    <p class="meta">
                      Vigencia {{ alert.validTo }}
                      · {{ expirationLabel(alert.daysUntilExpiration) }}
                    </p>
                  </article>
                }
              </div>
            }
          </article>
        </aside>

        <div class="detail-column">
          @if (selectedPermit(); as permitDetail) {
            <article class="detail-card">
              <div class="detail-header">
                <div>
                  <p class="page-kicker">Oficio seleccionado</p>
                  <h3>{{ permitDetail.financialName }}</h3>
                  <p class="meta">
                    {{ permitDetail.institutionOrDependency }} · {{ permitDetail.placeOrStand }}
                  </p>
                </div>
                <div class="detail-badges">
                  <span class="status-pill" [class]="permitStatusClass(permitDetail.statusCode)">
                    {{ permitDetail.statusName }}
                  </span>
                  <span class="status-pill" [class]="alertStateClass(permitDetail.alertState)">
                    {{ alertStateLabel(permitDetail.alertState) }}
                  </span>
                  <button
                    type="button"
                    class="ghost"
                    (click)="closeSelectedPermit()"
                    [disabled]="permitDetail.statusIsClosed"
                    [attr.title]="permitDetail.statusIsClosed ? 'El oficio ya se encuentra en estado terminal.' : 'Registrar cierre formal.'">
                    {{ permitDetail.statusIsClosed ? 'Ya terminal' : 'Cerrar formalmente' }}
                  </button>
                </div>
              </div>

              <p class="meta">
                Vigencia {{ permitDetail.validFrom }} a {{ permitDetail.validTo }}
                · {{ expirationLabel(permitDetail.daysUntilExpiration) }}
              </p>
              <p class="detail-notes">{{ permitDetail.schedule }}</p>
              <p class="detail-notes">{{ permitDetail.negotiatedTerms }}</p>

              @if (permitDetail.notes) {
                <p class="detail-notes">{{ permitDetail.notes }}</p>
              }

              <div class="summary-grid">
                <article>
                  <h4>Créditos</h4>
                  <p>{{ permitDetail.credits.length }}</p>
                </article>
                <article>
                  <h4>Comisiones</h4>
                  <p>{{ selectedPermitCommissionCount() }}</p>
                </article>
                <article>
                  <h4>Vigencia</h4>
                  <p>{{ permitDetail.validTo }}</p>
                </article>
              </div>
            </article>

            <div class="detail-grid">
              <article class="form-card">
                <div class="card-header">
                  <div>
                    <h3>Alta de crédito</h3>
                    <p>Registro individual con promotor, beneficiario y monto autorizado.</p>
                  </div>
                </div>

                @if (creditFormError()) {
                  <p class="alert error">{{ creditFormError() }}</p>
                }

                @if (creditFormSuccess()) {
                  <p class="alert success">{{ creditFormSuccess() }}</p>
                }

                <form class="form-grid" [formGroup]="creditForm" (ngSubmit)="submitCredit()">
                  <label>
                    <span>Contacto promotor</span>
                    <select formControlName="promoterContactId" (change)="syncPromoterFromContact()">
                      <option value="">Sin vincular</option>
                      @for (contact of contacts(); track contact.id) {
                        <option [value]="contact.id">{{ contact.name }}</option>
                      }
                    </select>
                  </label>

                  <label>
                    <span>Promotor</span>
                    <input type="text" formControlName="promoterName" placeholder="Nombre del promotor" />
                  </label>

                  <label>
                    <span>Contacto beneficiario</span>
                    <select formControlName="beneficiaryContactId" (change)="syncBeneficiaryFromContact()">
                      <option value="">Sin vincular</option>
                      @for (contact of contacts(); track contact.id) {
                        <option [value]="contact.id">{{ contact.name }}</option>
                      }
                    </select>
                  </label>

                  <label>
                    <span>Beneficiario</span>
                    <input type="text" formControlName="beneficiaryName" placeholder="Nombre del beneficiario" />
                  </label>

                  <label>
                    <span>Teléfono</span>
                    <input type="text" formControlName="phoneNumber" placeholder="Teléfono" />
                  </label>

                  <label>
                    <span>WhatsApp</span>
                    <input type="text" formControlName="whatsAppPhone" placeholder="WhatsApp" />
                  </label>

                  <label>
                    <span>Fecha de autorización</span>
                    <input type="date" formControlName="authorizationDate" />
                  </label>

                  <label>
                    <span>Monto</span>
                    <input type="number" min="0.01" step="0.01" formControlName="amount" />
                  </label>

                  <label class="full-width">
                    <span>Observaciones</span>
                    <textarea formControlName="notes" rows="3" placeholder="Observaciones del crédito"></textarea>
                  </label>

                  <div class="form-actions full-width">
                    <button type="submit" [disabled]="isSubmittingCredit()">Registrar crédito</button>
                    <button type="button" class="ghost" (click)="resetCreditForm()">Limpiar</button>
                  </div>
                </form>
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Créditos del oficio</h3>
                    <p>Listado individual con sus comisiones asociadas.</p>
                  </div>
                </div>

                @if (permitDetail.credits.length === 0) {
                  <p class="empty-state">Aún no hay créditos registrados.</p>
                } @else {
                  <div class="entity-list">
                    @for (credit of permitDetail.credits; track credit.id) {
                      <button
                        type="button"
                        class="entity-button"
                        [class.is-selected]="credit.id === selectedCreditId()"
                        (click)="selectCredit(credit.id)">
                        <div class="row-top">
                          <div>
                            <h4>{{ credit.beneficiaryName }}</h4>
                            <p class="meta">{{ credit.authorizationDate }} · Promotor {{ credit.promoterName }}</p>
                          </div>
                          <span class="status-pill neutral">
                            {{ credit.amount | number: '1.2-2' }}
                          </span>
                        </div>

                        <div class="permit-stats">
                          <span>Comisiones {{ credit.commissionCount }}</span>
                          <span>{{ credit.phoneNumber || 'Sin teléfono' }}</span>
                        </div>

                        @if (credit.notes) {
                          <p class="meta">{{ credit.notes }}</p>
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
                    <h3>Alta de comisión por crédito</h3>
                    <p>Registro local por crédito sin abrir aún el consolidado transversal.</p>
                  </div>
                </div>

                @if (commissionFormError()) {
                  <p class="alert error">{{ commissionFormError() }}</p>
                }

                @if (commissionFormSuccess()) {
                  <p class="alert success">{{ commissionFormSuccess() }}</p>
                }

                @if (selectedCredit(); as selectedCreditDetail) {
                  <p class="inline-note">
                    Crédito seleccionado: {{ selectedCreditDetail.beneficiaryName }}
                    · {{ selectedCreditDetail.amount | number: '1.2-2' }}
                  </p>
                } @else {
                  <p class="empty-state">Selecciona un crédito para registrar comisiones.</p>
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
                    <span>Categoría destinatario</span>
                    <select formControlName="recipientCategory">
                      <option value="">Selecciona una categoría</option>
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
                    <button type="submit" [disabled]="isSubmittingCommission() || !selectedCredit()">Registrar comisión</button>
                    <button type="button" class="ghost" (click)="resetCommissionForm()">Limpiar</button>
                  </div>
                </form>
              </article>

              <article class="list-card">
                <div class="card-header">
                  <div>
                    <h3>Comisiones del crédito</h3>
                    <p>Detalle de tipo, destinatario y monto por crédito.</p>
                  </div>
                </div>

                @if (selectedCredit(); as selectedCreditDetail) {
                  @if (selectedCreditDetail.commissions.length === 0) {
                    <p class="empty-state">El crédito seleccionado aún no tiene comisiones.</p>
                  } @else {
                    <div class="entity-list">
                      @for (commission of selectedCreditDetail.commissions; track commission.id) {
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

                          <div class="permit-stats">
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
                  <p class="empty-state">Selecciona un crédito para consultar sus comisiones.</p>
                }
              </article>
            </div>
          } @else {
            <article class="empty-card">
              <h3>Selecciona un oficio</h3>
              <p>
                Cuando exista al menos un oficio o autorización, su detalle quedará disponible aquí
                para registrar créditos individuales y comisiones por crédito.
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
      .permit-list,
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
      .permit-stats,
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
      .permit-card,
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

      .permit-card,
      .entity-row,
      .entity-button,
      .alert-row {
        display: grid;
        gap: 0.7rem;
        padding: 1rem;
        border-radius: 1rem;
        background: #f6f5ef;
      }

      .permit-card,
      .entity-button {
        text-align: left;
        cursor: pointer;
      }

      .permit-card.is-selected,
      .entity-button.is-selected {
        outline: 2px solid rgba(15, 118, 110, 0.35);
      }

      .permit-stats span {
        padding: 0.45rem 0.65rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.82rem;
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

      .status-pill.permit-open,
      .status-pill.alert-valid {
        background: rgba(15, 118, 110, 0.12);
        color: #0f766e;
      }

      .status-pill.permit-review,
      .status-pill.alert-due-soon,
      .status-pill.alert-renewal {
        background: rgba(148, 98, 0, 0.12);
        color: #7a5400;
      }

      .status-pill.permit-closed,
      .status-pill.alert-disabled {
        background: rgba(70, 85, 82, 0.14);
        color: #41514e;
      }

      .status-pill.alert-expired {
        background: rgba(190, 24, 93, 0.1);
        color: #9d174d;
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
export class FinancialsPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly financialsService = inject(FinancialsService);
  private readonly sharedCatalogsService = inject(SharedCatalogsService);

  protected readonly recipientCategories = [
    { value: 'COMPANY', label: 'Empresa' },
    { value: 'THIRD_PARTY', label: 'Intermediario / tercero' },
    { value: 'OTHER_PARTICIPANT', label: 'Otro participante' }
  ] as const;

  protected readonly isBootstrapping = signal(true);
  protected readonly pageError = signal<string | null>(null);

  protected readonly permitStatuses = signal<ModuleStatusCatalogEntry[]>([]);
  protected readonly contacts = signal<Contact[]>([]);
  protected readonly commissionTypes = signal<CatalogItem[]>([]);
  protected readonly permits = signal<FinancialPermitSummary[]>([]);
  protected readonly permitAlerts = signal<FinancialPermitAlert[]>([]);
  protected readonly selectedPermitId = signal<string | null>(null);
  protected readonly selectedPermit = signal<FinancialPermitDetail | null>(null);
  protected readonly selectedCreditId = signal<string | null>(null);

  protected readonly isSubmittingPermit = signal(false);
  protected readonly isSubmittingCredit = signal(false);
  protected readonly isSubmittingCommission = signal(false);

  protected readonly permitFormError = signal<string | null>(null);
  protected readonly permitFormSuccess = signal<string | null>(null);
  protected readonly creditFormError = signal<string | null>(null);
  protected readonly creditFormSuccess = signal<string | null>(null);
  protected readonly commissionFormError = signal<string | null>(null);
  protected readonly commissionFormSuccess = signal<string | null>(null);

  protected readonly selectedCredit = computed<FinancialCredit | null>(() => {
    const selectedCreditId = this.selectedCreditId();
    const selectedPermit = this.selectedPermit();

    if (!selectedPermit || !selectedCreditId) {
      return null;
    }

    return selectedPermit.credits.find((credit) => credit.id === selectedCreditId) ?? null;
  });

  protected readonly selectedPermitCommissionCount = computed(() =>
    this.selectedPermit()?.credits.reduce((total, credit) => total + credit.commissionCount, 0) ?? 0);

  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    statusCode: [''],
    alertsOnly: [false]
  });

  protected readonly permitForm = this.formBuilder.nonNullable.group({
    financialName: ['', [Validators.required, Validators.maxLength(200)]],
    institutionOrDependency: ['', [Validators.required, Validators.maxLength(200)]],
    placeOrStand: ['', [Validators.required, Validators.maxLength(200)]],
    validFrom: [this.todayIso(), Validators.required],
    validTo: [this.todayIso(), Validators.required],
    schedule: ['', [Validators.required, Validators.maxLength(120)]],
    negotiatedTerms: ['', [Validators.required, Validators.maxLength(2000)]],
    statusCatalogEntryId: [0, [Validators.required, Validators.min(1)]],
    notes: ['']
  });

  protected readonly creditForm = this.formBuilder.nonNullable.group({
    promoterContactId: [''],
    promoterName: ['', [Validators.required, Validators.maxLength(200)]],
    beneficiaryContactId: [''],
    beneficiaryName: ['', [Validators.required, Validators.maxLength(200)]],
    phoneNumber: [''],
    whatsAppPhone: [''],
    authorizationDate: [this.todayIso(), Validators.required],
    amount: [0, [Validators.required, Validators.min(0.01)]],
    notes: ['']
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

  constructor() {
    void this.bootstrap();
  }

  protected async applyFilters(): Promise<void> {
    await this.reloadPermits();
  }

  protected async clearFilters(): Promise<void> {
    this.filtersForm.setValue({
      statusCode: '',
      alertsOnly: false
    });

    await this.reloadPermits();
  }

  protected async reloadPage(): Promise<void> {
    await this.bootstrap();
  }

  protected async selectPermit(permitId: string): Promise<void> {
    this.selectedPermitId.set(permitId);
    this.selectedCreditId.set(null);
    await this.loadPermitDetail(permitId);
  }

  protected async closeSelectedPermit(): Promise<void> {
    const permit = this.selectedPermit();
    if (!permit) {
      return;
    }

    if (permit.statusIsClosed) {
      this.pageError.set('El oficio ya se encuentra en estado terminal y no admite un nuevo cierre formal.');
      return;
    }

    const reason = globalThis.prompt('Motivo breve de cierre formal del oficio. Deja vacío si no aplica.', '');
    if (reason === null) {
      return;
    }

    this.pageError.set(null);

    try {
      await firstValueFrom(this.financialsService.closePermit(permit.id, { reason: this.normalizeOptional(reason) }));
      await this.reloadPermits(permit.id);
      await this.loadPermitDetail(permit.id, this.selectedCreditId() ?? undefined);
      await this.reloadAlerts();
      globalThis.alert('Cierre formal registrado en bitácora.');
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible registrar el cierre formal del oficio.'));
    }
  }

  protected selectCredit(creditId: string): void {
    this.selectedCreditId.set(creditId);
    const selectedCredit = this.selectedPermit()?.credits.find((credit) => credit.id === creditId);
    if (selectedCredit) {
      this.commissionForm.patchValue({
        baseAmount: selectedCredit.amount
      });
    }
  }

  protected async submitPermit(): Promise<void> {
    this.permitFormError.set(null);
    this.permitFormSuccess.set(null);
    this.pageError.set(null);

    if (this.permitForm.invalid) {
      this.permitForm.markAllAsTouched();
      this.permitFormError.set('Completa los datos obligatorios del oficio.');
      return;
    }

    this.isSubmittingPermit.set(true);

    try {
      const rawValue = this.permitForm.getRawValue();
      const request: CreateFinancialPermitRequest = {
        financialName: rawValue.financialName.trim(),
        institutionOrDependency: rawValue.institutionOrDependency.trim(),
        placeOrStand: rawValue.placeOrStand.trim(),
        validFrom: rawValue.validFrom,
        validTo: rawValue.validTo,
        schedule: rawValue.schedule.trim(),
        negotiatedTerms: rawValue.negotiatedTerms.trim(),
        statusCatalogEntryId: Number(rawValue.statusCatalogEntryId),
        notes: this.normalizeOptional(rawValue.notes)
      };

      const permit = await firstValueFrom(this.financialsService.createPermit(request));
      this.permitFormSuccess.set('Oficio registrado.');
      this.resetPermitForm();
      await this.reloadPermits(permit.id);
      await this.reloadAlerts();
    } catch (error) {
      this.permitFormError.set(getApiErrorMessage(error, 'No fue posible registrar el oficio.'));
    } finally {
      this.isSubmittingPermit.set(false);
    }
  }

  protected async submitCredit(): Promise<void> {
    const selectedPermitId = this.selectedPermitId();

    this.creditFormError.set(null);
    this.creditFormSuccess.set(null);
    this.pageError.set(null);

    if (!selectedPermitId) {
      this.creditFormError.set('Selecciona un oficio antes de registrar un crédito.');
      return;
    }

    if (this.creditForm.invalid) {
      this.creditForm.markAllAsTouched();
      this.creditFormError.set('Completa los datos obligatorios del crédito.');
      return;
    }

    this.isSubmittingCredit.set(true);

    try {
      const rawValue = this.creditForm.getRawValue();
      const request: CreateFinancialCreditRequest = {
        promoterContactId: rawValue.promoterContactId || null,
        promoterName: rawValue.promoterName.trim(),
        beneficiaryContactId: rawValue.beneficiaryContactId || null,
        beneficiaryName: rawValue.beneficiaryName.trim(),
        phoneNumber: this.normalizeOptional(rawValue.phoneNumber),
        whatsAppPhone: this.normalizeOptional(rawValue.whatsAppPhone),
        authorizationDate: rawValue.authorizationDate,
        amount: Number(rawValue.amount),
        notes: this.normalizeOptional(rawValue.notes)
      };

      const credit = await firstValueFrom(this.financialsService.createCredit(selectedPermitId, request));
      this.creditFormSuccess.set('Crédito registrado.');
      this.resetCreditForm();
      await this.reloadPermits(selectedPermitId);
      await this.loadPermitDetail(selectedPermitId, credit.id);
    } catch (error) {
      this.creditFormError.set(getApiErrorMessage(error, 'No fue posible registrar el crédito.'));
    } finally {
      this.isSubmittingCredit.set(false);
    }
  }

  protected async submitCommission(): Promise<void> {
    const selectedCredit = this.selectedCredit();

    this.commissionFormError.set(null);
    this.commissionFormSuccess.set(null);
    this.pageError.set(null);

    if (!selectedCredit) {
      this.commissionFormError.set('Selecciona un crédito antes de registrar una comisión.');
      return;
    }

    if (this.commissionForm.invalid) {
      this.commissionForm.markAllAsTouched();
      this.commissionFormError.set('Completa los datos obligatorios de la comisión.');
      return;
    }

    this.isSubmittingCommission.set(true);

    try {
      const rawValue = this.commissionForm.getRawValue();
      const request: CreateFinancialCreditCommissionRequest = {
        commissionTypeId: Number(rawValue.commissionTypeId),
        recipientCategory: rawValue.recipientCategory,
        recipientContactId: rawValue.recipientContactId || null,
        recipientName: rawValue.recipientName.trim(),
        baseAmount: Number(rawValue.baseAmount),
        commissionAmount: Number(rawValue.commissionAmount),
        notes: this.normalizeOptional(rawValue.notes)
      };

      await firstValueFrom(this.financialsService.createCreditCommission(selectedCredit.id, request));
      this.commissionFormSuccess.set('Comisión registrada.');
      this.resetCommissionForm();

      const selectedPermitId = this.selectedPermitId();
      if (selectedPermitId) {
        await this.reloadPermits(selectedPermitId);
        await this.loadPermitDetail(selectedPermitId, selectedCredit.id);
      }
    } catch (error) {
      this.commissionFormError.set(getApiErrorMessage(error, 'No fue posible registrar la comisión.'));
    } finally {
      this.isSubmittingCommission.set(false);
    }
  }

  protected syncPromoterFromContact(): void {
    const contactId = this.creditForm.controls.promoterContactId.getRawValue();
    if (!contactId) {
      return;
    }

    const contact = this.contacts().find((item) => item.id === contactId);
    if (!contact) {
      return;
    }

    this.creditForm.patchValue({
      promoterName: contact.name
    });
  }

  protected syncBeneficiaryFromContact(): void {
    const contactId = this.creditForm.controls.beneficiaryContactId.getRawValue();
    if (!contactId) {
      return;
    }

    const contact = this.contacts().find((item) => item.id === contactId);
    if (!contact) {
      return;
    }

    this.creditForm.patchValue({
      beneficiaryName: contact.name,
      phoneNumber: contact.mobilePhone ?? this.creditForm.controls.phoneNumber.getRawValue(),
      whatsAppPhone: contact.whatsAppPhone ?? this.creditForm.controls.whatsAppPhone.getRawValue()
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

  protected resetPermitForm(): void {
    this.permitForm.reset({
      financialName: '',
      institutionOrDependency: '',
      placeOrStand: '',
      validFrom: this.todayIso(),
      validTo: this.addDaysIso(30),
      schedule: '',
      negotiatedTerms: '',
      statusCatalogEntryId: this.defaultPermitStatusId(),
      notes: ''
    });
  }

  protected resetCreditForm(): void {
    this.creditForm.reset({
      promoterContactId: '',
      promoterName: '',
      beneficiaryContactId: '',
      beneficiaryName: '',
      phoneNumber: '',
      whatsAppPhone: '',
      authorizationDate: this.todayIso(),
      amount: 0,
      notes: ''
    });
  }

  protected resetCommissionForm(): void {
    this.commissionForm.reset({
      commissionTypeId: 0,
      recipientCategory: '',
      recipientContactId: '',
      recipientName: '',
      baseAmount: this.selectedCredit()?.amount ?? 0,
      commissionAmount: 0,
      notes: ''
    });
  }

  protected permitStatusClass(statusCode: string): string {
    switch (statusCode) {
      case 'CLOSED':
      case 'REJECTED':
        return 'permit-closed';
      case 'RENEW':
        return 'permit-review';
      default:
        return 'permit-open';
    }
  }

  protected alertStateClass(alertState: string): string {
    switch (alertState) {
      case 'DUE_SOON':
        return 'alert-due-soon';
      case 'EXPIRED':
        return 'alert-expired';
      case 'RENEWAL':
        return 'alert-renewal';
      case 'ALERTS_DISABLED':
        return 'alert-disabled';
      default:
        return 'alert-valid';
    }
  }

  protected alertStateLabel(alertState: string): string {
    switch (alertState) {
      case 'DUE_SOON':
        return 'Por vencer';
      case 'EXPIRED':
        return 'Vencido';
      case 'RENEWAL':
        return 'Renovar';
      case 'ALERTS_DISABLED':
        return 'Sin alerta';
      default:
        return 'Vigente';
    }
  }

  protected expirationLabel(daysUntilExpiration: number): string {
    if (daysUntilExpiration < 0) {
      return `Vencido hace ${Math.abs(daysUntilExpiration)} días`;
    }

    if (daysUntilExpiration === 0) {
      return 'Vence hoy';
    }

    return `Vence en ${daysUntilExpiration} días`;
  }

  protected recipientCategoryLabel(value: string): string {
    return this.recipientCategories.find((item) => item.value === value)?.label ?? value;
  }

  private async bootstrap(): Promise<void> {
    this.isBootstrapping.set(true);
    this.pageError.set(null);

    try {
      await Promise.all([
        this.loadSharedData(),
        this.reloadPermits(this.selectedPermitId() ?? undefined),
        this.reloadAlerts()
      ]);
    } catch (error) {
      this.pageError.set(getApiErrorMessage(error, 'No fue posible cargar el módulo de Financieras.'));
    } finally {
      this.isBootstrapping.set(false);
    }
  }

  private async loadSharedData(): Promise<void> {
    const [permitStatuses, contacts, commissionTypes] = await Promise.all([
      firstValueFrom(this.sharedCatalogsService.getModuleStatuses('FINANCIALS', 'FINANCIAL_PERMIT')),
      firstValueFrom(this.sharedCatalogsService.getContacts()),
      firstValueFrom(this.sharedCatalogsService.getCommissionTypes())
    ]);

    this.permitStatuses.set(permitStatuses);
    this.contacts.set(contacts);
    this.commissionTypes.set(commissionTypes);

    if (!this.permitForm.controls.statusCatalogEntryId.getRawValue()) {
      this.permitForm.patchValue({
        statusCatalogEntryId: this.defaultPermitStatusId()
      });
    }
  }

  private async reloadPermits(preferredSelectionId?: string): Promise<void> {
    const rawFilters = this.filtersForm.getRawValue();
    const permits = await firstValueFrom(
      this.financialsService.listPermits({
        statusCode: rawFilters.statusCode || null,
        alertsOnly: rawFilters.alertsOnly
      }));

    this.permits.set(permits);

    const nextSelectedPermitId = preferredSelectionId
      ?? this.selectedPermitId()
      ?? permits[0]?.id
      ?? null;

    if (nextSelectedPermitId && permits.some((item) => item.id === nextSelectedPermitId)) {
      this.selectedPermitId.set(nextSelectedPermitId);
      await this.loadPermitDetail(nextSelectedPermitId, this.selectedCreditId() ?? undefined);
      return;
    }

    this.selectedPermitId.set(null);
    this.selectedPermit.set(null);
    this.selectedCreditId.set(null);
  }

  private async reloadAlerts(): Promise<void> {
    const alerts = await firstValueFrom(this.financialsService.getPermitAlerts());
    this.permitAlerts.set(alerts);
  }

  private async loadPermitDetail(permitId: string, preferredCreditId?: string): Promise<void> {
    const permitDetail = await firstValueFrom(this.financialsService.getPermit(permitId));
    this.selectedPermit.set(permitDetail);

    const nextSelectedCreditId = preferredCreditId
      ?? this.selectedCreditId()
      ?? permitDetail.credits[0]?.id
      ?? null;

    if (nextSelectedCreditId && permitDetail.credits.some((item) => item.id === nextSelectedCreditId)) {
      this.selectedCreditId.set(nextSelectedCreditId);
      const selectedCredit = permitDetail.credits.find((item) => item.id === nextSelectedCreditId);
      if (selectedCredit) {
        this.commissionForm.patchValue({
          baseAmount: selectedCredit.amount
        });
      }
    } else {
      this.selectedCreditId.set(permitDetail.credits[0]?.id ?? null);
      this.commissionForm.patchValue({
        baseAmount: permitDetail.credits[0]?.amount ?? 0
      });
    }
  }

  private defaultPermitStatusId(): number {
    return this.permitStatuses().find((status) => status.statusCode === 'IN_PROCESS')?.id
      ?? this.permitStatuses()[0]?.id
      ?? 0;
  }

  private normalizeOptional(value: string | null | undefined): string | null {
    return value && value.trim().length > 0 ? value.trim() : null;
  }

  private todayIso(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private addDaysIso(days: number): string {
    const date = new Date();
    date.setDate(date.getDate() + days);
    return date.toISOString().slice(0, 10);
  }
}
