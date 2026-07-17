import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryStudioService, StudioMetrics } from '../../core/services/story-studio.service';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { DecimalPipe } from '@angular/common';

type StudioMode = 'characters' | 'relationships' | 'places' | 'objects' | 'timeline' | 'goals' | 'lore' | 'magic' | 'research' | 'gallery' | 'metrics';

interface StudioItem {
  id: string;
  name: string;
  summary: string;
  details: string;
  motivation?: string;
  plot?: string;
  image?: string;
  eventDate?: string;
  impact?: string;
  characterIds?: string[];
  objectIds?: string[];
  placeIds?: string[];
  goalTarget?: number;
  goalProgress?: number;
}

interface Relationship {
  id: string;
  from: string;
  to: string;
  label: string;
  tone: 'positive' | 'neutral' | 'negative';
}

const MODE_META: Record<StudioMode, { title: string; eyebrow: string; description: string; icon: string; singular: string }> = {
  characters: { title: 'Characters', eyebrow: 'Cast', description: 'Shape the people who carry your story.', icon: 'group', singular: 'character' },
  relationships: { title: 'Relationship map', eyebrow: 'Connections', description: 'See alliances, tensions, and bonds at a glance.', icon: 'hub', singular: 'relationship' },
  places: { title: 'Places & scenarios', eyebrow: 'World', description: 'Document locations, atmosphere, history, and narrative purpose.', icon: 'landscape', singular: 'place' },
  objects: { title: 'Objects & artifacts', eyebrow: 'Details', description: 'Track meaningful objects, clues, tools, and symbols.', icon: 'deployed_code', singular: 'object' },
  timeline: { title: 'Story timeline', eyebrow: 'Chronology', description: 'Arrange events, eras, turning points, and consequences.', icon: 'timeline', singular: 'event' },
  goals: { title: 'Writing goals', eyebrow: 'Momentum', description: 'Set achievable milestones and celebrate steady progress.', icon: 'flag', singular: 'goal' },
  lore: { title: 'Lore & glossary', eyebrow: 'Canon', description: 'Define cultures, terms, history, and rules readers need to understand.', icon: 'auto_stories', singular: 'lore entry' },
  magic: { title: 'Magic system', eyebrow: 'Rules', description: 'Document powers, costs, limits, sources, and exceptions.', icon: 'wand_stars', singular: 'rule' },
  research: { title: 'Notes & research', eyebrow: 'Notebook', description: 'Keep sources, questions, fragments, and private working notes together.', icon: 'note_stack', singular: 'note' },
  gallery: { title: 'Gallery & media', eyebrow: 'References', description: 'Keep visual inspiration beside the manuscript.', icon: 'collections', singular: 'media item' },
  metrics: { title: 'Story metrics', eyebrow: 'Overview', description: 'A practical pulse check for your developing book.', icon: 'monitoring', singular: 'metric' }
};

@Component({
  selector: 'app-story-studio',
  standalone: true,
  imports: [FormsModule, DecimalPipe, RouterLink, RouterLinkActive, MatIconModule, DragDropModule, NavbarComponent],
  templateUrl: './story-studio.component.html',
  styleUrl: './story-studio.component.scss',
  changeDetection: ChangeDetectionStrategy.Eager
})
export class StoryStudioComponent {
  private route = inject(ActivatedRoute);
  private studio = inject(StoryStudioService);
  readonly bookId = this.route.snapshot.paramMap.get('bookId')!;
  readonly mode = this.route.snapshot.data['mode'] as StudioMode;
  readonly meta = MODE_META[this.mode];

  readonly items = signal<StudioItem[]>([]);
  readonly timelineCharacters = signal<StudioItem[]>([]);
  readonly timelineObjects = signal<StudioItem[]>([]);
  readonly timelinePlaces = signal<StudioItem[]>([]);
  readonly selectedEvent = signal<StudioItem | null>(null);
  readonly relationships = signal<Relationship[]>([]);
  readonly metrics = signal<StudioMetrics>({ enabled: false, characters: 0, places: 0, objects: 0, gallery: 0, relationships: 0, completeness: 0, storyCompleteness: 0, characterCoverage: 0, relationshipCoverage: 0, timelineCompleteness: 0, orphanCharacters: 0, emptyLocations: 0, unusedObjects: 0 });
  readonly loading = signal(true);
  readonly dialogOpen = signal(false);
  readonly metricsError = signal('');
  readonly metricsSaving = signal(false);
  readonly characterCount = computed(() => this.metrics().characters);
  readonly placeCount = computed(() => this.metrics().places);
  readonly objectCount = computed(() => this.metrics().objects);
  readonly galleryCount = computed(() => this.metrics().gallery);
  readonly completeness = computed(() => this.metrics().completeness);

  draft: Partial<StudioItem> = {};
  relationshipDraft: Partial<Relationship> = { tone: 'neutral' };

  constructor() { this.load(); }

  private load(): void {
    this.studio.metrics(this.bookId).subscribe(value => this.metrics.set(value));
    if (this.mode === 'metrics') { this.loading.set(false); return; }
    if (this.mode === 'relationships') {
      this.studio.list(this.bookId, 'characters').subscribe(items => this.items.set(items.map(x => this.toStudioItem(x))));
      this.studio.relationships(this.bookId).subscribe({ next: items => { this.relationships.set(items); this.loading.set(false); }, error: () => this.loading.set(false) });
      return;
    }
    this.studio.list(this.bookId, this.mode).subscribe({ next: items => {
      this.items.set(items.map(x => this.toStudioItem(x)));
      this.loading.set(false);
    }, error: () => this.loading.set(false) });
    if (this.mode === 'timeline') {
      this.studio.list(this.bookId, 'characters').subscribe(items => this.timelineCharacters.set(items.map(x => this.toStudioItem(x))));
      this.studio.list(this.bookId, 'objects').subscribe(items => this.timelineObjects.set(items.map(x => this.toStudioItem(x))));
      this.studio.list(this.bookId, 'places').subscribe(items => this.timelinePlaces.set(items.map(x => this.toStudioItem(x))));
    }
  }

