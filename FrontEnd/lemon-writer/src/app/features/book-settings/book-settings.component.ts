import { Component, inject, OnInit, signal } from '@angular/core';
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

@Component({
  selector: 'app-book-settings',
  standalone: true,
  imports: [
    RouterLink, ReactiveFormsModule, NavbarComponent, MatFormFieldModule, MatInputModule,
    MatButtonModule, MatIconModule, MatCheckboxModule, MatDividerModule, MatProgressSpinnerModule, MatSnackBarModule
  ],
  templateUrl: './book-settings.component.html',
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
    this.booksService.uploadCoverImage(this.bookId, file).subscribe({
      next: (res) => {
        const current = this.book();
        if (current) this.book.set({ ...current, coverImageUrl: res.coverImageUrl });
        this.snackBar.open('Cover image updated!', 'Dismiss', { duration: 3000 });
      },
      error: () => this.snackBar.open('Could not upload cover image.', 'Dismiss', { duration: 3000 })
    });
  }

  exportEpub(): void {
    this.exportService.downloadEpub(this.bookId, this.book()?.title || 'book');
    this.snackBar.open('Preparing EPUB download...', 'Dismiss', { duration: 3000 });
  }

  exportPdf(): void {
    this.exportService.downloadPdf(this.bookId, this.book()?.title || 'book');
    this.snackBar.open('Preparing PDF download...', 'Dismiss', { duration: 3000 });
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
