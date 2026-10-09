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

  it('should update a project and update both projects and currentProject signals', () => {
    const existingProject: ProjectDto = {
      id: 'prj-1',
      tenantId: 't-1',
      workspaceId: 'ws-1',
      name: 'TaskForge App',
      key: 'TFA',
      description: 'Old Description',
      createdAtUtc: new Date().toISOString()
    };

    service.projects.set([existingProject]);
    service.currentProject.set({
      ...existingProject,
      boards: [{ id: 'b-1', name: 'Main Board', createdAtUtc: new Date().toISOString() }]
    });

    const updateReq = { name: 'TaskForge App Updated', description: 'New Description' };
    const updatedProject: ProjectDto = {
      ...existingProject,
      name: 'TaskForge App Updated',
      description: 'New Description'
    };

    service.updateProject('prj-1', updateReq).subscribe(res => {
      expect(res.name).toBe('TaskForge App Updated');
      expect(res.description).toBe('New Description');
      expect(service.projects()[0].name).toBe('TaskForge App Updated');
      expect(service.projects()[0].description).toBe('New Description');
      expect(service.currentProject()?.name).toBe('TaskForge App Updated');
      expect(service.currentProject()?.description).toBe('New Description');
      expect(service.currentProject()?.boards.length).toBe(1);
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/projects/prj-1`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(updateReq);
    req.flush(updatedProject);
  });

  it('should delete a project and remove it from projects signal and reset currentProject if matching', () => {
    const prj1: ProjectDto = {
      id: 'prj-1',
      tenantId: 't-1',
      workspaceId: 'ws-1',
      name: 'TaskForge App',
      key: 'TFA',
      description: null,
      createdAtUtc: new Date().toISOString()
    };
    const prj2: ProjectDto = {
      id: 'prj-2',
      tenantId: 't-1',
      workspaceId: 'ws-1',
      name: 'Mobile App',
      key: 'MA',
      description: null,
      createdAtUtc: new Date().toISOString()
    };

    service.projects.set([prj1, prj2]);
    service.currentProject.set({
      ...prj1,
      boards: []
    });

    service.deleteProject('prj-1').subscribe(res => {
      expect(res).toBe(true);
      expect(service.projects().length).toBe(1);
      expect(service.projects()[0].id).toBe('prj-2');
      expect(service.currentProject()).toBeNull();
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/projects/prj-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(true);
  });

  it('should delete a project and keep currentProject if id does not match', () => {
    const prj1: ProjectDto = {
      id: 'prj-1',
      tenantId: 't-1',
      workspaceId: 'ws-1',
      name: 'TaskForge App',
      key: 'TFA',
      description: null,
      createdAtUtc: new Date().toISOString()
    };
    const prj2: ProjectDto = {
      id: 'prj-2',
      tenantId: 't-1',
      workspaceId: 'ws-1',
      name: 'Mobile App',
      key: 'MA',
      description: null,
      createdAtUtc: new Date().toISOString()
    };

    service.projects.set([prj1, prj2]);
    service.currentProject.set({
      ...prj2,
      boards: []
    });

    service.deleteProject('prj-1').subscribe(res => {
      expect(res).toBe(true);
      expect(service.projects().length).toBe(1);
      expect(service.projects()[0].id).toBe('prj-2');
      expect(service.currentProject()?.id).toBe('prj-2');
    });

    const req = httpTesting.expectOne(`${environment.apiUrl}/projects/prj-1`);
    expect(req.request.method).toBe('DELETE');
    req.flush(true);
  });
});