  setMetricsEnabled(enabled: boolean): void {
    this.metricsError.set('');
    this.metricsSaving.set(true);
    this.studio.setMetricsEnabled(enabled).subscribe({
      next: () => this.studio.metrics(this.bookId).subscribe({
        next: value => { this.metrics.set(value); this.metricsSaving.set(false); },
        error: () => { this.metricsError.set('Story metrics could not be calculated. Please try again.'); this.metricsSaving.set(false); }
      }),
      error: () => { this.metricsError.set('Your metrics preference could not be saved. Please try again.'); this.metricsSaving.set(false); }
    });
  }

  openCreate(): void {
    this.draft = {};
    this.relationshipDraft = { tone: 'neutral' };
    this.dialogOpen.set(true);
  }

  closeCreate(): void { this.dialogOpen.set(false); }

  saveItem(): void {
    if (!this.draft.name?.trim()) return;
    const item: StudioItem = {
      id: '',
      name: this.draft.name.trim(),
      summary: this.draft.summary?.trim() ?? '',
      details: this.draft.details?.trim() ?? '',
      motivation: this.draft.motivation?.trim(),
      plot: this.draft.plot?.trim(),
      image: this.draft.image
      , eventDate: this.draft.eventDate?.trim(), impact: this.draft.impact?.trim(),
      characterIds: this.draft.characterIds ?? [], objectIds: this.draft.objectIds ?? [], placeIds: this.draft.placeIds ?? []
      , goalTarget: this.draft.goalTarget, goalProgress: this.draft.goalProgress ?? 0
    };
    this.studio.create(this.bookId, this.mode, { ...item, imageData: item.image,
      characterIds: item.characterIds?.join(','), objectIds: item.objectIds?.join(','), placeIds: item.placeIds?.join(',') }).subscribe(saved => {
      const value = this.toStudioItem(saved);
      this.items.update(items => this.mode === 'timeline' ? [...items, value] : [value, ...items]);
      this.studio.metrics(this.bookId).subscribe(value => this.metrics.set(value));
      this.closeCreate();
    });
  }

  saveRelationship(): void {
    const value = this.relationshipDraft;
    if (!value.from || !value.to || value.from === value.to) return;
    this.studio.createRelationship(this.bookId, value).subscribe(saved => {
      this.relationships.update(items => [saved, ...items]); this.closeCreate();
    });
  }

  remove(id: string): void {
    this.studio.delete(this.bookId, id).subscribe(() => {
      this.items.update(items => items.filter(item => item.id !== id));
      this.studio.metrics(this.bookId).subscribe(value => this.metrics.set(value));
    });
  }

  removeRelationship(id: string): void {
    this.studio.deleteRelationship(this.bookId, id).subscribe(() => this.relationships.update(items => items.filter(item => item.id !== id)));
  }

  reorderTimeline(event: CdkDragDrop<StudioItem[]>): void {
    if (event.previousIndex === event.currentIndex) return;
    const reordered = [...this.items()];
    moveItemInArray(reordered, event.previousIndex, event.currentIndex);
    this.items.set(reordered);
    this.studio.reorderTimeline(this.bookId, reordered.map(item => item.id)).subscribe({
      error: () => this.load()
    });
  }

  onMediaSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file || file.size > 5 * 1024 * 1024) return;
    const reader = new FileReader();
    reader.onload = () => {
      this.draft.image = String(reader.result);
      if (!this.draft.name) this.draft.name = file.name;
    };
    reader.readAsDataURL(file);
  }

  characterName(id: string): string {
    return this.items().find(item => item.id === id)?.name ?? 'Unknown';
  }

  openEvent(item: StudioItem): void { this.selectedEvent.set(item); }
  closeEvent(): void { this.selectedEvent.set(null); }
  goalPercent(item: StudioItem): number { return item.goalTarget ? Math.min(100, Math.round((item.goalProgress ?? 0) * 100 / item.goalTarget)) : 0; }
  updateGoal(item: StudioItem, value: string): void {
    const progress = Math.max(0, Number(value) || 0);
    this.studio.updateGoalProgress(this.bookId, item.id, progress).subscribe(saved =>
      this.items.update(items => items.map(current => current.id === item.id ? this.toStudioItem(saved) : current)));
  }
  relatedNames(ids: string[] | undefined, source: StudioItem[]): string {
    return (ids ?? []).map(id => source.find(item => item.id === id)?.name).filter(Boolean).join(', ') || 'None selected';
  }
  private ids(value?: string): string[] { return value?.split(',').filter(Boolean) ?? []; }
  private toStudioItem(value: import('../../core/services/story-studio.service').StudioItemDto): StudioItem {
    return {
      id: value.id, name: value.name, summary: value.summary, details: value.details,
      motivation: value.motivation, plot: value.plot, image: value.imageData,
      eventDate: value.eventDate, impact: value.impact,
      goalTarget: value.goalTarget, goalProgress: value.goalProgress,
      characterIds: this.ids(value.relatedCharacterIds), objectIds: this.ids(value.relatedObjectIds), placeIds: this.ids(value.relatedPlaceIds)
    };
  }
}
