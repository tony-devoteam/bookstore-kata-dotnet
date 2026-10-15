# Bookstore Inventory API

An ASP.NET Core Minimal API for browsing a bookstore catalogue, registering books, and managing stock and availability. MongoDB persists inventory records.

## Prerequisites

- .NET 10 SDK
- Docker Engine with Docker Compose v2
- A MongoDB 7-compatible server when running the API outside Compose

## Getting started

Start the API and MongoDB together:

```bash
docker compose up --build
```

The API listens on `http://localhost:8080`. To run the API from the .NET CLI instead, start MongoDB and run:

```bash
docker compose up -d mongodb
dotnet run --project Bookstore.Api --urls http://localhost:8080
```

Configuration is supplied through environment variables:

| Variable | Default | Purpose |
|---|---|---|
| `MONGODB_URI` | `mongodb://localhost:27017` | MongoDB connection string |
| `MONGODB_DATABASE` | `bookstore` | Database name |
| `MONGODB_COLLECTION` | `books` | Inventory collection |
| `ASPNETCORE_URLS` | ASP.NET Core default | Listening addresses; Compose sets port `8080` |

## API documentation

- OpenAPI 3.1 JSON: <http://localhost:8080/openapi/v1.json> (available in every environment)
- Scalar interactive UI: <http://localhost:8080/scalar/v1> (non-production environments only)
- Executable requests: [docs/technical/smoke-tests.http](docs/technical/smoke-tests.http)
- [Functional documentation](docs/functional/functional-documentation.md)
- [Technical documentation](docs/technical/technical-documentation.md)
- [Known issues and resolutions](docs/technical/known-issues.md)

The file [docs/openapi/openapi.yaml](docs/openapi/openapi.yaml) is retained as the Java 0.1 migration-baseline contract. This migration introduces the versioned `/api/v1` contract; clients must move from `/api/books`, use `category` instead of the legacy string-valued `categoryId`, and use `PATCH /api/v1/books/{id}` for partial stock updates. MongoDB book documents remain readable without a data conversion. The generated OpenAPI document is the contract for the current .NET service.

## Main routes

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/health` | Readiness check including MongoDB connectivity |
| `GET` | `/api/v1/books` | List, filter, search, and page through books |
| `GET` | `/api/v1/books/{id}` | Retrieve a book by ObjectId |
| `POST` | `/api/v1/books` | Register a book |
| `PATCH` | `/api/v1/books/{id}` | Partially update quantity and/or availability |

All API errors use RFC 7807 Problem Details. The service currently has no authentication; see KI-33 in the known-issues document.

## Testing and coverage

Run unit, HTTP integration, and MongoDB Testcontainers integration tests:

```bash
dotnet test Bookstore.sln
```

The test project enforces **at least 70% line coverage** for the complete application assembly (generated OpenAPI support code is excluded). The most recent full run reported 96.39% line coverage. The report is written to `Bookstore.Tests/coverage.json`. MongoDB integration tests require Docker to be running.

## Architecture and design notes

- .NET 10, nullable reference types, Minimal APIs, FluentValidation, Serilog JSON logging, and central NuGet package management.
- Book aggregate and business rules in `Bookstore.Api/Domain`; application coordination and repository contracts in `Bookstore.Api/Application`; MongoDB adapter and indexes in `Bookstore.Api/Infrastructure`.
- One DI-managed `MongoClient` is shared for its lifetime. Database operations are asynchronous and cancellable.
- Filtering, sorting, and bounded pagination run in MongoDB. Compound indexes support category/title/author filters and registration-order sorting.
- No events, financial calculations, or authentication provider are present in the current domain and requirements.
- MongoDB documents keep the legacy `_id` and lower-camel-case fields; the reimplementation does not require a data migration.

## Maintainers

No code owner or support channel has been designated in the repository yet.
