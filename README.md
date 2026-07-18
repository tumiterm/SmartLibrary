# SmartLibrary

Enterprise, API-first, multi-tenant (SaaS) smart library system built on .NET 10.

## Architecture decisions

| Concern | Decision |
|---|---|
| Runtime | .NET 10 (LTS), pinned via `global.json` |
| Style | API-first, N-tier clean architecture, modular monolith |
| Multi-tenancy | Shared database, `TenantId` column, enforced with Finbuckle.MultiTenant + EF Core global query filters |
| Database | SQL Server (EF Core 10) |
| Auth | ASP.NET Core Identity + JWT bearer tokens (tenant claim in token) |
| CQRS | MediatR **12.5.0 — pinned; v13+ requires a commercial license** |
| Validation | FluentValidation |
| Logging | Serilog (bootstrap + two-stage init, request logging) |
| API docs | Built-in OpenAPI + Scalar UI (`/scalar/v1`, dev only) |
| Versioning | Asp.Versioning (`v1` default, URL segment substitution ready) |

## Solution layout

```
src/
  SmartLibrary.Domain/          entities, value objects — no dependencies
  SmartLibrary.Application/     use cases (MediatR), validators, abstractions
  SmartLibrary.Infrastructure/  EF Core, Identity, multi-tenant store
  SmartLibrary.Api/             controllers, auth, versioning, OpenAPI
tests/
  SmartLibrary.UnitTests/       Domain + Application tests
  SmartLibrary.IntegrationTests/ full-host tests via WebApplicationFactory
```

Dependencies point inward only: `Api → Infrastructure → Application → Domain`.

## Build conventions

- `Directory.Build.props` — nullable, implicit usings, **warnings as errors**, `latest-recommended` analyzers, NuGet audit on all dependencies.
- `Directory.Packages.props` — central package management; all versions live here, csproj files carry no versions.

## API surface (so far)

| Endpoint | Purpose |
|---|---|
| `GET /api/v1/books/isbn/{isbn}` | Add-book lookup flow: local DB → Google Books → `NotFound` (manual entry). Accepts ISBN-10 or -13, hyphens ok. `existsInLibrary: false` = show "Add book". |
| `POST /api/v1/books` | Save the local snapshot (prefilled or manual). 409 if the ISBN is already in this tenant's catalog. |
| `GET /health` | Liveness. |

All catalog requests require a tenant — send `X-Tenant: demo` (dev tenants live in
`appsettings.Development.json` under `Finbuckle:MultiTenant`).

## Staff app (frontend)

`clients/staff-app` — React 19 + Vite + TypeScript, Tailwind v4 with a token-based
design system ("Ink & Brass": warm paper light mode, near-black dark mode, brass accent),
shadcn-style components (cva + tailwind-merge), TanStack Query, sonner toasts,
Fraunces/Inter variable fonts (self-hosted). Light/dark toggle persists and applies
before first paint. Vite dev server proxies `/api` to the API — no CORS in dev.

```
cd clients/staff-app
npm run dev        # http://localhost:5173
```

## Running locally

```
dotnet run --project src/SmartLibrary.Api --launch-profile http
```

- Health: <http://localhost:5205/health>
- API reference: <http://localhost:5205/scalar/v1>

Dev connection string targets `(localdb)\MSSQLLocalDB`; the dev JWT signing key lives in
`appsettings.Development.json`. Production values must come from environment variables or a
secret store — `Jwt:SigningKey` is required at startup by design.
