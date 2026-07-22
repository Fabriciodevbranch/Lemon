import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { AuthService } from '../../core/auth/auth.service';
import { StoryStudioService, StudioMetrics } from '../../core/services/story-studio.service';
import { StoryStudioComponent } from './story-studio.component';
import { routes } from '../../app.routes';

describe('StoryStudioComponent gallery', () => {
  let fixture: ComponentFixture<StoryStudioComponent>;
  let component: StoryStudioComponent;
  let studio: jasmine.SpyObj<StoryStudioService>;

  const metrics: StudioMetrics = {
    enabled: false, characters: 0, places: 0, objects: 0, gallery: 0, relationships: 0,
    completeness: 0, storyCompleteness: 0, characterCoverage: 0, relationshipCoverage: 0,
    timelineCompleteness: 0, orphanCharacters: 0, emptyLocations: 0, unusedObjects: 0
  };
  const collectionItems = [
    { id: 'c1-a', name: 'Forest', summary: 'Misty woodland', details: '', image: 'forest.jpg', collectionId: 'c1', collectionName: 'Woodlands' },
    { id: 'c1-b', name: 'Moss', summary: '', details: '', image: 'moss.jpg', collectionId: 'c1', collectionName: 'Woodlands' }
  ];
  const standalone = { id: 'm1', name: 'Loose sketch', summary: 'A quick reference', details: '', image: 'sketch.jpg' };

  beforeEach(async () => {
    studio = jasmine.createSpyObj<StoryStudioService>('StoryStudioService', ['metrics', 'list', 'delete']);
    studio.metrics.and.returnValue(of(metrics));
    studio.list.and.returnValue(of([]));
    studio.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [StoryStudioComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'book-1' }, data: { mode: 'gallery' } }, fragment: of(null) } },
        { provide: StoryStudioService, useValue: studio },
        { provide: AuthService, useValue: { currentUser$: () => null, logout: jasmine.createSpy('logout') } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(StoryStudioComponent);
    component = fixture.componentInstance;
    component.loading.set(false);
  });

  function setItems(items: object[]): void {
    (component.items as unknown as { set(value: object[]): void }).set(items);
    fixture.detectChanges();
  }

  it('renders collections before unsorted media using distinct card markup', () => {
    setItems([...collectionItems, standalone]);
    const root = fixture.nativeElement as HTMLElement;
    const collections = root.querySelector('.collections-section')!;
    const unsorted = root.querySelector('.unsorted-section')!;

    expect(collections.compareDocumentPosition(unsorted) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(root.querySelectorAll('.collection-card').length).toBe(1);
    expect(root.querySelectorAll('.collection-preview img').length).toBe(2);
    expect(root.querySelectorAll('.unsorted-media-card').length).toBe(1);
    expect(root.querySelector('.unsorted-media-card .eyebrow')).toBeNull();
  });

  it('keeps both section empty states visible', () => {
    setItems([]);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('No collections yet');
    expect(text).toContain('Everything is neatly organized.');
    expect((fixture.nativeElement as HTMLElement).querySelector('.gallery-empty--collections button')).not.toBeNull();
  });

  it('opens the existing collection modal when a collection card is clicked', () => {
    setItems(collectionItems);
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.collection-card')!.click();
    fixture.detectChanges();
    expect(component.selectedCollectionId()).toBe('c1');
    expect((fixture.nativeElement as HTMLElement).querySelector('.collection-modal')).not.toBeNull();
  });

  it('preserves standalone media deletion behavior', () => {
    setItems([standalone]);
    (fixture.nativeElement as HTMLElement).querySelector<HTMLButtonElement>('.unsorted-media-card .delete')!.click();
    expect(studio.delete).toHaveBeenCalledWith('book-1', 'm1');
    expect(component.items().length).toBe(0);
  });

  it('uses dedicated responsive layout containers', () => {
    setItems([...collectionItems, standalone]);
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.collections-grid')).not.toBeNull();
    expect(root.querySelector('.unsorted-media-grid')).not.toBeNull();
  });
});

