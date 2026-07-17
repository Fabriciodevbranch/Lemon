import { Component, Inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DraftsService } from '../../../core/services/drafts.service';
import { Draft } from '../../../core/models/draft.model';

export interface PublishDraftDialogData {
  bookId: string;
  chapterId: string;
  draft: Draft;
}

@Component({
  selector: 'app-publish-draft-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './publish-draft-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './publish-draft-dialog.component.scss'
})
export class PublishDraftDialogComponent {
  form: FormGroup;
  loading = false;
  error = '';

  constructor(
    private fb: FormBuilder,
    private draftsService: DraftsService,
    public dialogRef: MatDialogRef<PublishDraftDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: PublishDraftDialogData
  ) {
    this.form = this.fb.group({
      publishMessage: ['', Validators.required]
    });
  }

  onPublish(): void {
    if (this.form.invalid) return;
    this.loading = true;
    this.draftsService.publishDraft(
      this.data.bookId, this.data.chapterId, this.data.draft.id,
      { publishMessage: this.form.value.publishMessage }
    ).subscribe({
      next: (draft) => this.dialogRef.close(draft),
      error: (err) => {
        this.error = err?.error?.message || 'Could not publish draft.';
        this.loading = false;
      }
    });
  }
}
