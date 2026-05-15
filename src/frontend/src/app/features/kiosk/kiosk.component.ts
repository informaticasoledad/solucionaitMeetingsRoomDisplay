import { Component, signal, computed, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { RoomsService } from '../../core/services/rooms.service';
import { RoomStatus } from '../../core/models/room-status.model';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-kiosk',
  standalone: true,
  imports: [DatePipe, FormsModule],
  template: `
    @if (!loading() && !notFound() && status(); as s) {
      <div class="kiosk">
        <div class="kiosk-clock-panel" [class]="clockBgClass(s)">
          <div style="position:absolute;top:48px;left:50%;transform:translateX(-50%);">
            <span class="material-symbols-outlined opacity-40" style="font-size:32px;color:var(--text-secondary);">calendar_month</span>
          </div>

          @if (s.clockMode === 'Analog') {
            <div class="analog-clock" [class]="clockBorderClass(s)">
              <div class="analog-clock-face">
                @for (tick of clockTicks; track tick) {
                  <div [style]="tick.style" [class]="tick.cls"></div>
                }
                <div class="analog-clock-hour" [style]="'transform: translateX(-50%) rotate('+hourAngle()+'deg);'"></div>
                <div class="analog-clock-minute" [style]="'transform: translateX(-50%) rotate('+minuteAngle()+'deg);'"></div>
                <div class="analog-clock-second" [style]="'transform: translateX(-50%) rotate('+secondAngle()+'deg);'"></div>
                <div class="analog-clock-center"></div>
              </div>
            </div>
          } @else {
            <div class="kiosk-clock-time tabular-nums">{{ now() | date:'HH:mm' }}</div>
          }

          <div class="kiosk-clock-date">{{ now() | date:'EEEE d MMMM' }}</div>
          <div class="flex items-center justify-center gap-sm mt-6">
            <span class="status-dot">
              <span class="status-dot-ring" [class]="pingColor(s.status)"></span>
              <span class="status-dot-center" [class]="dotColor(s.status)"></span>
            </span>
            <span [class]="'text-caption tracking-wider ' + dotColor(s.status)">{{ statusLabel(s.status) }}</span>
          </div>
          <div style="position:absolute;bottom:48px;left:50%;transform:translateX(-50%);">
            <button (click)="openAssociateModal()" class="kiosk-config-btn">
              <span class="material-symbols-outlined" style="font-size:24px;">settings</span>
              <span class="text-caption">Configurar</span>
            </button>
          </div>
        </div>

        <div class="kiosk-info-panel">
          @if (s.status === 'Occupied') {
            <div class="flex-1 flex flex-col p-safe justify-center" style="background:rgba(255,60,50,0.04);">
              <header class="flex items-center gap-lg mb-8 pb-6 border-b-error">
                <span class="material-symbols-outlined fill flex-shrink-0" style="font-size:40px;color:var(--text);">meeting_room</span>
                <div><h1 class="text-headline">{{ s.roomName }}</h1></div>
              </header>
              <div class="border-b-error pb-12 mb-12">
                <h2 style="font-family:var(--font-display);font-size:96px;line-height:1;font-weight:700;letter-spacing:-0.03em;">
                  {{ s.currentMeeting!.start | date:'HH:mm' }} - {{ s.currentMeeting!.end | date:'HH:mm' }}
                </h2>
                <div class="flex items-center gap mt-2">
                  <p class="text-headline" style="color:#ff5046;">Reservado</p>
                  <p class="text-body-lg" style="color:var(--text-secondary);">{{ s.currentMeeting!.summary }}</p>
                </div>
              </div>
              <div class="flex justify-between items-end">
                <div>@if(s.nextMeeting){<h3 class="text-body-lg text-primary">{{ s.nextMeeting.start | date:'HH:mm' }} - {{ s.nextMeeting.end | date:'HH:mm' }}</h3><p class="text-label" style="color:var(--text-secondary);">{{ s.nextMeeting.summary }}</p>}@else{<h3 class="text-body-lg text-primary">{{ s.currentMeeting!.end | date:'HH:mm' }} - Resto del día</h3><p class="text-label" style="color:var(--text-secondary);">Disponible</p>}</div>
                <button (click)="openBookingModal()" class="btn-outline-round">Reservar</button>
              </div>
            </div>
          }
          @else if (s.status === 'BusySoon') {
            <div class="flex-1 flex flex-col p-safe justify-center" style="background:rgba(255,200,0,0.04);">
              <header class="flex items-center gap-lg mb-8 pb-6 border-b-warning">
                <span class="material-symbols-outlined flex-shrink-0" style="font-size:40px;">meeting_room</span>
                <div><h1 class="text-headline">{{ s.roomName }}</h1></div>
              </header>
              <div class="flex-1 flex flex-col items-center justify-center text-center gap-lg">
                <p class="text-display-sm">{{ now() | date:'HH:mm' }} - {{ s.nextMeeting!.start | date:'HH:mm' }}</p>
                <div class="bg-warning-20 border border-warning rounded-full px-8 py-4">
                  <h2 class="text-headline" style="color:#ffc800;">Disponible (Próximamente)</h2>
                </div>
                <p class="text-body-lg" style="color:var(--text-secondary);">Reunión empieza en {{ minutesUntilNext() }} min</p>
              </div>
              <div class="flex flex-col items-center gap pt-8 border-t">
                @if (s.nextMeeting) { <p class="text-body" style="color:var(--text-secondary);">{{ s.nextMeeting.start | date:'HH:mm' }} — {{ s.nextMeeting.summary }}</p> }
                <button (click)="openBookingModal()" class="btn-outline-round" style="width:320px;">Reservar</button>
              </div>
            </div>
          }
          @else if (s.status === 'Free') {
            <div class="flex-1 flex flex-col p-safe overflow-hidden" style="background:rgba(74,222,128,0.03);">
              <header class="flex items-center gap-lg pb-6 border-b-primary flex-shrink-0">
                <span class="material-symbols-outlined fill text-primary flex-shrink-0" style="font-size:40px;">meeting_room</span>
                <div><h1 class="text-headline">{{ s.roomName }}</h1></div>
              </header>
              <div class="flex-1 flex mt-4 min-h-0">
                <div class="w-55 flex flex-col justify-center pr-gutter">
                  <div class="border-l-8 border-primary pl-gutter mb-element">
                    <p class="text-headline-lg mb-4">{{ now() | date:'HH:mm' }}@if(s.nextMeeting){ - {{ s.nextMeeting.start | date:'HH:mm' }} }@else{ - Resto del día }</p>
                    <h2 class="text-display-sm text-primary uppercase tracking-wider">Disponible</h2>
                  </div>
                  <div class="mb-gutter mt-auto">@if(s.nextMeeting){<p class="text-body-lg" style="color:var(--text-secondary);">Próxima reunión: <span style="color:var(--text);font-weight:600;">{{ s.nextMeeting.start | date:'HH:mm' }} - {{ s.nextMeeting.summary }}</span></p>}@else{<p class="text-body-lg" style="color:var(--text-secondary);">Libre el resto del día</p>}</div>
                  <button (click)="openBookingModal()" class="btn btn-primary btn-block btn-lg shadow-primary">Reservar</button>
                </div>
                <aside class="w-45 bg-glass rounded-3xl border p-8 flex flex-col overflow-hidden min-h-0">
                  <div class="flex items-center pb-gutter border-b flex-shrink-0">
                    <button class="bg-primary-container text-on-primary px-6 py-3 rounded-xl text-button flex items-center gap-sm"><span class="material-symbols-outlined">today</span> Hoy</button>
                  </div>
                  <div class="timeline">
                    <div class="timeline-current-line"><div class="timeline-current-time">{{ now() | date:'HH:mm' }}</div><div class="timeline-current-bar"></div></div>
                    @for (slot of timelineSlots(); track slot.time + slot.label) {
                      <div class="timeline-row" [class.opacity-70]="slot.isPast">
                        <div class="timeline-time" [class.timeline-time-current]="slot.isCurrent" [class]="slot.isCurrent?'':'text-secondary'">{{ slot.time }}</div>
                        <div [class]="'timeline-slot '+(slot.isFree?'timeline-slot-free':'timeline-slot-event')">
                          <span class="text-body-sm" [class.italic]="slot.isFree" [class]="slot.isFree?'text-secondary':''">{{ slot.label }}</span>
                          @if (slot.event && !slot.isPast) { <p class="text-body-xs text-secondary mt-1">{{ slot.event.start | date:'HH:mm' }} - {{ slot.event.end | date:'HH:mm' }}</p> }
                        </div>
                      </div>
                    }
                  </div>
                </aside>
              </div>
            </div>
          }
        </div>
      </div>
    }
    @else if (loading()) {
      <div class="h-screen w-screen flex items-center justify-center bg-dim">
        <div class="loading"><span class="material-symbols-outlined text-primary animate-spin" style="font-size:48px;">progress_activity</span><p class="text-body-lg text-secondary">Cargando…</p></div>
      </div>
    }
    @else if (notFound()) {
      <div class="h-screen w-screen overflow-hidden flex bg-dim">
        <div class="w-1-4 flex items-center justify-center border-r p-8"><div class="text-center"><div class="text-display-sm mb-2">{{ now() | date:'HH:mm' }}</div><div class="text-body-sm text-secondary">{{ now() | date:'EEEE d MMM' }}</div></div></div>
        <div class="w-3-4 flex items-center justify-center" style="padding:64px;">
          <div class="not-found">
            <div class="not-found-icon"><span class="material-symbols-outlined text-error" style="font-size:56px;">door_back</span></div>
            <h2 class="text-headline-sm mb-3">Sala no encontrada</h2>
            <p class="text-body text-secondary" style="line-height:1.6;">El identificador <span class="text-error text-mono">{{ roomId }}</span> no existe o no está configurado.</p>
            <button (click)="openAssociateModal()" class="btn btn-primary btn-lg shadow-primary mt-8"><span class="material-symbols-outlined">link</span> Asociar sala</button>
          </div>
        </div>
      </div>
    }

    @if (showBookingModal) {
      <div class="modal-backdrop" (click)="closeBookingModal()">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal-header"><div class="flex items-center gap"><span class="material-symbols-outlined fill text-primary" style="font-size:40px;">edit_calendar</span><h2 class="text-headline-sm">Reservar Sala</h2></div><button (click)="closeBookingModal()" class="btn-icon-sm"><span class="material-symbols-outlined text-secondary" style="font-size:32px;">close</span></button></div>
          <div class="flex flex-col gap-xl">
            <div><p class="text-caption text-secondary mb-1">Sala</p><p class="text-body-lg">{{ status()!.roomName }} &middot; {{ status()!.capacity }} personas</p></div>
            <div><p class="text-caption text-secondary mb-2">Duración</p><div class="duration-selector"><button (click)="adjustDuration(-5)" [disabled]="bookingDuration <= 5" class="btn-icon"><span class="material-symbols-outlined" style="font-size:32px;">remove</span></button><div class="duration-value"><span style="font-family:var(--font-display);font-size:56px;line-height:1;font-weight:700;color:var(--primary);" class="tabular-nums">{{ bookingDuration }}</span><span class="text-body-lg text-secondary ml-1">min</span></div><button (click)="adjustDuration(5)" [disabled]="bookingDuration >= 480" class="btn-icon"><span class="material-symbols-outlined" style="font-size:32px;">add</span></button></div><div class="duration-presets">@for(p of durationPresets; track p){<button (click)="bookingDuration=p" [class]="'duration-preset '+(bookingDuration===p?'bg-primary-container text-on-primary':'bg-surface-variant text-secondary')">{{p}} min</button>}</div></div>
             <div><label class="form-label">Fecha y hora de inicio</label><div class="grid grid-cols-2 gap"><div class="relative"><span class="form-input-icon material-symbols-outlined">calendar_today</span><input type="date" [(ngModel)]="bookingDate" class="form-input form-input-pl" style="color-scheme:dark;"></div><div class="relative"><span class="form-input-icon material-symbols-outlined">schedule</span><input type="time" [(ngModel)]="bookingStartTime" class="form-input form-input-pl" style="color-scheme:dark;"></div></div></div>
            <div><label class="form-label">Quién reserva</label><div class="relative"><span class="form-input-icon material-symbols-outlined">person</span><input type="text" [(ngModel)]="bookingOrganizer" placeholder="Tu nombre" class="form-input form-input-pl"></div></div>
            <div><label class="form-label">Título de la reunión</label><div class="relative"><span class="form-input-icon material-symbols-outlined">title</span><input type="text" [(ngModel)]="bookingTitle" placeholder="Ej: Daily Scrum" class="form-input form-input-pl"></div></div>
            @if (bookingConflict(); as conflict) {<div class="feedback feedback-warning"><span class="material-symbols-outlined text-error flex-shrink-0" style="font-size:28px;">warning</span><div><p class="text-body text-error font-semibold">Conflicto de horario</p><p class="text-body text-secondary mt-1">{{ conflict.summary }} &middot; {{ conflict.start | date:'HH:mm' }} – {{ conflict.end | date:'HH:mm' }}</p></div></div>}
            <div class="flex gap pt-4"><button (click)="closeBookingModal()" class="btn btn-ghost flex-1">Cancelar</button><button (click)="confirmBooking()" [disabled]="reserving() || !!bookingConflict()" class="btn btn-primary flex-1 shadow-primary">@if(reserving()){<span class="material-symbols-outlined animate-spin">progress_activity</span> Reservando…}@else{<span class="material-symbols-outlined">check</span> Confirmar Reserva}</button></div>
            @if(bookingError){<p class="text-body text-error text-center">{{ bookingError }}</p>}
          </div>
        </div>
      </div>
    }

    @if (showAssociateModal) {
      <div class="modal-backdrop">
        <div class="modal" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <div class="flex items-center gap">
              <span class="material-symbols-outlined fill text-primary" style="font-size:40px;">link</span>
              <h2 class="text-headline-sm">Asociar sala</h2>
            </div>
            <button (click)="closeAssociateModal()" class="btn-icon-sm">
              <span class="material-symbols-outlined text-secondary" style="font-size:32px;">close</span>
            </button>
          </div>

          @if (associateStep() === 'pin') {
            <div class="flex flex-col gap-xl">
              <p class="text-body text-secondary" style="line-height:1.6;">Introduce el PIN de 6 dígitos para autorizar la asociación de una sala a este kiosko.</p>
              <div>
                <label class="form-label">PIN de administración</label>
                <div class="relative">
                  <span class="form-input-icon material-symbols-outlined">lock</span>
                  <input
                    type="password"
                    [(ngModel)]="pinInput"
                    placeholder="••••••"
                    maxlength="6"
                    inputmode="numeric"
                    pattern="[0-9]*"
                    class="form-input form-input-pl text-center text-headline tracking-widest"
                    (keyup.enter)="submitPin()"
                    style="letter-spacing:0.3em;font-family:var(--font-display);"
                  >
                </div>
              </div>
              @if (pinError()) { <p class="text-body-sm text-error text-center">{{ pinError() }}</p> }
              @if (pinValidating()) {
                <p class="text-body-sm text-secondary text-center flex items-center justify-center gap">
                  <span class="material-symbols-outlined animate-spin">progress_activity</span> Verificando…
                </p>
              }
              <div class="flex gap pt-4">
                <button (click)="closeAssociateModal()" class="btn btn-ghost flex-1">Cancelar</button>
                <button (click)="submitPin()" [disabled]="pinInput.length !== 6 || pinValidating()" class="btn btn-primary flex-1 shadow-primary">
                  <span class="material-symbols-outlined">arrow_forward</span> Continuar
                </button>
              </div>
            </div>
          }

          @if (associateStep() === 'room') {
            <div class="flex flex-col gap-xl">
              <p class="text-body text-secondary" style="line-height:1.6;">Introduce el identificador de la sala que quieres vincular a este kiosko.</p>
              <div>
                <label class="form-label">ID de la sala</label>
                <div class="relative">
                  <span class="form-input-icon material-symbols-outlined">meeting_room</span>
                  <input
                    type="text"
                    [(ngModel)]="roomIdInput"
                    placeholder="Ej: sala-reuniones-1"
                    class="form-input form-input-pl"
                    (keyup.enter)="submitRoomId()"
                  >
                </div>
              </div>
              @if (roomError()) { <p class="text-body-sm text-error text-center">{{ roomError() }}</p> }
              @if (roomValidating()) {
                <p class="text-body-sm text-secondary text-center flex items-center justify-center gap">
                  <span class="material-symbols-outlined animate-spin">progress_activity</span> Verificando sala…
                </p>
              }
              <div class="flex gap pt-4">
                <button (click)="closeAssociateModal()" class="btn btn-ghost flex-1">Cancelar</button>
                <button (click)="submitRoomId()" [disabled]="!roomIdInput.trim() || roomValidating()" class="btn btn-primary flex-1 shadow-primary">
                  <span class="material-symbols-outlined">check</span> Vincular sala
                </button>
              </div>
            </div>
          }

          @if (associateStep() === 'success') {
            <div class="flex flex-col items-center gap-xl py-20">
              <div class="flex items-center justify-center w-24 h-24 rounded-full bg-primary-20">
                <span class="material-symbols-outlined fill text-primary" style="font-size:48px;">check_circle</span>
              </div>
              <div class="text-center">
                <h3 class="text-headline-sm mb-2">Sala vinculada</h3>
                <p class="text-body text-secondary">El kiosko mostrará automáticamente esta sala al abrirse.</p>
              </div>
              <button (click)="closeAssociateModalAndReload()" class="btn btn-primary btn-lg shadow-primary" style="width:280px;">Aceptar</button>
            </div>
          }
        </div>
      </div>
    }
  `,
})
export class KioskComponent implements OnDestroy {
  private route = inject(ActivatedRoute);
  private roomsService = inject(RoomsService);
  status = signal<RoomStatus | null>(null); reserving = signal(false); now = signal(new Date()); notFound = signal(false); loading = signal(true); roomId = '';
  showBookingModal = false; bookingDuration = 30; bookingDate = ''; bookingStartTime = ''; bookingOrganizer = ''; bookingTitle = ''; bookingError = '';
  readonly durationPresets = [15, 30, 45, 60, 90];
  private clockTimer: any; private pollTimer: any;

