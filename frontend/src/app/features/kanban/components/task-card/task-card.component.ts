import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskDto, TaskPriority } from '../../../../core/models/task.models';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-card.component.html',
  styleUrl: './task-card.component.css'
})
export class TaskCardComponent {
  readonly task = input.required<TaskDto>();
  readonly openDetail = output<string>();

  getPriorityLabel(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low: return 'Bassa';
      case TaskPriority.Medium: return 'Media';
      case TaskPriority.High: return 'Alta';
      case TaskPriority.Urgent: return 'Urgente';
      default: return 'Normale';
    }
  }

  getPriorityClass(priority: TaskPriority): string {
    switch (priority) {
      case TaskPriority.Low: return 'priority-low';
      case TaskPriority.Medium: return 'priority-medium';
      case TaskPriority.High: return 'priority-high';
      case TaskPriority.Urgent: return 'priority-urgent';
      default: return 'priority-medium';
    }
  }

  formatDue(utcStr: string): string {
    try {
      const d = new Date(utcStr);
      return d.toLocaleDateString();
    } catch {
      return utcStr;
    }
  }
}
