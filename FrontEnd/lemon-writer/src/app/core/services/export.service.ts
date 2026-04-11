import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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

  downloadEpub(bookId: string, fileName: string): void {
    this.exportEpub(bookId).subscribe(blob => {
      saveAs(blob, `${fileName}.epub`);
    });
  }

  downloadPdf(bookId: string, fileName: string): void {
    this.exportPdf(bookId).subscribe(blob => {
      saveAs(blob, `${fileName}.pdf`);
    });
  }
}
