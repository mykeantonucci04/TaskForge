import { Component, signal, OnInit } from '@angular/core';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected readonly apiStatus = signal<string>('Verifica in corso...');

  ngOnInit(): void {
    this.checkBackendHealth();
  }

  async checkBackendHealth(): Promise<void> {
    this.apiStatus.set('Verifica in corso...');
    try {
      const response = await fetch('https://taskforge-api-phy5.onrender.com/health');
      if (response.ok) {
        const text = await response.text();
        this.apiStatus.set(text.trim() || 'Healthy');
      } else {
        this.apiStatus.set(`Errore HTTP ${response.status}`);
      }
    } catch {
      this.apiStatus.set('Non raggiungibile');
    }
  }
}
