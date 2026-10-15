# Bookstore

Basic app using Java 11 and MongoDB.

It requires a bookstore database to be created in MongoDB.

This API manages book inventory in a bookstore. 
List books, create new ones, update, and delete are some of the supported operations.

## Optional: Use docker compose to run the application

```bash
docker-compose up
``` 

## API Documentation

The static [OpenAPI 3.1 specification](docs/openapi/openapi.yaml) documents the **current
behaviour** of every endpoint, including its known quirks. The running application does not expose
an OpenAPI or Swagger UI endpoint.

Validate the specification with `npx @redocly/cli lint`, or generate a standalone HTML page with:

```bash
npx @redocly/cli build-docs docs/openapi/openapi.yaml -o target/api-docs.html
```

| Document | Audience |
|----------|----------|
| [Functional documentation](docs/functional/functional-documentation.md) | Product Owner / business: business rules, use cases, limitations |
| [Technical documentation](docs/technical/technical-documentation.md) | Developers / architects: high-level and low-level design, data model, integrations, build and run |
| [Known issues](docs/technical/known-issues.md) | Backlog of defects and gaps against the company guidelines |
| [Smoke tests](docs/technical/smoke-tests.http) | Executable HTTP requests covering every endpoint and error case |

## Testing

```bash
mvn test
# or, without a local JDK/Maven:
docker run --rm -v "$PWD:/app" -v bookstore-m2:/root/.m2 -w /app maven:3.8.6-eclipse-temurin-11 mvn -B test
```

# Harness Architecture

## Tools

| Tool | Why | Install (reference) |
|------|-----|---------------------|
| **GitHub MCP** | Fetch issues (included in `.mcp.json`) | Uses Copilot GitHub auth — `https://api.githubcopilot.com/mcp/` with `X-MCP-Toolsets: issues` |
| **CodeGraph** | Code navigation (base) | `npm i -g @colbymchenry/codegraph` → `codegraph install` → in repo `codegraph init` |
| **RTK** | Compress shell/test noise | `brew install rtk-ai/tap/rtk` (or Cargo from `rtk-ai/rtk`) → `rtk gain` to verify → `rtk init -g --copilot` |
| **Caveman** | Chat prose lite only | `npx skills add JuliusBrussee/caveman -g` → use `/caveman lite` in chat |
| **Graphify** (optional) | Project/docs/schema map | Install per [Graphify](https://github.com/safishamsi/graphify), then add the MCP block below |
| **Spec Kit** (optional) | Spec-driven delivery | [github/spec-kit](https://github.com/github/spec-kit) `specify init` for Copilot |
| **grill-with-docs** (optional) | ADRs / CONTEXT alignment | From [mattpocock/skills](https://github.com/mattpocock/skills) |

### Optional Graphify MCP

Add only if Graphify is installed. Portable `.mcp.json`:

```json
"graphify": {
  "type": "stdio",
  "command": "graphify",
  "args": [".", "--mcp"]
}
```

VS Code `.vscode/mcp.json` uses the same server entry under `"servers"`.

## How Harness is configured

- **GitHub MCP** to load an issue before work  
- **CodeGraph** for code structure  
- **Graphify** for project/docs (if present)  
- **RTK** for noisy shell  
- **Caveman lite** for chat only — never for SPEC/ADR/PR text  
- **Spec Kit** alone as the delivery spine if you use one  

No phase profiles. No parallel Superpowers + Spec Kit.
