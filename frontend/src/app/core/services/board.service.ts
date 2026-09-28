import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { BoardDetailDto, BoardColumnDto, CreateBoardColumnRequest } from '../models/board.models';

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
          this.currentBoard.set(detail);
          this.isLoading.set(false);
        },
        error: () => this.isLoading.set(false)
      })
    );
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
            ]
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
