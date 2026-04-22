import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-section-placeholder-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="placeholder">
      <div class="placeholder-copy">
        <p class="placeholder-stage">{{ stage() }}</p>
        <h2>{{ title() }}</h2>
        <p>{{ description() }}</p>
      </div>

      <div class="placeholder-meta">
        <span class="placeholder-tag">Placeholder</span>
        <span class="placeholder-tag">Sin logica de negocio</span>
        <span class="placeholder-tag">Listo para la siguiente etapa</span>
      </div>
    </section>
  `,
  styles: [
    `
      .placeholder {
        display: grid;
        gap: 1.25rem;
        padding: 1.6rem;
        border-radius: 1.35rem;
        background: rgba(255, 255, 255, 0.78);
        border: 1px solid rgba(29, 45, 42, 0.08);
        box-shadow: 0 16px 30px rgba(32, 44, 41, 0.06);
      }

      .placeholder-stage {
        margin: 0 0 0.5rem;
        letter-spacing: 0.12em;
        text-transform: uppercase;
        font-size: 0.78rem;
        font-weight: 700;
        color: #0f766e;
      }

      .placeholder-copy h2 {
        margin: 0;
        font-size: clamp(1.5rem, 3vw, 2.4rem);
      }

      .placeholder-copy p:last-child {
        margin: 0.85rem 0 0;
        line-height: 1.65;
        color: #4d615c;
      }

      .placeholder-meta {
        display: flex;
        flex-wrap: wrap;
        gap: 0.65rem;
      }

      .placeholder-tag {
        padding: 0.6rem 0.8rem;
        border-radius: 999px;
        background: rgba(15, 118, 110, 0.08);
        color: #17423d;
        font-size: 0.85rem;
        font-weight: 600;
      }
    `
  ]
})
export class SectionPlaceholderPageComponent {
  readonly title = input.required<string>();
  readonly description = input.required<string>();
  readonly stage = input.required<string>();
}
