# Known Issues and Migration Resolutions

This checklist records the Java implementation issues identified in the migration baseline and their status in the .NET 10 implementation.

| ID | Area | Migration result |
|---|---|---|
| KI-01 | Partial stock update | Fixed: omitted fields remain unchanged. |
| KI-02 | Lookup by id | Fixed: `GET /api/v1/books/{id}` queries MongoDB by ObjectId. |
| KI-03 | Update of unknown id | Fixed: returns `404`. |
| KI-04 | Empty update id | Fixed: id is a required route segment and malformed ids return `400`. |
| KI-05 | Update returns `201` | Fixed: PATCH returns `200`. |
| KI-06 | Partial update response | Fixed: response contains the full updated book. |
| KI-07 | Missing title status | Fixed: title validation returns `400` Problem Details. |
| KI-08 | Missing update id status | Fixed by moving the identifier into the PATCH route; the route cannot silently create an id. |
| KI-09 | Invalid category | Fixed: unknown category returns `400` with a validation detail. |
| KI-10 | Request/response category naming | Fixed: request field is `category`; response field is numeric `categoryId`. |
| KI-11 | Missing book result | Fixed: returns `404` Problem Details. |
| KI-12 | Author search | Fixed: exact author filtering is available on the list endpoint. |
| KI-13 | Unknown category filter | Fixed: returns `400`, not the unfiltered catalogue. |
| KI-14 | Category case sensitivity | Fixed: category matching is case-insensitive. Title and author searches remain exact matches. |
| KI-15 | Invalid title/negative stock/duplicates | Empty titles and negative quantities are rejected. Duplicate titles remain allowed, matching the functional business rule. |
| KI-16 | Inactive registration date | Fixed: inactive books receive an automatic inactive date. |
| KI-17 | Null title during search | Fixed: API-created books require a title; legacy null titles restore as empty and do not cause a null dereference. |
| KI-18 | Duplicate category source | Fixed: categories are a single in-code reference list; the obsolete category seed script was removed. |
| KI-19 | README claimed deletion | Fixed: documentation states that deletion is not supported. |
| KI-20 | HTML/500 errors | Fixed: malformed JSON and request validation errors return appropriate client statuses and Problem Details. |
| KI-21 | Empty error bodies | Fixed: errors include RFC 7807 type, title, status, detail, instance trace URN, and trace ID. |
| KI-22 | Non-RFC date values | Fixed: date-time input requires an explicit offset and output uses ISO 8601 `DateTimeOffset`. |
| KI-23 | Missing create Location | Fixed: POST returns a `Location` header. |
| KI-24 | Verb-like lookup route | Fixed: id lookup uses `/api/v1/books/{id}`. |
| KI-25 | Health nested under books | Fixed: readiness is `/health`. |
| KI-26 | Missing API version | Fixed: book routes use `/api/v1`. |
| KI-27 | Update id in body / partial PUT | Fixed: partial update uses PATCH and the id is in the path. |
| KI-28 | Unbounded catalogue | Fixed: list results are paged with a maximum page size of 100. |
| KI-29 | Hard-coded Mongo URI | Fixed: `MONGODB_URI` and related settings are configurable. |
| KI-30 | MongoClient per operation | Fixed: one singleton MongoClient is shared by the application. |
| KI-31 | Whole collection loaded for filters | Fixed: filters, sorting, and paging execute in MongoDB with supporting indexes. |
| KI-32 | Health ignores MongoDB | Fixed: readiness pings MongoDB; server selection times out after three seconds. |
| KI-33 | No authentication | **Open:** no authentication provider or authorization policy was specified. Anonymous access is preserved and documented; deploy only on a trusted network until an auth requirement is agreed. |
| KI-34 | Unstructured logs / missing trace id | Fixed: JSON console logs and `X-Trace-Id` / Problem Details trace fields are enabled. |
| KI-35 | Fixed port / no graceful shutdown | Fixed: host addresses use ASP.NET Core configuration; host lifecycle handles shutdown. |
| KI-36 | No dependency injection | Fixed: services, repository, id generation, and Mongo client are registered through DI. |
| KI-37 | Insufficient test coverage | Fixed: xUnit unit, WebApplicationFactory, and MongoDB Testcontainers tests; `dotnet test` enforces 70% line coverage. |
| KI-38 | Java/Maven platform | Fixed: .NET 10, ASP.NET Core Minimal APIs, and the `dotnet` CLI replace Java/Jersey/Maven. |

No database schema conversion was needed: the MongoDB adapter preserves the existing ObjectId and field names. KI-33 is the sole unresolved guideline gap because choosing an authentication protocol would change access behavior without a supplied requirement.
