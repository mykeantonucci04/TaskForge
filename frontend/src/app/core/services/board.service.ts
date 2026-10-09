import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  BoardDetailDto,
  BoardColumnDto,
  BoardDto,
  CreateBoardRequest,
  CreateBoardColumnRequest,
  UpdateBoardColumnRequest,
  ReorderColumnsRequest
} from '../models/board.models';

@Injectable({
  providedIn: 'root'
})
export class BoardService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiUrl;

  readonly currentBoard = signal<BoardDetailDto | null>(null);
  readonly isLoading = signal<boolean>(false);

  getBoardDetails(boardId: string): Observable<BoardDetailDto> {
    this.isLoading.set(true);
    return this.http.get<BoardDetailDto>(`${this.apiBase}/boards/${boardId}`).pipe(
      tap({
        next: (detail) => {
          if (detail?.columns) {
            detail.columns.sort((a, b) => a.order - b.order);
          }
          this.currentBoard.set(detail);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
  }

  getBoardsByProject(projectId: string): Observable<BoardDto[]> {
    return this.http.get<BoardDto[]>(`${this.apiBase}/projects/${projectId}/boards`);
  }

  createBoard(projectId: string, request: CreateBoardRequest): Observable<BoardDto> {
    return this.http.post<BoardDto>(`${this.apiBase}/projects/${projectId}/boards`, request);
  }

  createColumn(boardId: string, request: CreateBoardColumnRequest): Observable<BoardColumnDto> {
    return this.http.post<BoardColumnDto>(`${this.apiBase}/boards/${boardId}/columns`, request).pipe(
      tap((newCol) => {
        const board = this.currentBoard();
        if (board && board.id === boardId) {
          this.currentBoard.set({
            ...board,
            columns: [
              ...board.columns,
              {
                id: newCol.id,
                boardId: newCol.boardId,
                name: newCol.name,
                order: newCol.order,
                targetStatus: newCol.targetStatus,
                tasks: []
              }
            ].sort((a, b) => a.order - b.order)
          });
        }
      })
    );
  }

  updateColumn(columnId: string, request: UpdateBoardColumnRequest): Observable<BoardColumnDto> {
    return this.http.put<BoardColumnDto>(`${this.apiBase}/columns/${columnId}`, request).pipe(
      tap((updatedCol) => {
        const board = this.currentBoard();
        if (board) {
          const updatedColumns = board.columns
            .map((col) => {
              if (col.id === columnId) {
                return {
                  ...col,
                  name: updatedCol.name,
                  order: updatedCol.order,
                  targetStatus: updatedCol.targetStatus,
                  tasks: col.tasks
                };
              }
              return col;
            })
            .sort((a, b) => a.order - b.order);

          this.currentBoard.set({ ...board, columns: updatedColumns });
        }
      })
    );
  }

  reorderColumns(boardId: string, request: ReorderColumnsRequest): Observable<boolean> {
    return this.http.put<boolean>(`${this.apiBase}/boards/${boardId}/columns/reorder`, request).pipe(
      tap(() => {
        const board = this.currentBoard();
        if (board && board.id === boardId) {
          const orderMap = new Map(request.columns.map(c => [c.columnId, c.order]));
          const updatedColumns = board.columns
            .map(col => {
              const newOrder = orderMap.get(col.id);
              return newOrder !== undefined ? { ...col, order: newOrder } : col;
            })
            .sort((a, b) => a.order - b.order);

          this.currentBoard.set({
            ...board,
            columns: updatedColumns
          });
        }
      })
    );
  }

  deleteColumn(columnId: string): Observable<boolean> {
    return this.http.delete<boolean>(`${this.apiBase}/columns/${columnId}`).pipe(
      tap(() => {
        const board = this.currentBoard();
        if (board) {
          this.currentBoard.set({
            ...board,
            columns: board.columns.filter(c => c.id !== columnId)
          });
        }
      })
    );
  }
}