  showAssociateModal = false;
  associateStep = signal<'pin' | 'room' | 'success'>('pin');
  pinInput = '';
  roomIdInput = '';
  pinError = signal('');
  roomError = signal('');
  pinValidating = signal(false);
  roomValidating = signal(false);
  private validatedPin = '';

  hourAngle = computed(() => ((this.now().getHours() % 12) * 30) + (this.now().getMinutes() * 0.5));
  minuteAngle = computed(() => this.now().getMinutes() * 6);
  secondAngle = computed(() => this.now().getSeconds() * 6);

  readonly clockTicks = (() => {
    const ticks: { style: string; cls: string }[] = [];
    for (let i = 1; i <= 12; i++) {
      const angle = i * 30;
      ticks.push({ style: `transform: rotate(${angle}deg);`, cls: 'analog-clock-tick-major' });
      for (let j = 1; j <= 4; j++) {
        ticks.push({ style: `transform: rotate(${angle + j * 6}deg);`, cls: 'analog-clock-tick' });
      }
    }
    return ticks;
  })();

  minutesUntilNext = computed(() => { const s = this.status(); if (!s?.nextMeeting) return 0; return Math.max(0, Math.ceil((new Date(s.nextMeeting.start).getTime() - this.now().getTime()) / 60000)); });

