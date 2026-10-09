import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommentListComponent } from './comment-list.component';
import { CommentDto } from '../../../../core/models/task.models';

describe('CommentListComponent', () => {
  let component: CommentListComponent;
  let fixture: ComponentFixture<CommentListComponent>;

  const mockComments: CommentDto[] = [
    {
      id: 'comment-1',
      taskItemId: 'task-1',
      authorId: 'user-1',
      content: 'Prima nota di test',
      createdAtUtc: '2026-10-09T10:00:00.000Z'
    },
    {
      id: 'comment-2',
      taskItemId: 'task-1',
      authorId: 'user-2',
      content: 'Seconda nota di test',
      createdAtUtc: '2026-10-09T11:30:00.000Z'
    }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CommentListComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(CommentListComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('comments', mockComments);
    fixture.detectChanges();
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should render comments list correctly', () => {
    const commentEls = fixture.nativeElement.querySelectorAll('.comment-item');
    expect(commentEls.length).toBe(2);

    const firstContent = commentEls[0].querySelector('.comment-content');
    expect(firstContent?.textContent).toContain('Prima nota di test');

    const secondContent = commentEls[1].querySelector('.comment-content');
    expect(secondContent?.textContent).toContain('Seconda nota di test');

    const authorEls = fixture.nativeElement.querySelectorAll('.comment-author');
    expect(authorEls.length).toBe(2);
    expect(authorEls[0].textContent).toBe('Utente');

    const dateEls = fixture.nativeElement.querySelectorAll('.comment-date');
    expect(dateEls.length).toBe(2);
    expect(dateEls[0].textContent?.length).toBeGreaterThan(0);
  });

  it('should emit delete output with comment ID when delete button is clicked', () => {
    const deleteSpy = vi.fn();
    component.delete.subscribe(deleteSpy);

    const deleteButtons = fixture.nativeElement.querySelectorAll('.btn-delete-comment');
    expect(deleteButtons.length).toBe(2);

    deleteButtons[0].click();
    expect(deleteSpy).toHaveBeenCalledWith('comment-1');

    deleteButtons[1].click();
    expect(deleteSpy).toHaveBeenCalledWith('comment-2');
  });

  it('should emit add output when submitting a comment', () => {
    const addSpy = vi.fn();
    component.add.subscribe(addSpy);

    component.newCommentText.set('Nuovo commento inserito');
    fixture.detectChanges();

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.add-comment-box button');
    expect(submitBtn.disabled).toBe(false);

    submitBtn.click();
    expect(addSpy).toHaveBeenCalledWith('Nuovo commento inserito');
    expect(component.newCommentText()).toBe('');
  });

  it('should not emit add output if textarea is empty or whitespace only', () => {
    const addSpy = vi.fn();
    component.add.subscribe(addSpy);

    component.newCommentText.set('   ');
    component.onAdd();

    expect(addSpy).not.toHaveBeenCalled();
  });

  it('should disable submit button when textarea is empty or whitespace', () => {
    component.newCommentText.set('');
    fixture.detectChanges();
    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.add-comment-box button');
    expect(submitBtn.disabled).toBe(true);

    component.newCommentText.set('   ');
    fixture.detectChanges();
    expect(submitBtn.disabled).toBe(true);
  });

  it('should disable submit button when isSubmitting is true', () => {
    component.newCommentText.set('Commento valido');
    fixture.componentRef.setInput('isSubmitting', true);
    fixture.detectChanges();

    const submitBtn: HTMLButtonElement = fixture.nativeElement.querySelector('.add-comment-box button');
    expect(submitBtn.disabled).toBe(true);
  });

  it('should call onDelete method directly and emit comment ID', () => {
    const deleteSpy = vi.fn();
    component.delete.subscribe(deleteSpy);

    component.onDelete('comment-custom-id');
    expect(deleteSpy).toHaveBeenCalledWith('comment-custom-id');
  });
});
