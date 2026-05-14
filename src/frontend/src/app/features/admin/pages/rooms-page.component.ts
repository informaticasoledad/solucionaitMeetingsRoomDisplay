import { Component, signal, computed, inject, OnInit, effect } from '@angular/core';
import { RoomsService } from '../../../core/services/rooms.service';
import { Room } from '../../../core/models/room.model';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-rooms-page',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  template: `
    <div class="p-8">
      <div class="page-header">
        <div>
          <h2 class="text-headline-sm">Salas</h2>
          <p class="text-body text-secondary mt-1">Gestiona las salas de reuniones</p>
        </div>
        <button (click)="toggleForm()" [class]="newRoomBtnClass()">
          @if (showForm) { <span class="material-symbols-outlined">close</span> Cancelar }
          @else { <span class="material-symbols-outlined">add_circle</span> Nueva Sala }
        </button>
      </div>

      @if (showForm) {
        <div class="mb-8 card-section">
          <div class="card-section-header">
            <div class="bg-primary-30 rounded-xl flex items-center justify-center" style="width:40px;height:40px;">
              <span class="material-symbols-outlined text-primary" style="font-size:24px;">{{ editingId ? 'edit' : 'add_home' }}</span>
            </div>
            <h3 class="text-headline-xs">{{ editingId ? 'Editar' : 'Nueva' }} Sala</h3>
          </div>
          <form [formGroup]="form" (ngSubmit)="submit()" class="p-8 grid grid-cols-2 gap-lg">
            <div>
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">tag</span> Código</label>
              <input formControlName="id" placeholder="Ej: SALA01" maxlength="8"
                     class="form-input uppercase" style="text-transform:uppercase;"
                     [readonly]="!!editingId" />
            </div>
            <div>
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">edit</span> Nombre</label>
              <input formControlName="name" placeholder="Ej: Sala Atlántico" class="form-input" />
            </div>
            <div>
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">group</span> Capacidad</label>
              <input type="number" min="1" formControlName="capacity" class="form-input" />
            </div>
            <div>
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">schedule</span> Reloj</label>
              <div class="select-wrapper">
                <select formControlName="clockMode" class="form-input">
                  <option value="Digital" class="bg-surface-high">Digital</option>
                  <option value="Analog" class="bg-surface-high">Analógico</option>
                </select>
                <span class="material-symbols-outlined">unfold_more</span>
              </div>
            </div>
            <div class="col-span-2">
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">cloud_sync</span> Proveedor</label>
              <div class="select-wrapper">
                <select formControlName="provider" (change)="onProviderChange()" class="form-input">
                  <option value="Google" class="bg-surface-high">Google Calendar</option>
                  <option value="Office365" class="bg-surface-high">Microsoft Office 365</option>
                  <option value="Zoho" class="bg-surface-high">Zoho Calendar</option>
                  <option value="Local" class="bg-surface-high">Local (BD)</option>
                </select>
                <span class="material-symbols-outlined">unfold_more</span>
              </div>
            </div>
            <div class="col-span-2">
              <label class="form-label"><span class="material-symbols-outlined" style="font-size:16px;">calendar_month</span> Calendar ID</label>
              @if (isLocalProvider()) {
                <input formControlName="calendarId" class="form-input opacity-50"
                       style="cursor:not-allowed;" readonly
                       placeholder="Usa el código de sala automáticamente" />
                <p class="text-body-xs text-secondary mt-1">Para salas locales se usa el código de sala como Calendar ID automáticamente.</p>
              } @else {
                <input formControlName="calendarId" placeholder="ej: sala.atlantico@miempresa.com" class="form-input" />
              }
            </div>
            <div class="col-span-2 flex gap pt-2">
              <button type="button" (click)="toggleForm()" class="btn btn-ghost">Cancelar</button>
              <button type="submit" [disabled]="form.invalid" class="btn btn-primary flex-1 shadow-primary">Guardar</button>
            </div>
          </form>
        </div>
      }

      <div class="flex flex-col gap">
        @if (rooms().length === 0 && !showForm) {
          <div class="empty-state">
            <div class="empty-state-icon">
              <span class="material-symbols-outlined text-secondary" style="font-size:32px;">door_back</span>
            </div>
            <p class="text-body-lg text-secondary">No hay salas aún</p>
            <p class="text-body-sm text-secondary mt-1">Crea tu primera sala para empezar</p>
          </div>
        }
        @for (room of rooms(); track room.id) {
          <div class="flex items-center justify-between p-5 card-section" style="padding:20px 24px;">
            <div class="flex items-center gap min-w-0">
              <div [class]="avatarClass(room.provider)" style="position:relative;">
                <span class="material-symbols-outlined" style="font-size:28px;">meeting_room</span>
                <span class="absolute" style="bottom:-4px;right:-6px;font-family:var(--font-display);font-size:10px;font-weight:700;background:var(--surface);padding:1px 5px;border-radius:4px;letter-spacing:.05em;color:var(--text);border:1px solid var(--outline-variant);">
                  {{ room.id }}
                </span>
              </div>
              <div class="min-w-0">
                <div class="flex items-center gap-sm">
                  <span class="text-body-sm font-bold tracking-wider uppercase" style="color:var(--primary);font-family:var(--font-display);">{{ room.id }}</span>
                  <p class="text-body-lg font-semibold text-truncate">{{ room.name }}</p>
                  <span [class]="badgeClass(room.provider)">{{ room.provider }}</span>
                  <span class="badge" [class]="room.clockMode === 'Analog' ? 'badge-blue' : 'badge-default'">{{ room.clockMode }}</span>
                </div>
                <p class="text-body-xs text-secondary mt-1">
                  <span class="material-symbols-outlined" style="font-size:14px;">group</span>
                  {{ room.capacity }} personas &middot;
                  <span class="material-symbols-outlined" style="font-size:14px;">calendar_month</span>
                  {{ room.calendarId }}
                </p>
              </div>
            </div>
            <div class="flex items-center gap-sm flex-shrink-0 ml-4">
              <a [routerLink]="['/kiosk', room.id]" class="flex items-center gap-sm px-4 py-3 rounded-xl text-body-xs font-semibold text-primary bg-primary-10 border border-primary" style="display:inline-flex;">
                <span class="material-symbols-outlined" style="font-size:18px;">visibility</span> Kiosko
              </a>
              <button (click)="edit(room)" class="btn-ghost-sm">Editar</button>
              <button (click)="remove(room.id)" class="btn-danger-ghost"><span class="material-symbols-outlined" style="font-size:20px;">delete</span></button>
            </div>
          </div>
        }
      </div>
    </div>
  `,
})
export class RoomsPageComponent implements OnInit {
  private roomsService = inject(RoomsService);
  private fb = inject(FormBuilder);
  rooms = signal<Room[]>([]);
  showForm = false;
  editingId: string | null = null;
  selectedProvider = signal('Google');
  isLocalProvider = computed(() => this.selectedProvider() === 'Local');

  form: FormGroup = this.fb.group({
    id: ['', [Validators.required, Validators.maxLength(8), Validators.pattern(/^[A-Za-z0-9]+$/)]],
    name: ['', Validators.required],
    capacity: [1, [Validators.required, Validators.min(1)]],
    clockMode: ['Digital', Validators.required],
    provider: ['Google', Validators.required],
    calendarId: [''],
  });

  ngOnInit() {
    this.load();
    const p = this.form.get('provider')!;
    p.valueChanges.subscribe(v => { this.selectedProvider.set(v); this.onProviderChange(); });
    p.updateValueAndValidity();
  }

  newRoomBtnClass() { return this.showForm ? 'btn btn-ghost' : 'btn btn-primary shadow-primary'; }

  toggleForm() {
    this.showForm = !this.showForm;
    if (!this.showForm) {
      this.editingId = null;
      this.form.reset({ id: '', name: '', capacity: 1, clockMode: 'Digital', provider: 'Google', calendarId: '' });
      this.updateCalendarIdValidator();
    } else {
      this.suggestRoomId();
    }
  }

  suggestRoomId() {
    const existing = this.rooms().map(r => r.id);
    let next = 1;
    while (existing.includes(`SALA${String(next).padStart(2, '0')}`)) {
      next++;
    }
    const suggested = `SALA${String(next).padStart(2, '0')}`;
    this.form.patchValue({ id: suggested });
  }

  onProviderChange() {
    if (this.isLocalProvider()) {
      const code = this.form.get('id')?.value || '';
      this.form.patchValue({ calendarId: code });
    }
    this.updateCalendarIdValidator();
  }

  updateCalendarIdValidator() {
    const ctrl = this.form.get('calendarId')!;
    if (this.isLocalProvider()) {
      ctrl.clearValidators();
      const code = this.form.get('id')?.value || '';
      ctrl.setValue(code, { emitEvent: false });
    } else {
      ctrl.setValidators(Validators.required);
    }
    ctrl.updateValueAndValidity();
  }

  avatarClass(p: string) { return ({ Google: 'avatar-blue', Office365: 'avatar-orange', Zoho: 'avatar-purple' } as any)[p] ?? 'avatar-default'; }
  badgeClass(p: string) { return ({ Google: 'badge badge-blue', Office365: 'badge badge-orange', Zoho: 'badge badge-purple' } as any)[p] ?? 'badge badge-default'; }

  private load() { this.roomsService.loadRooms().subscribe(r => this.rooms.set(r)); }
  submit() {
    if (this.form.invalid) return;
    const r = this.form.value;
    if (!this.editingId) { r.id = r.id.toUpperCase(); }
    if (this.isLocalProvider()) { r.calendarId = r.id; }
    const o = this.editingId ? this.roomsService.updateRoom(this.editingId, r) : this.roomsService.createRoom(r);
    o.subscribe(() => { this.load(); this.toggleForm(); });
  }
  edit(room: Room) { this.editingId = room.id; this.form.patchValue({ ...room, id: room.id }); this.selectedProvider.set(room.provider); this.showForm = true; this.updateCalendarIdValidator(); }
  remove(id: string) { this.roomsService.deleteRoom(id).subscribe(() => this.load()); }
}
