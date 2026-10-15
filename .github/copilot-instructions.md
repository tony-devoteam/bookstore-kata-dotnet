# Copilot Harness & Project Standards

Use the tools that fit the task. Stay in one chat. Do not invent phases, profiles, or process frameworks beyond what is installed.

---

## Policy: Harness Add-ons Only

Do **not** use, install, or suggest tools that GitHub Copilot already provides (read/edit/search/shell, filesystem, and other built-ins). Do not add a Filesystem MCP or anything that duplicates those capabilities.

Do **not** use or suggest MCPs, CLIs, or skills that require product registration, signup, API-key portals, or third-party accounts beyond the company’s existing Copilot / forge entitlements (e.g. Context7).

Prefer add-ons that are local-first or company-entitlement based and fill a gap Copilot does not already cover.

---

## Tool Map (Avoid Collisions)

| Need | Use | Do Not Also |
| :--- | :--- | :--- |
| Fetch a GitHub issue (title, body, comments, labels) | **GitHub MCP** (`issues` toolset) | Guessing issue text from memory; scraping HTML |
| Symbols, callers, callees, impact, “where is X?” | **CodeGraph** | Graphify for the same code-edge question; wide grep/read loops first |
| Docs, ADRs, schemas, cross-artifact / project map | **Graphify** | CodeGraph as if it held docs/ADR meaning |
| Noisy shell, tests, git logs | **RTK** (if wired) | Re-run the same command hoping for a shorter log; on failure/truncation open the RTK tee/full output |
| Shorter chat replies | **Caveman lite** only | Caveman on SPEC, ADR, OpenAPI, PR text, or any committed prose |
| Specs / plans / tasks | **Spec Kit** (`/speckit-*`) if installed | A second delivery framework (e.g. Superpowers) in the same repo |

When the user points at an issue (`#123` or a URL), load it with the GitHub MCP before planning or coding.

One answer path per question. If CodeGraph already answered a structural code question, do not re-ask Graphify (and the reverse for project/doc questions).

---

## Compression

- **RTK** shrinks what you *read* from the shell. Prefer it for tests and git noise when available.
- **Caveman lite** may shorten chat. Never caveman-compress durable artifacts.
- Do not enable Caveman `full` / `ultra` as a standing habit in this repo.

---

## Specs and Docs

- Prefer Spec Kit when it is installed; otherwise plain Markdown specs are fine.
- When writing ADRs, CONTEXT, OpenAPI, or SPEC: precise full sentences, no caveman fragments, no drive-by feature implementation unless the user asked for code.
- When implementing: follow the agreed spec; use CodeGraph to navigate; run real tests; fix from full failure output if RTK truncated.

---

## Conflict Rules (Hard)

1. **CodeGraph = code scope. Graphify = project scope.** Do not dual-query both for the same fact.
2. **One delivery spine.** Spec Kit *or* nothing process-heavy — never Spec Kit + Superpowers together.
3. **grill-with-docs** (if installed) is for alignment and ADRs/CONTEXT — not a cue to start implementing or running TDD loops.
4. Do not “suppress” tools with ritual phase talk. Just pick the right tool for the current ask.
5. Keep diffs small and matched to existing project conventions when this file is vendored into an app repo.

---

## Company .NET API Standards

### 1. Supported .NET Versions & Tooling
- **Mandatory Target Framework**: `net10.0` exclusively for all services[cite: 21].
- **Build Tooling**: The `.NET CLI` (`dotnet`) is the sole permitted build tool across local and CI/CD environments.

### 2. API Design & Architecture (Minimal APIs)
- **Architecture**: ASP.NET Core Minimal APIs exclusively (`app.MapGet()`, `app.MapPost()`, `app.MapPut()`, `app.MapDelete()`)[cite: 26]. Controller-based architectures (`ControllerBase` / `[ApiController]`) are strictly prohibited[cite: 26].
- **Route Modularization**: Routes must be organized logically using `RouteGroupBuilder` or extension modules.
- **Async Handlers**: All handlers must be asynchronous (`async Task<IResult>`) and accept a `CancellationToken` parameter.
- **Immutability**: Request and response DTOs must use C# `record` types with primary constructors.

### 3. API Specification (OpenAPI)
- **OpenAPI Requirement**: Expose an OpenAPI 3.0+ specification accessible via standard endpoints (e.g., `/openapi/v1.json`).
- **UI Documentation**: Include Scalar or Swagger UI in non-production environments.
- **Completeness**: Include full descriptions, request/response payloads, authentication requirements, and all possible HTTP status codes via `TypedResults` helpers[cite: 26].

### 4. Naming Conventions
- **REST Endpoints (URIs)**:
  - `kebab-case` for URIs (e.g., `/user-profiles`).
  - Plural nouns for resources (e.g., `/api/v1/users`).
  - No verbs in URIs (use HTTP verbs instead).
- **C# Codebase**:
  - **Namespaces**: `PascalCase` domain hierarchy using file-scoped namespaces (`namespace Company.Service.Users;`).
  - **Classes/Interfaces**: `PascalCase`. Interfaces prefixed with `I` (`IUserService`).
  - **Methods/Properties**: `PascalCase`. Async methods must end with `Async` (`GetUserByIdAsync`).
  - **Variables/Parameters**: `camelCase` (`customerName`).
  - **Private Fields**: `_camelCase` with leading underscore (`_logger`).
  - **Constants**: `UPPER_SNAKE_CASE` or `PascalCase`.

### 5. Project Setup & Package Best Practices
- **`.csproj` Standards**:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk.Web">
    <PropertyGroup>
      <TargetFramework>net10.0</TargetFramework>
      <Nullable>enable</Nullable>
      <ImplicitUsings>enable</ImplicitUsings>
    </PropertyGroup>
  </Project>