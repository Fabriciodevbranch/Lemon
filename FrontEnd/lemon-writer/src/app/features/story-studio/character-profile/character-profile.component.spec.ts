import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CharacterProfileComponent } from './character-profile.component';
import { CharacterProfileModel } from './character-profile.model';

describe('CharacterProfileComponent', () => {
  let fixture: ComponentFixture<CharacterProfileComponent>;
  const character: CharacterProfileModel = {
    id: 'char-1', name: 'Mara', summary: 'A reluctant guide', details: 'Legacy biography',
    motivation: 'Legacy motivation', plot: 'Legacy plot involvement'
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [CharacterProfileComponent] }).compileComponents();
    fixture = TestBed.createComponent(CharacterProfileComponent);
    fixture.componentRef.setInput('character', character);
    fixture.componentRef.setInput('portraits', []);
    fixture.detectChanges();
  });

  it('renders the profile header and four primary sections', () => {
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.profile-header')?.textContent).toContain('Mara');
    expect(root.querySelector('#character-overview')).not.toBeNull();
    expect(root.querySelector('#character-inner')).not.toBeNull();
    expect(root.querySelector('#character-arc')).not.toBeNull();
    expect(root.querySelector('#character-notes')).not.toBeNull();
    expect(root.querySelector('.profile-close')?.getAttribute('aria-label')).toBe('Close character profile');
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
