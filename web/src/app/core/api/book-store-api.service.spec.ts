import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';

import { BookStoreApiService } from './book-store-api.service';

describe('BookStoreApiService', () => {
  let api: BookStoreApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });

    api = TestBed.inject(BookStoreApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads the locally owned catalog', async () => {
    const result = firstValueFrom(api.getBooks());
    const request = http.expectOne('/api/v1/books');

    expect(request.request.method).toBe('GET');
    request.flush([]);
    await expect(result).resolves.toEqual([]);
  });

  it('sends discovery searches through the API boundary', async () => {
    const result = firstValueFrom(api.searchBooks('fantastic fox', 1, 12));
    const request = http.expectOne(
      (candidate) =>
        candidate.url === '/api/v1/discovery/books' &&
        candidate.params.get('query') === 'fantastic fox',
    );

    expect(request.request.method).toBe('GET');
    request.flush({ query: 'fantastic fox', page: 1, pageSize: 12, total: 0, items: [] });
    expect((await result).items).toEqual([]);
  });
});
