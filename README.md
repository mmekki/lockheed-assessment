# Book Store

Book Store is a two-project application for finding real books through Open Library and importing selected books into a store-owned SQLite catalog. Once imported, a book is served entirely from the local store.

## Repository structure

- `api/BookStore.Api` — ASP.NET Core REST API targeting .NET 10
- `web` — Angular 21 client with PrimeNG 21 and Tailwind CSS 4

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 24 LTS](https://nodejs.org/) and npm 11

The repository pins the .NET 10 SDK line in `global.json` while allowing compatible feature-band updates. The exact JavaScript dependency graph is recorded in `web/package-lock.json`.

## Run the API

From the repository root:

```powershell
dotnet restore api/BookStore.slnx --configfile NuGet.Config
dotnet tool restore
dotnet tool run dotnet-ef database update --project api/BookStore.Api
dotnet run --project api/BookStore.Api --launch-profile http
```

The API listens on `http://localhost:5080`. Its health endpoint is `http://localhost:5080/health`, and its development OpenAPI document is `http://localhost:5080/openapi/v1.json`.

## Run the web client

From the repository root:

```powershell
cd web
npm ci
npm start
```

The web client listens on `http://localhost:4200`.

## Configuration and secrets

The application requires no secrets or API keys. Open Library does not require an API key. The default SQLite connection creates `bookstore.db` locally. Safe development defaults belong in `appsettings.Development.json`; sensitive values must use environment variables or ASP.NET Core user secrets and must never be committed.

ASP.NET Core configuration keys can be supplied as environment variables by replacing nested separators with double underscores. For example, the future `OpenLibrary:BaseUrl` setting can be overridden with `OpenLibrary__BaseUrl`.

Local `.env` files, SQLite databases, build output, and IDE state are ignored by Git.

## Verify the projects

```powershell
dotnet build api/BookStore.slnx --no-restore
cd web
npm test -- --watch=false
npm run build
```

## Current status

The repository foundation and SQLite book model are complete. Open Library discovery and the import workflow will be added in subsequent incremental commits.
