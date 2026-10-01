import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
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
