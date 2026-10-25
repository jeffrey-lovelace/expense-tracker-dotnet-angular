import { provideNativeDateAdapter } from '@angular/material/core';
import { Routes } from '@angular/router';

// Loaded with loadChildren from app.routes: none of this code is downloaded before the user signs in.
export const expensesRoutes: Routes = [
  {
    path: '',
    // Date pickers work with plain Date objects; iso-date.ts converts them to "yyyy-MM-dd" for the API.
    // Provided here, not at the root, so Material's date code stays out of the first download.
    providers: [provideNativeDateAdapter()],
    title: 'Expenses | Expense Tracker',
    loadComponent: () => import('./expenses-page').then((m) => m.ExpensesPage),
  },
];
