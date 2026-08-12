import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { ShellComponent } from './shared/layout/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent),
  },
  {
    path: 'forbidden',
    loadComponent: () => import('./features/misc/forbidden.component').then((m) => m.ForbiddenComponent),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: 'dashboard',
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then((m) => m.DashboardComponent),
      },
      {
        path: 'quick-send',
        loadComponent: () => import('./features/sending/quick-send.component').then((m) => m.QuickSendComponent),
      },
      {
        path: 'bulk-send',
        loadComponent: () => import('./features/sending/bulk-send.component').then((m) => m.BulkSendComponent),
      },
      {
        path: 'campaigns',
        loadComponent: () => import('./features/campaigns/campaigns-list.component').then((m) => m.CampaignsListComponent),
      },
      {
        path: 'campaigns/new',
        loadComponent: () => import('./features/campaigns/campaign-form.component').then((m) => m.CampaignFormComponent),
      },
      {
        path: 'campaigns/:id/edit',
        loadComponent: () => import('./features/campaigns/campaign-form.component').then((m) => m.CampaignFormComponent),
      },
      {
        path: 'scheduled-sends',
        loadComponent: () =>
          import('./features/scheduled-sends/scheduled-sends-list.component').then((m) => m.ScheduledSendsListComponent),
      },
      {
        path: 'history',
        loadComponent: () => import('./features/history/history.component').then((m) => m.HistoryComponent),
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports.component').then((m) => m.ReportsComponent),
      },
      {
        path: 'link-clicks/:batchId',
        loadComponent: () => import('./features/link-clicks/link-clicks.component').then((m) => m.LinkClicksComponent),
      },
      {
        path: 'payments/review',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/payments/payments-review.component').then((m) => m.PaymentsReviewComponent),
      },
      {
        path: 'payments',
        loadComponent: () => import('./features/payments/payments.component').then((m) => m.PaymentsComponent),
      },
      {
        path: 'accounts',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/accounts/accounts.component').then((m) => m.AccountsComponent),
      },
      {
        path: 'spam-keywords',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/spam-keywords/spam-keywords.component').then((m) => m.SpamKeywordsComponent),
      },
      {
        path: 'admin-budget',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/admin-budget/admin-budget.component').then((m) => m.AdminBudgetComponent),
      },
      {
        path: 'sending-window',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/sending-window/sending-window.component').then((m) => m.SendingWindowComponent),
      },
      {
        path: 'accounts-logs',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/accounts/accounts-logs.component').then((m) => m.AccountsLogsComponent),
      },
      {
        path: 'customers',
        canActivate: [roleGuard('Account')],
        loadComponent: () => import('./features/customers/customers-list.component').then((m) => m.CustomersListComponent),
      },
      {
        path: 'customers/new',
        canActivate: [roleGuard('Account')],
        loadComponent: () => import('./features/customers/customer-form.component').then((m) => m.CustomerFormComponent),
      },
      {
        path: 'customers/:id/edit',
        canActivate: [roleGuard('Account')],
        loadComponent: () => import('./features/customers/customer-form.component').then((m) => m.CustomerFormComponent),
      },
      {
        path: 'accounts/:id/company-profile',
        canActivate: [roleGuard('Superadmin')],
        loadComponent: () => import('./features/company-profile/company-profile.component').then((m) => m.CompanyProfileComponent),
      },
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
