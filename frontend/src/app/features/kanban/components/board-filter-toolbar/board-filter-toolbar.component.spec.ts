import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BoardFilterToolbarComponent } from './board-filter-toolbar.component';
import { LabelDto, TaskPriority } from '../../../../core/models/task.models';
import { BoardFilterCriteria } from '../../../../core/models/board.models';

describe('BoardFilterToolbarComponent', () => {
  let component: BoardFilterToolbarComponent;
  let fixture: ComponentFixture<BoardFilterToolbarComponent>;

  const mockLabels: LabelDto[] = [
    { id: 'lbl-1', workspaceId: 'ws-1', name: 'Frontend', colorHex: '#3b82f6' },
    { id: 'lbl-2', workspaceId: 'ws-1', name: 'Backend', colorHex: '#10b981' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BoardFilterToolbarComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(BoardFilterToolbarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should create with initial default filter states', () => {
    expect(component).toBeTruthy();
    expect(component.search()).toBe('');
    expect(component.priority()).toBeNull();
    expect(component.labelId()).toBeNull();
    expect(component.hasActiveFilters()).toBe(false);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.reset-btn')).toBeNull();
  });

  it('should render label options when labels input is provided', () => {
    fixture.componentRef.setInput('labels', mockLabels);
    fixture.detectChanges();

    const selectEl = fixture.nativeElement.querySelector('#label-filter') as HTMLSelectElement;
    expect(selectEl).toBeTruthy();
    expect(selectEl.options.length).toBe(3); // "Tutte le etichette" + 2 labels
    expect(selectEl.options[1].textContent).toContain('Frontend');
    expect(selectEl.options[2].textContent).toContain('Backend');
  });

  it('should debounce text search input and emit filterChange after 300ms', () => {
    vi.useFakeTimers();

    let emittedCriteria: BoardFilterCriteria | undefined;
    component.filterChange.subscribe(c => (emittedCriteria = c));

    const inputEl = fixture.nativeElement.querySelector('.search-input') as HTMLInputElement;
    inputEl.value = 'refactor auth';
    inputEl.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.search()).toBe('refactor auth');
    expect(component.hasActiveFilters()).toBe(true);
    // Before 300ms debounce
    expect(emittedCriteria).toBeUndefined();

    // Advance timers by 300ms
    vi.advanceTimersByTime(300);

    expect(emittedCriteria).toEqual({
      search: 'refactor auth',
      priority: null,
      labelId: null
    });
  });

  it('should emit filterChange immediately when priority dropdown changes', () => {
    let emittedCriteria: BoardFilterCriteria | undefined;
    component.filterChange.subscribe(c => (emittedCriteria = c));

    const selectEl = fixture.nativeElement.querySelector('#priority-filter') as HTMLSelectElement;
    selectEl.value = String(TaskPriority.High);
    selectEl.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(component.priority()).toBe(TaskPriority.High);
    expect(component.hasActiveFilters()).toBe(true);
    expect(emittedCriteria).toEqual({
      search: '',
      priority: TaskPriority.High,
      labelId: null
    });
  });

  it('should emit filterChange immediately when label dropdown changes', () => {
    fixture.componentRef.setInput('labels', mockLabels);
    fixture.detectChanges();

    let emittedCriteria: BoardFilterCriteria | undefined;
    component.filterChange.subscribe(c => (emittedCriteria = c));

    const selectEl = fixture.nativeElement.querySelector('#label-filter') as HTMLSelectElement;
    selectEl.value = 'lbl-2';
    selectEl.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(component.labelId()).toBe('lbl-2');
    expect(component.hasActiveFilters()).toBe(true);
    expect(emittedCriteria).toEqual({
      search: '',
      priority: null,
      labelId: 'lbl-2'
    });
  });

  it('should show reset button when filters are active and reset on click', () => {
    let resetEmitted = false;
    let emittedCriteria: BoardFilterCriteria | undefined;

    component.reset.subscribe(() => (resetEmitted = true));
    component.filterChange.subscribe(c => (emittedCriteria = c));

    // Activate a filter
    component.onPriorityChange(TaskPriority.Urgent);
    fixture.detectChanges();

    expect(component.hasActiveFilters()).toBe(true);
    const resetBtn = fixture.nativeElement.querySelector('.reset-btn') as HTMLButtonElement;
    expect(resetBtn).toBeTruthy();

    // Click reset button
    resetBtn.click();
    fixture.detectChanges();

    expect(resetEmitted).toBe(true);
    expect(component.search()).toBe('');
    expect(component.priority()).toBeNull();
    expect(component.labelId()).toBeNull();
    expect(component.hasActiveFilters()).toBe(false);
    expect(fixture.nativeElement.querySelector('.reset-btn')).toBeNull();
    expect(emittedCriteria).toEqual({
      search: '',
      priority: null,
      labelId: null
    });
  });

  it('should clear search input and emit updated filters on onClearSearch', () => {
    let emittedCriteria: BoardFilterCriteria | undefined;
    component.filterChange.subscribe(c => (emittedCriteria = c));

    component.search.set('test query');
    fixture.detectChanges();

    component.onClearSearch();
    fixture.detectChanges();

    expect(component.search()).toBe('');
    expect(emittedCriteria).toEqual({
      search: '',
      priority: null,
      labelId: null
    });
  });
});
