import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { ShellComponent } from './layout/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/auth/login-page.component').then((m) => m.LoginPageComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'dashboard',
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./pages/dashboard/dashboard-page.component').then((m) => m.DashboardPageComponent),
      },
      {
        path: 'catalog',
        loadComponent: () => import('./pages/catalog/catalog-page.component').then((m) => m.CatalogPageComponent),
      },
      {
        path: 'customers',
        loadComponent: () => import('./pages/customers/customers-page.component').then((m) => m.CustomersPageComponent),
      },
      {
        path: 'sales',
        loadComponent: () => import('./pages/sales/sales-page.component').then((m) => m.SalesPageComponent),
      },
      {
        path: 'purchasing',
        loadComponent: () => import('./pages/purchasing/purchasing-page.component').then((m) => m.PurchasingPageComponent),
      },
      {
        path: 'inventory',
        loadComponent: () => import('./pages/inventory/inventory-page.component').then((m) => m.InventoryPageComponent),
      },
      {
        path: 'billing',
        loadComponent: () => import('./pages/billing/billing-page.component').then((m) => m.BillingPageComponent),
      },
      {
        path: 'reporting',
        loadComponent: () => import('./pages/reporting/reporting-page.component').then((m) => m.ReportingPageComponent),
      },
      {
        path: 'operations',
        loadComponent: () => import('./pages/operations/operations-page.component').then((m) => m.OperationsPageComponent),
      },
      {
        path: 'settings',
        loadComponent: () => import('./pages/settings/settings-page.component').then((m) => m.SettingsPageComponent),
      },
    ],
  },
  {
    path: '**',
    redirectTo: '',
  },
];
