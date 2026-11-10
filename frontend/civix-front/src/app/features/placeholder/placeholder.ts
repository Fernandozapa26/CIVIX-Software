import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-placeholder',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="placeholder">
      <h2>{{ title() }}</h2>
      <p>Esta sección está en construcción.</p>
    </section>
  `,
  styles: [
    `.placeholder { padding: 2rem; }
     .placeholder h2 { color: var(--civix-accent); }`,
  ],
})
export class Placeholder {
  protected readonly title = input<string>('Módulo');
}