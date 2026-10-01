import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SubtaskDto } from '../../../../core/models/task.models';

@Component({
  selector: 'app-subtask-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subtask-list.component.html',
  styleUrl: './subtask-list.component.css'
})
export class SubtaskListComponent {
  readonly subtasks = input.required<SubtaskDto[]>();
  readonly toggle = output<string>();
  readonly delete = output<string>();
  readonly add = output<string>();

  readonly newSubtaskTitle = signal('');

  onAdd(): void {
    const title = this.newSubtaskTitle().trim();
    if (title) {
      this.add.emit(title);
      this.newSubtaskTitle.set('');
    }
  }
}
