import { Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { QuillModule } from 'ngx-quill';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { NavbarComponent } from '../../../shared/components/navbar/navbar.component';
import { DraftsService } from '../../../core/services/drafts.service';
import { Draft } from '../../../core/models/draft.model';
import { Subject, debounceTime, takeUntil } from 'rxjs';

@Component({
  selector: 'app-draft-editor',
  standalone: true,
  imports: [RouterLink, FormsModule, ReactiveFormsModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, QuillModule, NavbarComponent],
  templateUrl: './draft-editor.component.html',
  styleUrl: './draft-editor.component.scss'
})
export class DraftEditorComponent implements OnInit {
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

  quillModules = {
    toolbar: [
      [{ header: [1, 2, 3, false] }],
      ['bold', 'italic', 'underline'],
      [{ list: 'ordered' }, { list: 'bullet' }],
      ['blockquote', 'link'],
      ['clean']
    ]
  };

  ngOnInit(): void {
    this.draftsService.getDraft(this.bookId, this.chapterId, this.draftId).subscribe(draft => {
      this.draft.set(draft);
      this.content.set(draft.content || '');
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

  onContentChanged(event: { html: string | null; text: string }): void {
    this.content.set(event.html || '');
    this.wordCount.set(event.text.trim().split(/\s+/).filter(w => w.length > 0).length);
    this.autoSaveStatus.set('unsaved');
    this.contentChanged$.next(event.html || '');
  }

  private autoSave(content: string): void {
    this.autoSaveStatus.set('saving');
    this.draftsService.updateDraft(this.bookId, this.chapterId, this.draftId, { content }).subscribe({
      next: () => this.autoSaveStatus.set('saved'),
      error: () => this.autoSaveStatus.set('unsaved')
    });
  }
}
