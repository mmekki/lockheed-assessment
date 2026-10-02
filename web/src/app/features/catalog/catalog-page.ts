import { CurrencyPipe } from '@angular/common';
import { Component, DestroyRef, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SkeletonModule } from 'primeng/skeleton';
import { TagModule } from 'primeng/tag';

import { BookStoreApiService } from '../../core/api/book-store-api.service';
import { Book, formatGenre } from '../../core/api/book.models';

@Component({
  selector: 'app-catalog-page',
  imports: [ButtonModule, CardModule, CurrencyPipe, RouterLink, SkeletonModule, TagModule],
  templateUrl: './catalog-page.html',
})
export class CatalogPage implements OnInit {
  private readonly api = inject(BookStoreApiService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly books = signal<Book[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly deletingId = signal<number | null>(null);
  protected readonly formatGenre = formatGenre;

  ngOnInit(): void {
    this.loadBooks();
  }

  protected loadBooks(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api
      .getBooks()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (books) => {
          this.books.set(books);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('The local catalog could not be loaded. Confirm that the API is running.');
          this.loading.set(false);
        },
      });
  }

  protected deleteBook(book: Book): void {
    if (!window.confirm(`Remove “${book.title}” from your store?`)) {
      return;
    }

    this.deletingId.set(book.id);
    this.api
      .deleteBook(book.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.books.update((books) => books.filter((candidate) => candidate.id !== book.id));
          this.deletingId.set(null);
        },
        error: () => {
          this.error.set(`“${book.title}” could not be removed.`);
          this.deletingId.set(null);
        },
      });
  }
}