  bookingConflict = computed(() => {
    const s = this.status(); if (!s || !this.showBookingModal) return null;
    const [h, m] = this.bookingStartTime.split(':').map(Number); if (isNaN(h) || isNaN(m)) return null;
    const bs = parseDate(this.bookingDate, h, m); const be = new Date(bs.getTime() + this.bookingDuration * 60000);
    return s.todaysEvents.find(ev => bs < new Date(ev.end) && be > new Date(ev.start)) ?? null;
  });

  timelineSlots = computed(() => {
    const s = this.status(); if (!s) return [];
    const today = new Date(this.now().getFullYear(), this.now().getMonth(), this.now().getDate());
    const todayEnd = new Date(today.getTime() + 86400000);
    const slots: any[] = []; let cursor = today;
    if (s.todaysEvents.length === 0) { slots.push({ time: pad(today), label: 'Libre todo el día', isCurrent: true, isFree: true, event: null, isPast: false }); return slots; }
    for (const ev of s.todaysEvents) {
      const es = new Date(ev.start), ee = new Date(ev.end);
      if (es > cursor) { const end = es < todayEnd ? es : todayEnd; slots.push({ time: pad(cursor), label: free(cursor,end), isCurrent: this.now()>=cursor&&this.now()<end, isFree:true, event:null, isPast:end<=this.now() }); }
      slots.push({ time: pad(es), label: ev.summary, isCurrent: this.now()>=es&&this.now()<ee, isFree:false, event:ev, isPast:ee<=this.now() });
      cursor = ee > cursor ? ee : cursor;
    }
    if (cursor < todayEnd) slots.push({ time: pad(cursor), label: 'Libre hasta fin del día', isCurrent: this.now()>=cursor, isFree:true, event:null, isPast: cursor <= this.now() });
    return slots;
  });