describe('StoryStudioComponent character quick creation', () => {
  let fixture: ComponentFixture<StoryStudioComponent>;
  let component: StoryStudioComponent;
  let studio: jasmine.SpyObj<StoryStudioService>;

  beforeEach(async () => {
    studio = jasmine.createSpyObj<StoryStudioService>('StoryStudioService', ['metrics', 'list', 'create', 'relationships', 'characterMedia', 'characterTimeline', 'characterAttributes']);
    studio.metrics.and.returnValue(of({ enabled: false, characters: 0, places: 0, objects: 0, gallery: 0, relationships: 0,
      completeness: 0, storyCompleteness: 0, characterCoverage: 0, relationshipCoverage: 0, timelineCompleteness: 0,
      orphanCharacters: 0, emptyLocations: 0, unusedObjects: 0 }));
    studio.list.and.returnValue(of([]));
    studio.relationships.and.returnValue(of([])); studio.characterMedia.and.returnValue(of([]));
    studio.characterTimeline.and.returnValue(of([])); studio.characterAttributes.and.returnValue(of([]));
    studio.create.and.returnValue(of({ id: 'new-char', name: 'Iris', summary: '', details: '' }));
    await TestBed.configureTestingModule({ imports: [StoryStudioComponent], providers: [provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => 'book-1' }, data: { mode: 'characters' } }, fragment: of(null) } },
      { provide: StoryStudioService, useValue: studio },
      { provide: AuthService, useValue: { currentUser$: () => null, logout: jasmine.createSpy('logout') } }
    ] }).compileComponents();
    fixture = TestBed.createComponent(StoryStudioComponent);
    component = fixture.componentInstance;
    component.loading.set(false);
    component.openCreate();
    fixture.detectChanges();
  });

  it('contains only quick identity fields and accessible close controls', () => {
    const modal = (fixture.nativeElement as HTMLElement).querySelector('.studio-modal')!;
    expect(modal.textContent).toContain('Name');
    expect(modal.textContent).toContain('Short summary');
    expect(modal.textContent).toContain('Story role');
    expect(modal.textContent).toContain('Portrait from Gallery');
    expect(modal.textContent).not.toContain('Motivation');
    expect(modal.textContent).not.toContain('Plot involvement');
    expect(modal.textContent).not.toContain('Details');
    expect(modal.querySelector('.icon-button')?.getAttribute('aria-label')).toBeTruthy();
  });

  it('requires a name but allows all optional fields to be omitted', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    component.saveItem();
    expect(studio.create).not.toHaveBeenCalled();
    component.draft.name = 'Iris';
    component.saveItem();
    expect(studio.create).toHaveBeenCalled();
    expect(component.items().some(item => item.name === 'Iris')).toBeTrue();
    expect(router.navigate).toHaveBeenCalledWith(['/books', 'book-1', 'characters', 'new-char'], { fragment: 'overview' });
  });
});

describe('StoryStudioComponent routed character profile', () => {
  const character = { id: 'char-1', name: 'Mara', summary: 'Guide', details: '', storyRole: 'Mentor' };
  const metrics: StudioMetrics = { enabled: false, characters: 1, places: 0, objects: 0, gallery: 0, relationships: 0,
    completeness: 0, storyCompleteness: 0, characterCoverage: 0, relationshipCoverage: 0, timelineCompleteness: 0,
    orphanCharacters: 0, emptyLocations: 0, unusedObjects: 0 };

  function configure(characterId: string, characters = [character]): Promise<void> {
    const studio = jasmine.createSpyObj<StoryStudioService>('StoryStudioService', ['metrics', 'list', 'relationships', 'characterMedia', 'characterTimeline', 'characterAttributes', 'updateCharacterProfile']);
    studio.metrics.and.returnValue(of(metrics));
    studio.list.and.callFake((_book, type) => of(type === 'characters' ? characters : []));
    studio.relationships.and.returnValue(of([])); studio.characterMedia.and.returnValue(of([]));
    studio.characterTimeline.and.returnValue(of([])); studio.characterAttributes.and.returnValue(of([]));
    studio.updateCharacterProfile.and.returnValue(of({ ...character, summary: 'Updated guide' }));
    return TestBed.configureTestingModule({ imports: [StoryStudioComponent], providers: [provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: (key: string) => key === 'bookId' ? 'book-1' : characterId }, data: { mode: 'characters' } }, fragment: of('overview') } },
      { provide: StoryStudioService, useValue: studio },
      { provide: AuthService, useValue: { currentUser$: () => null, logout: jasmine.createSpy('logout') } }
    ] }).compileComponents();
  }

  it('declares one canonical direct character route', () => {
    expect(routes.filter(route => route.path === 'books/:bookId/characters/:characterId').length).toBe(1);
  });

  it('opens the correct character from a direct URL context and preserves book identity', async () => {
    await configure('char-1');
    const fixture = TestBed.createComponent(StoryStudioComponent); fixture.detectChanges();
    expect(fixture.componentInstance.bookId).toBe('book-1');
    expect(fixture.componentInstance.selectedCharacter()?.id).toBe('char-1');
    expect((fixture.nativeElement as HTMLElement).querySelector('app-character-profile')).not.toBeNull();
  });

  it('shows not found and no interactive profile for an invalid or cross-book character id', async () => {
    await configure('other-book-character', []);
    const fixture = TestBed.createComponent(StoryStudioComponent); fixture.detectChanges();
    expect(fixture.componentInstance.selectedCharacter()).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('app-character-profile')).toBeNull();
    expect((fixture.nativeElement as HTMLElement).querySelector('.profile-not-found')?.textContent).toContain('Character not found');
  });

  it('refreshes the routed visible profile after a successful save', async () => {
    await configure('char-1');
    const fixture = TestBed.createComponent(StoryStudioComponent); fixture.detectChanges();
    fixture.componentInstance.saveCharacterProfile(character); fixture.detectChanges();
    expect(fixture.componentInstance.selectedCharacter()?.summary).toBe('Updated guide');
    expect(fixture.componentInstance.profileSaving()).toBeFalse();
  });

  it('uses router history navigation when closing the profile', async () => {
    await configure('char-1');
    const fixture = TestBed.createComponent(StoryStudioComponent);
    const router = TestBed.inject(Router); spyOn(router, 'navigate').and.resolveTo(true);
    fixture.componentInstance.closeCharacter();
    expect(router.navigate).toHaveBeenCalledWith(['/books', 'book-1', 'characters']);
  });
});
