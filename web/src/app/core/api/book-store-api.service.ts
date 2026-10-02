import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { Book, BookSearchResponse, ImportBookRequest } from './book.models';

@Injectable({ providedIn: 'root' })
export class BookStoreApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/v1';

  getBooks(): Observable<Book[]> {
    return this.http.get<Book[]>(`${this.baseUrl}/books`);
  }

  searchBooks(query: string, page = 1, pageSize = 12): Observable<BookSearchResponse> {
    return this.http.get<BookSearchResponse>(`${this.baseUrl}/discovery/books`, {
      params: { query, page, pageSize },
    });
  }

  importBook(request: ImportBookRequest): Observable<Book> {
    return this.http.post<Book>(`${this.baseUrl}/books`, request);
  }

  deleteBook(id: number): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/books/${id}`);
  }
}