  constructor() {
    const routeId = this.route.snapshot.paramMap.get('roomId') ?? '';

    if (routeId) {
      this.roomId = routeId;
    } else {
      const stored = localStorage.getItem('kioskRoomId');
      if (stored) {
        this.roomId = stored;
      }
    }

    if (this.roomId) {
      this.load(this.roomId);
    } else {
      this.loading.set(false);
      this.notFound.set(true);
    }

    this.clockTimer = setInterval(() => this.now.set(new Date()), 1000);
  }

  private load(id: string) {
    this.loading.set(true);
    this.notFound.set(false);
    this.roomId = id;
    this.roomsService.getRoomStatus(id).subscribe({
      next: s => {
        this.status.set(s);
        this.loading.set(false);
        clearInterval(this.pollTimer);
        this.pollTimer = setInterval(() => this.load(this.roomId), 30000);
      },
      error: () => {
        this.loading.set(false);
        this.notFound.set(true);
        this.status.set(null);
      }
    });
  }

  statusLabel = (s: string) => ({ Free: 'Disponible', BusySoon: 'Próximamente', Occupied: 'Reservado' } as any)[s] ?? s;
  dotColor = (s: string) => ({ Free: 'bg-primary shadow-primary', BusySoon: 'bg-warning', Occupied: 'bg-error shadow-error' } as any)[s] ?? '';
  pingColor = (s: string) => ({ Free: 'bg-primary', BusySoon: 'bg-warning', Occupied: 'bg-error' } as any)[s] ?? '';

