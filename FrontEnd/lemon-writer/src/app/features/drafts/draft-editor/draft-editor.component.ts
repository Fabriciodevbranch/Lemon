import { Component, inject, OnInit, OnDestroy, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TextFieldModule } from '@angular/cdk/text-field';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { DraftsService } from '../../../core/services/drafts.service';
import { Draft } from '../../../core/models/draft.model';
import { Subject, debounceTime, takeUntil } from 'rxjs';

@Component({
  selector: 'app-draft-editor',
  standalone: true,
  imports: [
    RouterLink, FormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule,
    MatFormFieldModule, MatInputModule, TextFieldModule, NavbarComponent
  ],
  templateUrl: './draft-editor.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './draft-editor.component.scss'
})
export class DraftEditorComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private draftsService = inject(DraftsService);
  private snackBar = inject(MatSnackBar);
  private destroy$ = new Subject<void>();
  private contentChanged$ = new Subject<string>();

  bookId = this.route.snapshot.paramMap.get('bookId')!;
  chapterId = this.route.snapshot.paramMap.get('chapterId')!;
  draftId = this.route.snapshot.paramMap.get('draftId')!;

  draft = signal<Draft | null>(null);
  content = signal('');
  autoSaveStatus = signal<'saved' | 'saving' | 'unsaved'>('saved');
  wordCount = signal(0);
  loading = signal(true);

  ngOnInit(): void {
    this.draftsService.getDraft(this.bookId, this.chapterId, this.draftId).subscribe(draft => {
      this.draft.set(draft);
      this.content.set(draft.content || '');
      this.wordCount.set(this.countWords(draft.content || ''));
      this.loading.set(false);
    });

    this.contentChanged$.pipe(debounceTime(1500), takeUntil(this.destroy$)).subscribe(content => {
      this.autoSave(content);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onContentInput(value: string): void {
    this.content.set(value);
    this.wordCount.set(this.countWords(value));
    this.autoSaveStatus.set('unsaved');
    this.contentChanged$.next(value);
  }

  private autoSave(content: string): void {
    this.autoSaveStatus.set('saving');
    this.draftsService.updateDraft(this.bookId, this.chapterId, this.draftId, { content }).subscribe({
      next: () => this.autoSaveStatus.set('saved'),
      error: () => this.autoSaveStatus.set('unsaved')
    });
  }

  private countWords(text: string): number {
    return text.trim().split(/\s+/).filter(w => w.length > 0).length;
  }
}
