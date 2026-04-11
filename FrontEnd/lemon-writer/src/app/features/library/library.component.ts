import { Component, inject, OnInit, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog } from '@angular/material/dialog';
import { BooksService } from '../../core/services/books.service';
import { Book } from '../../core/models/book.model';
import { BookCardComponent } from './book-card/book-card.component';
import { CreateBookDialogComponent } from './create-book-dialog/create-book-dialog.component';
import { NavbarComponent } from '../../shared/components/navbar/navbar.component';

@Component({
  selector: 'app-library',
  standalone: true,
  imports: [MatButtonModule, MatIconModule, MatProgressSpinnerModule, BookCardComponent, NavbarComponent],
  templateUrl: './library.component.html',
  styleUrl: './library.component.scss'
})
export class LibraryComponent implements OnInit {
  private booksService = inject(BooksService);
  private dialog = inject(MatDialog);

  books = signal<Book[]>([]);
  loading = signal(true);
  error = signal('');

  ngOnInit(): void {
    this.loadBooks();
  }

  loadBooks(): void {
    this.loading.set(true);
    this.booksService.getBooks().subscribe({
      next: (books) => { this.books.set(books); this.loading.set(false); },
      error: () => { this.error.set('Could not load your library.'); this.loading.set(false); }
    });
  }

  openCreateDialog(): void {
    const ref = this.dialog.open(CreateBookDialogComponent, { width: '500px' });
    ref.afterClosed().subscribe(book => {
      if (book) this.books.update(list => [book, ...list]);
    });
  }

  onBookDeleted(bookId: string): void {
    this.books.update(list => list.filter(b => b.id !== bookId));
  }
}
