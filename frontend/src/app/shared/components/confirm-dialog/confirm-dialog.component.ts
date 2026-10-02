import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModalComponent } from '../modal/modal.component';

@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  imports: [CommonModule, ModalComponent],
  templateUrl: './confirm-dialog.component.html',
  styleUrl: './confirm-dialog.component.css'
})
export class ConfirmDialogComponent {
  readonly isOpen = input<boolean>(false);
  readonly title = input.required<string>();
  readonly message = input.required<string>();
  readonly confirmLabel = input<string>('Conferma');
  readonly cancelLabel = input<string>('Annulla');
  readonly variant = input<'danger' | 'primary'>('primary');
  readonly isLoading = input<boolean>(false);

  readonly confirm = output<void>();
  readonly cancel = output<void>();
}