  clockBgClass(s: RoomStatus) {
    const base = 'border-r ';
    if (s.status === 'Occupied') return base + 'kiosk-state-red';
    if (s.status === 'BusySoon') return base + 'kiosk-state-yellow';
    return base + 'kiosk-state-green';
  }
  clockBorderClass(s: RoomStatus) {
    if (s.status === 'Occupied') return 'kiosk-panel-red';
    if (s.status === 'BusySoon') return 'kiosk-panel-yellow';
    return 'kiosk-panel-green';
  }

  openBookingModal() { const n = new Date(); const yd = `${n.getFullYear()}-${String(n.getMonth()+1).padStart(2,'0')}-${String(n.getDate()).padStart(2,'0')}`; this.bookingDate = yd; n.setMinutes(n.getMinutes() + 1); this.bookingStartTime = `${String(n.getHours()).padStart(2,'0')}:${String(n.getMinutes()).padStart(2,'0')}`; this.bookingDuration = 30; this.bookingOrganizer = ''; this.bookingTitle = ''; this.bookingError = ''; this.showBookingModal = true; }
  closeBookingModal() { this.showBookingModal = false; }
  adjustDuration(d: number) { const x = this.bookingDuration + d; if (x >= 5 && x <= 480) this.bookingDuration = x; }

