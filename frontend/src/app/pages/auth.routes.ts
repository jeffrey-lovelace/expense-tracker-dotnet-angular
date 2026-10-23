import { Routes } from '@angular/router';

import { guestGuard } from '../auth/guest.guard';

// Loaded with loadChildren from app.routes. Signed-in users are sent to /expenses by the guest guard.
export const authRoutes: Routes = [
  {
    path: 'login',
    title: 'Sign in | Expense Tracker',
    canActivate: [guestGuard],
    loadComponent: () => import('./login/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create an account | Expense Tracker',
    canActivate: [guestGuard],
    loadComponent: () => import('./register/register-page').then((m) => m.RegisterPage),
  },
];
