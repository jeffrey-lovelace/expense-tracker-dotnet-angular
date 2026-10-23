import { Routes } from '@angular/router';

// Every page is lazy loaded. The login, register and expenses routes arrive with their features (F2, F3).
export const routes: Routes = [
  {
    path: '**',
    title: 'Not found | Expense Tracker',
    loadComponent: () => import('./pages/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
