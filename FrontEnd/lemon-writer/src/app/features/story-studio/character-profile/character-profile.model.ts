export interface CharacterProfileModel {
  id: string; name: string; summary: string; details: string; motivation?: string; plot?: string; image?: string;
  storyRole?: string; characterStatus?: string; age?: string; pronouns?: string; aliases?: string; portraitMediaId?: string;
  externalGoal?: string; internalNeed?: string; fear?: string; secret?: string; internalConflict?: string; externalConflict?: string;
  narrativeFunction?: string; arcSummary?: string; startingState?: string; turningPoint?: string; endingState?: string; notes?: string;
}

export interface GalleryPortrait { id: string; name: string; image?: string; summary?: string; }

// Phase 1 extension point only; persistence intentionally remains out of scope.
export interface FutureCharacterAttribute { label: string; valueType: 'text' | 'number' | 'date' | 'boolean'; value: unknown; group?: string; displayOrder: number; }
