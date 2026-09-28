import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { TaskService } from './task.service';
import { environment } from '../../../environments/environment';
import { TaskDto, TaskPriority } from '../models/task.models';
import { TaskItemStatus } from '../models/board.models';

describe('TaskService', () => {
  let service: TaskService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TaskService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(TaskService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should create task via POST', () => {
    const mockTask: TaskDto = {
      id: 'task-1',
      columnId: 'col-1',
      title: 'Setup CI/CD',
      description: null,
      priority: TaskPriority.High,
      status: TaskItemStatus.Todo,
      position: 1000,
      createdAtUtc: new Date().toISOString()
    };

    service.createTask({ columnId: 'col-1', title: 'Setup CI/CD' }).subscribe(task => {
      expect(task.id).toBe('task-1');
      expect(task.title).toBe('Setup CI/CD');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/tasks`);
    expect(req.request.method).toBe('POST');
    req.flush(mockTask);
  });

  it('should move task via POST /tasks/:id/move', () => {
    const movedTask: TaskDto = {
      id: 'task-1',
      columnId: 'col-2',
      title: 'Setup CI/CD',
      description: null,
      priority: TaskPriority.High,
      status: TaskItemStatus.InProgress,
      position: 1500,
      createdAtUtc: new Date().toISOString()
    };

    service.moveTask('task-1', {
      targetColumnId: 'col-2',
      newPosition: 1500,
      newStatus: TaskItemStatus.InProgress
    }).subscribe(task => {
      expect(task.columnId).toBe('col-2');
      expect(task.position).toBe(1500);
      expect(task.status).toBe(TaskItemStatus.InProgress);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/tasks/task-1/move`);
    expect(req.request.method).toBe('POST');
    req.flush(movedTask);
  });
});
