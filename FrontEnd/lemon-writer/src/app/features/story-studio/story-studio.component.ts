import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink, RouterLinkActive } from '@angular/router';
import { MatIconModule } from '@angular/material/icon';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { StoryStudioService, StudioMetrics } from '../../core/services/story-studio.service';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { DecimalPipe } from '@angular/common';
import { CharacterProfileComponent } from './character-profile/character-profile.component';
import { CharacterProfileModel, GalleryPortrait } from './character-profile/character-profile.model';
import { A11yModule } from '@angular/cdk/a11y';

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
  storyRole?: string; characterStatus?: string; age?: string; pronouns?: string; aliases?: string; portraitMediaId?: string;
  externalGoal?: string; internalNeed?: string; fear?: string; secret?: string; internalConflict?: string; externalConflict?: string;
  narrativeFunction?: string; arcSummary?: string; startingState?: string; turningPoint?: string; endingState?: string; notes?: string;
}

interface MediaDraft { fileName: string; name: string; summary: string; details: string; image: string; }
interface MediaCollectionView { id: string; name: string; items: StudioItem[]; cover?: string; description?: string; }
interface AdaptivePalette { surface: string; action: string; accent: string; actionText: string; }
interface ColorCluster { red: number; green: number; blue: number; count: number; }