  confirmBooking() {
    const rid = this.status()?.roomId, s = this.status(); if (!rid || !s) return; this.bookingError = '';
    if (s.status === 'Occupied') { this.bookingError = 'La sala está ocupada en este momento.'; return; }
    const [h,m] = this.bookingStartTime.split(':').map(Number); if (isNaN(h) || isNaN(m)) { this.bookingError = 'Hora inválida.'; return; }
    const st = parseDate(this.bookingDate, h, m);
    this.reserving.set(true);
    this.roomsService.quickReserve({ roomId:rid, durationMinutes:this.bookingDuration, title:this.bookingTitle||'', organizerName:this.bookingOrganizer||'', startTime:st.toISOString() })
      .subscribe({ next: () => { this.reserving.set(false); this.showBookingModal = false; this.load(rid); }, error: e => { this.reserving.set(false); this.bookingError = e?.error?.error || 'Error al realizar la reserva.'; } });
  }

  openAssociateModal() {
    this.showAssociateModal = true;
    this.associateStep.set('pin');
    this.pinInput = '';
    this.roomIdInput = '';
    this.pinError.set('');
    this.roomError.set('');
    this.pinValidating.set(false);
    this.roomValidating.set(false);
    this.validatedPin = '';
  }

  closeAssociateModal() {
    this.showAssociateModal = false;
  }

