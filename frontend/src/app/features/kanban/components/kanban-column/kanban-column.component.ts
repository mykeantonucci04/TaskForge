import { Component, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { BoardColumnDetailDto } from '../../../../core/models/board.models';
import { TaskDto } from '../../../../core/models/task.models';
import { TaskCardComponent } from '../task-card/task-card.component';
import { QuickAddTaskComponent } from '../quick-add-task/quick-add-task.component';

@Component({
  selector: 'app-kanban-column',
  standalone: true,
  imports: [CommonModule, DragDropModule, TaskCardComponent, QuickAddTaskComponent],
  templateUrl: './kanban-column.component.html',
  styleUrl: './kanban-column.component.css'
})
export class KanbanColumnComponent {
  readonly column = input.required<BoardColumnDetailDto>();
  readonly index = input.required<number>();
  readonly totalColumns = input.required<number>();
  readonly activeAddColumnId = input<string | null>(null);

  readonly moveLeft = output<void>();
  readonly moveRight = output<void>();
  readonly editColumn = output<void>();
  readonly deleteColumn = output<void>();
  readonly addTask = output<{columnId: string, title: string}>();
  readonly openTask = output<string>();
  readonly taskDrop = output<CdkDragDrop<TaskDto[]>>();
  readonly startQuickAdd = output<string>();
  readonly cancelQuickAdd = output<void>();

  onTaskDrop(event: CdkDragDrop<TaskDto[]>): void {
    this.taskDrop.emit(event);
  }

  onQuickAddSubmit(title: string): void {
    this.addTask.emit({ columnId: this.column().id, title });
  }
}
