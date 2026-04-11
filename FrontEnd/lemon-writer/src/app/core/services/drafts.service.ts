import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Draft, CreateDraftRequest, UpdateDraftRequest, PublishDraftRequest } from '../models/draft.model';

@Injectable({ providedIn: 'root' })
export class DraftsService {
  private baseUrl = (bookId: string, chapterId: string) =>
    `${environment.apiUrl}/books/${bookId}/chapters/${chapterId}/drafts`;

  constructor(private http: HttpClient) {}

  getDrafts(bookId: string, chapterId: string): Observable<Draft[]> {
    return this.http.get<Draft[]>(this.baseUrl(bookId, chapterId));
  }

  getDraft(bookId: string, chapterId: string, draftId: string): Observable<Draft> {
    return this.http.get<Draft>(`${this.baseUrl(bookId, chapterId)}/${draftId}`);
  }

  createDraft(bookId: string, chapterId: string, request: CreateDraftRequest): Observable<Draft> {
    return this.http.post<Draft>(this.baseUrl(bookId, chapterId), request);
  }

  updateDraft(bookId: string, chapterId: string, draftId: string, request: UpdateDraftRequest): Observable<Draft> {
    return this.http.put<Draft>(`${this.baseUrl(bookId, chapterId)}/${draftId}`, request);
  }

  publishDraft(bookId: string, chapterId: string, draftId: string, request: PublishDraftRequest): Observable<Draft> {
    return this.http.post<Draft>(`${this.baseUrl(bookId, chapterId)}/${draftId}/publish`, request);
  }

  deleteDraft(bookId: string, chapterId: string, draftId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl(bookId, chapterId)}/${draftId}`);
  }
}
