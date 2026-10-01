import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LabelDto } from '../../../../core/models/task.models';

@Component({
  selector: 'app-label-manager',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './label-manager.component.html',
  styleUrl: './label-manager.component.css'
})
export class LabelManagerComponent {
  readonly taskLabels = input.required<LabelDto[]>();
  readonly availableLabels = input.required<LabelDto[]>();

  readonly assign = output<string>();
  readonly remove = output<string>();
  readonly create = output<{name: string, colorHex: string}>();

  readonly newLabelName = signal('');
  readonly newLabelColor = signal('#2563eb');

  onAssign(id: string): void {
    if (id) {
      this.assign.emit(id);
    }
  }

  onCreate(): void {
    const name = this.newLabelName().trim();
    const colorHex = this.newLabelColor();
    if (name) {
      this.create.emit({ name, colorHex });
      this.newLabelName.set('');
    }
  }
}
