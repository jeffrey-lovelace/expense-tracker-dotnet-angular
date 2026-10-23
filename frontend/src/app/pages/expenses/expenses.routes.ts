import { Routes } from '@angular/router';

// Loaded with loadChildren from app.routes: none of this code is downloaded before the user signs in.
export const expensesRoutes: Routes = [
  {
    path: '',
    title: 'Expenses | Expense Tracker',
    loadComponent: () => import('./expenses-page').then((m) => m.ExpensesPage),
  },
];
