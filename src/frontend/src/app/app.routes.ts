import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'kiosk/:roomId',
    loadComponent: () =>
      import('./features/kiosk/kiosk.component').then((m) => m.KioskComponent),
  },
  {
    path: 'kiosk',
    loadComponent: () =>
      import('./features/kiosk/kiosk.component').then((m) => m.KioskComponent),
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/admin.component').then((m) => m.AdminComponent),
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/admin/pages/dashboard-page.component').then(
            (m) => m.DashboardPageComponent,
          ),
      },
      {
        path: 'rooms',
        loadComponent: () =>
          import('./features/admin/pages/rooms-page.component').then(
            (m) => m.RoomsPageComponent,
          ),
      },
      {
        path: 'meetings',
        loadComponent: () =>
          import('./features/admin/pages/meetings-page.component').then(
            (m) => m.MeetingsPageComponent,
          ),
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./features/admin/pages/settings-page.component').then(
            (m) => m.SettingsPageComponent,
          ),
      },
    ],
  },
  { path: '', redirectTo: 'admin', pathMatch: 'full' },
  { path: '**', redirectTo: 'admin' },
];
