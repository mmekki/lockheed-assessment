# Book Store

Book Store is an Angular and ASP.NET Core application for finding real books through Open Library and importing selected books into a store-owned SQLite catalog. Once imported, a book is served entirely from the local store.

## Repository structure

- `api/BookStore.Domain` — framework-independent entities and domain types
- `api/BookStore.Application` — application ports plus CQRS feature slices and handlers
- `api/BookStore.Infrastructure` — EF Core/SQLite persistence and Open Library adapters
- `api/BookStore.Api` — ASP.NET Core transport and dependency-composition layer
- `api/BookStore.Api.Tests` — application-handler and integration-boundary tests
- `web` — Angular 21 client with PrimeNG 21 and Tailwind CSS 4

The API follows Clean Architecture dependency direction: Domain has no project dependencies,
Application depends only on Domain, Infrastructure implements Application ports, and API is the
outer composition layer. Application behavior is organized as vertical CQRS slices under
`BookStore.Application/Features`; controllers send requests through the in-process mediator and
do not access EF Core or Open Library directly.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24 LTS](https://nodejs.org/) and npm 11

The repository pins the .NET 10 SDK line in `global.json` while allowing compatible feature-band updates. The exact JavaScript dependency graph is recorded in `web/package-lock.json`.

## Run the API

From the repository root:

```powershell
dotnet restore api/BookStore.slnx --configfile NuGet.Config
dotnet tool restore
dotnet tool run dotnet-ef database update --project api/BookStore.Infrastructure --startup-project api/BookStore.Api
dotnet run --project api/BookStore.Api --launch-profile http
```

The API listens on `http://localhost:5080`. Its health endpoint is `http://localhost:5080/health`, and its development OpenAPI document is `http://localhost:5080/openapi/v1.json`.

Core endpoints:

- `GET /api/v1/discovery/books?query=...` searches Open Library without persisting results.
- `POST /api/v1/books` imports a selected result with its store-owned genre, price, and stock.
- `GET /api/v1/books` and `GET /api/v1/books/{id}` read only from SQLite.
- `PUT /api/v1/books/{id}` updates store-owned fields and the optional description.
- `DELETE /api/v1/books/{id}` removes a locally owned book.

Cover images are downloaded once during import, stored in SQLite, and served from `GET /api/v1/books/{id}/cover`. Imported-book pages therefore do not depend on Open Library being available.

## Run the web client

From the repository root:

```powershell
cd web
npm ci
npm start
```

The web client listens on `http://localhost:4200`.

The Angular development server proxies `/api` requests to `http://localhost:5080`, so both projects should be running while using the application. The catalog is available at `/`; use `/discover` to search Open Library and import a selected result.

## Configuration and secrets

The application requires no secrets or API keys. Open Library does not require an API key. The default SQLite connection creates `bookstore.db` locally. Safe development defaults belong in `appsettings.Development.json`; sensitive values must use environment variables or ASP.NET Core user secrets and must never be committed.

ASP.NET Core configuration keys can be supplied as environment variables by replacing nested separators with double underscores. For example, `OpenLibrary:BaseUrl` can be overridden with `OpenLibrary__BaseUrl`.

Local `.env` files, SQLite databases, build output, and IDE state are ignored by Git.

## Verify the projects

```powershell
dotnet build api/BookStore.slnx --no-restore
dotnet test api/BookStore.slnx --no-restore
cd web
npm test -- --watch=false
npm run build
```

## Current status

The end-to-end catalog and import workflow is implemented. The next increments will add local book editing, broader component coverage, CI, and final submission documentation.
