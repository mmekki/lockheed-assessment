import { DecimalPipe } from '@angular/common';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';

import { BookStoreApiService } from '../../core/api/book-store-api.service';
import {
  bookGenres,
  BookGenre,
  DiscoveredBook,
  ImportBookRequest,
} from '../../core/api/book.models';

@Component({
  selector: 'app-discovery-page',
  imports: [
    ButtonModule,
    CardModule,
    DecimalPipe,
    FormsModule,
    InputNumberModule,
    InputTextModule,
    ReactiveFormsModule,
    RouterLink,
    SelectModule,
    TextareaModule,
  ],
  templateUrl: './discovery-page.html',
})
export class DiscoveryPage {
  private readonly api = inject(BookStoreApiService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly router = inject(Router);

  protected query = '';
  protected readonly results = signal<DiscoveredBook[]>([]);
  protected readonly total = signal(0);
  protected readonly selectedBook = signal<DiscoveredBook | null>(null);
  protected readonly searching = signal(false);
  protected readonly importing = signal(false);
  protected readonly searchAttempted = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly genres = [...bookGenres];

  protected readonly importForm = new FormGroup({
    genre: new FormControl<BookGenre>('Fiction', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    price: new FormControl<number | null>(null, [Validators.required, Validators.min(0)]),
    stock: new FormControl<number | null>(null, [Validators.min(0)]),
    description: new FormControl('', {
      nonNullable: true,
      validators: [Validators.maxLength(5000)],
    }),
  });

  protected search(): void {
    const query = this.query.trim();
    if (!query) {
      this.error.set('Enter a title, author, or ISBN to search.');
      return;
    }

    this.searching.set(true);
    this.searchAttempted.set(true);
    this.error.set(null);
    this.selectedBook.set(null);

    this.api
      .searchBooks(query)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.results.set(response.items);
          this.total.set(response.total);
          this.searching.set(false);
        },
        error: () => {
          this.error.set('Open Library could not be searched. Please try again.');
          this.searching.set(false);
        },
      });
  }

  protected selectBook(book: DiscoveredBook): void {
    this.selectedBook.set(book);
    this.error.set(null);
    this.importForm.reset({
      genre: 'Fiction',
      price: null,
      stock: null,
      description: '',
    });
  }

  protected importBook(): void {
    const book = this.selectedBook();
    if (!book) {
      return;
    }

    if (this.importForm.invalid) {
      this.importForm.markAllAsTouched();
      return;
    }

    const form = this.importForm.getRawValue();
    const request: ImportBookRequest = {
      ...book,
      genre: form.genre,
      price: form.price!,
      stock: form.stock,
      description: form.description.trim() || null,
    };

    this.importing.set(true);
    this.error.set(null);
    this.api
      .importBook(request)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => void this.router.navigateByUrl('/'),
        error: (error: { status?: number }) => {
          this.error.set(
            error.status === 409
              ? 'That Open Library book is already in your store.'
              : 'The book could not be imported. Please review the details and try again.',
          );
          this.importing.set(false);
        },
      });
  }
}
