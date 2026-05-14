import { Component, Input, computed } from '@angular/core';

@Component({
  selector: 'app-status-indicator',
  standalone: true,
  template: `
    <div class="status-pill" [class]="bgClass()">
      <span class="status-dot">
        <span class="status-dot-ring" [class]="pingClass()"></span>
        <span class="status-dot-center" [class]="dotClass()"></span>
      </span>
      {{ label() }}
    </div>
  `,
})
export class StatusIndicatorComponent {
  @Input() status!: 'Free' | 'BusySoon' | 'Occupied';

  label = computed(() => ({ Free: 'Disponible', BusySoon: 'Próximamente', Occupied: 'Reservado' } as Record<string,string>)[this.status] ?? this.status);

  bgClass = computed(() => ({
    Free: 'bg-primary-20 text-primary border-primary',
    BusySoon: 'bg-warning-10 text-warning border-warning',
    Occupied: 'bg-error-20 text-error border-error',
  } as Record<string,string>)[this.status] ?? '');

  dotClass = computed(() => ({
    Free: 'bg-primary shadow-primary',
    BusySoon: 'bg-warning shadow-warning',
    Occupied: 'bg-error shadow-error',
  } as Record<string,string>)[this.status] ?? '');

  pingClass = computed(() => ({ Free: 'bg-primary', BusySoon: 'bg-warning', Occupied: 'bg-error' } as Record<string,string>)[this.status] ?? '');
}
