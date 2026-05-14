import { Component, signal, computed, inject, OnInit } from '@angular/core';
import { RoomsService } from '../../../core/services/rooms.service';
import { Room } from '../../../core/models/room.model';
import { LocalMeeting, RoomStatus, MeetingEvent } from '../../../core/models/room-status.model';
import { FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';

@Component({
  selector: 'app-meetings-page',
  standalone: true,
  imports: [FormsModule, DatePipe],
  template: `
    <div class="p-8">
      <div class="page-header">
        <div>
          <h2 class="text-headline-sm">Reuniones</h2>
          <p class="text-body text-secondary mt-1">Gestiona reuniones locales con el mismo sistema del kiosko</p>
        </div>
        <button (click)="openBookingModal()" class="btn btn-primary shadow-primary">
          <span class="material-symbols-outlined">add_circle</span> Nueva Reunión
        </button>
      </div>

      <div class="flex flex-col gap">
        @if (meetings().length === 0) {
          <div class="empty-state">
            <div class="empty-state-icon"><span class="material-symbols-outlined text-secondary" style="font-size:32px;">event_busy</span></div>
            <p class="text-body-lg text-secondary">Sin reuniones locales</p>
          </div>
        }
        @for (m of meetings(); track m.id) {
          <div class="flex items-center justify-between p-5 card-section" style="padding:20px 24px;">
            <div class="flex items-center gap min-w-0">
              <div class="avatar-default rounded-xl flex items-center justify-center flex-shrink-0" style="width:48px;height:48px;background:var(--primary);opacity:0.15;color:var(--primary);">
                <span class="material-symbols-outlined" style="font-size:28px;">event</span>
              </div>
              <div class="min-w-0">
                <div class="flex items-center gap-sm">
                  <span class="text-body-sm font-bold tracking-wider uppercase text-primary" style="font-family:var(--font-display);">{{ m.roomId }}</span>
                  <p class="text-body-lg font-semibold text-truncate">{{ m.title }}</p>
                </div>
                <p class="text-body-xs text-secondary mt-1">
                  {{ m.start | date:'dd/MM HH:mm' }} – {{ m.end | date:'HH:mm' }} &middot; {{ m.organizer }}
                </p>
              </div>
            </div>
            <button (click)="remove(m.id)" class="btn-danger-ghost"><span class="material-symbols-outlined" style="font-size:20px;">delete</span></button>
          </div>
        }
      </div>
    </div>

    @if (showBookingModal) {
      <div class="modal-backdrop" (click)="closeBookingModal()">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div class="flex items-center gap">
              <span class="material-symbols-outlined fill text-primary" style="font-size:40px;">edit_calendar</span>
              <h2 class="text-headline-sm">Nueva Reunión</h2>
            </div>
            <button (click)="closeBookingModal()" class="btn-icon-sm">
              <span class="material-symbols-outlined text-secondary" style="font-size:32px;">close</span>
            </button>
          </div>

          <div class="flex flex-col gap-xl">
            <div>
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">meeting_room</span> Sala</label>
              <div class="select-wrapper">
                <select [(ngModel)]="selectedRoomId" class="form-input">
                  <option value="" class="bg-surface-high">Seleccionar sala...</option>
                  @for (room of rooms(); track room.id) {
                    <option [value]="room.id" class="bg-surface-high">{{ room.id }} — {{ room.name }} ({{ room.capacity }} personas)</option>
                  }
                </select>
                <span class="material-symbols-outlined">unfold_more</span>
              </div>
              @if (selectedRoom()) {
                <p class="text-body-xs text-secondary mt-1">Proveedor: {{ selectedRoom()!.provider }} &middot; {{ selectedRoom()!.capacity }} personas</p>
              }
            </div>

            <div>
              <p class="text-caption text-secondary mb-2">Duración</p>
              <div class="duration-selector">
                <button (click)="adjustDuration(-5)" [disabled]="bookingDuration <= 5" class="btn-icon"><span class="material-symbols-outlined" style="font-size:32px;">remove</span></button>
                <div class="duration-value"><span style="font-family:var(--font-display);font-size:56px;line-height:1;font-weight:700;color:var(--primary);" class="tabular-nums">{{ bookingDuration }}</span><span class="text-body-lg text-secondary ml-1">min</span></div>
                <button (click)="adjustDuration(5)" [disabled]="bookingDuration >= 480" class="btn-icon"><span class="material-symbols-outlined" style="font-size:32px;">add</span></button>
              </div>
              <div class="duration-presets">
                @for(p of durationPresets; track p) {
                  <button (click)="bookingDuration=p" [class]="'duration-preset '+(bookingDuration===p?'bg-primary-container text-on-primary':'bg-surface-variant text-secondary')">{{p}} min</button>
                }
              </div>
            </div>

            <div>
              <label class="form-label">Hora de inicio</label>
              <div class="relative">
                <span class="form-input-icon material-symbols-outlined">schedule</span>
                <input type="time" [(ngModel)]="bookingStartTime" class="form-input form-input-pl" style="color-scheme:dark;">
              </div>
            </div>

            <div>
              <label class="form-label">Quién organiza</label>
              <div class="relative">
                <span class="form-input-icon material-symbols-outlined">person</span>
                <input type="text" [(ngModel)]="bookingOrganizer" placeholder="Tu nombre" class="form-input form-input-pl">
              </div>
            </div>

            <div>
              <label class="form-label">Título de la reunión</label>
              <div class="relative">
                <span class="form-input-icon material-symbols-outlined">title</span>
                <input type="text" [(ngModel)]="bookingTitle" placeholder="Ej: Daily Scrum" class="form-input form-input-pl">
              </div>
            </div>

            @if (bookingConflict(); as conflict) {
              <div class="feedback feedback-warning">
                <span class="material-symbols-outlined text-error flex-shrink-0" style="font-size:28px;">warning</span>
                <div>
                  <p class="text-body text-error font-semibold">Conflicto de horario</p>
                  <p class="text-body text-secondary mt-1">{{ conflict.summary }} &middot; {{ conflict.start | date:'HH:mm' }} – {{ conflict.end | date:'HH:mm' }}</p>
                </div>
              </div>
            }

            <div class="flex gap pt-4">
              <button (click)="closeBookingModal()" class="btn btn-ghost flex-1">Cancelar</button>
              <button (click)="confirmBooking()" [disabled]="creating() || !selectedRoomId || !!bookingConflict()" class="btn btn-primary flex-1 shadow-primary">
                @if (creating()) {
                  <span class="material-symbols-outlined animate-spin">progress_activity</span> Creando…
                } @else {
                  <span class="material-symbols-outlined">check</span> Crear Reunión
                }
              </button>
            </div>
            @if (bookingError) {
              <p class="text-body text-error text-center">{{ bookingError }}</p>
            }
          </div>
        </div>
      </div>
    }
  `,
})
export class MeetingsPageComponent implements OnInit {
  private roomsService = inject(RoomsService);

  rooms = signal<Room[]>([]);
  meetings = signal<LocalMeeting[]>([]);
  roomStatuses = signal<Map<string, RoomStatus>>(new Map());

  showBookingModal = false;
  selectedRoomId = '';
  bookingDuration = 30;
  bookingStartTime = '';
  bookingOrganizer = '';
  bookingTitle = '';
  bookingError = '';
  creating = signal(false);
  readonly durationPresets = [15, 30, 45, 60, 90];

  selectedRoom = computed(() => this.rooms().find(r => r.id === this.selectedRoomId));

  bookingConflict = computed(() => {
    if (!this.showBookingModal || !this.selectedRoomId) return null;
    const [h, m] = this.bookingStartTime.split(':').map(Number);
    if (isNaN(h) || isNaN(m)) return null;
    const bs = new Date();
    bs.setHours(h, m, 0, 0);
    const be = new Date(bs.getTime() + this.bookingDuration * 60000);

    const status = this.roomStatuses().get(this.selectedRoomId);
    if (!status?.todaysEvents) return null;

    return status.todaysEvents.find(ev => bs < new Date(ev.end) && be > new Date(ev.start)) ?? null;
  });

  ngOnInit() {
    this.roomsService.loadRooms().subscribe(r => this.rooms.set(r));
    this.loadMeetings();
    this.loadAllStatuses();
  }

  private loadMeetings() {
    this.roomsService.loadMeetings().subscribe(m => this.meetings.set(m));
  }

  private loadAllStatuses() {
    this.roomsService.getAllStatuses().subscribe(statuses => {
      const map = new Map<string, RoomStatus>();
      statuses.forEach(s => map.set(s.roomId, s));
      this.roomStatuses.set(map);
    });
  }

  openBookingModal() {
    const n = new Date();
    n.setMinutes(n.getMinutes() + 1);
    this.bookingStartTime = `${String(n.getHours()).padStart(2, '0')}:${String(n.getMinutes()).padStart(2, '0')}`;
    this.bookingDuration = 30;
    this.bookingOrganizer = '';
    this.bookingTitle = '';
    this.bookingError = '';
    this.selectedRoomId = this.rooms().length === 1 ? this.rooms()[0].id : '';
    this.showBookingModal = true;
    this.loadAllStatuses();
  }

  closeBookingModal() {
    this.showBookingModal = false;
  }

  adjustDuration(d: number) {
    const x = this.bookingDuration + d;
    if (x >= 5 && x <= 480) this.bookingDuration = x;
  }

  confirmBooking() {
    if (!this.selectedRoomId || !this.bookingTitle.trim()) {
      this.bookingError = 'Selecciona una sala y pon un título.';
      return;
    }
    this.bookingError = '';

    const [h, m] = this.bookingStartTime.split(':').map(Number);
    const st = new Date();
    st.setHours(h || 0, m || 0, 0, 0);

    this.creating.set(true);
    this.roomsService.createMeeting({
      roomId: this.selectedRoomId,
      title: this.bookingTitle,
      organizer: this.bookingOrganizer || 'Admin',
      durationMinutes: this.bookingDuration,
      start: st.toISOString(),
    }).subscribe({
      next: () => {
        this.creating.set(false);
        this.showBookingModal = false;
        this.loadMeetings();
        this.loadAllStatuses();
      },
      error: e => {
        this.creating.set(false);
        this.bookingError = e?.error?.error || 'Error al crear la reunión.';
      },
    });
  }

  remove(id: string) {
    this.roomsService.deleteMeeting(id).subscribe(() => {
      this.loadMeetings();
      this.loadAllStatuses();
    });
  }
}
