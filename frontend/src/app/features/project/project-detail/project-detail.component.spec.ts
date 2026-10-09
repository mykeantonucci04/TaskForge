import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router, ActivatedRoute } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';
import { ProjectDetailComponent } from './project-detail.component';
import { ProjectService } from '../../../core/services/project.service';
import { WorkspaceService } from '../../../core/services/workspace.service';
import { ProjectDetailDto, ProjectDto } from '../../../core/models/project.models';

describe('ProjectDetailComponent', () => {
  let component: ProjectDetailComponent;
  let fixture: ComponentFixture<ProjectDetailComponent>;
  let projectService: ProjectService;
  let workspaceService: WorkspaceService;
  let router: Router;

  const mockProjectDetail: ProjectDetailDto = {
    id: 'prj-1',
    tenantId: 't-1',
    workspaceId: 'ws-1',
    name: 'TaskForge Web',
    key: 'TFW',
    description: 'Main Web Platform',
    createdAtUtc: new Date().toISOString(),
    boards: [
      {
        id: 'b-1',
        name: 'Sprint 1',
        createdAtUtc: new Date().toISOString()
      }
    ]
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProjectDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([
          { path: 'projects/:projectId', component: ProjectDetailComponent },
          { path: 'workspaces/:workspaceId', children: [] },
          { path: 'workspaces', children: [] },
          { path: 'boards/:boardId', children: [] }
        ]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) => (key === 'projectId' ? 'prj-1' : null)
              }
            }
          }
        }
      ]
    }).compileComponents();

    projectService = TestBed.inject(ProjectService);
    workspaceService = TestBed.inject(WorkspaceService);
    router = TestBed.inject(Router);

    vi.spyOn(projectService, 'getProjectById').mockReturnValue(of(mockProjectDetail));

    fixture = TestBed.createComponent(ProjectDetailComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load project by id on init', () => {
    expect(component).toBeTruthy();
    expect(component.projectId()).toBe('prj-1');
    expect(projectService.getProjectById).toHaveBeenCalledWith('prj-1');
  });

  describe('Create Board Modal', () => {
    it('should open and close create board modal', () => {
      component.openCreateModal();
      expect(component.isModalOpen()).toBe(true);

      component.closeCreateModal();
      expect(component.isModalOpen()).toBe(false);
    });

    it('should create board and navigate to board view', () => {
      const createSpy = vi.spyOn(projectService, 'createBoard').mockReturnValue(of({
        id: 'b-2',
        tenantId: 't-1',
        projectId: 'prj-1',
        name: 'Sprint 2',
        createdAtUtc: new Date().toISOString()
      }));
      const navSpy = vi.spyOn(router, 'navigate');

      component.form.setValue({ name: 'Sprint 2', createDefaultColumns: true });
      component.onCreateBoard();

      expect(createSpy).toHaveBeenCalledWith('prj-1', { name: 'Sprint 2', createDefaultColumns: true });
      expect(navSpy).toHaveBeenCalledWith(['/boards', 'b-2']);
      expect(component.isModalOpen()).toBe(false);
      expect(component.isSubmitting()).toBe(false);
    });
  });

  describe('Edit Project Modal', () => {
    it('should open edit modal and pre-populate form from currentProject', () => {
      projectService.currentProject.set(mockProjectDetail);

      component.openEditModal();

      expect(component.isEditModalOpen()).toBe(true);
      expect(component.editErrorMessage()).toBeNull();
      expect(component.editForm.value.name).toBe('TaskForge Web');
      expect(component.editForm.value.description).toBe('Main Web Platform');
    });

    it('should close edit modal', () => {
      component.openEditModal();
      expect(component.isEditModalOpen()).toBe(true);

      component.closeEditModal();
      expect(component.isEditModalOpen()).toBe(false);
    });

    it('should not submit if edit form is invalid', () => {
      const updateSpy = vi.spyOn(projectService, 'updateProject');
      component.openEditModal();
      component.editForm.setValue({ name: '', description: '' });

      component.onUpdateProject();

      expect(updateSpy).not.toHaveBeenCalled();
      expect(component.isSubmittingEdit()).toBe(false);
    });

    it('should call updateProject and close modal on success', () => {
      const updatedPrj: ProjectDto = {
        ...mockProjectDetail,
        name: 'TaskForge Web Renamed',
        description: 'Updated Description'
      };
      const updateSpy = vi.spyOn(projectService, 'updateProject').mockReturnValue(of(updatedPrj));

      component.openEditModal();
      component.editForm.setValue({ name: 'TaskForge Web Renamed', description: 'Updated Description' });

      component.onUpdateProject();

      expect(updateSpy).toHaveBeenCalledWith('prj-1', {
        name: 'TaskForge Web Renamed',
        description: 'Updated Description'
      });
      expect(component.isSubmittingEdit()).toBe(false);
      expect(component.isEditModalOpen()).toBe(false);
      expect(component.editErrorMessage()).toBeNull();
    });

    it('should set editErrorMessage on update failure', () => {
      vi.spyOn(projectService, 'updateProject').mockReturnValue(
        throwError(() => ({ error: { detail: 'Project name already in use' } }))
      );

      component.openEditModal();
      component.editForm.setValue({ name: 'Duplicate Name', description: '' });

      component.onUpdateProject();

      expect(component.isSubmittingEdit()).toBe(false);
      expect(component.editErrorMessage()).toBe('Project name already in use');
      expect(component.isEditModalOpen()).toBe(true);
    });
  });

  describe('Delete Project Dialog', () => {
    it('should open and close delete confirm dialog', () => {
      component.openDeleteConfirm();
      expect(component.isDeleteConfirmOpen()).toBe(true);
      expect(component.deleteErrorMessage()).toBeNull();

      component.closeDeleteConfirm();
      expect(component.isDeleteConfirmOpen()).toBe(false);
    });

    it('should delete project and redirect to parent workspace when workspaceId is present', () => {
      projectService.currentProject.set(mockProjectDetail);
      const deleteSpy = vi.spyOn(projectService, 'deleteProject').mockReturnValue(of(true));
      const navSpy = vi.spyOn(router, 'navigate');

      component.openDeleteConfirm();
      component.onConfirmDelete();

      expect(deleteSpy).toHaveBeenCalledWith('prj-1');
      expect(component.isDeleting()).toBe(false);
      expect(component.isDeleteConfirmOpen()).toBe(false);
      expect(navSpy).toHaveBeenCalledWith(['/workspaces', 'ws-1']);
    });

    it('should delete project and redirect to /workspaces when no workspaceId is available', () => {
      projectService.currentProject.set(null);
      workspaceService.selectedWorkspace.set(null);
      const deleteSpy = vi.spyOn(projectService, 'deleteProject').mockReturnValue(of(true));
      const navSpy = vi.spyOn(router, 'navigate');

      component.onConfirmDelete();

      expect(deleteSpy).toHaveBeenCalledWith('prj-1');
      expect(navSpy).toHaveBeenCalledWith(['/workspaces']);
    });

    it('should set deleteErrorMessage on delete failure', () => {
      vi.spyOn(projectService, 'deleteProject').mockReturnValue(
        throwError(() => ({ error: { message: 'Cannot delete project with active sprint' } }))
      );

      component.openDeleteConfirm();
      component.onConfirmDelete();

      expect(component.isDeleting()).toBe(false);
      expect(component.deleteErrorMessage()).toBe('Cannot delete project with active sprint');
    });
  });
});
