import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { WorkspaceService } from './workspace.service';
import { environment } from '../../../environments/environment';
import { WorkspaceDto } from '../models/workspace.models';

describe('WorkspaceService', () => {
  let service: WorkspaceService;
  let httpTesting: HttpTestingController;

  const mockWorkspaces: WorkspaceDto[] = [
    {
      id: 'ws-1',
      tenantId: 't-1',
      name: 'Engineering',
      slug: 'engineering',
      description: 'Tech dept',
      createdAtUtc: new Date().toISOString()
    }
  ];

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        WorkspaceService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(WorkspaceService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
    localStorage.clear();
  });

  it('should load workspaces and auto-select first one', () => {
    service.loadWorkspaces().subscribe(list => {
      expect(list.length).toBe(1);
      expect(service.workspaces().length).toBe(1);
      expect(service.selectedWorkspace()?.id).toBe('ws-1');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/workspaces`);
    expect(req.request.method).toBe('GET');
    req.flush(mockWorkspaces);
  });

  it('should create a new workspace and select it', () => {
    const newWs: WorkspaceDto = {
      id: 'ws-2',
      tenantId: 't-1',
      name: 'Marketing',
      slug: 'marketing',
      description: null,
      createdAtUtc: new Date().toISOString()
    };

    service.createWorkspace({ name: 'Marketing', slug: 'marketing' }).subscribe(created => {
      expect(created.id).toBe('ws-2');
      expect(service.selectedWorkspace()?.slug).toBe('marketing');
      expect(service.workspaces().some(w => w.id === 'ws-2')).toBe(true);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/workspaces`);
    expect(req.request.method).toBe('POST');
    req.flush(newWs);
  });
});
