import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface StudioItemDto {
  id: string; name: string; summary: string; details: string;
  motivation?: string; plot?: string; imageData?: string; image?: string;
  eventDate?: string; impact?: string; relatedCharacterIds?: string; relatedObjectIds?: string; relatedPlaceIds?: string;
  characterIds?: string; objectIds?: string; placeIds?: string;
  goalTarget?: number; goalProgress?: number;
}
export interface RelationshipDto { id: string; from: string; to: string; label: string; tone: 'positive' | 'neutral' | 'negative'; }
export interface StudioMetrics {
  enabled: boolean; characters: number; places: number; objects: number; gallery: number; relationships: number;
  completeness: number; storyCompleteness: number; characterCoverage: number; relationshipCoverage: number;
  timelineCompleteness: number; orphanCharacters: number; emptyLocations: number; unusedObjects: number;
}

@Injectable({ providedIn: 'root' })
export class StoryStudioService {
  private http = inject(HttpClient);
  private base(bookId: string): string { return `${environment.apiUrl}/books/${bookId}/studio`; }
  list(bookId: string, type: string): Observable<StudioItemDto[]> { return this.http.get<StudioItemDto[]>(`${this.base(bookId)}/${type}`); }
  create(bookId: string, type: string, value: Partial<StudioItemDto>): Observable<StudioItemDto> { return this.http.post<StudioItemDto>(`${this.base(bookId)}/${type}`, value); }
  delete(bookId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/entries/${id}`); }
  relationships(bookId: string): Observable<RelationshipDto[]> { return this.http.get<RelationshipDto[]>(`${this.base(bookId)}/relationships`); }
  createRelationship(bookId: string, value: Partial<RelationshipDto>): Observable<RelationshipDto> { return this.http.post<RelationshipDto>(`${this.base(bookId)}/relationships`, value); }
  deleteRelationship(bookId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/relationships/${id}`); }
  reorderTimeline(bookId: string, ids: string[]): Observable<void> { return this.http.put<void>(`${this.base(bookId)}/timeline/order`, { ids }); }
  updateGoalProgress(bookId: string, id: string, progress: number): Observable<StudioItemDto> { return this.http.patch<StudioItemDto>(`${this.base(bookId)}/goals/${id}/progress`, { progress }); }
  metrics(bookId: string): Observable<StudioMetrics> { return this.http.get<StudioMetrics>(`${this.base(bookId)}/metrics`); }
  setMetricsEnabled(enabled: boolean): Observable<void> { return this.http.put<void>(`${environment.apiUrl}/privacy/story-metrics`, { enabled }); }
}
