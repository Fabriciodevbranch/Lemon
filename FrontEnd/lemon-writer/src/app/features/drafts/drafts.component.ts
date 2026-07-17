import { Component, inject, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { TimeAgoPipe } from '../../shared/pipes/time-ago.pipe';
import { DraftsService } from '../../core/services/drafts.service';
import { Draft } from '../../core/models/draft.model';
import { PublishDraftDialogComponent } from './publish-draft-dialog/publish-draft-dialog.component';
import { ConfirmDialogComponent } from '../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-drafts',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatIconModule, MatCardModule, MatChipsModule, MatProgressSpinnerModule, NavbarComponent, TimeAgoPipe],
  templateUrl: './drafts.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './drafts.component.scss'
})
export class DraftsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private draftsService = inject(DraftsService);
  private dialog = inject(MatDialog);
  private snackBar = inject(MatSnackBar);

  bookId = this.route.snapshot.paramMap.get('bookId')!;
  chapterId = this.route.snapshot.paramMap.get('chapterId')!;

  drafts = signal<Draft[]>([]);
  loading = signal(true);

  ngOnInit(): void {
    this.draftsService.getDrafts(this.bookId, this.chapterId).subscribe({
      next: (drafts) => { this.drafts.set(drafts); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  createDraft(): void {
    const title = prompt('Name your draft (e.g. "Action scene rewrite"):');
    if (!title) return;
    this.draftsService.createDraft(this.bookId, this.chapterId, { title }).subscribe(draft => {
      this.drafts.update(list => [draft, ...list]);
    });
  }

  publishDraft(draft: Draft): void {
    const ref = this.dialog.open(PublishDraftDialogComponent, {
      data: { bookId: this.bookId, chapterId: this.chapterId, draft },
      width: '480px'
    });
    ref.afterClosed().subscribe(published => {
      if (published) {
        this.drafts.update(list => list.map(d => d.id === published.id ? published : d));
        this.snackBar.open('Draft published!', 'Dismiss', { duration: 3000 });
      }
    });
  }

  deleteDraft(draft: Draft): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: { title: 'Delete Draft', message: `Delete "${draft.title}"?`, confirmLabel: 'Delete', danger: true }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.draftsService.deleteDraft(this.bookId, this.chapterId, draft.id).subscribe(() => {
          this.drafts.update(list => list.filter(d => d.id !== draft.id));
        });
      }
    });
  }
}
