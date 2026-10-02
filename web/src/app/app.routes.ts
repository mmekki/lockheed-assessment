import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/catalog/catalog-page').then((module) => module.CatalogPage),
    title: 'Book Store',
  },
  {
    path: 'discover',
    loadComponent: () =>
      import('./features/discovery/discovery-page').then((module) => module.DiscoveryPage),
    title: 'Add a book | Book Store',
  },
  { path: '**', redirectTo: '' },
];
