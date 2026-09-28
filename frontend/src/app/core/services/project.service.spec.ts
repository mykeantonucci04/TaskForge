import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ProjectService } from './project.service';
import { environment } from '../../../environments/environment';
import { ProjectDto } from '../models/project.models';

describe('ProjectService', () => {
  let service: ProjectService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProjectService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(ProjectService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('should load projects by workspaceId', () => {
    const mockProjects: ProjectDto[] = [
      {
        id: 'prj-1',
        tenantId: 't-1',
        workspaceId: 'ws-1',
        name: 'TaskForge App',
        key: 'TFA',
        description: null,
        createdAtUtc: new Date().toISOString()
      }
    ];

    service.loadProjectsByWorkspace('ws-1').subscribe(list => {
      expect(list.length).toBe(1);
      expect(service.projects().length).toBe(1);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/workspaces/ws-1/projects`);
    expect(req.request.method).toBe('GET');
    req.flush(mockProjects);
  });

  it('should create a project and append to list', () => {
    const newPrj: ProjectDto = {
      id: 'prj-2',
      tenantId: 't-1',
      workspaceId: 'ws-1',
      name: 'Mobile Client',
      key: 'MC',
      description: null,
      createdAtUtc: new Date().toISOString()
    };

    service.createProject('ws-1', { name: 'Mobile Client', key: 'MC' }).subscribe(created => {
      expect(created.key).toBe('MC');
      expect(service.projects().some(p => p.id === 'prj-2')).toBe(true);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/workspaces/ws-1/projects`);
    expect(req.request.method).toBe('POST');
    req.flush(newPrj);
  });
});
