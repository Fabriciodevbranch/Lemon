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
  collectionId?: string;
  collectionName?: string;
}

interface MediaDraft { fileName: string; name: string; summary: string; details: string; image: string; }
interface MediaCollectionView { id: string; name: string; items: StudioItem[]; cover?: string; }

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
  styleUrls: ['./story-studio.component.scss', './story-studio-media.component.scss'],
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
  readonly mediaDrafts = signal<MediaDraft[]>([]);
  readonly activeMediaIndex = signal(0);
  readonly mediaLoading = signal(false);
  readonly mediaError = signal('');
  readonly mediaSaving = signal(false);
  readonly selectedCollectionId = signal<string | null>(null);
  readonly selectedMedia = signal<StudioItem | null>(null);
  readonly mediaEditSaving = signal(false);
  readonly mediaEditError = signal('');
  readonly collectionColors = signal<Record<string, string>>({});
  readonly characterCount = computed(() => this.metrics().characters);
  readonly placeCount = computed(() => this.metrics().places);
  readonly objectCount = computed(() => this.metrics().objects);
  readonly galleryCount = computed(() => this.metrics().gallery);
  readonly completeness = computed(() => this.metrics().completeness);
  readonly mediaCollections = computed<MediaCollectionView[]>(() => {
    const groups = new Map<string, MediaCollectionView>();
    for (const item of this.items().filter(item => item.collectionId)) {
      const id = item.collectionId!;
      const group = groups.get(id) ?? { id, name: item.collectionName ?? 'Untitled collection', items: [], cover: item.image };
      group.items.push(item);
      groups.set(id, group);
    }
    return [...groups.values()];
  });
  readonly ungroupedMedia = computed(() => this.items().filter(item => !item.collectionId));
  readonly selectedCollection = computed(() => this.mediaCollections().find(x => x.id === this.selectedCollectionId()) ?? null);

  draft: Partial<StudioItem> = {};
  metadataMode: 'shared' | 'individual' = 'shared';
  createCollection = true;
  collectionName = '';
  sharedSummary = '';
  sharedDetails = '';
  mediaEdit = { name: '', summary: '', details: '' };
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
    this.mediaDrafts.set([]);
    this.activeMediaIndex.set(0);
    this.metadataMode = 'shared';
    this.createCollection = true;
    this.collectionName = '';
    this.sharedSummary = '';
    this.sharedDetails = '';
    this.mediaError.set('');
    this.mediaSaving.set(false);
    this.relationshipDraft = { tone: 'neutral' };
    this.dialogOpen.set(true);
  }

  closeCreate(): void { this.dialogOpen.set(false); }

  saveItem(): void {
    if (this.mode === 'gallery' && this.mediaDrafts().length > 1) { this.saveMediaBatch(); return; }
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
    this.mediaError.set('');
    const selected = Array.from((event.target as HTMLInputElement).files ?? []);
    const files = selected.filter(file => file.type.startsWith('image/') && file.size <= 5 * 1024 * 1024).slice(0, 20);
    if (files.length !== selected.length) {
      this.mediaError.set('Use up to 20 image files of 5 MB or less each.');
      return;
    }
    if (files.reduce((total, file) => total + file.size, 0) > 50 * 1024 * 1024) {
      this.mediaError.set('The selected images must be 50 MB or less in total.');
      return;
    }
    if (!files.length) return;
    this.mediaLoading.set(true);
    Promise.all(files.map(file => this.readMedia(file))).then(drafts => {
      this.mediaDrafts.set(drafts);
      this.activeMediaIndex.set(0);
      if (drafts.length === 1) { this.draft.image = drafts[0].image; this.draft.name = drafts[0].name; }
      else this.collectionName = `${drafts[0].name} collection`;
      this.mediaLoading.set(false);
    });
  }

  selectMedia(index: number): void { this.activeMediaIndex.set(index); }
  updateActiveMedia(field: 'name' | 'summary' | 'details', value: string): void {
    const index = this.activeMediaIndex();
    this.mediaDrafts.update(items => items.map((item, i) => i === index ? { ...item, [field]: value } : item));
  }
  private saveMediaBatch(): void {
    const drafts = this.mediaDrafts();
    if (drafts.some(item => !item.name.trim())) return;
    const items = drafts.map(item => ({ name: item.name.trim(), image: item.image,
      summary: (this.metadataMode === 'shared' ? this.sharedSummary : item.summary).trim(),
      details: (this.metadataMode === 'shared' ? this.sharedDetails : item.details).trim() }));
    this.mediaError.set('');
    this.mediaSaving.set(true);
    this.studio.createGalleryBatch(this.bookId, {
      collectionName: this.createCollection ? this.collectionName.trim() || undefined : undefined, items
    }).subscribe({
      next: saved => {
        this.items.update(current => [...saved.map(x => this.toStudioItem(x)), ...current]);
        this.studio.metrics(this.bookId).subscribe(value => this.metrics.set(value));
        this.closeCreate();
      },
      error: error => {
        this.mediaSaving.set(false);
        this.mediaError.set(error?.error?.message ?? (error?.status === 413
          ? 'The selected images are too large to upload together.'
          : 'The media items could not be saved. Please try again.'));
      }
    });
  }
  private readMedia(file: File): Promise<MediaDraft> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.onload = () => resolve({ fileName: file.name, name: this.cleanFileName(file.name), summary: '', details: '', image: String(reader.result) });
      reader.onerror = () => reject(reader.error);
      reader.readAsDataURL(file);
    });
  }
  private cleanFileName(name: string): string {
    return name.replace(/\.[^.]+$/, '').replace(/[_-]+/g, ' ').replace(/\s+/g, ' ').trim();
  }

  openCollection(id: string): void { this.selectedCollectionId.set(id); }
  closeCollection(): void { this.selectedCollectionId.set(null); }
  collectionColor(id: string): string { return this.collectionColors()[id] ?? 'var(--primary-color)'; }
  mediaColor(item: StudioItem): string {
    return this.collectionColors()[`media-${item.id}`] ?? 'var(--primary-color)';
  }
  captureCollectionColor(id: string, event: Event): void {
    if (this.collectionColors()[id]) return;
    const image = event.currentTarget as HTMLImageElement;
    const canvas = document.createElement('canvas');
    canvas.width = 32; canvas.height = 32;
    const context = canvas.getContext('2d', { willReadFrequently: true });
    if (!context) return;
    try {
      context.drawImage(image, 0, 0, 32, 32);
      const pixels = context.getImageData(0, 0, 32, 32).data;
      let red = 0, green = 0, blue = 0, weight = 0;
      for (let i = 0; i < pixels.length; i += 4) {
        if (pixels[i + 3] < 128) continue;
        const brightness = (pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3;
        if (brightness < 18 || brightness > 242) continue;
        const saturationWeight = Math.max(pixels[i], pixels[i + 1], pixels[i + 2]) - Math.min(pixels[i], pixels[i + 1], pixels[i + 2]) + 24;
        red += pixels[i] * saturationWeight; green += pixels[i + 1] * saturationWeight; blue += pixels[i + 2] * saturationWeight; weight += saturationWeight;
      }
      if (!weight) return;
      const color = this.normalizedAccent(red / weight, green / weight, blue / weight);
      this.collectionColors.update(colors => ({ ...colors, [id]: color }));
    } catch { /* Fall back to the theme color if canvas sampling is unavailable. */ }
  }
  private normalizedAccent(red: number, green: number, blue: number): string {
    red /= 255; green /= 255; blue /= 255;
    const max = Math.max(red, green, blue), min = Math.min(red, green, blue);
    let hue = 0, saturation = 0;
    const lightness = (max + min) / 2;
    const delta = max - min;
    if (delta) {
      saturation = delta / (1 - Math.abs(2 * lightness - 1));
      if (max === red) hue = 60 * (((green - blue) / delta) % 6);
      else if (max === green) hue = 60 * ((blue - red) / delta + 2);
      else hue = 60 * ((red - green) / delta + 4);
    }
    if (hue < 0) hue += 360;
    return `hsl(${Math.round(hue)} ${Math.round(Math.min(65, Math.max(28, saturation * 100)))}% ${Math.round(Math.min(58, Math.max(38, lightness * 100)))}%)`;
  }
  openMedia(item: StudioItem): void {
    this.selectedMedia.set(item);
    this.mediaEdit = { name: item.name, summary: item.summary, details: item.details };
    this.mediaEditError.set('');
  }
  closeMedia(): void { this.selectedMedia.set(null); this.mediaEditSaving.set(false); }
  saveMediaMetadata(): void {
    const item = this.selectedMedia();
    if (!item || !this.mediaEdit.name.trim()) { this.mediaEditError.set('Name is required.'); return; }
    this.mediaEditSaving.set(true);
    this.mediaEditError.set('');
    this.studio.updateMetadata(this.bookId, item.id, {
      name: this.mediaEdit.name.trim(), summary: this.mediaEdit.summary.trim(), details: this.mediaEdit.details.trim()
    }).subscribe({
      next: saved => {
        const value = this.toStudioItem(saved);
        this.items.update(items => items.map(current => current.id === value.id ? value : current));
        this.selectedMedia.set(value);
        this.mediaEditSaving.set(false);
      },
      error: error => {
        this.mediaEditSaving.set(false);
        this.mediaEditError.set(error?.error?.message ?? 'The media details could not be saved.');
      }
    });
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
      collectionId: value.collectionId, collectionName: value.collectionName,
      characterIds: this.ids(value.relatedCharacterIds), objectIds: this.ids(value.relatedObjectIds), placeIds: this.ids(value.relatedPlaceIds)
    };
  }
}
