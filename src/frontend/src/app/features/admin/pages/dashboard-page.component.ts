import { Component, signal, inject, OnDestroy, OnInit } from '@angular/core';
import { RoomsService } from '../../../core/services/rooms.service';
import { RoomStatus } from '../../../core/models/room-status.model';
import { StatusIndicatorComponent } from '../../../shared/components/status-indicator.component';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [StatusIndicatorComponent, DatePipe, RouterLink],
  template: `
    <div class="p-8">
      <div class="page-header">
        <div>
          <h2 class="text-headline-sm">Dashboard</h2>
          <p class="text-body text-secondary mt-1">Estado en tiempo real de todas las salas</p>
        </div>
        <span class="text-body text-secondary">{{ statuses().length }} salas</span>
      </div>

      @if (statuses().length === 0) {
        <div class="empty-state">
          <div class="empty-state-icon">
            <span class="material-symbols-outlined text-secondary" style="font-size:32px;">meeting_room</span>
          </div>
          <h3 class="text-headline-xs mb-2">Sin salas configuradas</h3>
          <p class="text-body text-secondary" style="max-width:400px;">
            Añade salas desde la sección <a routerLink="/admin/rooms" class="text-primary">Salas</a> para comenzar a monitorizarlas.
          </p>
        </div>
      } @else {
        <div class="grid grid-cols-2 xl:grid-cols-3 gap-lg">
          @for (s of statuses(); track s.roomId) {
            <div class="card hover-lift" [class]="cardBorder(s.status)">
              <div class="card-glass-overlay"></div>
              <div class="relative p-0">
                <div class="flex items-start justify-between mb-4">
                  <div class="flex-1 min-w-0">
                    <h3 class="text-headline-xs text-truncate">{{ s.roomName }}</h3>
                    <div class="flex items-center gap-sm mt-2">
                      <span class="material-symbols-outlined text-secondary" style="font-size:18px;">group</span>
                      <span class="text-body text-secondary">{{ s.capacity }} personas</span>
                    </div>
                  </div>
                </div>
                <div class="mb-5"><app-status-indicator [status]="s.status" /></div>
                <div class="flex flex-col gap">
                  @if (s.currentMeeting) {
                    <div class="flex gap p-4 bg-surface rounded-2xl border">
                      <span class="material-symbols-outlined text-error flex-shrink-0" style="font-size:24px;">event_busy</span>
                      <div class="min-w-0">
                        <p class="text-body font-semibold text-truncate">{{ s.currentMeeting.summary }}</p>
                        <p class="text-body-xs text-secondary mt-1">{{ s.currentMeeting.start | date:'HH:mm' }} – {{ s.currentMeeting.end | date:'HH:mm' }}</p>
                      </div>
                    </div>
                  }
                  @if (!s.currentMeeting && s.nextMeeting) {
                    <div class="flex gap p-4 bg-surface rounded-2xl border">
                      <span class="material-symbols-outlined text-warning flex-shrink-0" style="font-size:24px;">upcoming</span>
                      <div class="min-w-0">
                        <p class="text-body font-semibold text-truncate">{{ s.nextMeeting.summary }}</p>
                        <p class="text-body-xs text-secondary mt-1">Próximo: {{ s.nextMeeting.start | date:'HH:mm' }}</p>
                      </div>
                    </div>
                  }
                  @if (!s.currentMeeting && !s.nextMeeting) {
                    <div class="flex gap p-4 bg-surface rounded-2xl border">
                      <span class="material-symbols-outlined text-primary flex-shrink-0" style="font-size:24px;">check_circle</span>
                      <p class="text-body text-secondary">Sin reservas hoy</p>
                    </div>
                  }
                </div>
              </div>
            </div>
          }
        </div>
      }
    </div>
  `,
})
export class DashboardPageComponent implements OnInit, OnDestroy {
  private roomsService = inject(RoomsService);
  statuses = signal<RoomStatus[]>([]);
  private pollTimer: any;

  ngOnInit() { this.load(); this.pollTimer = setInterval(() => this.load(), 30000); }
  private load() { this.roomsService.getAllStatuses().subscribe({ next: (s) => this.statuses.set(s) }); }
  cardBorder(s: string) {
    const m: Record<string,string> = { Free: 'border-primary', BusySoon: 'border-warning', Occupied: 'border-error' };
    return m[s] ?? '';
  }
  ngOnDestroy() { clearInterval(this.pollTimer); }
}
