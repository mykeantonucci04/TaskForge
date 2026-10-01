import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-quick-add-task',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './quick-add-task.component.html',
  styleUrl: './quick-add-task.component.css'
})
export class QuickAddTaskComponent {
  readonly isActive = input<boolean>(false);
  readonly submit = output<string>();
  readonly activate = output<void>();
  readonly cancel = output<void>();

  readonly title = signal<string>('');

  onTitleInput(event: Event): void {
    this.title.set((event.target as HTMLInputElement).value);
  }

  onSubmit(): void {
    const val = this.title().trim();
    if (val) {
      this.submit.emit(val);
      this.title.set('');
    }
  }

  onCancel(): void {
    this.title.set('');
    this.cancel.emit();
  }
}
