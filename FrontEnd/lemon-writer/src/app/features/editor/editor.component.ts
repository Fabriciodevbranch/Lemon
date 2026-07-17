import { Component, inject, OnInit, OnDestroy, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TextFieldModule } from '@angular/cdk/text-field';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { EditorToolbarComponent } from './editor-toolbar/editor-toolbar.component';
import { ChaptersService } from '../../core/services/chapters.service';
import { SnapshotsService } from '../../core/services/snapshots.service';
import { Chapter } from '../../core/models/chapter.model';

@Component({
  selector: 'app-editor',
  standalone: true,
  imports: [
    FormsModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule,
    TextFieldModule, NavbarComponent, EditorToolbarComponent
  ],
  templateUrl: './editor.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './editor.component.scss'
})
export class EditorComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private chaptersService = inject(ChaptersService);
  private snapshotsService = inject(SnapshotsService);
  private snackBar = inject(MatSnackBar);
  private destroy$ = new Subject<void>();
  private contentChanged$ = new Subject<string>();

  bookId = this.route.snapshot.paramMap.get('bookId')!;
  chapterId = this.route.snapshot.paramMap.get('chapterId')!;

  chapter = signal<Chapter | null>(null);
  content = signal('');
  autoSaveStatus = signal<'saved' | 'saving' | 'unsaved'>('saved');
  wordCount = signal(0);
  loading = signal(true);

  ngOnInit(): void {
    this.chaptersService.getChapter(this.bookId, this.chapterId).subscribe(chapter => {
      this.chapter.set(chapter);
      this.content.set(chapter.content || '');
      this.wordCount.set(this.countWords(chapter.content || ''));
      this.loading.set(false);
    });

    this.contentChanged$.pipe(
      debounceTime(1500),
      takeUntil(this.destroy$)
    ).subscribe(content => {
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
    this.chaptersService.updateChapter(this.bookId, this.chapterId, { content }).subscribe({
      next: () => this.autoSaveStatus.set('saved'),
      error: () => this.autoSaveStatus.set('unsaved')
    });
  }

  onTakeSnapshot(): void {
    const message = prompt('Describe this version (e.g. "Finished the opening scene"):');
    if (message === null) return;
    this.snapshotsService.takeSnapshot(this.bookId, this.chapterId, {
      message: message || 'Snapshot',
      content: this.content()
    }).subscribe({
      next: () => this.snackBar.open('Snapshot taken! Your version is saved.', 'Dismiss', { duration: 3000 }),
      error: () => this.snackBar.open('Could not take snapshot. Please try again.', 'Dismiss', { duration: 3000 })
    });
  }

  goToTimeline(): void {
    this.router.navigate(['/books', this.bookId, 'chapters', this.chapterId, 'timeline']);
  }

  goToDrafts(): void {
    this.router.navigate(['/books', this.bookId, 'chapters', this.chapterId, 'drafts']);
  }

  private countWords(text: string): number {
    return text.trim().split(/\s+/).filter(w => w.length > 0).length;
  }
}
