import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="flex h-screen bg-dim">
      <aside class="sidebar border-r">
        <div class="sidebar-brand border-b">
          <div class="flex items-center gap">
            <div class="rounded-xl bg-primary-container flex items-center justify-center shadow-primary" style="width:40px;height:40px;">
              <span class="material-symbols-outlined fill text-on-primary" style="font-size:24px;">meeting_room</span>
            </div>
            <div>
              <h1 class="text-headline-xs text-truncate">Salas</h1>
              <p class="text-caption text-secondary mt-1">Panel de Administración</p>
            </div>
          </div>
        </div>
        <nav class="sidebar-nav">
          @for (item of navItems; track item.path) {
            <a [routerLink]="item.path" routerLinkActive="active">
              <span class="material-symbols-outlined" style="font-size:24px;">{{ item.icon }}</span>
              <span class="text-body">{{ item.label }}</span>
            </a>
          }
        </nav>
      </aside>
      <main class="flex-1 overflow-y-auto z-10">
        <router-outlet />
      </main>
    </div>
  `,
})
export class AdminComponent {
  navItems = [
    { path: 'dashboard', label: 'Dashboard', icon: 'dashboard' },
    { path: 'rooms', label: 'Salas', icon: 'door_open' },
    { path: 'meetings', label: 'Reuniones', icon: 'event' },
    { path: 'settings', label: 'Configuración', icon: 'settings' },
  ];
}
