import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Book, CreateBookRequest, UpdateBookRequest } from '../models/book.model';

@Injectable({ providedIn: 'root' })
export class BooksService {
  private baseUrl = `${environment.apiUrl}/books`;

  constructor(private http: HttpClient) {}

  getBooks(): Observable<Book[]> {
    return this.http.get<Book[]>(this.baseUrl);
  }

  getBook(bookId: string): Observable<Book> {
    return this.http.get<Book>(`${this.baseUrl}/${bookId}`);
  }

  createBook(request: CreateBookRequest): Observable<Book> {
    return this.http.post<Book>(this.baseUrl, request);
  }

  updateBook(bookId: string, request: UpdateBookRequest): Observable<Book> {
    return this.http.put<Book>(`${this.baseUrl}/${bookId}`, request);
  }

  deleteBook(bookId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${bookId}`);
  }

  uploadCoverImage(bookId: string, file: File): Observable<{ coverImageUrl: string }> {
    const formData = new FormData();
    formData.append('cover', file);
    return this.http.post<{ coverImageUrl: string }>(`${this.baseUrl}/${bookId}/cover`, formData);
  }
}
