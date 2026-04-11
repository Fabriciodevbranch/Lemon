import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Snapshot, CreateSnapshotRequest, SnapshotDiff } from '../models/snapshot.model';

@Injectable({ providedIn: 'root' })
export class SnapshotsService {
  private baseUrl = (bookId: string, chapterId: string) =>
    `${environment.apiUrl}/books/${bookId}/chapters/${chapterId}/snapshots`;

  constructor(private http: HttpClient) {}

  getSnapshots(bookId: string, chapterId: string): Observable<Snapshot[]> {
    return this.http.get<Snapshot[]>(this.baseUrl(bookId, chapterId));
  }

  getSnapshot(bookId: string, chapterId: string, snapshotId: string): Observable<Snapshot> {
    return this.http.get<Snapshot>(`${this.baseUrl(bookId, chapterId)}/${snapshotId}`);
  }

  takeSnapshot(bookId: string, chapterId: string, request: CreateSnapshotRequest): Observable<Snapshot> {
    return this.http.post<Snapshot>(this.baseUrl(bookId, chapterId), request);
  }

  restoreToVersion(bookId: string, chapterId: string, snapshotId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl(bookId, chapterId)}/${snapshotId}/restore`, {});
  }

  grabContentFromVersion(bookId: string, chapterId: string, snapshotId: string): Observable<{ content: string }> {
    return this.http.get<{ content: string }>(`${this.baseUrl(bookId, chapterId)}/${snapshotId}/content`);
  }

  compareVersions(bookId: string, chapterId: string, snapshotAId: string, snapshotBId: string): Observable<SnapshotDiff> {
    return this.http.get<SnapshotDiff>(
      `${this.baseUrl(bookId, chapterId)}/compare?a=${snapshotAId}&b=${snapshotBId}`
    );
  }
}
