import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="shell-layout">
      <!-- Top Navigation Bar -->
      <header class="topbar">
        <div class="topbar-left">
          <div class="brand-logo">
            <span class="logo-icon">TF</span>
            <span class="logo-title">TaskForge</span>
          </div>

          <div class="tenant-badge">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="2" y="7" width="20" height="14" rx="2" ry="2"></rect>
              <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16"></path>
            </svg>
            <span class="tenant-name">{{ authService.currentUser()?.tenantName || 'Tenant Aziendale' }}</span>
          </div>
        </div>

        <div class="topbar-right">
          <div class="user-profile">
            <span class="avatar">
              {{ getUserInitials() }}
            </span>
            <span class="user-name">{{ authService.currentUser()?.fullName }}</span>
          </div>

          <button class="btn btn-secondary btn-sm" (click)="onLogout()" title="Esci dall'account">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"></path>
              <polyline points="16 17 21 12 16 7"></polyline>
              <line x1="21" y1="12" x2="9" y2="12"></line>
            </svg>
            <span>Esci</span>
          </button>
        </div>
      </header>

      <!-- Main Body: Sidebar + View Content -->
      <div class="main-body">
        <aside class="sidebar">
          <nav class="nav-menu">
            <div class="nav-section-title">NAVIGAZIONE</div>
            <a routerLink="/dashboard" routerLinkActive="active" class="nav-item">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="7" height="7"></rect>
                <rect x="14" y="3" width="7" height="7"></rect>
                <rect x="14" y="14" width="7" height="7"></rect>
                <rect x="3" y="14" width="7" height="7"></rect>
              </svg>
              <span>Dashboard</span>
            </a>

            <div class="nav-section-title">PROSSIMI SPRINT</div>
            <span class="nav-item disabled" title="Disponibile in Sprint 2">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <path d="M22 19a2 2 0 0 1-2 2H4a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h5l2 3h9a2 2 0 0 1 2 2z"></path>
              </svg>
              <span>Workspaces & Progetti</span>
              <span class="badge-mini">Sprint 2</span>
            </span>

            <span class="nav-item disabled" title="Disponibile in Sprint 3">
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                <line x1="9" y1="3" x2="9" y2="21"></line>
                <line x1="15" y1="3" x2="15" y2="21"></line>
              </svg>
              <span>Kanban Boards</span>
              <span class="badge-mini">Sprint 3</span>
            </span>
          </nav>

          <div class="sidebar-footer">
            <span class="build-info">TaskForge v1.0.0</span>
          </div>
        </aside>

        <main class="content-area">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styles: [`
    .shell-layout {
      display: flex;
      flex-direction: column;
      height: 100vh;
      overflow: hidden;
      background-color: var(--bg-app);
    }

    .topbar {
      height: 56px;
      background-color: var(--bg-surface);
      border-bottom: 1px solid var(--border-color);
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0 1.25rem;
      z-index: 10;
    }

    .topbar-left {
      display: flex;
      align-items: center;
      gap: 1.25rem;
    }

    .brand-logo {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .logo-icon {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      width: 28px;
      height: 28px;
      background-color: var(--color-primary);
      color: #ffffff;
      font-weight: 700;
      font-size: 0.75rem;
      border-radius: var(--radius-sm);
    }

    .logo-title {
      font-size: 1.1rem;
      font-weight: 700;
      color: var(--text-main);
      letter-spacing: -0.02em;
    }

    .tenant-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.4rem;
      padding: 0.25rem 0.65rem;
      background-color: var(--bg-subtle);
      border: 1px solid var(--border-color);
      border-radius: var(--radius-md);
      font-size: 0.8125rem;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .topbar-right {
      display: flex;
      align-items: center;
      gap: 1rem;
    }

    .user-profile {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.875rem;
      color: var(--text-main);
      font-weight: 500;
    }

    .avatar {
      width: 30px;
      height: 30px;
      border-radius: 50%;
      background-color: var(--color-primary-light);
      color: var(--color-primary);
      display: flex;
      align-items: center;
      justify-content: center;
      font-weight: 600;
      font-size: 0.75rem;
      border: 1px solid var(--color-primary-border);
    }

    .main-body {
      display: flex;
      flex: 1;
      overflow: hidden;
    }

    .sidebar {
      width: 240px;
      background-color: var(--bg-surface);
      border-right: 1px solid var(--border-color);
      display: flex;
      flex-direction: column;
      justify-content: space-between;
      padding: 1rem 0.75rem;
    }

    .nav-section-title {
      font-size: 0.6875rem;
      font-weight: 600;
      color: var(--text-muted);
      letter-spacing: 0.05em;
      padding: 0.5rem 0.65rem 0.25rem;
      margin-top: 0.5rem;
    }

    .nav-section-title:first-child {
      margin-top: 0;
    }

    .nav-item {
      display: flex;
      align-items: center;
      gap: 0.65rem;
      padding: 0.5rem 0.65rem;
      border-radius: var(--radius-md);
      font-size: 0.875rem;
      color: var(--text-secondary);
      text-decoration: none;
      font-weight: 500;
      transition: all 0.15s ease;
      margin-bottom: 0.15rem;
    }

    .nav-item:hover:not(.disabled) {
      background-color: var(--bg-subtle);
      color: var(--text-main);
    }

    .nav-item.active {
      background-color: var(--color-primary-light);
      color: var(--color-primary);
      font-weight: 600;
    }

    .nav-item.disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .badge-mini {
      margin-left: auto;
      font-size: 0.65rem;
      font-weight: 600;
      padding: 0.1rem 0.35rem;
      border-radius: var(--radius-sm);
      background-color: var(--bg-subtle);
      border: 1px solid var(--border-color);
      color: var(--text-muted);
    }

    .sidebar-footer {
      padding: 0.5rem 0.65rem;
      font-size: 0.75rem;
      color: var(--text-muted);
      border-top: 1px solid var(--border-color);
    }

    .content-area {
      flex: 1;
      overflow-y: auto;
      padding: 1.5rem;
    }
  `]
})
export class AppShellComponent {
  readonly authService = inject(AuthService);

  onLogout(): void {
    this.authService.logout();
  }

  getUserInitials(): string {
    const name = this.authService.currentUser()?.fullName || 'U';
    const parts = name.split(' ');
    if (parts.length >= 2) {
      return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    }
    return name.slice(0, 2).toUpperCase();
  }
}
