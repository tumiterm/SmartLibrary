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
| `GET /api/v1/books/isbn/{isbn}` | Lookup chain: local DB → Google Books → **Open Library** → `NotFound` (manual entry). External hits are snapshotted automatically with their source recorded; an ISBN never hits the same external API twice per tenant. |
| `POST /api/v1/books/{id}/asset` + `GET .../asset/view` | Soft copy upload (PDF, max 60 MB, per-tenant disk storage) and the guarded stream: `no-store`, inline-only, consumed exclusively by the in-app view-only reader (pdf.js canvas — no download/print/text-selection, dynamic watermark). Deterrence, not absolute DRM. |
| `GET /api/v1/books/{id}` | Full record: metadata, cover, classification, copies per branch with availability, borrow history (empty until circulation). |
| `POST /api/v1/books` | Manual entry (the final fallback). 409 on duplicate ISBN. |
| `PUT /api/v1/books/{id}` | Complete/correct a record (e.g. right after an external lookup cached it). ISBN immutable. |
| `POST /api/v1/books/{id}/copies` | Register a circulating copy: barcode (unique/tenant), branch (physical only), shelf no., condition, price. |
| `GET/POST /api/v1/branches` | Branch list / creation. Physical copies belong to a branch; digital items don't. |
| `GET /api/v1/members?search=` | Search members by name, email or card number (top 50). |
| `GET /api/v1/members/{id}` | Member details incl. card data and audit info. |
| `POST /api/v1/members` | Register a patron (library-wide or on a home branch); issues a unique `M-YYYY-NNNNNN` card number, 1-year expiry. 409 on duplicate email. |
| `GET /api/v1/loans/active` | Active loans, soonest due first. |
| `POST /api/v1/loans` | Checkout by scan: card + one or many barcodes (one transaction, per-copy failures reported). The full rulebook: active same-tenant member, loan cap, overdue-items cap, fine threshold, copy Available & at the desk's branch, not reserved for someone else, not reference-only, **one copy per title per member**. |
| `POST /api/v1/loans/return` | Return against the active loan only: records return time + receiving branch, judges condition (normal/damaged + optional charge), assesses overdue fines, feeds the waitlist. The borrowing record is permanent. |
| `POST /api/v1/loans/lost` | Write a loaned copy off as lost: loan closed (history kept), copy → Lost, replacement charge (explicit or copy price). |
| `POST /api/v1/loans/renew` | Renew by barcode; refused when overdue, at the renewal limit, or when members are waiting. |
| `POST /api/v1/loans/fines/{id}/settle` | Pay, or waive **with a required reason**. |
| `GET/POST /api/v1/holds`, `POST /api/v1/holds/{id}/cancel` | Waitlist: place/cancel; ready holds expire after the pickup window (lazy sweep). |
| `POST /api/v1/transfers` + `/{id}/action` + `/receive` + `/history` | Full transfer workflow: Requested → Dispatch → InTransit → Received, plus Reject/Cancel/LostInTransit/DamagedInTransit. Copy is unborrowable until received; permanent history with actors and timestamps. |
| `POST/GET /api/v1/stocktakes` + `/{id}/scans` + `/{id}/complete` | Stocktake: start (branch or whole library), scan copies in, complete. Unscanned expected copies → Missing; scanning a Lost/Missing copy recovers it. Statuses adjust, history never deletes. |
| `POST /api/v1/books/copies/{id}/status` | Mark a copy Lost/Damaged/Withdrawn or restore it. |
| `PUT /api/v1/members/{id}`, `POST .../status` | Edit a member; suspend/reactivate. |
| `GET/PUT /api/v1/settings` | **Per-tenant library rules** (loan days, fine rate, caps, pickup window) overriding platform defaults. |
| `GET /api/v1/dashboard` | Stats + recent circulation activity. |
| `GET /api/v1/search?q=` | Global search: books (title/author/ISBN), copies (barcode), members (name/card/email). |
| `GET /api/v1/reports/circulation?from&to`, `/inventory`, `/fines?from&to` | Reports on screen, or `format=csv` for a download. |
| `GET /api/v1/opac/books`, `/opac/books/{id}` | **Public catalog** (patron-facing UI at `/opac`): search + title view with per-branch availability and waitlist size. Never exposes borrower names or barcodes. |
| `GET /health` | Liveness. |

Platform-default circulation policy lives in the `Circulation` section of appsettings;
each tenant can override it via `PUT /settings` (Settings page in the staff app).

### Reader Score

Every member profile carries a 0–100 behavioural reputation with bookish tiers
(Laureate / Scholar / Reader / Drifter / Truant), computed in
`Domain/Members/ReaderScore.cs` from on-time return rate, currently-overdue books, and
unpaid fines. Every deduction is returned as a human-readable reason — explainable, not
a black box. New members start at 100.

### Auditing

All entities implement `IAuditable`; an EF `SaveChangesInterceptor` stamps
`CreatedAtUtc/CreatedBy/UpdatedAtUtc/UpdatedBy` automatically. The acting user comes from
`ICurrentUserService` — hardcoded to `librarian@demo` until auth lands, then it reads JWT claims.

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

**Visual Studio:** set `SmartLibrary.Api` as the startup project and press F5. The SPA proxy
launches the Vite dev server automatically and opens the browser on http://localhost:5173.
(Requires Node.js on PATH — restart VS after installing Node. Use the `api-only` launch
profile to run the API without the frontend.)

**CLI:**

```
dotnet run --project src/SmartLibrary.Api --launch-profile http
```

- Health: <http://localhost:5205/health>
- API reference: <http://localhost:5205/scalar/v1>

Dev connection string targets `(localdb)\MSSQLLocalDB`; the dev JWT signing key lives in
`appsettings.Development.json`. Production values must come from environment variables or a
secret store — `Jwt:SigningKey` is required at startup by design.
