import { Component, inject, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDividerModule } from '@angular/material/divider';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';
import { BooksService } from '../../core/services/books.service';
import { ExportService } from '../../core/services/export.service';
import { ChaptersService } from '../../core/services/chapters.service';
import { Book } from '../../core/models/book.model';
import { Chapter } from '../../core/models/chapter.model';
import { HttpErrorResponse } from '@angular/common/http';

@Component({
  selector: 'app-book-settings',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, NavbarComponent, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatCheckboxModule, MatDividerModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './book-settings.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './book-settings.component.scss'
})
export class BookSettingsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private booksService = inject(BooksService);
  private chaptersService = inject(ChaptersService);
  private exportService = inject(ExportService);
  private snackBar = inject(MatSnackBar);
  private fb = inject(FormBuilder);

  bookId = this.route.snapshot.paramMap.get('bookId')!;
  book = signal<Book | null>(null);
  chapters = signal<Chapter[]>([]);
  loading = signal(true);
  saving = signal(false);

  form: FormGroup = this.fb.group({
    title: ['', Validators.required],
    description: [''],
    authorName: ['', Validators.required],
    isbn: [''],
    inbr: [''],
    isSeries: [false],
    seriesName: [''],
    seriesVolume: [null]
  });

  get isSeries() { return this.form.get('isSeries')?.value; }

  ngOnInit(): void {
    this.booksService.getBook(this.bookId).subscribe(book => {
      this.book.set(book);
      this.form.patchValue(book);
      this.loading.set(false);
    });
    this.chaptersService.getChapters(this.bookId).subscribe(chapters => {
      this.chapters.set(chapters);
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.booksService.updateBook(this.bookId, this.form.value).subscribe({
      next: (updated) => {
        this.book.set(updated);
        this.saving.set(false);
        this.snackBar.open('Book settings saved!', 'Dismiss', { duration: 3000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Could not save settings.', 'Dismiss', { duration: 3000 });
      }
    });
  }

  onCoverImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];

    const allowedTypes = ['image/jpeg', 'image/png', 'image/webp', 'image/gif'];
    if (!allowedTypes.includes(file.type)) {
      this.snackBar.open('Choose a JPEG, PNG, WebP, or GIF image.', 'Dismiss', { duration: 5000 });
      input.value = '';
      return;
    }

    const maxBytes = 5 * 1024 * 1024;
    if (file.size > maxBytes) {
      const actualSize = (file.size / 1024 / 1024).toFixed(1);
      this.snackBar.open(`This image is ${actualSize} MB. Cover images must be 5 MB or smaller.`, 'Dismiss', { duration: 7000 });
      input.value = '';
      return;
    }

    this.booksService.uploadCoverImage(this.bookId, file).subscribe({
      next: (res) => {
        const current = this.book();
        if (current) this.book.set({ ...current, coverImageUrl: res.coverImageUrl });
        this.snackBar.open('Cover image updated!', 'Dismiss', { duration: 3000 });
      },
      error: (error) => {
        const message = error?.error?.error || error?.error?.message ||
          'The cover could not be uploaded. Use JPEG, PNG, WebP, or GIF up to 5 MB.';
        this.snackBar.open(message, 'Dismiss', { duration: 7000 });
      }
    });
  }

  exportEpub(): void {
    this.snackBar.open('Preparing EPUB download...', 'Dismiss', { duration: 3000 });
    this.exportService.downloadEpub(this.bookId, this.book()?.title || 'book').subscribe({
      next: () => this.snackBar.open('EPUB downloaded successfully.', 'Dismiss', { duration: 3000 }),
      error: error => this.showExportError(error, 'EPUB')
    });
  }

  exportPdf(): void {
    this.snackBar.open('Preparing PDF download...', 'Dismiss', { duration: 3000 });
    this.exportService.downloadPdf(this.bookId, this.book()?.title || 'book').subscribe({
      next: () => this.snackBar.open('PDF downloaded successfully.', 'Dismiss', { duration: 3000 }),
      error: error => this.showExportError(error, 'PDF')
    });
  }

  private async showExportError(error: HttpErrorResponse, format: string): Promise<void> {
    let message = `Could not export this book as ${format}. Please try again.`;
    if (error.error instanceof Blob) {
      try {
        const payload = JSON.parse(await error.error.text()) as { error?: string; message?: string };
        message = payload.error || payload.message || message;
      } catch { /* Keep the user-friendly fallback for non-JSON server responses. */ }
    } else if (error.error?.error || error.error?.message) {
      message = error.error.error || error.error.message;
    }
    this.snackBar.open(message, 'Dismiss', { duration: 7000 });
  }

  addChapter(): void {
    const title = prompt('New chapter title:');
    if (!title) return;
    const existingOrders = this.chapters().map(c => c.order);
    const order = existingOrders.length > 0 ? Math.max(...existingOrders) + 1 : 0;
    this.chaptersService.createChapter(this.bookId, { title, order }).subscribe({
      next: (ch) => this.chapters.update(list => [...list, ch]),
      error: () => this.snackBar.open('Could not add chapter.', 'Dismiss', { duration: 3000 })
    });
  }
}
