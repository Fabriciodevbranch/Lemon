import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable, tap } from 'rxjs';
import { saveAs } from 'file-saver';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ExportService {
  constructor(private http: HttpClient) {}

  exportEpub(bookId: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/books/${bookId}/export/epub`, {
      responseType: 'blob'
    });
  }

  exportPdf(bookId: string): Observable<Blob> {
    return this.http.get(`${environment.apiUrl}/books/${bookId}/export/pdf`, {
      responseType: 'blob'
    });
  }

  downloadEpub(bookId: string, fileName: string): Observable<void> {
    return this.exportEpub(bookId).pipe(
      tap(blob => saveAs(blob, `${fileName}.epub`)),
      map(() => undefined)
    );
  }

  downloadPdf(bookId: string, fileName: string): Observable<void> {
    return this.exportPdf(bookId).pipe(
      tap(blob => saveAs(blob, `${fileName}.pdf`)),
      map(() => undefined)
    );
  }
}