  closeAssociateModalAndReload() {
    this.showAssociateModal = false;
    if (this.roomId) {
      this.load(this.roomId);
    }
  }

  submitPin() {
    if (this.pinInput.length !== 6) return;
    this.pinError.set('');
    this.pinValidating.set(true);

    this.roomsService.validatePin(this.pinInput).subscribe({
      next: () => {
        this.pinValidating.set(false);
        this.validatedPin = this.pinInput;
        this.associateStep.set('room');
        this.pinError.set('');
      },
      error: () => {
        this.pinValidating.set(false);
        this.pinError.set('PIN incorrecto. Inténtalo de nuevo.');
      }
    });
  }

  submitRoomId() {
    const id = this.roomIdInput.trim();
    if (!id || !this.validatedPin) return;

    this.roomError.set('');
    this.roomValidating.set(true);

    this.roomsService.validateRoom(this.validatedPin, id).subscribe({
      next: () => {
        this.roomValidating.set(false);
        localStorage.setItem('kioskRoomId', id);
        this.roomId = id;
        this.associateStep.set('success');
      },
      error: (err) => {
        this.roomValidating.set(false);
        if (err.status === 404) {
          this.roomError.set('No se encontró ninguna sala con ese ID.');
        } else if (err.status === 401) {
          this.roomError.set('PIN no válido. Vuelve a intentarlo.');
          this.associateStep.set('pin');
        } else {
          this.roomError.set('Error al verificar la sala. Inténtalo de nuevo.');
        }
      }
    });
  }

  ngOnDestroy() { clearInterval(this.clockTimer); clearInterval(this.pollTimer); }
}

function pad(d: Date) { return `${String(d.getHours()).padStart(2,'0')}:${String(d.getMinutes()).padStart(2,'0')}`; }
function parseDate(dateStr: string, hours: number, minutes: number) {
  const d = dateStr ? new Date(dateStr + 'T00:00:00') : new Date();
  d.setHours(hours, minutes, 0, 0);
  return d;
}
function free(s: Date, e: Date) { const m = Math.round((e.getTime()-s.getTime())/60000); if(m<=0)return'Disponible'; const h=Math.floor(m/60),min=m%60; return h>0&&min>0?`Libre ${h}h ${min}m`:h>0?`Libre ${h}h`:`Libre ${min} min`; }
