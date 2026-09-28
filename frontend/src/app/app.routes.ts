import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { AppShellComponent } from './core/layout/app-shell.component';

export const routes: Routes = [
  // Rotte pubbliche di autenticazione (guest only)
  {
    path: 'auth/login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'auth/register-tenant',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/register-tenant/register-tenant.component').then(m => m.RegisterTenantComponent)
  },

  // Rotte protette (richiedono token JWT) incapsulate nella Shell dell'app
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'workspaces'
      },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'workspaces',
        loadComponent: () => import('./features/workspace/workspace-list/workspace-list.component').then(m => m.WorkspaceListComponent)
      },
      {
        path: 'workspaces/:workspaceId',
        loadComponent: () => import('./features/workspace/workspace-detail/workspace-detail.component').then(m => m.WorkspaceDetailComponent)
      },
      {
        path: 'projects/:projectId',
        loadComponent: () => import('./features/project/project-detail/project-detail.component').then(m => m.ProjectDetailComponent)
      },
      {
        path: 'boards/:boardId',
        loadComponent: () => import('./features/kanban/kanban-board.component').then(m => m.KanbanBoardComponent)
      }
    ]
  },

  // Wildcard fallback
  {
    path: '**',
    redirectTo: ''
  }
];
