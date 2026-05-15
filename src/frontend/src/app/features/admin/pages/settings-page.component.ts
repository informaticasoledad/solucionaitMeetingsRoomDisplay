import { Component, signal, inject } from '@angular/core';
import { RoomsService } from '../../../core/services/rooms.service';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-settings-page',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div class="p-8">
      <div class="mb-10">
        <h2 class="text-headline-sm">Configuración</h2>
        <p class="text-body text-secondary mt-1">Gestiona los proveedores de calendario</p>
      </div>
      <div class="max-w-3xl flex flex-col gap-xl">
        <div class="card-section">
          <div class="card-section-header">
            <div class="bg-primary-30 rounded-xl flex items-center justify-center" style="width:40px;height:40px;">
              <span class="material-symbols-outlined text-primary" style="font-size:24px;">key</span>
            </div>
            <h3 class="text-headline-xs">Credenciales</h3>
          </div>
          <div class="p-8 flex flex-col gap-xl">
            <div>
              <label class="form-label">Proveedor</label>
              <div class="provider-grid">
                @for (p of providers; track p.value) {
                  <button (click)="selectProvider(p.value)" [class]="'provider-card ' + (selectedProvider === p.value ? 'selected' : '')">
                    <span class="material-symbols-outlined" style="font-size:32px;" [class]="p.color">{{ p.icon }}</span>
                    <span class="text-body-xs font-semibold" [class]="selectedProvider === p.value ? p.color : 'text-secondary'">{{ p.label }}</span>
                  </button>
                }
              </div>
            </div>
            @if (selectedProvider !== 'Local') {
              <div class="info-box">
                <span class="material-symbols-outlined text-blue flex-shrink-0" style="font-size:20px;">info</span>
                <div class="min-w-0">
                  <p class="text-caption text-secondary mb-2">Formato esperado — {{ selectedProvider }}</p>
                  <pre class="text-body-xs text-secondary font-mono" style="white-space:pre-wrap;line-height:1.6;">{{ exampleJson() }}</pre>
                </div>
              </div>
              <div>
                <label class="form-label">JSON de credenciales</label>
                <textarea #credentials rows="10" class="form-input" placeholder="Pega aquí las credenciales..." style="font-family:monospace;font-size:13px;resize:vertical;"></textarea>
              </div>
              <button (click)="upload(credentials.value)" [disabled]="uploading() || !credentials.value" class="btn btn-primary btn-block shadow-primary">
                @if (uploading()) { <span class="material-symbols-outlined animate-spin">progress_activity</span> Configurando… }
                @else { <span class="material-symbols-outlined">cloud_upload</span> Guardar Credenciales }
              </button>
            } @else {
              <div class="info-box">
                <span class="material-symbols-outlined text-primary flex-shrink-0" style="font-size:20px;">check_circle</span>
                <div class="min-w-0">
                  <p class="text-body text-primary font-semibold mb-1">Proveedor Local Activo</p>
                  <p class="text-body-sm text-secondary">Las reuniones se almacenan en la base de datos local sin necesidad de configuración externa.</p>
                </div>
              </div>
            }
            @if (message()) {
              <div [class]="'feedback ' + (error() ? 'feedback-error' : 'feedback-success')">
                <span class="material-symbols-outlined flex-shrink-0" style="font-size:24px;" [class.text-error]="error()" [class.text-primary]="!error()">{{ error() ? 'error' : 'check_circle' }}</span>
                <p class="text-body" [class.text-error]="error()" [class.text-primary]="!error()">{{ message() }}</p>
              </div>
            }
          </div>
        </div>

        <div class="card-section">
          <div class="card-section-header">
            <div class="bg-warning-10 rounded-xl flex items-center justify-center" style="width:40px;height:40px;">
              <span class="material-symbols-outlined text-warning" style="font-size:24px;">sync</span>
            </div>
            <h3 class="text-headline-xs">Sincronización</h3>
          </div>
          <div class="p-8">
            <p class="text-body text-secondary mb-6">La sincronización automática ocurre cada <span class="font-semibold">60 segundos</span>. Puedes forzar una sincronización manual aquí.</p>
            <button (click)="sync()" [disabled]="syncing()" class="btn-sync">
              @if (syncing()) { <span class="material-symbols-outlined animate-spin">progress_activity</span> Sincronizando… }
              @else { <span class="material-symbols-outlined">sync</span> Sincronizar Ahora }
            </button>
            @if (syncMessage()) {
              <div class="feedback feedback-success mt-4">
                <span class="material-symbols-outlined text-primary flex-shrink-0" style="font-size:24px;">check_circle</span>
                <p class="text-body text-primary">{{ syncMessage() }}</p>
              </div>
            }
          </div>
        </div>
      </div>
    </div>
  `,
})
export class SettingsPageComponent {
  private roomsService = inject(RoomsService);
  selectedProvider = 'Google'; uploading = signal(false); syncing = signal(false); message = signal(''); error = signal(false); syncMessage = signal('');

  providers = [
    { value: 'Google', label: 'Google', icon: 'cloud', color: 'text-blue' },
    { value: 'Office365', label: 'Office 365', icon: 'cloud_sync', color: 'text-orange' },
    { value: 'Zoho', label: 'Zoho', icon: 'cloud_done', color: 'text-purple' },
    { value: 'CalDav', label: 'CalDAV', icon: 'folder_shared', color: 'text-teal' },
    { value: 'Local', label: 'Local', icon: 'database', color: 'text-primary' },
  ];

  private readonly examples: Record<string, string> = {
    Google: `{\n  "type": "service_account",\n  "project_id": "...",\n  "private_key_id": "...",\n  "private_key": "-----BEGIN PRIVATE KEY-----\\n...\\n-----END PRIVATE KEY-----\\n",\n  "client_email": "...@....iam.gserviceaccount.com"\n}`,
    Office365: `{\n  "clientId": "00000000-0000-0000-0000-000000000000",\n  "tenantId": "00000000-0000-0000-0000-000000000000",\n  "clientSecret": "..."\n}`,
    Zoho: `{\n  "clientId": "...",\n  "clientSecret": "...",\n  "refreshToken": "..."\n}`,
    CalDav: `{\n  "url": "https://tudominio.com/remote.php/dav",\n  "username": "admin",\n  "password": "tu-contrasena"\n}`,
    Local: '',
  };

  exampleJson = signal(this.examples['Google']);
  selectProvider(p: string) { this.selectedProvider = p; this.exampleJson.set(this.examples[p] ?? ''); }
  upload(j: string) { this.uploading.set(true); this.message.set(''); this.error.set(false); this.roomsService.setCredentials(j, this.selectedProvider).subscribe({ next: () => { this.message.set(`${this.selectedProvider} configurado correctamente.`); this.uploading.set(false); }, error: () => { this.message.set(`Error al configurar ${this.selectedProvider}.`); this.error.set(true); this.uploading.set(false); } }); }
  sync() { this.syncing.set(true); this.syncMessage.set(''); this.roomsService.syncCalendars().subscribe({ next: () => { this.syncMessage.set('Sincronización completada.'); this.syncing.set(false); setTimeout(() => this.syncMessage.set(''), 4000); }, error: () => this.syncing.set(false) }); }
}
