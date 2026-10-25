import { Routes } from '@angular/router';

import { authGuard } from './auth/auth.guard';

// Lazy at two levels: each area's routes (loadChildren) and each page (loadComponent) are separate chunks, so the
// first load only carries the shell, and the expenses code is fetched after sign-in.
export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'expenses' },
  {
    path: '',
    loadChildren: () => import('./pages/auth.routes').then((m) => m.authRoutes),
  },
  {
    path: 'expenses',
    canActivate: [authGuard],
    loadChildren: () => import('./pages/expenses/expenses.routes').then((m) => m.expensesRoutes),
  },
  {
    path: '**',
    title: 'Not found | Expense Tracker',
    loadComponent: () => import('./pages/not-found/not-found-page').then((m) => m.NotFoundPage),
  },
];
