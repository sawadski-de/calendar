# Stack Version & Currency Review — ARCHITECTURE-SPINE.md

**Reviewed:** 2026-07-02
**Scope:** "Stack" table (lines 144–156) plus the related architectural choices AD-9/AD-10 (ASP.NET Core Identity, cookie-based same-origin auth) and AD-11/EF Core usage.
**Method:** WebSearch verification of each claimed version/status against vendor release notes and independent sources, dated 2026-07-02.

## Overall Verdict: **All current — no staleness, no deprecated choices, no materially better default identified.**

Every version number in the Stack table checks out as accurate for 2026-07-02, and the auth/ORM choices remain the unremarkable, standard default for this application shape (same-origin SPA + API, admin-provisioned accounts, no self-signup, no third-party OIDC clients to serve). One freshness-risk flag on Angular 22 (not staleness — the opposite: it's very new) is worth the team's attention but does not indicate a wrong pick.

---

## Row-by-row verification

### .NET 10 (LTS)
- **Claim:** .NET 10, LTS.
- **Verified:** .NET 10 shipped November 11, 2025 as an LTS release, supported for 3 years until November 10, 2028 (per dotnet/core GitHub release notes and the official .NET Blog "Announcing .NET 10"). As of 2026-07-02 this is the current LTS and the correct choice for a new project wanting long support.
- **Sources:**
  - [Announcing .NET 10 - .NET Blog](https://devblogs.microsoft.com/dotnet/announcing-dotnet-10/)
  - [core/release-notes/10.0/README.md · dotnet/core](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md)
  - [.NET and .NET Core official support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- **Verdict:** Correct, current, appropriate (LTS is the right call for a greenfield app expected to run for years without a forced upgrade).

### ASP.NET Core Web API 10 / Entity Framework Core 10
- **Verified:** EF Core 10.0 ("EF10") shipped alongside .NET 10 in November 2025, is itself an LTS-aligned release supported until November 10, 2028, and requires the .NET 10 SDK/runtime (won't run on earlier versions or .NET Framework). Notable EF10 features (LeftJoin/RightJoin LINQ operators, complex types improvements, named query filters) confirm it's a real, current, actively-developed release, not a stale/abandoned line.
- **Sources:**
  - [What's New in EF Core 10 | Microsoft Learn](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)
  - [What's new in .NET 10 | Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)
- **Verdict:** Correct, current. EF Core remains the standard, unremarkable ORM choice for a .NET/Postgres backend — no indication any alternative (Dapper, raw ADO.NET, etc.) would be a better *default* for this app's CRUD + sync-upsert shape; EF Core's LINQ-to-SQL translation and migrations tooling are exactly what AD-1/AD-7 (layered architecture, idempotent upserts) need.

### ASP.NET Core Identity 10, cookie-based same-origin auth (AD-9/AD-10)
- **Verified:** Microsoft's current official guidance (docs updated for aspnetcore-10.0) is explicit: for an application consisting of a SPA + Web API where users are authenticated locally via ASP.NET Core Identity, **cookie-based authentication is the recommended approach** — cookies are automatically handled by the browser without exposing them to JavaScript, and this is technically a first-party-application scenario where OAuth/OIDC best practices themselves recommend this pattern over token-in-JS storage. A token-based option exists for non-cookie-capable clients, but is explicitly the fallback, not the default.
- **Sources:**
  - [Use Identity to secure a Web API backend for SPAs | Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-api-authorization?view=aspnetcore-10.0)
  - [ASP.NET Core Authentication in 2026: JWT, Cookies, OAuth, and Repeat Mistakes (Medium, Mar 2026)](https://medium.com/@kerimkkara/asp-net-core-authentication-in-2026-jwt-cookies-oauth-and-repeat-mistakes-76ee1c41b039) — corroborates that the "right fit for the shape of the application" (browser-based, first-party) is cookies, not that teams should default to JWT/OAuth.
- **Verdict:** This is exactly the scenario the spine describes (AD-10: same-origin via Caddy path-routing, HttpOnly/Secure/SameSite=Lax cookie, no bearer token to JS). No stale or deprecated pattern here, and no materially better default exists — a full external IdP (Duende IdentityServer, Keycloak, Auth0) would be *over-engineering* for a single-tenant app with admin-only account provisioning and no external OIDC clients to serve; ASP.NET Core Identity's built-in `UserManager`/`SignInManager` cookie flow is the right-sized, standard choice AD-9 correctly picked.

### Angular 22
- **Claim:** Angular 22.
- **Verified:** Angular v22 was released **June 3, 2026** (current patch v22.0.1 at review time), making it the current stable release as of 2026-07-02 — under one month old. It is in active support through December 2026 with LTS through May 2028. Angular 21 (Nov 2025) remains in LTS through May 2027, and Angular 20 (May 2025) LTS through Nov 2026; Angular 19 and earlier are already fully EOL (Angular 19 LTS ended May 19, 2026).
- **Sources:**
  - [Angular v22 Release • angular.dev](https://angular.dev/events/v22)
  - [Announcing Angular v22 — Angular Blog](https://blog.angular.dev/announcing-angular-v22-c52bb83a4664)
  - [Angular | endoflife.date](https://endoflife.date/angular)
  - [HeroDevs: Angular v19 Goes EOL May 19. Angular 22 Is Coming the Same Month.](https://www.herodevs.com/blog-posts/angular-v19-goes-eol-may-19-angular-22-is-coming-the-same-month-here-is-how-to-navigate-both)
- **Verdict — accurate but flag a freshness risk, not staleness:** The version number is correct and it is genuinely the current stable release, so this is *not* a deprecated/stale pick. However, at spine-creation time Angular 22 was only ~4 weeks old. Independent commentary explicitly warns that "library ecosystem drift is a concern — popular packages drop old Angular support quickly [after a new major], and when that happens you're either pinned to old library versions or forced into manual patching," plus Node.js version-range churn around the v22 boundary (Node 20 going EOL almost simultaneously). For a greenfield project starting today that will need third-party Angular libraries (calendar/date-picker UI, drag-and-drop, etc. — relevant given this app's calendar-grid UX), Angular 21 (already LTS through May 2027, one release cycle more mature) would carry materially less ecosystem-lag risk with only a marginal loss of bleeding-edge features (Signal Forms stable, selectorless components, Angular ARIA stable were introduced in preview in v21 already). This is worth a conscious call by the team, not an automatic "use 22", but is a minor/defensible risk rather than a real problem — Angular 22 is not wrong, just the most aggressive currently-defensible option.

### PostgreSQL 18
- **Verified:** PostgreSQL 18 was released September 25, 2025 (official postgresql.org announcement). It is the current major version as of 2026-07-02, roughly 9 months old — squarely in the "current and battle-tested enough for production" window, not bleeding-edge in the way Angular 22 is. Notable features (async I/O subsystem, `uuidv7()` — directly relevant since the spine mandates GUID/uuid PKs, virtual generated columns, temporal constraints) confirm active, meaningful development.
- **Sources:**
  - [PostgreSQL: PostgreSQL 18 Released!](https://www.postgresql.org/about/news/postgresql-18-released-3142/)
  - [PostgreSQL 18 New Features — Neon](https://neon.com/postgresql/18-new-features)
- **Verdict:** Correct, current, appropriate. Note: PostgreSQL 18's new `uuidv7()` built-in is directly useful for the spine's "GUID for all entities" convention (time-ordered UUIDs improve index locality vs. random uuidv4) — not a problem, just a synergy worth the implementation team knowing about.

### Docker / Docker Compose — "aktuelle stabile Version"
- **Verified:** The spine deliberately doesn't pin a version here, which is appropriate for infrastructure tooling that should track whatever stable release is current at deployment time. No currency concern — Docker/Compose are actively maintained, ubiquitous, and remain the standard choice for this single-host deployment model (AD-14, Structural Seed).
- **Verdict:** Fine as written; not independently version-checked since no specific number was claimed.

### Caddy (reverse proxy / TLS termination) — "aktuelle stabile Version"
- **Verified:** Latest stable Caddy release found was v2.11.4 (June 2026), confirming Caddy is actively maintained and current. Caddy remains a reasonable, low-maintenance choice for automatic TLS + simple path-based routing (`/` → Angular static, `/api` → backend) as specified in AD-10 — arguably a better fit than Nginx for this specific job (automatic HTTPS, less config) though Nginx would also be a defensible, equally-current alternative; not a case of Caddy being a wrong or stale pick.
- **Sources:**
  - [Releases · caddyserver/caddy](https://github.com/caddyserver/caddy/releases)
  - [Caddy | endoflife.date](https://endoflife.date/caddy)
- **Verdict:** Current, appropriate, no concerns.

### Serilog — "aktuelle stabile Version"
- **Verified:** Serilog is actively maintained in 2026, with first-class OpenTelemetry integration (either directly, or via the Microsoft.Extensions.Logging/ILogger bridge). It has not been supplanted or deprecated by the industry's move toward OpenTelemetry-centric observability — the two are complementary (Serilog for structured log authoring/sinks, OTel for correlation/export), and Serilog remains one of the two dominant options (with NLog) for .NET structured logging.
- **Sources:**
  - [OpenTelemetry Serilog Logging in .NET — SigNoz](https://signoz.io/blog/opentelemetry-serilog/)
  - [.NET Logging with Serilog and OpenTelemetry — Last9](https://last9.io/blog/serilog-and-opentelemetry/)
- **Verdict:** Current, appropriate, no concerns. Given the spine's requirement is only "structured logging to stdout, log aggregation is the host's job" (no APM/tracing requirement stated), Serilog is sufficient and arguably simpler than standing up a full OTel collector pipeline for this single-host, bus-factor-1 deployment — a good match for the app's actual scale, not over-engineered.

---

## Summary of Findings

1. **No stale or deprecated technology in the stack.** Every specific version claim (.NET 10 LTS, EF Core 10, Angular 22, PostgreSQL 18) was independently confirmed accurate as of 2026-07-02 against vendor/official sources.
2. **ASP.NET Core Identity + cookie-based same-origin auth (AD-9/AD-10) is confirmed as Microsoft's own current official recommendation** for exactly this topology (first-party SPA + API, local Identity accounts) — not a legacy pattern, and a full external IdP would be over-engineering here, not an improvement.
3. **EF Core remains the standard, unremarkable ORM default** for a .NET/Postgres backend with the CRUD + idempotent-upsert access patterns this app needs (AD-1, AD-7); no better default identified.
4. **Minor freshness-risk flag (not staleness): Angular 22 was only ~4 weeks old** at the time the spine was written (released 2026-06-03, spine dated 2026-07-02). Independent sources note that popular Angular ecosystem libraries lag behind new majors and that Node.js version-range churn accompanies major Angular releases. Angular 21 (LTS through May 2027) already carries the notable v21/v22-era features (Signal Forms, Angular ARIA introduced in preview) with one more release cycle of ecosystem maturity. This is a judgment call for the team, not a defect in the spine — flagging it so the decision is conscious rather than incidental, particularly since this app's calendar-grid/multi-person view UI is likely to depend on third-party Angular component libraries that may lag a brand-new major.
5. **Positive synergy note (not a problem):** PostgreSQL 18's new `uuidv7()` function pairs well with the spine's "GUID for all entities" convention (Consistency Conventions table) — worth the implementation team's awareness for better index locality than random v4 UUIDs, though this is an optimization opportunity, not something the spine got wrong.
