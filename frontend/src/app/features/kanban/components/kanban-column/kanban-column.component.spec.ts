import { ComponentFixture, TestBed } from '@angular/core/testing';
import { KanbanColumnComponent } from './kanban-column.component';
import { BoardColumnDetailDto, TaskItemStatus } from '../../../../core/models/board.models';
import { TaskDto, TaskPriority } from '../../../../core/models/task.models';

describe('KanbanColumnComponent', () => {
  let component: KanbanColumnComponent;
  let fixture: ComponentFixture<KanbanColumnComponent>;

  const mockTasks: TaskDto[] = [
    {
      id: 'task-1',
      columnId: 'col-1',
      title: 'First task',
      description: 'First description',
      status: TaskItemStatus.Todo,
      priority: TaskPriority.Medium,
      position: 1000,
      dueDateUtc: null,
      assigneeId: null,
      createdAtUtc: new Date().toISOString()
    },
    {
      id: 'task-2',
      columnId: 'col-1',
      title: 'Second task',
      description: null,
      status: TaskItemStatus.Todo,
      priority: TaskPriority.High,
      position: 2000,
      dueDateUtc: null,
      assigneeId: null,
      createdAtUtc: new Date().toISOString()
    }
  ];

  const mockColumn: BoardColumnDetailDto = {
    id: 'col-1',
    boardId: 'board-1',
    name: 'To Do Column',
    order: 0,
    targetStatus: TaskItemStatus.Todo,
    tasks: mockTasks
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [KanbanColumnComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(KanbanColumnComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('column', mockColumn);
    fixture.componentRef.setInput('index', 0);
    fixture.componentRef.setInput('totalColumns', 3);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render column title and task count badge correctly', () => {
    const titleEl = fixture.nativeElement.querySelector('.column-title');
    expect(titleEl?.textContent?.trim()).toBe('To Do Column');

    const countEl = fixture.nativeElement.querySelector('.column-count');
    expect(countEl?.textContent?.trim()).toBe('2');
  });

  it('should render task cards for all column tasks', () => {
    const taskCards = fixture.nativeElement.querySelectorAll('app-task-card');
    expect(taskCards.length).toBe(2);
  });

  it('should render empty column placeholder when tasks array is empty', () => {
    const emptyColumn: BoardColumnDetailDto = {
      ...mockColumn,
      tasks: []
    };
    fixture.componentRef.setInput('column', emptyColumn);
    fixture.detectChanges();

    const placeholder = fixture.nativeElement.querySelector('.empty-column-placeholder');
    expect(placeholder).toBeTruthy();
    expect(placeholder.textContent).toContain('Trascina qui i task');
  });

  it('should emit editColumn output when the edit button is clicked', () => {
    const editSpy = vi.fn();
    component.editColumn.subscribe(editSpy);

    const editButton: HTMLButtonElement | null = fixture.nativeElement.querySelector('.btn-edit-col');
    expect(editButton).toBeTruthy();

    editButton?.click();
    expect(editSpy).toHaveBeenCalledTimes(1);
  });

  it('should emit deleteColumn output when delete button is clicked', () => {
    const deleteSpy = vi.fn();
    component.deleteColumn.subscribe(deleteSpy);

    const deleteButton: HTMLButtonElement | null = fixture.nativeElement.querySelector('.btn-delete-col');
    expect(deleteButton).toBeTruthy();

    deleteButton?.click();
    expect(deleteSpy).toHaveBeenCalledTimes(1);
  });

  it('should disable moveLeft button when index is 0', () => {
    fixture.componentRef.setInput('index', 0);
    fixture.detectChanges();

    const moveButtons = fixture.nativeElement.querySelectorAll('.btn-col-action');
    const moveLeftBtn: HTMLButtonElement = moveButtons[0];
    expect(moveLeftBtn.disabled).toBe(true);
  });

  it('should emit moveLeft output when left button is clicked and index > 0', () => {
    fixture.componentRef.setInput('index', 1);
    fixture.detectChanges();

    const moveLeftSpy = vi.fn();
    component.moveLeft.subscribe(moveLeftSpy);

    const moveButtons = fixture.nativeElement.querySelectorAll('.btn-col-action');
    const moveLeftBtn: HTMLButtonElement = moveButtons[0];
    expect(moveLeftBtn.disabled).toBe(false);

    moveLeftBtn.click();
    expect(moveLeftSpy).toHaveBeenCalledTimes(1);
  });

  it('should disable moveRight button when index >= totalColumns - 1', () => {
    fixture.componentRef.setInput('index', 2);
    fixture.componentRef.setInput('totalColumns', 3);
    fixture.detectChanges();

    const moveButtons = fixture.nativeElement.querySelectorAll('.btn-col-action');
    const moveRightBtn: HTMLButtonElement = moveButtons[1];
    expect(moveRightBtn.disabled).toBe(true);
  });

  it('should emit moveRight output when right button is clicked and index < totalColumns - 1', () => {
    fixture.componentRef.setInput('index', 0);
    fixture.componentRef.setInput('totalColumns', 3);
    fixture.detectChanges();

    const moveRightSpy = vi.fn();
    component.moveRight.subscribe(moveRightSpy);

    const moveButtons = fixture.nativeElement.querySelectorAll('.btn-col-action');
    const moveRightBtn: HTMLButtonElement = moveButtons[1];
    expect(moveRightBtn.disabled).toBe(false);

    moveRightBtn.click();
    expect(moveRightSpy).toHaveBeenCalledTimes(1);
  });

  it('should emit addTask output when onQuickAddSubmit is called', () => {
    const addTaskSpy = vi.fn();
    component.addTask.subscribe(addTaskSpy);

    component.onQuickAddSubmit('New Task Title');

    expect(addTaskSpy).toHaveBeenCalledWith({
      columnId: 'col-1',
      title: 'New Task Title'
    });
  });
});
