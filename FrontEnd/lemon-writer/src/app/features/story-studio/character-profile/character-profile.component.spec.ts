import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CharacterProfileComponent } from './character-profile.component';
import { CharacterProfileModel } from './character-profile.model';
import { StoryStudioService } from '../../../core/services/story-studio.service';
import { of } from 'rxjs';
import { Subject } from 'rxjs';
import { ActivatedRoute, provideRouter } from '@angular/router';

describe('CharacterProfileComponent', () => {
  let fixture: ComponentFixture<CharacterProfileComponent>;
  let fragments: Subject<string | null>;
  const character: CharacterProfileModel = {
    id: 'char-1', name: 'Mara', summary: 'A reluctant guide', details: 'Legacy biography',
    motivation: 'Legacy motivation', plot: 'Legacy plot involvement'
  };

  beforeEach(async () => {
    fragments = new Subject<string | null>();
    const connections = jasmine.createSpyObj<StoryStudioService>('StoryStudioService', ['relationships', 'characterMedia', 'characterTimeline', 'characterAttributes']);
    connections.relationships.and.returnValue(of([]));
    connections.characterMedia.and.returnValue(of([]));
    connections.characterTimeline.and.returnValue(of([]));
    connections.characterAttributes.and.returnValue(of([]));
    await TestBed.configureTestingModule({ imports: [CharacterProfileComponent], providers: [provideRouter([]), { provide: ActivatedRoute, useValue: { fragment: fragments.asObservable() } }, { provide: StoryStudioService, useValue: connections }] }).compileComponents();
    fixture = TestBed.createComponent(CharacterProfileComponent);
    fixture.componentRef.setInput('bookId', 'book-1');
    fixture.componentRef.setInput('character', character);
    fixture.componentRef.setInput('portraits', []);
    fixture.detectChanges();
  });

  it('renders the profile header and all progressive profile sections', () => {
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.profile-header')?.textContent).toContain('Mara');
    expect(root.querySelector('#character-overview')).not.toBeNull();
    expect(root.querySelector('#character-inner')).not.toBeNull();
    expect(root.querySelector('#character-arc')).not.toBeNull();
    expect(root.querySelector('#character-relationships')).not.toBeNull();
    expect(root.querySelector('#character-gallery')).not.toBeNull();
    expect(root.querySelector('#character-timeline')).not.toBeNull();
    expect(root.querySelector('#character-attributes')).not.toBeNull();
    expect(root.querySelector('#character-notes')).not.toBeNull();
    expect(root.querySelector('.profile-close')?.getAttribute('aria-label')).toBe('Back to characters');
  });

  it('renders every section navigation item as a keyboard-native deep link', () => {
    const links = [...(fixture.nativeElement as HTMLElement).querySelectorAll<HTMLAnchorElement>('.profile-nav a')];
    expect(links.map(link => link.textContent?.trim())).toEqual(['Overview', 'Inner world', 'Story arc', 'Relationships', 'Gallery', 'Timeline', 'Custom attributes', 'Notes']);
    expect(links.map(link => link.getAttribute('href'))).toEqual([
      '/books/book-1/characters/char-1#overview', '/books/book-1/characters/char-1#inner', '/books/book-1/characters/char-1#arc',
      '/books/book-1/characters/char-1#relationships', '/books/book-1/characters/char-1#gallery', '/books/book-1/characters/char-1#timeline',
      '/books/book-1/characters/char-1#attributes', '/books/book-1/characters/char-1#notes'
    ]);
    links.forEach(link => expect(link.tabIndex).toBeGreaterThanOrEqual(0));
  });

  it('tracks fragment changes for active state, including history-style changes', () => {
    fragments.next('timeline'); fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.profile-nav a.active')?.textContent).toContain('Timeline');
    fragments.next('inner'); fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.profile-nav a.active')?.textContent).toContain('Inner world');
  });

  it('opens identity and each editable section with a fresh character draft', () => {
    const root = fixture.nativeElement as HTMLElement;
    root.querySelector<HTMLButtonElement>('.edit-profile')!.click(); expect(fixture.componentInstance.editing()).toBe('overview');
    fixture.componentInstance.cancel();
    for (const [sectionId, state] of [['character-overview','overview'], ['character-inner','inner'], ['character-arc','arc'], ['character-notes','notes']] as const) {
      root.querySelector<HTMLButtonElement>(`#${sectionId} > header button`)!.click();
      expect(fixture.componentInstance.editing()).toBe(state);
      expect(fixture.componentInstance.draft.id).toBe('char-1');
      fixture.componentInstance.cancel(); fixture.detectChanges();
    }
  });

  it('keeps legacy details, motivation, and plot content visible', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Legacy biography');
    expect(text).toContain('Legacy motivation');
    expect(text).toContain('Legacy plot involvement');
  });

  it('shows useful empty optional-section language', () => {
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Their conscious goal can emerge');
    expect(text).toContain('No additional notes yet');
    expect(text).toContain('No relationships yet');
    expect(text).toContain('No visual references yet');
    expect(text).toContain('not connected to any timeline events');
    expect(text).toContain('No custom details yet');
  });

  it('emits structured edits when saved', () => {
    const saved = jasmine.createSpy('saved');
    fixture.componentInstance.saved.subscribe(saved);
    fixture.componentInstance.edit('inner');
    fixture.detectChanges();
    fixture.componentInstance.draft.externalGoal = 'Find the lost city';
    fixture.componentInstance.save();
    expect(saved).toHaveBeenCalledWith(jasmine.objectContaining({ externalGoal: 'Find the lost city' }));
  });

  it('cancel leaves the input character unchanged', () => {
    fixture.componentInstance.edit('overview');
    fixture.componentInstance.draft.name = 'Changed';
    fixture.componentInstance.cancel();
    expect(fixture.componentInstance.character().name).toBe('Mara');
    expect(fixture.componentInstance.editing()).toBeNull();
  });
});
