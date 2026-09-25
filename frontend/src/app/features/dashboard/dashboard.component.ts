import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard-container">
      <div class="page-header">
        <div>
          <h1>Dashboard Panoramica</h1>
          <p class="page-subtitle">Benvenuto in TaskForge, {{ authService.currentUser()?.fullName }}.</p>
        </div>
        <div class="badge badge-success">
          ● Tenant Attivo: {{ authService.currentUser()?.tenantName || 'Standard' }}
        </div>
      </div>

      <!-- Overview Cards Grid -->
      <div class="metrics-grid">
        <div class="metric-card card">
          <div class="metric-label">Organizzazione / Tenant</div>
          <div class="metric-value">{{ authService.currentUser()?.tenantName || 'Azienda' }}</div>
          <div class="metric-subtext">ID: {{ authService.currentUser()?.tenantId }}</div>
        </div>

        <div class="metric-card card">
          <div class="metric-label">Utente Connesso</div>
          <div class="metric-value truncate">{{ authService.currentUser()?.email }}</div>
          <div class="metric-subtext">Ruolo: Amministratore Tenant</div>
        </div>

        <div class="metric-card card">
          <div class="metric-label">Stato Connessione API</div>
          <div class="metric-value">
            <span class="badge" [class.badge-success]="apiStatus() === 'Connesso (Healthy)'" [class.badge-warning]="apiStatus() !== 'Connesso (Healthy)'">
              {{ apiStatus() }}
            </span>
          </div>
          <div class="metric-subtext">Backend Render: PostgreSQL Neon</div>
        </div>
      </div>

      <!-- Quick Action / Next Sprint Banner -->
      <div class="banner card">
        <div class="banner-content">
          <div class="banner-badge">Sprint 1 Completato</div>
          <h3>Architettura Core & Autenticazione Pronta</h3>
          <p>
            Il sistema di autenticazione multi-tenant con token JWT e isolamento logico è ora attivo e funzionante.
            Nei prossimi sprint collegheremo la gestione dei Workspace e la Kanban Board interattiva con Drag & Drop.
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-container {
      max-width: 1100px;
      margin: 0 auto;
      display: flex;
      flex-direction: column;
      gap: 1.5rem;
    }

    .page-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding-bottom: 1rem;
      border-bottom: 1px solid var(--border-color);
    }

    .page-subtitle {
      color: var(--text-secondary);
      font-size: 0.875rem;
      margin-top: 0.25rem;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1rem;
    }

    .metric-card {
      padding: 1.25rem;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .metric-label {
      font-size: 0.8125rem;
      color: var(--text-secondary);
      font-weight: 500;
    }

    .metric-value {
      font-size: 1.25rem;
      font-weight: 600;
      color: var(--text-main);
    }

    .metric-value.truncate {
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
    }

    .metric-subtext {
      font-size: 0.75rem;
      color: var(--text-muted);
      font-family: monospace;
      word-break: break-all;
    }

    .banner {
      padding: 1.75rem;
      background: linear-gradient(to right, #eff6ff, #f8fafc);
      border-color: var(--color-primary-border);
    }

    .banner-badge {
      display: inline-block;
      font-size: 0.75rem;
      font-weight: 600;
      color: var(--color-primary);
      background-color: #dbeafe;
      padding: 0.2rem 0.5rem;
      border-radius: var(--radius-sm);
      margin-bottom: 0.75rem;
    }

    .banner h3 {
      font-size: 1.15rem;
      margin-bottom: 0.5rem;
    }

    .banner p {
      color: var(--text-secondary);
      font-size: 0.875rem;
      line-height: 1.6;
      max-width: 700px;
    }
  `]
})
export class DashboardComponent implements OnInit {
  readonly authService = inject(AuthService);
  private readonly http = inject(HttpClient);

  readonly apiStatus = signal('Verifica in corso...');

  ngOnInit(): void {
    this.http.get('https://taskforge-api-phy5.onrender.com/health', { responseType: 'text' }).subscribe({
      next: (res) => this.apiStatus.set(res.trim() === 'Healthy' ? 'Connesso (Healthy)' : res),
      error: () => this.apiStatus.set('Non raggiungibile')
    });
  }
}
