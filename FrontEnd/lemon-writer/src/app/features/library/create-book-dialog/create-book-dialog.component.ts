import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BooksService } from '../../../core/services/books.service';
import { AuthService } from '../../../core/auth/auth.service';

@Component({
  selector: 'app-create-book-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatCheckboxModule, MatProgressSpinnerModule],
  templateUrl: './create-book-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './create-book-dialog.component.scss'
})
export class CreateBookDialogComponent {
  private fb = inject(FormBuilder);
  private booksService = inject(BooksService);
  private authService = inject(AuthService);
  private dialogRef = inject(MatDialogRef<CreateBookDialogComponent>);

  form: FormGroup = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    authorName: ['', Validators.required],
    isSeries: [false],
    seriesName: [''],
    seriesVolume: [null]
  });

  loading = false;
  error = '';

  get isSeries() { return this.form.get('isSeries')?.value; }

  onSubmit(): void {
    if (this.form.invalid) return;
    const authorId = this.authService.currentUser$()?.id;
    if (!authorId) {
      this.error = 'You must be logged in to create a book.';
      return;
    }
    this.loading = true;
    this.booksService.createBook({ ...this.form.value, authorId }).subscribe({
      next: (book) => this.dialogRef.close(book),
      error: (err) => {
        this.error = err?.error?.message || 'Could not create book.';
        this.loading = false;
      }
    });
  }
}
