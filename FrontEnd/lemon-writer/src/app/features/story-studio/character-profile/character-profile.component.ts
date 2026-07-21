import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { CharacterProfileModel, GalleryPortrait } from './character-profile.model';
import { A11yModule } from '@angular/cdk/a11y';
import { CharacterConnectionsComponent } from './character-connections.component';

@Component({
  selector: 'app-character-profile', standalone: true, imports: [FormsModule, MatIconModule, A11yModule, CharacterConnectionsComponent],
  templateUrl: './character-profile.component.html', styleUrl: './character-profile.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CharacterProfileComponent {
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
  readonly editing = signal<'overview' | 'inner' | 'arc' | 'notes' | null>(null);
  draft!: CharacterProfileModel;

  portrait(): string | undefined { return this.portraits().find(x => x.id === this.character().portraitMediaId)?.image ?? this.character().image; }
  edit(section: 'overview' | 'inner' | 'arc' | 'notes'): void { this.draft = { ...this.character() }; this.editing.set(section); }
  cancel(): void { this.editing.set(null); }
  save(): void { if (!this.draft.name.trim()) return; this.saved.emit({ ...this.draft, name: this.draft.name.trim() }); this.editing.set(null); }
  aliases(): string[] { return this.character().aliases?.split(',').map(x => x.trim()).filter(Boolean) ?? []; }
  setPrimaryPortrait(mediaId: string): void { this.saved.emit({ ...this.character(), portraitMediaId: mediaId }); }
}
