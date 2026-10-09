import { Component, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CommentDto } from '../../../../core/models/task.models';

@Component({
  selector: 'app-comment-list',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './comment-list.component.html',
  styleUrl: './comment-list.component.css'
})
export class CommentListComponent {
  readonly comments = input.required<CommentDto[]>();
  readonly isSubmitting = input<boolean>(false);
  readonly add = output<string>();
  readonly delete = output<string>();

  readonly newCommentText = signal('');

  onAdd(): void {
    const text = this.newCommentText().trim();
    if (text) {
      this.add.emit(text);
      this.newCommentText.set('');
    }
  }

  onDelete(commentId: string): void {
    this.delete.emit(commentId);
  }

  formatDate(utcStr: string): string {
    try {
      const d = new Date(utcStr);
      return d.toLocaleDateString() + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return utcStr;
    }
  }
}
