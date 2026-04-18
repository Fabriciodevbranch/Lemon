import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Chapter, CreateChapterRequest, UpdateChapterRequest } from '../models/chapter.model';

@Injectable({ providedIn: 'root' })
export class ChaptersService {
  private baseUrl = `${environment.apiUrl}/chapters`;

  constructor(private http: HttpClient) {}

  getChapters(bookId: string): Observable<Chapter[]> {
    return this.http.get<Chapter[]>(this.baseUrl, { params: { bookId } });
  }

  getChapter(bookId: string, chapterId: string): Observable<Chapter> {
    return this.http.get<Chapter>(`${this.baseUrl}/${chapterId}`);
  }

  createChapter(bookId: string, request: CreateChapterRequest): Observable<Chapter> {
    return this.http.post<Chapter>(this.baseUrl, { bookId, ...request });
  }

  updateChapter(bookId: string, chapterId: string, request: UpdateChapterRequest): Observable<Chapter> {
    return this.http.put<Chapter>(`${this.baseUrl}/${chapterId}`, request);
  }

  deleteChapter(bookId: string, chapterId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${chapterId}`);
  }
}
