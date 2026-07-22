import { ChangeDetectionStrategy, Component, effect, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CharacterProfileModel, GalleryPortrait } from './character-profile.model';
import { A11yModule } from '@angular/cdk/a11y';
import { CharacterConnectionsComponent } from './character-connections.component';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-character-profile', standalone: true, imports: [FormsModule, MatIconModule, A11yModule, CharacterConnectionsComponent, RouterLink],
  templateUrl: './character-profile.component.html', styleUrls: ['./character-profile.component.scss', './character-profile-navigation.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CharacterProfileComponent {
  private route = inject(ActivatedRoute);
  private readonly sections = ['overview', 'inner', 'arc', 'relationships', 'gallery', 'timeline', 'attributes', 'notes'] as const;
  readonly character = input.required<CharacterProfileModel>();
  readonly bookId = input.required<string>();
  readonly characters = input<CharacterProfileModel[]>([]);
  readonly portraits = input<GalleryPortrait[]>([]);
  readonly events = input<{ id: string; name: string }[]>([]);
  readonly closed = output<void>();
  readonly saved = output<CharacterProfileModel>();
  readonly deleted = output<string>();
  readonly relatedCharacter = output<string>();
  readonly relatedMedia = output<string>();
  readonly relatedEvent = output<string>();
  readonly saving = input(false);
  readonly saveError = input('');
  readonly editing = signal<'overview' | 'inner' | 'arc' | 'notes' | null>(null);
  readonly activeSection = signal<string>('overview');
  draft!: CharacterProfileModel;
  private wasSaving = false;

  constructor() {
    this.route.fragment.subscribe(fragment => {
      const section = this.sections.includes(fragment as typeof this.sections[number]) ? fragment! : 'overview';
      this.activeSection.set(section);
      requestAnimationFrame(() => document.getElementById(`character-${section}`)?.scrollIntoView({ block: 'start' }));
    });
    effect(() => {
      const saving = this.saving();
      if (this.wasSaving && !saving && !this.saveError()) this.editing.set(null);
      this.wasSaving = saving;
    });
  }

  portrait(): string | undefined { return this.portraits().find(x => x.id === this.character().portraitMediaId)?.image ?? this.character().image; }
  edit(section: 'overview' | 'inner' | 'arc' | 'notes'): void { if (this.saving()) return; this.draft = { ...this.character() }; this.editing.set(section); }
  cancel(): void { this.editing.set(null); }
  save(): void { if (this.saving() || !this.draft.name.trim()) return; this.saved.emit({ ...this.draft, name: this.draft.name.trim() }); }
  aliases(): string[] { return this.character().aliases?.split(',').map(x => x.trim()).filter(Boolean) ?? []; }
  setPrimaryPortrait(mediaId: string): void { this.saved.emit({ ...this.character(), portraitMediaId: mediaId }); }
  profileRoute(): unknown[] { return ['/books', this.bookId(), 'characters', this.character().id]; }
}
