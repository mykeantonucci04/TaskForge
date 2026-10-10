import {
  Component,
  computed,
  input,
  OnDestroy,
  OnInit,
  output,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { BoardFilterCriteria } from '../../../../core/models/board.models';
import { LabelDto, TaskPriority } from '../../../../core/models/task.models';

@Component({
  selector: 'app-board-filter-toolbar',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './board-filter-toolbar.component.html',
  styleUrl: './board-filter-toolbar.component.css'
})
export class BoardFilterToolbarComponent implements OnInit, OnDestroy {
  readonly labels = input<LabelDto[]>([]);
  readonly filterChange = output<BoardFilterCriteria>();
  readonly reset = output<void>();

  readonly search = signal('');
  readonly priority = signal<TaskPriority | null>(null);
  readonly labelId = signal<string | null>(null);

  readonly hasActiveFilters = computed(
    () => !!this.search().trim() || this.priority() !== null || !!this.labelId()
  );

  readonly TaskPriority = TaskPriority;

  private readonly searchSubject = new Subject<string>();
  private searchSub?: Subscription;

  ngOnInit(): void {
    this.searchSub = this.searchSubject
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(() => {
        this.emitFilters();
      });
  }

  ngOnDestroy(): void {
    this.searchSub?.unsubscribe();
  }

  onSearchInput(eventOrValue: Event | string): void {
    const val =
      typeof eventOrValue === 'string'
        ? eventOrValue
        : (eventOrValue.target as HTMLInputElement).value;
    this.search.set(val);
    this.searchSubject.next(val);
  }

  onClearSearch(): void {
    this.search.set('');
    this.searchSubject.next('');
    this.emitFilters();
  }

  onPriorityChange(eventOrValue: Event | TaskPriority | null): void {
    if (eventOrValue && typeof eventOrValue === 'object' && 'target' in eventOrValue) {
      const val = (eventOrValue.target as HTMLSelectElement).value;
      this.priority.set(val === '' || val === 'null' ? null : (Number(val) as TaskPriority));
    } else {
      this.priority.set((eventOrValue as TaskPriority | null) ?? null);
    }
    this.emitFilters();
  }

  onLabelChange(eventOrValue: Event | string | null): void {
    if (eventOrValue && typeof eventOrValue === 'object' && 'target' in eventOrValue) {
      const val = (eventOrValue.target as HTMLSelectElement).value;
      this.labelId.set(!val || val === 'null' ? null : val);
    } else {
      this.labelId.set((eventOrValue as string | null) ?? null);
    }
    this.emitFilters();
  }

  onReset(): void {
    this.search.set('');
    this.priority.set(null);
    this.labelId.set(null);
    this.reset.emit();
    this.emitFilters();
  }

  private emitFilters(): void {
    this.filterChange.emit({
      search: this.search(),
      priority: this.priority(),
      labelId: this.labelId()
    });
  }
}
