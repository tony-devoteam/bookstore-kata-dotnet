# Known Issues and Guideline Gaps

This catalogue lists every defect, risky behaviour and deviation from
[`COMPANY_GUIDELINES.md`](../../COMPANY_GUIDELINES.md) found while documenting the Bookstore API.
All behaviours were reproduced against the running service (see [`smoke-tests.http`](smoke-tests.http)).
**None of them has been fixed**; the OpenAPI document describes them as-is.

It is intended as the backlog for the .NET 10 re-implementation.

**Severity scale:** 🔴 High (data corruption or a broken feature), 🟠 Medium (wrong contract or a security/operational risk), 🟡 Low (a convention issue or cosmetic).

## 1. Functional defects

| ID | Severity | Area | Behaviour observed | Root cause | Expected |
|----|----------|------|--------------------|------------|----------|
| KI-01 | 🔴 | `PUT /books` | Omitting `active` **deactivates the book** and stamps `inactiveDate`. Sending `{"id": "…"}` alone deactivates it and sets `quantity` to 0. | `UpdateBookDTO.active` and `quantity` are primitives defaulting to `false` and `0`. The service always `$set`s both. | Partial update: only change the fields the client sent. |
| KI-02 | 🔴 | `GET /books/single?id=` | Lookup by id never finds a book. | `BookService.getBook` compares strings with `==` (reference equality). | Use `equals`, ideally via a database query by `_id`. |
| KI-03 | 🟠 | `PUT /books` | An unknown but well-formed `id` returns `201` and nothing changes. | `updateOne` result (`matchedCount`) is ignored. | `404 Not Found`. |
| KI-04 | 🟠 | `PUT /books` | An empty-string `id` returns `201` with a newly generated id, and nothing is persisted. | `upsertBook` generates a new ObjectId for blank ids, but follows the update branch. | `400 Bad Request`. |
| KI-05 | 🟠 | `PUT /books` | Returns `201 Created` for an update. | Hard-coded status. | `200 OK` (or `204`). |
| KI-06 | 🟠 | `PUT /books` | The response is partial: title, author etc. are missing and `categoryId` is always `0`. | The response is built from the in-memory `Book` that was only partially filled. | Return the full updated book. |
| KI-07 | 🟠 | `POST /books` | A missing `title` returns `404`. | Hard-coded status. | `400 Bad Request` with a validation message. |
| KI-08 | 🟠 | `PUT /books` | A missing `id` returns `404`. | Hard-coded status. | `400 Bad Request`. |
| KI-09 | 🟠 | `POST /books` | An unknown, empty or numeric category returns `500`. | `validateCategory` returns `null`, which is dereferenced (`NullPointerException`). | `400 Bad Request` listing the valid categories. |
| KI-10 | 🟠 | `POST /books` | Request `categoryId` is a category **name** (string) while response `categoryId` is a number. | The DTO and the model use different types under the same name. | Consistent naming (`category` / `categoryId`). |
| KI-11 | 🟠 | `GET /books/single` | No match (or no parameter) returns `200` with an empty body. | `Response.ok().entity(null)`. | `404 Not Found`. |
| KI-12 | 🟠 | `GET /books/single?author=` | Always `500` ("not implemented"). | `RuntimeException` thrown in the controller. | Implement the filter, or return `501` / `400`. |
| KI-13 | 🟠 | `GET /books?category=` | An unknown category returns **all** books. | The fallback branch in `getBookList`. | An empty list or `400`. |
| KI-14 | 🟡 | `GET /books?category=`, `/single?name=` | Matching is exact and case-sensitive. | `String.equals`. | Case-insensitive matching on category; possibly partial matching on title. |
| KI-15 | 🟡 | `POST /books` | Empty title, negative quantity and duplicate titles are accepted. | No validation. | FluentValidation rules. |
| KI-16 | 🟡 | `POST /books` with `active: false` | `inactiveDate` is not set automatically (unlike `PUT`). | The create branch skips the deactivation rule. | Apply the same rule on create. |
| KI-17 | 🟡 | `GET /books/single?name=` | A stored book with a `null` title would make every title search fail with `500`. | `b.getTitle().equals(name)`. | Null-safe comparison. |
| KI-18 | 🟡 | Catalogue | The seed script creates a `categories` collection that the API never reads; categories are hard-coded in `Category.STATIC_CATEGORIES`. | Duplicate source of truth. | Pick one source. |
| KI-19 | 🟡 | README | The README says the API supports deleting books; there is no `DELETE` operation (`405`). | Documentation drift. | Implement it or fix the README. |

## 2. Error handling and contract

| ID | Severity | Behaviour observed | Guideline |
|----|----------|--------------------|-----------|
| KI-20 | 🟠 | Every unhandled error returns a Grizzly **HTML** page with `500` and `Connection: close`. That includes client errors such as malformed JSON and wrong JSON types, which should be `400`. | §7: RFC 7807 Problem Details, standard status codes |
| KI-21 | 🟠 | Other errors (`400`, `404`, `405`, `406`, `415`) have empty bodies, with no `type`, `title`, `detail` or `instance` (trace id). | §7 |
| KI-22 | 🟡 | Dates are serialised as `2002-11-08T00:00:00Z[UTC]` (not RFC 3339). Date-only input fails with `500`. | General interoperability |
| KI-23 | 🟡 | `POST` returns no `Location` header. | REST conventions |

## 3. API design and naming

| ID | Severity | Behaviour observed | Guideline |
|----|----------|--------------------|-----------|
| KI-24 | 🟡 | `/books/single?id=…` instead of `/books/{id}`. | §4: resource-oriented URIs |
| KI-25 | 🟡 | The health probe lives under the resource path (`/books/health`). | §4 |
| KI-26 | 🟡 | No versioning in the path (`/api/v1/...`). | §4 example `/api/v1/users` |
| KI-27 | 🟡 | `PUT /books` takes the id in the body, not the path. `PUT` performs a partial update (semantically `PATCH`). | REST conventions |
| KI-28 | 🟡 | No pagination on `GET /books`. | Scalability |

## 4. Operational and security

| ID | Severity | Behaviour observed | Guideline |
|----|----------|--------------------|-----------|
| KI-29 | 🟠 | The MongoDB URI is hard-coded (`mongodb://mongodb:27017`); the `MONGODB_URI` environment variable set by Docker Compose is ignored. | Configuration best practice |
| KI-30 | 🟠 | A new `MongoClient` (connection pool) is created and closed on **every** call, and clients leak when an exception is thrown before `close()`. | Performance and resource safety |
| KI-31 | 🟠 | Every lookup and filter reads the **whole collection** into memory, then filters in Java. | Database performance |
| KI-32 | 🟠 | When MongoDB is down, requests hang about 30 s before the `500`, and `/books/health` still answers `200`. | Observability |
| KI-33 | 🟠 | No authentication or authorisation; anyone can change stock. | §3: authentication requirements |
| KI-34 | 🟡 | No structured (JSON) logging and no trace id; full stack traces are written to stdout. | §8 |
| KI-35 | 🟡 | The port and base URI are hard-coded; there is no graceful shutdown. | Operations |
| KI-36 | 🟡 | No dependency injection: `BookController()` creates `new BookService()`. | Testability |
| KI-37 | 🟡 | Only one controller unit test; no service or integration tests. Coverage is far below the 70% target. | §9 |

## 5. Platform

| ID | Severity | Observation | Guideline |
|----|----------|-------------|-----------|
| KI-38 | 🟠 | Java 11 / Jersey / Maven instead of .NET 10 Minimal APIs with the `dotnet` CLI. | §1, §2 |
