# Bookstore Inventory — Functional Documentation

| | |
|---|---|
| **Audience** | Product owners, business managers, QA and API consumers |
| **Scope** | Current .NET 10 Bookstore Inventory API, version 1 |
| **Contract** | Runtime OpenAPI document at `/openapi/v1.json`; the static YAML under `docs/openapi` documents the former Java implementation |

## 1. Purpose and actors

The service is the record of the titles held by a bookstore, their stock quantities, and whether each title is currently offered for sale. Catalogue managers register books, stock clerks adjust quantity, merchandisers activate or deactivate books, and storefronts browse the catalogue. Operations can check API and database readiness.

The service has no user interface other than its non-production Scalar API explorer. There is currently no login or authorization: any reachable client can read and change inventory.

## 2. Book and category data

A book has a system-generated 24-character MongoDB ObjectId, a required non-empty title, optional author/description/language/publisher/publication date, a category, quantity, availability, and optional inactive date. Duplicate titles are allowed. Books are not deleted; inactive books remain visible.

The fixed categories are Fiction (1), Non-Fiction (2), Science (3), Biography (4), Children (5), and Programming (6). Category `0` means no category. Requests use the category **name**; responses use numeric `categoryId`. Category names are matched without regard to capitalization. An unknown category is rejected.

## 3. Business rules

### Catalogue

- Lists both active and inactive books.
- Supports optional category, exact-title, and exact-author filters.
- Category filters are case-insensitive; title and author filters are exact matches.
- Results are ordered by registration identifier and paged. The default page size is 50; the maximum is 100.
- An invalid category, page, or page size returns a validation error rather than silently broadening the result.

### Registration

- Title is mandatory and cannot be empty or whitespace.
- Quantity defaults to zero and cannot be negative.
- Active defaults to true. Registering an inactive book sets `inactiveDate` automatically.
- Category is optional; omitted category is stored as `categoryId: 0`.
- Publication and inactive dates require an ISO 8601 date-time with an explicit UTC offset.
- The server generates the identifier. Duplicate titles remain allowed.

### Stock and availability

- `PATCH /api/v1/books/{id}` changes only the `quantity` and/or `active` fields supplied by the caller.
- At least one of those fields must be supplied. Omitted fields retain their current values.
- Negative quantity is rejected.
- Deactivation stamps the current UTC time. Reactivation clears `inactiveDate`.
- A successful update returns the complete updated book. A missing book returns `404`.

## 4. HTTP operations

| Method and path | Behavior |
|---|---|
| `GET /health` | Returns `200` when MongoDB can be pinged; otherwise `503`. |
| `GET /api/v1/books` | Lists books; supports `category`, `title`, `author`, `page`, and `pageSize` query parameters. |
| `GET /api/v1/books/{id}` | Returns one book or `404`; a malformed ObjectId returns `400`. |
| `POST /api/v1/books` | Creates a book, returns `201`, and sets `Location`. |
| `PATCH /api/v1/books/{id}` | Partially updates stock/availability and returns the complete book. |

All error responses use RFC 7807 Problem Details with an instance URN and trace ID. Raw exception details are not returned. The API has no delete operation and no authentication scheme.

## 5. Operational expectations

The API uses `MONGODB_URI`, `MONGODB_DATABASE`, and `MONGODB_COLLECTION` configuration. MongoDB disconnection is observable through the readiness endpoint; connection selection times out after three seconds. See [technical documentation](../technical/technical-documentation.md) for startup, deployment, and verification details.