interface Relationship {
  id: string;
  from: string;
  to: string;
  label: string;
  tone: string;
  relationshipType?: string;
  description?: string;
  status?: string;
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
  imports: [FormsModule, DecimalPipe, RouterLink, RouterLinkActive, MatIconModule, DragDropModule, NavbarComponent, CharacterProfileComponent, A11yModule],
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
  readonly selectedCharacter = signal<StudioItem | null>(null);
  readonly characterPortraits = signal<GalleryPortrait[]>([]);
  readonly characterTimelineEvents = signal<StudioItem[]>([]);
  readonly characterNameError = signal('');
  readonly mediaEditSaving = signal(false);
  readonly mediaEditError = signal('');
  readonly adaptivePalettes = signal<Record<string, AdaptivePalette>>({});
  readonly characterCount = computed(() => this.metrics().characters);
  readonly placeCount = computed(() => this.metrics().places);
  readonly objectCount = computed(() => this.metrics().objects);
  readonly galleryCount = computed(() => this.metrics().gallery);
  readonly completeness = computed(() => this.metrics().completeness);
  readonly mediaCollections = computed<MediaCollectionView[]>(() => {
    const groups = new Map<string, MediaCollectionView>();
    for (const item of this.items().filter(item => item.collectionId)) {
      const id = item.collectionId!;
      const group = groups.get(id) ?? { id, name: item.collectionName ?? 'Untitled collection', items: [], cover: item.image, description: item.summary || undefined };
      group.items.push(item);
      if (!group.description && item.summary) group.description = item.summary;
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
    if (this.mode === 'characters') {
      this.studio.list(this.bookId, 'gallery').subscribe(items =>
        this.characterPortraits.set(items.map(x => ({ id: x.id, name: x.name, image: x.imageData, summary: x.summary }))));
      this.studio.list(this.bookId, 'timeline').subscribe(items => this.characterTimelineEvents.set(items.map(x => this.toStudioItem(x))));
    }
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
    this.characterNameError.set('');
    this.relationshipDraft = { tone: 'neutral' };
    this.dialogOpen.set(true);
  }

  closeCreate(): void { this.dialogOpen.set(false); }

  saveItem(): void {
    if (this.mode === 'gallery' && this.mediaDrafts().length > 1) { this.saveMediaBatch(); return; }
    if (!this.draft.name?.trim()) { if (this.mode === 'characters') this.characterNameError.set('A name is required to create a character.'); return; }
    this.characterNameError.set('');
    const item: StudioItem = {
      id: '',
      name: this.draft.name.trim(),
      summary: this.draft.summary?.trim() ?? '',
      details: this.draft.details?.trim() ?? '',
      motivation: this.draft.motivation?.trim(),
      plot: this.draft.plot?.trim(),
      image: this.draft.image, storyRole: this.draft.storyRole, portraitMediaId: this.draft.portraitMediaId
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
      if (this.mode === 'characters') this.selectedCharacter.set(value);
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
  palette(id: string): AdaptivePalette {
    return this.adaptivePalettes()[id] ?? { surface: 'var(--primary-color)', action: 'var(--primary-color)', accent: 'var(--primary-color)', actionText: '#fff' };
  }
  mediaPalette(item: StudioItem): AdaptivePalette { return this.palette(`media-${item.id}`); }
  captureCollectionColor(id: string, event: Event): void {
    if (this.adaptivePalettes()[id]) return;
    const image = event.currentTarget as HTMLImageElement;
    const canvas = document.createElement('canvas');
    canvas.width = 32; canvas.height = 32;
    const context = canvas.getContext('2d', { willReadFrequently: true });
    if (!context) return;
    try {
      context.drawImage(image, 0, 0, 32, 32);
      const pixels = context.getImageData(0, 0, 32, 32).data;
      const samples: number[][] = [];
      for (let i = 0; i < pixels.length; i += 4) {
        if (pixels[i + 3] < 128) continue;
        const brightness = (pixels[i] + pixels[i + 1] + pixels[i + 2]) / 3;
        if (brightness < 18 || brightness > 242) continue;
        samples.push([pixels[i], pixels[i + 1], pixels[i + 2]]);
      }
      if (!samples.length) return;
      const palette = this.buildPalette(this.clusterColors(samples, 5));
      this.adaptivePalettes.update(palettes => ({ ...palettes, [id]: palette }));
    } catch { /* Fall back to the theme color if canvas sampling is unavailable. */ }
  }
  private clusterColors(samples: number[][], count: number): ColorCluster[] {
    const centers = Array.from({ length: Math.min(count, samples.length) }, (_, index) => [...samples[Math.floor(index * samples.length / count)]]);
    let assignments = new Array<number>(samples.length).fill(0);
    for (let pass = 0; pass < 7; pass++) {
      assignments = samples.map(sample => centers.reduce((best, center, index) =>
        this.colorDistance(sample, center) < this.colorDistance(sample, centers[best]) ? index : best, 0));
      centers.forEach((center, index) => {
        const members = samples.filter((_, sampleIndex) => assignments[sampleIndex] === index);
        if (members.length) for (let channel = 0; channel < 3; channel++) center[channel] = members.reduce((sum, color) => sum + color[channel], 0) / members.length;
      });
    }
    return centers.map((center, index) => ({ red: center[0], green: center[1], blue: center[2], count: assignments.filter(value => value === index).length }))
      .filter(cluster => cluster.count).sort((a, b) => b.count - a.count);
  }
  private colorDistance(a: number[], b: number[]): number {
    const redMean = (a[0] + b[0]) / 2;
    return (2 + redMean / 256) * (a[0] - b[0]) ** 2 + 4 * (a[1] - b[1]) ** 2 + (2 + (255 - redMean) / 256) * (a[2] - b[2]) ** 2;
  }
  private buildPalette(clusters: ColorCluster[]): AdaptivePalette {
    const primary = clusters[0];
    const ranked = clusters.map(color => ({ color, hsl: this.toHsl(color.red, color.green, color.blue) }));
    const actionCandidate = [...ranked].sort((a, b) => (b.hsl.saturation * .7 + b.color.count / 1024 * .3) - (a.hsl.saturation * .7 + a.color.count / 1024 * .3))[0];
    const accentPool = ranked.length > 1 ? ranked.filter(candidate => candidate !== actionCandidate) : ranked;
    const accentCandidate = [...accentPool].sort((a, b) =>
      (b.hsl.saturation * 120 + this.colorDistance([primary.red, primary.green, primary.blue], [b.color.red, b.color.green, b.color.blue])) -
      (a.hsl.saturation * 120 + this.colorDistance([primary.red, primary.green, primary.blue], [a.color.red, a.color.green, a.color.blue])))[0];
    const surface = this.safeHsl(primary, 28, 58, 38, 58);
    const action = this.safeHsl(actionCandidate.color, 38, 68, 34, 52);
    const accent = this.safeHsl(accentCandidate.color, 45, 75, 38, 60);
    return { surface, action, accent, actionText: this.contrastText(actionCandidate.color) };
  }
  private toHsl(red: number, green: number, blue: number): { hue: number; saturation: number; lightness: number } {
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
    return { hue, saturation, lightness };
  }
  private safeHsl(color: ColorCluster, minSaturation: number, maxSaturation: number, minLightness: number, maxLightness: number): string {
    const hsl = this.toHsl(color.red, color.green, color.blue);
    return `hsl(${Math.round(hsl.hue)} ${Math.round(Math.min(maxSaturation, Math.max(minSaturation, hsl.saturation * 100)))}% ${Math.round(Math.min(maxLightness, Math.max(minLightness, hsl.lightness * 100)))}%)`;
  }
  private contrastText(color: ColorCluster): string {
    const linear = (value: number) => { value /= 255; return value <= .04045 ? value / 12.92 : ((value + .055) / 1.055) ** 2.4; };
    const luminance = .2126 * linear(color.red) + .7152 * linear(color.green) + .0722 * linear(color.blue);
    return luminance > .42 ? '#1b1815' : '#fff';
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
  openCharacter(item: StudioItem): void { this.selectedCharacter.set(item); }
  openCharacterById(id: string): void { const character=this.items().find(x=>x.id===id); if(character)this.selectedCharacter.set(character); }
  openRelatedMedia(id: string): void { const media=this.characterPortraits().find(x=>x.id===id); if(!media)return; this.selectedCharacter.set(null); this.selectedMedia.set({id:media.id,name:media.name,summary:media.summary??'',details:'',image:media.image}); }
  openRelatedEvent(id: string): void { const event=this.characterTimelineEvents().find(x=>x.id===id); if(!event)return; this.selectedCharacter.set(null); this.selectedEvent.set(event); }
  closeCharacter(): void { this.selectedCharacter.set(null); }
  saveCharacterProfile(profile: CharacterProfileModel): void {
    this.studio.updateCharacterProfile(this.bookId, profile.id, profile).subscribe(saved => {
      const value = this.toStudioItem(saved);
      this.items.update(items => items.map(item => item.id === value.id ? value : item));
      this.selectedCharacter.set(value);
    });
  }
  deleteCharacter(id: string): void { this.remove(id); this.closeCharacter(); }
  characterPortrait(item: StudioItem): string | undefined {
    return this.characterPortraits().find(image => image.id === item.portraitMediaId)?.image ?? item.image;
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
      storyRole: value.storyRole, characterStatus: value.characterStatus, age: value.age, pronouns: value.pronouns,
      aliases: value.aliases, portraitMediaId: value.portraitMediaId, externalGoal: value.externalGoal,
      internalNeed: value.internalNeed, fear: value.fear, secret: value.secret, internalConflict: value.internalConflict,
      externalConflict: value.externalConflict, narrativeFunction: value.narrativeFunction, arcSummary: value.arcSummary,
      startingState: value.startingState, turningPoint: value.turningPoint, endingState: value.endingState, notes: value.notes,
      characterIds: this.ids(value.relatedCharacterIds), objectIds: this.ids(value.relatedObjectIds), placeIds: this.ids(value.relatedPlaceIds)
    };
  }
}
