import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (isOpen()) {
      <div class="modal-backdrop" (click)="onBackdropClick($event)">
        <div class="modal-card card" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3>{{ title() }}</h3>
            <button class="btn-close" (click)="close.emit()" title="Chiudi">&times;</button>
          </div>
          <div class="modal-body">
            <ng-content></ng-content>
          </div>
        </div>
      </div>
    }
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background-color: rgba(15, 23, 42, 0.5);
      backdrop-filter: blur(4px);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      padding: 1rem;
    }

    .modal-card {
      width: 100%;
      max-width: 520px;
      max-height: 90vh;
      overflow-y: auto;
      background-color: var(--bg-surface);
      border-radius: var(--radius-lg);
      box-shadow: var(--shadow-lg);
      animation: modalFadeIn 0.15s ease-out;
    }

    .modal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.25rem;
      border-bottom: 1px solid var(--border-color);
    }

    .modal-header h3 {
      font-size: 1.1rem;
      font-weight: 600;
      color: var(--text-main);
    }

    .btn-close {
      background: none;
      border: none;
      font-size: 1.5rem;
      color: var(--text-muted);
      cursor: pointer;
      line-height: 1;
      padding: 0;
      transition: color 0.15s ease;
    }

    .btn-close:hover {
      color: var(--text-main);
    }

    .modal-body {
      padding: 1.25rem;
    }

    @keyframes modalFadeIn {
      from {
        opacity: 0;
        transform: scale(0.96);
      }
      to {
        opacity: 1;
        transform: scale(1);
      }
    }
  `]
})
export class ModalComponent {
  readonly isOpen = input<boolean>(false);
  readonly title = input<string>('');
  readonly close = output<void>();

  onBackdropClick(event: MouseEvent): void {
    this.close.emit();
  }
}
