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
  collectionId?: string; collectionName?: string;
  storyRole?: string; characterStatus?: string; age?: string; pronouns?: string; aliases?: string; portraitMediaId?: string;
  externalGoal?: string; internalNeed?: string; fear?: string; secret?: string; internalConflict?: string; externalConflict?: string;
  narrativeFunction?: string; arcSummary?: string; startingState?: string; turningPoint?: string; endingState?: string; notes?: string;
}
export interface RelationshipDto { id: string; from: string; to: string; label: string; tone: string; relationshipType: string; description?: string; status?: string; }
export interface CharacterMediaReferenceDto { id: string; characterId: string; mediaId: string; role: string; displayOrder: number; mediaName: string; imageData?: string; collectionId?: string; collectionName?: string; }
export interface CharacterTimelineReferenceDto { id: string; characterId: string; eventId: string; role: string; note?: string; eventName: string; eventDate?: string; sortOrder: number; legacy: boolean; }
export interface CharacterAttributeDto { id: string; characterId: string; label: string; valueType: string; value: string; groupName?: string; displayOrder: number; options: string[]; }
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
  createGalleryBatch(bookId: string, value: { collectionName?: string; items: Partial<StudioItemDto>[] }): Observable<StudioItemDto[]> {
    return this.http.post<StudioItemDto[]>(`${this.base(bookId)}/gallery/batch`, value);
  }
  updateMetadata(bookId: string, id: string, value: { name: string; summary: string; details: string }): Observable<StudioItemDto> {
    return this.http.put<StudioItemDto>(`${this.base(bookId)}/entries/${id}/metadata`, value);
  }
  updateCharacterProfile(bookId: string, id: string, value: Partial<StudioItemDto>): Observable<StudioItemDto> {
    return this.http.put<StudioItemDto>(`${this.base(bookId)}/characters/${id}/profile`, value);
  }
  delete(bookId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/entries/${id}`); }
  relationships(bookId: string): Observable<RelationshipDto[]> { return this.http.get<RelationshipDto[]>(`${this.base(bookId)}/relationships`); }
  createRelationship(bookId: string, value: Partial<RelationshipDto>): Observable<RelationshipDto> { return this.http.post<RelationshipDto>(`${this.base(bookId)}/relationships`, value); }
  updateRelationship(bookId: string, id: string, value: Partial<RelationshipDto>): Observable<RelationshipDto> { return this.http.put<RelationshipDto>(`${this.base(bookId)}/relationships/${id}`, value); }
  deleteRelationship(bookId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/relationships/${id}`); }
  reorderTimeline(bookId: string, ids: string[]): Observable<void> { return this.http.put<void>(`${this.base(bookId)}/timeline/order`, { ids }); }
  updateGoalProgress(bookId: string, id: string, progress: number): Observable<StudioItemDto> { return this.http.patch<StudioItemDto>(`${this.base(bookId)}/goals/${id}/progress`, { progress }); }
  metrics(bookId: string): Observable<StudioMetrics> { return this.http.get<StudioMetrics>(`${this.base(bookId)}/metrics`); }
  setMetricsEnabled(enabled: boolean): Observable<void> { return this.http.put<void>(`${environment.apiUrl}/privacy/story-metrics`, { enabled }); }
  characterMedia(bookId: string, characterId: string): Observable<CharacterMediaReferenceDto[]> { return this.http.get<CharacterMediaReferenceDto[]>(`${this.base(bookId)}/characters/${characterId}/media`); }
  saveCharacterMedia(bookId: string, characterId: string, value: { mediaId: string; role: string; displayOrder?: number }, id?: string): Observable<CharacterMediaReferenceDto> { return id ? this.http.put<CharacterMediaReferenceDto>(`${this.base(bookId)}/characters/${characterId}/media/${id}`, value) : this.http.post<CharacterMediaReferenceDto>(`${this.base(bookId)}/characters/${characterId}/media`, value); }
  removeCharacterMedia(bookId: string, characterId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/characters/${characterId}/media/${id}`); }
  characterTimeline(bookId: string, characterId: string): Observable<CharacterTimelineReferenceDto[]> { return this.http.get<CharacterTimelineReferenceDto[]>(`${this.base(bookId)}/characters/${characterId}/timeline`); }
  saveCharacterTimeline(bookId: string, characterId: string, value: { eventId: string; role: string; note?: string }, id?: string): Observable<CharacterTimelineReferenceDto> { return id ? this.http.put<CharacterTimelineReferenceDto>(`${this.base(bookId)}/characters/${characterId}/timeline/${id}`, value) : this.http.post<CharacterTimelineReferenceDto>(`${this.base(bookId)}/characters/${characterId}/timeline`, value); }
  removeCharacterTimeline(bookId: string, characterId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/characters/${characterId}/timeline/${id}`); }
  characterAttributes(bookId: string, characterId: string): Observable<CharacterAttributeDto[]> { return this.http.get<CharacterAttributeDto[]>(`${this.base(bookId)}/characters/${characterId}/attributes`); }
  saveCharacterAttribute(bookId: string, characterId: string, value: Partial<CharacterAttributeDto>, id?: string): Observable<CharacterAttributeDto> { return id ? this.http.put<CharacterAttributeDto>(`${this.base(bookId)}/characters/${characterId}/attributes/${id}`, value) : this.http.post<CharacterAttributeDto>(`${this.base(bookId)}/characters/${characterId}/attributes`, value); }
  removeCharacterAttribute(bookId: string, characterId: string, id: string): Observable<void> { return this.http.delete<void>(`${this.base(bookId)}/characters/${characterId}/attributes/${id}`); }
}
