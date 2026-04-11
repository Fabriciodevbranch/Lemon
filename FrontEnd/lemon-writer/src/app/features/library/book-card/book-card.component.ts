import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatChipsModule } from '@angular/material/chips';
import { MatDialog } from '@angular/material/dialog';
import { Book } from '../../../core/models/book.model';
import { BooksService } from '../../../core/services/books.service';
import { ConfirmDialogComponent } from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-book-card',
  standalone: true,
  imports: [RouterLink, MatCardModule, MatButtonModule, MatIconModule, MatMenuModule, MatChipsModule],
  templateUrl: './book-card.component.html',
  styleUrl: './book-card.component.scss'
})
export class BookCardComponent {
  @Input({ required: true }) book!: Book;
  @Output() bookDeleted = new EventEmitter<string>();

  private booksService = inject(BooksService);
  private dialog = inject(MatDialog);

  deleteBook(): void {
    const ref = this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Book',
        message: `Are you sure you want to delete "${this.book.title}"? This action cannot be undone.`,
        confirmLabel: 'Delete',
        danger: true
      }
    });
    ref.afterClosed().subscribe(confirmed => {
      if (confirmed) {
        this.booksService.deleteBook(this.book.id).subscribe(() => {
          this.bookDeleted.emit(this.book.id);
        });
      }
    });
  }
}
