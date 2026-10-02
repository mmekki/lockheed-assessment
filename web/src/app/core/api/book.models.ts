export type BookGenre =
  | 'Fiction'
  | 'NonFiction'
  | 'Mystery'
  | 'Thriller'
  | 'ScienceFiction'
  | 'Fantasy'
  | 'Romance'
  | 'HistoricalFiction'
  | 'Biography'
  | 'History'
  | 'Science'
  | 'Technology'
  | 'SelfHelp'
  | 'Children'
  | 'YoungAdult'
  | 'Other';

export interface DiscoveredBook {
  openLibraryKey: string;
  title: string;
  author: string;
  isbn: string | null;
  publishedYear: number | null;
  coverUrl: string | null;
}

export interface BookSearchResponse {
  query: string;
  page: number;
  pageSize: number;
  total: number;
  items: DiscoveredBook[];
}

export interface Book extends DiscoveredBook {
  id: number;
  description: string | null;
  genre: BookGenre;
  price: number;
  stock: number | null;
}

export interface ImportBookRequest extends DiscoveredBook {
  description: string | null;
  genre: BookGenre;
  price: number;
  stock: number | null;
}

export const bookGenres: ReadonlyArray<{ label: string; value: BookGenre }> = [
  { label: 'Fiction', value: 'Fiction' },
  { label: 'Non-fiction', value: 'NonFiction' },
  { label: 'Mystery', value: 'Mystery' },
  { label: 'Thriller', value: 'Thriller' },
  { label: 'Science fiction', value: 'ScienceFiction' },
  { label: 'Fantasy', value: 'Fantasy' },
  { label: 'Romance', value: 'Romance' },
  { label: 'Historical fiction', value: 'HistoricalFiction' },
  { label: 'Biography', value: 'Biography' },
  { label: 'History', value: 'History' },
  { label: 'Science', value: 'Science' },
  { label: 'Technology', value: 'Technology' },
  { label: 'Self-help', value: 'SelfHelp' },
  { label: 'Children', value: 'Children' },
  { label: 'Young adult', value: 'YoungAdult' },
  { label: 'Other', value: 'Other' },
];

export function formatGenre(genre: BookGenre): string {
  return bookGenres.find((option) => option.value === genre)?.label ?? genre;
}
