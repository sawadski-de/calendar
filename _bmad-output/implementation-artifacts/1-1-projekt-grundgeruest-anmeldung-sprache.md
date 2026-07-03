---
baseline_commit: NO_VCS
---

# Story 1.1: Projekt-Grundgerüst, Anmeldung & Sprache

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a team member,
I want to log in with email and password and use the interface in German or English,
so that I can securely access my personal calendar without unauthorized access being possible.

## Acceptance Criteria

1. **Solution scaffold.** Given the five-layer solution structure (Domain/Application/Infrastructure/Api/Worker/frontend/deploy) does not exist yet, when the project is set up, then the five backend layers exist as separate projects following the AD-1 dependency direction (Domain depends on nothing; Application depends only on Domain; Infrastructure implements Application/Domain interfaces; Api and Worker are pure composition roots), and a Docker Compose stack with at least Postgres, Api, Angular, and Caddy (path routing `/` → Angular, `/api` → Api) starts successfully locally.
2. **Login & cookie auth.** Given an admin has created an account (email + password) for a team member — no self-signup possible — when the team member enters valid credentials on the login page, then they are logged in and authenticated via an HttpOnly, Secure, SameSite=Lax cookie; no access token is ever delivered to client-side JavaScript.
3. **Route protection.** Given an unauthenticated user, when they try to open a calendar route directly, then they are redirected to the login page and receive no calendar data whatsoever.
4. **Role field.** Given the `Person` entity, when it is created, then it has a `Role` field (`Admin` | `Member`) that only an existing admin can change.
5. **RFC-7807 errors.** Given an API endpoint returns an error, when the response is sent, then it follows the RFC-7807 `application/problem+json` format with a stable `code` field, never localized free text.
6. **Instant language switch.** Given the Angular UI is loaded, when a user activates the language switcher, then the UI switches instantly between German (canonical) and English without reloading; all existing texts (login, navigation) exist in both languages — no hardcoded strings.
7. **Design tokens.** Given the design token system from DESIGN.md (colors, typography, spacing, radius; dark-native as default with a complete `-light` set), when the login page and app frame render, then they use exclusively these tokens, including correct contrast for text/background pairs.
8. **Focus ring.** Given an interactive element (login button, language switcher), when it receives keyboard focus, then it shows a visible focus ring in the accent color.
9. **Reduced motion.** Given `prefers-reduced-motion: reduce` is active, when UI transitions occur (e.g. form feedback), then they are near-instant or disabled.
10. **Initial admin bootstrap.** Given no admin account exists yet (very first system start), when the Api container boots for the first time, then exactly one initial admin account is created from environment variables (email + password), so that a chicken-and-egg problem (no admin can create accounts because no admin exists) cannot occur.
11. **Last-admin protection.** Given exactly one admin account exists, when an admin tries to demote themselves or the last remaining admin to `Member`, then the system refuses the action with an RFC-7807 error code — at least one active admin account must always exist (bus-factor-1 protection). The analogous rule for deactivation (`IsActive`) is enforced later, in Epic 5 / Story 5.1, once that field is introduced — do **not** add `IsActive` in this story.
12. **Invalid credentials (deliberately minimal).** Given a user enters invalid credentials, when the login attempt fails, then the concrete error UX (message wording, lockout after N attempts) is explicitly out of scope for this story (EXPERIENCE.md UX-DR16 defers this until the final auth mechanism is settled) — a simple, generic error hint is sufficient.
13. **Session expiry mid-use.** Given a logged-in user whose session cookie expires while using the app, when they next perform an action requiring authentication, then they are redirected to the login page in a controlled way — no unhandled error, no silent data loss in an open form.

## Tasks / Subtasks

- [x] **Task 1: Solution & deployment scaffold** (AC: 1)
  - [x] Create the five backend projects (`src/Domain`, `src/Application`, `src/Infrastructure`, `src/Api`, `src/Worker`) with project references matching AD-1 exactly: Domain → (none); Application → Domain; Infrastructure → Application, Domain; Api → Application, Infrastructure; Worker → Application, Infrastructure. Api/Worker must never be referenced by the other layers.
  - [x] `Worker` can be an empty `BackgroundService` host for now (its real work starts in Epic 2) — it only needs to exist and build, per AC 1's "five layers exist as separate projects." (Renamed the template's `Worker` class to `SyncBackgroundService` — the .NET Worker Service template's default `Worker` class name collides with the project's own `Worker` root namespace and fails to compile.)
  - [x] Scaffold the Angular 22 workspace under `frontend/` (standalone components, no NgModules). Use `ng new ... --ssr=false` — recent Angular CLI versions default to scaffolding SSR, which conflicts with the "static build served via nginx behind Caddy" deployment model assumed below.
  - [x] Add `deploy/docker-compose.yml` (Postgres 18, Api, Angular served as static build via nginx or similar, Caddy v2 with path routing `/` → Angular, `/api` → Api) and `deploy/Caddyfile`. The Worker container and backup sidecar are **not** required to be wired into this compose stack yet (AC 1 only requires Postgres, Api, Angular, Caddy) — add them in Epic 2 / Epic 5 respectively.
  - [x] Add a local `.env.example` for compose (DB connection string, initial-admin credentials, token-encryption-key placeholder for later epics).
  - [x] Wire up Serilog (Console sink, structured JSON to stdout) in the Api project — this is a binding Consistency Convention (see Dev Notes), not optional polish.
  - [x] Verify: `docker compose up` succeeds locally and Angular is reachable through Caddy at `/`, a trivial Api health endpoint at `/api`. (Verified manually: `GET /` → 200 from the Angular container via Caddy, `GET /api/health` → `{"status":"healthy"}` from the Api container via Caddy. The Postgres 18 image requires the volume mounted at `/var/lib/postgresql`, not the pre-18 `/var/lib/postgresql/data` — fixed in `docker-compose.yml`. Caddy's host port is configurable via `CADDY_HTTP_PORT` (default 8090) to avoid colliding with an unrelated pre-existing project already bound to port 8080 on this host.)

- [x] **Task 2: Identity, auth & role model** (AC: 2, 3, 4, 10, 11, 13)
  - [x] Add `Npgsql.EntityFrameworkCore.PostgreSQL` and `Microsoft.AspNetCore.Identity.EntityFrameworkCore` to Infrastructure.
  - [x] Follow the **Domain/Identity split guardrail** (see Dev Notes: Critical Guardrails #3): keep `Person` a plain Domain entity (Id: `Guid`, Email, Role); put the ASP.NET Core Identity user (`ApplicationUser : IdentityUser<Guid>`) in Infrastructure only, sharing the same `Id` value as its corresponding `Person`. Domain must not reference `Microsoft.AspNetCore.Identity`.
  - [x] `ApplicationDbContext` in Infrastructure; first EF Core migration creates the Postgres schema (uuid PKs). Apply the migration automatically on Api startup (e.g. `dbContext.Database.Migrate()` before the app starts serving), and make Postgres readiness explicit in `docker-compose.yml` (healthcheck on the `postgres` service + `depends_on: condition: service_healthy` on `api`) — without this, first-run `docker compose up` can race the Api against an unready/unmigrated database (AC 1, AC 10). **Deviation from the literal spec:** used `IdentityUserContext<ApplicationUser, Guid>` instead of the full `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` — since guardrail #6 (below) means Identity's role/claims tables are never used for authorization, the leaner role-less base class avoids creating unused `AspNetRoles`/`AspNetUserRoles` schema entirely. Verified in the generated migration: only `AspNetUsers` + `People` (+ claims/logins/tokens tables) exist, no roles tables.
  - [x] Register Identity in Api explicitly — `AddIdentityCore<ApplicationUser>()` alone does **not** wire up `SignInManager` or the cookie scheme. Added `.AddEntityFrameworkStores<ApplicationDbContext>()`, `.AddSignInManager()`, plus an explicit `AddAuthentication(IdentityConstants.ApplicationScheme).AddCookie(IdentityConstants.ApplicationScheme, ...)` registration configured for HttpOnly/Secure/SameSite=Lax (`.AddIdentityCookies()` was skipped as unnecessarily broad — it also sets up an external-login cookie scheme this story doesn't use). Set `options.SignIn.RequireConfirmedAccount = false`, `RequireConfirmedEmail = false`, and `options.User.RequireUniqueEmail = true` explicitly.
  - [x] **Person.Role is the sole authorization source of truth — not Identity role claims.** Implemented `AdminOnlyRequirement`/`AdminOnlyAuthorizationHandler` (Api/Authorization) that loads `Person.Role` fresh from the database via `IPersonRepository` on every check — no `[Authorize(Roles=...)]` claim-based check anywhere. **Verified live**: demoted an admin, then replayed their still-valid, already-issued session cookie against an admin-only endpoint — got `403` immediately, no re-login needed.
  - [x] **Did not call `MapIdentityApi<TUser>()`** (see Critical Guardrails #1). Hand-rolled exactly `POST /api/auth/login` (`SignInManager.PasswordSignInAsync(..., isPersistent: false, lockoutOnFailure: false)`) and `POST /api/auth/logout` (`SignInManager.SignOutAsync`, `RequireAuthorization()`). No `/register` endpoint exists anywhere in the app.
  - [x] Added an admin-only endpoint (`POST /api/admin/persons`) that creates a `Person` + `ApplicationUser` pair atomically inside one DB transaction (`IdentityAccountProvisioningService`) — this is the only way new accounts are created (no self-signup, AC 2).
  - [x] `PUT /api/admin/persons/{id}/role` enforces the **last-admin-protection rule** (AC 11) via `RoleChangeService` (Application layer, pure — no EF/Identity dependency): before demoting an Admin to Member, counts remaining active Admins via `AdminPolicy.CanDemoteLastAdmin`; rejects with `409` + code `last-admin-cannot-be-demoted` if this is the last one. **Verified live**: demoting the sole admin → 409 with that exact code; after creating a second admin, demoting the first → 204 succeeds.
  - [x] Startup admin bootstrap (AC 10) in `AdminBootstrap.EnsureInitialAdminAsync`: if zero `Admin` people exist, creates exactly one from `INITIAL_ADMIN_EMAIL`/`INITIAL_ADMIN_PASSWORD`, throwing a clear startup exception if those env vars are missing instead of silently starting with no admin. `.env.example`'s default password (`Admin#12345`) satisfies Identity's default password policy — **verified live** (fresh `docker compose up` bootstrapped the admin and logged in successfully with it).
  - [x] Secured all admin/logout routes with `RequireAuthorization()`/`RequireAuthorization("Admin")`; unauthenticated requests get `401` (verified live). Called `UseForwardedHeaders()` (`X-Forwarded-For`/`X-Forwarded-Proto`, `KnownIPNetworks`/`KnownProxies` cleared since Caddy is the only ingress on the compose network) before the auth middleware.
  - [x] Angular: `authGuard` (`core/auth/auth.guard.ts`) calls `GET /api/auth/me` (added — the HttpOnly cookie is invisible to client JS, so this is the only way the SPA can know whether a session is valid) and redirects to `/login` on failure; `unauthorizedInterceptor` catches any `401` mid-session (AC 13) and redirects, excluding the login request's own `401` (AC 12) so the login page can show its own generic error instead of being redirected away from itself.
  - [x] Angular: `withCredentialsInterceptor` forces `withCredentials: true` on every `HttpClient` request centrally (not per call site), so no future call can forget it.

- [x] **Task 3: RFC-7807 error convention** (AC: 5)
  - [x] Added `ProblemDetailsExceptionHandler` (global `IExceptionHandler`) plus a `ProblemResults.Problem(...)` helper used directly by expected-error endpoint branches (invalid login, last-admin conflict, etc.) — together they shape every error response as `application/problem+json` with a stable `code` extension.
  - [x] Normalized Identity's own errors: `IdentityAccountProvisioningService` passes `IdentityError.Code` (Identity's own stable machine-readable strings, e.g. `PasswordTooShort`, `DuplicateUserName`) straight through as our `code` field. Also backstopped framework-generated problem+json (e.g. malformed request bodies) with a fallback `code: "invalid-request"` via `AddProblemDetails(options => options.CustomizeProblemDetails = ...)`, so **every** problem+json response has a `code`, not just the ones our own endpoints construct.
  - [x] No endpoint returns localized/free-text error bodies; `code` values are plain machine identifiers (e.g. `invalid-credentials`, `last-admin-cannot-be-demoted`) with no language-specific wording — translation happens only in Angular (Task 4).

- [x] **Task 4: Frontend app shell, design tokens & i18n foundation** (AC: 6, 7, 8, 9)
  - [x] Defined all DESIGN.md tokens actually used by this story's components (colors, typography, spacing, radius, elevation) as CSS custom properties in `src/styles/tokens.css`: unsuffixed = dark (default), overridden under `@media (prefers-color-scheme: light)`. **Assumption:** this story has no manual theme-toggle control (none specified in EXPERIENCE.md's IA for Login/Home), so light mode activates via OS preference only; a future story can add an explicit toggle without changing the token contract.
  - [x] Installed `@jsverse/transloco` v8 (see Critical Guardrails #2). `de` is `defaultLang`/`fallbackLang`; `TranslocoHttpLoader` fetches `public/i18n/{lang}.json` at runtime; `reRenderOnLangChange: true` gives instant, no-reload switching. Chosen language persists via `localStorage` across reloads (not itself AC-tested, but consistent with "real i18n" intent — a switch that reverts on every refresh would be a weak foundation for later epics).
  - [x] Built the login page (`pages/login`) — reactive form, single generic `login.genericError` message on any failure per AC 12 (no lockout/detailed-error UX) — and the app-frame/home page (`pages/home`) as the authenticated landing placeholder (the actual calendar view is Story 1.2's scope; this page only proves the authenticated-shell + logout mechanism).
  - [x] Built `shared/language-switcher` (`components.language-switcher`) — minimal pill, two buttons, active language shown via muted/full-text contrast, no flags/dropdown.
  - [x] Global `:focus-visible` rule in `styles.css` applies `outline: 2px solid var(--color-accent)` to every link/button/input/tabbable element — covers the login button and language-switcher without per-component repetition.
  - [x] `@media (prefers-reduced-motion: reduce)` sets a `--motion-duration` custom property to near-zero, applied globally via a `transition-duration`/`animation-duration` override — collapses any current or future hover/open/close transition without needing per-component media queries.

- [x] **Task 5: Tests** (AC: all)
  - [x] Backend unit tests (xUnit, `tests/UnitTests`): `AdminPolicyTests` (pure, 3 cases) + `RoleChangeServiceTests` (4 cases, against a hand-rolled `FakePersonRepository` — no EF Core/DB dependency). All 7 pass.
  - [x] Backend integration tests (xUnit + Testcontainers `postgres:18`, `tests/IntegrationTests`, one shared container + a fresh database per test class for isolation): `AuthEndpointsTests` (login success/failure with RFC-7807 shape, unauthenticated → 401, login→me→logout→me-401 cycle), `AdminBootstrapTests` (bootstrap creates exactly one admin from env vars against an empty DB, and is idempotent on a second invocation), `AdminEndpointsTests` (admin creates a member, last-admin demotion → 409 + `last-admin-cannot-be-demoted`, demotion succeeds once a second admin exists, a Member gets 403 on admin endpoints, **and the security-critical case**: a just-demoted admin's still-valid, already-issued session gets 403 immediately with no re-login — proves Critical Guardrail #6 end-to-end, not just in isolation). All 11 pass.
  - [x] Frontend tests (**not** Jasmine/Karma as Dev Notes assumed — see Debug Log References): `login.spec.ts` (generic error on failure, no error before submission, navigates on success, doesn't submit an incomplete form), `language-switcher.spec.ts` (defaults to German, switches instantly + persists to `localStorage`, buttons are natively keyboard-focusable), `auth.guard.spec.ts` (allows/redirects based on `/api/auth/me`), `with-credentials.interceptor.spec.ts`, `unauthorized.interceptor.spec.ts` (redirects on mid-session 401, does not redirect on the login endpoint's own 401). 13 tests across 6 files, all pass. **Focus-ring visual verification limitation:** jsdom (this project's test environment) does not reliably compute real-browser `:focus-visible` styling/cascade, so the ring's actual visual appearance is not unit-tested — the tests instead verify the AC 8 prerequisite (native, tab-reachable `<button>` elements that become `document.activeElement` on focus); the CSS rule itself was verified manually in Task 4.

### Review Findings

- [x] [Review][Patch] Postgres columns don't actually match the Dev Notes' "snake_case" convention (plain EF Core + Npgsql produces quoted PascalCase columns) — resolved by user decision: add `EFCore.NamingConventions` + `.UseSnakeCaseNamingConvention()` and regenerate the migration (no real data exists yet, so this is free now and expensive to retrofit after Epic 2/3 add more entities) [src/Infrastructure/Persistence/ApplicationDbContext.cs, src/Api/Program.cs]
- [x] [Review][Defer] `Cookie.SecurePolicy = CookieSecurePolicy.Always` only works today because browsers treat `localhost` as a secure context — the compose stack has no real TLS (`deploy/Caddyfile` binds plain `:80`), so login would silently fail on any non-localhost host — deferred, reason: no real domain exists yet for this prototype; resolve when a deployment/TLS story is scheduled, not by scope-creeping this story
- [x] [Review][Patch] `ForwardedHeadersOptions.KnownIPNetworks`/`KnownProxies` are both cleared, which trusts `X-Forwarded-For`/`X-Forwarded-Proto` from any source rather than only Caddy — low exploitability today since `docker-compose.yml` never publishes the `api` service's port to the host, but not defense-in-depth [src/Api/Program.cs]
- [x] [Review][Patch] Last-admin demotion has a TOCTOU race — two concurrent demote requests on a two-admin system can both pass `CanDemoteLastAdmin` before either commits, leaving zero admins [src/Application/Accounts/RoleChangeService.cs, src/Infrastructure/Accounts/PersonRepository.cs]
- [x] [Review][Patch] Admin bootstrap has a check-then-act race if the Api ever runs with more than one instance — could double-provision or crash on a duplicate-email constraint [src/Api/Startup/AdminBootstrap.cs]
- [x] [Review][Patch] Duplicate-email race in account provisioning surfaces as a generic 500 instead of a stable 409/`email-already-taken` code [src/Infrastructure/Accounts/IdentityAccountProvisioningService.cs]
- [x] [Review][Patch] `CreatePersonRequest.Role`/`ChangeRoleRequest.Role` silently default to `PersonRole.Admin` (enum value 0) when the `role` JSON property is omitted — a typo'd admin request that forgets the field grants unintended admin rights [src/Api/Contracts/AdminContracts.cs:1-5]
- [x] [Review][Patch] `POST /api/admin/persons` has no null/empty guard on email or password (login has one, this endpoint doesn't) — an unhandled exception surfaces as a generic 500 instead of a clean 400 [src/Api/Endpoints/AdminEndpoints.cs]
- [x] [Review][Patch] `ProblemDetailsExceptionHandler.TryHandleAsync` doesn't check `HttpContext.Response.HasStarted` before writing — throws `InvalidOperationException` if an exception occurs after the response has begun streaming [src/Api/Errors/ProblemDetailsExceptionHandler.cs]
- [x] [Review][Patch] `ProblemDetailsExceptionHandler` logs/returns a generic 500 for `OperationCanceledException` (e.g. a client disconnecting) instead of treating it as benign [src/Api/Errors/ProblemDetailsExceptionHandler.cs]
- [x] [Review][Patch] `Home.ngOnInit`'s `me()` subscription and `logout()`'s subscription have no error handler — a failed request surfaces as an unhandled RxJS error with no user feedback [frontend/src/app/pages/home/home.ts]
- [x] [Review][Patch] `unauthorizedInterceptor` doesn't exclude `/api/auth/me` — when `authGuard` calls it and gets a 401, both the interceptor and the guard independently navigate to `/login`, a redundant double-navigation [frontend/src/app/core/auth/unauthorized.interceptor.ts]
- [x] [Review][Patch] RFC-7807 `type` field hardcodes an external, non-project-owned URL (`https://httpstatuses.io/{statusCode}`) — RFC 7807 specifies `about:blank` as the default when no more specific type is registered [src/Api/Errors/ProblemResults.cs, src/Api/Errors/ProblemDetailsExceptionHandler.cs]
- [x] [Review][Patch] Language switcher's `aria-label="Language"` is hardcoded English, never routed through Transloco, unlike every visible label in the same component — violates AC 6 ("no hardcoded strings") [frontend/src/app/shared/language-switcher/language-switcher.html]
- [x] [Review][Patch] `--font-family: "IBM Plex Sans", ...` is declared in tokens but the font is never actually loaded (no `@font-face`/link) — DESIGN.md explicitly flags this as required for production, so the app silently renders in a fallback system font [frontend/src/index.html, frontend/src/styles/tokens.css]
- [x] [Review][Patch] `login.spec.ts` asserts the literal German string "Anmeldung fehlgeschlagen" rather than through a stable, translation-independent selector — breaks on any copy change unrelated to actual behavior [frontend/src/app/pages/login/login.spec.ts]
- [x] [Review][Patch] No integration test asserts the actual `Set-Cookie` response attributes (`HttpOnly`/`Secure`/`SameSite=Lax`) — this story's own Testing Standards call this out by name as required for AC 2, and it's the one part of AC 2 left unverified by the test suite [tests/IntegrationTests/AuthEndpointsTests.cs]
- [x] [Review][Defer] No CSRF defense-in-depth beyond `SameSite=Lax` (e.g. double-submit token) [src/Api/Program.cs] — deferred, legitimate future hardening once the admin-facing write surface grows; not required by any AC and SameSite=Lax is the AD-10-mandated baseline

## Dev Notes

### Architecture Compliance

- **AD-1 (layering):** Domain has zero references to any other project or to `Microsoft.AspNetCore.Identity`/EF Core. Application depends only on Domain. Infrastructure implements Application/Domain interfaces. Api and Worker are composition roots only — no business logic in controllers/endpoints beyond orchestration.
- **AD-9 (decoupled auth):** ASP.NET Core Identity (email + password), admin-created accounts only, no self-signup. Login must stay fully independent of the Settings → Calendar-connections OAuth flows that arrive in Epic 2 — don't couple them now even implicitly.
- **AD-10 (session auth):** Angular and Api sit behind the same Caddy reverse proxy on one origin (`/` and `/api`). Auth is an HttpOnly/Secure/SameSite=Lax cookie. No bearer token ever reaches client JS.
- **AD-13 (error format):** RFC-7807 `application/problem+json` with a stable `code` field for every error, backend-wide. All localization happens in Angular.
- **AD-17 (role enforcement, partial in this story):** `Person.Role` is set and checked server-side. This story only needs the admin-only account-creation/role-change endpoints; the first *consumer* of role-gated aggregate data (Admin → Sync-Übersicht) arrives in Epic 2, Story 2.3 — don't build that surface now, just make sure the role model and server-side check pattern are solid, since Epic 2/3/5 build directly on it.
- **Consistency Conventions (binding, not just this story's ACs):** GUID ids everywhere (no sequential integers); PascalCase C# entities / snake_case Postgres columns via EF Core default mapping (don't override manually); Serilog structured logging to stdout in the Api process now (Worker's turn comes with real work in Epic 2); secrets only via environment variables, never committed.

### Critical Guardrails (read before writing code)

1. **Do not call `MapIdentityApi<TUser>()`.** It maps `POST /register`, `POST /login`, `POST /refresh`, `GET /confirmEmail`, `POST /resendConfirmationEmail`, `POST /forgotPassword`, `POST /resetPassword`, `POST/GET /manage/*` — several of which (starting with `/register`) directly violate "no self-signup" (AC 2/AD-9) and are unneeded scope (email confirmation, 2FA, password recovery aren't in any AC here). Use `AddIdentityCore<ApplicationUser>()` + `SignInManager`/`UserManager` directly and hand-roll only `login`/`logout` as described in Task 2. [Source: Microsoft Learn — Use Identity to secure a Web API backend for SPAs, aspnetcore-10.0]
2. **Angular i18n library: `@jsverse/transloco`, not `@angular/localize`.** Angular's built-in `@angular/localize` i18n is compile-time — each locale is a separate build/bundle, and switching locale means a full reload/navigation. That directly fails AC 6 ("switches instantly... without reloading"). Also note the package-scope migration: Transloco moved from `@ngneat/transloco` (unmaintained) to `@jsverse/transloco` (current) — install the `@jsverse` scope only.
3. **Split `Person` (Domain) from the Identity user (Infrastructure).** The architecture spine's ERD treats `Person` as a plain domain entity referenced by `Appointment`/`StatusOverride`/etc. in later epics; ASP.NET Core Identity's `IdentityUser<TKey>` is a framework type. To keep AD-1's "Domain kennt nichts" intact for all later epics (Domain must not gain a transitive dependency on `Microsoft.AspNetCore.Identity`), model `ApplicationUser : IdentityUser<Guid>` as an Infrastructure-only type carrying auth concerns (password hash, security stamp), and keep `Person` as the Domain entity carrying business fields (`Role`, and later `IsActive` in Epic 5). Both share the same `Guid Id` value so they can be joined/looked up without Domain ever knowing Identity exists. Resolve any 1:1 provisioning (create `Person` + `ApplicationUser` together, same Id) in an Application/Infrastructure use-case, not in Domain.
4. **Postgres + GUID keys:** `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` requires the `Guid` key overload throughout (not the default `string`-keyed `IdentityUser`). Npgsql maps `Guid` to native Postgres `uuid` — don't introduce a string/varchar id column for users.
5. **`IsActive` is explicitly out of scope for this story.** The epic text calls this out directly: the last-admin protection for *deactivation* is Epic 5/Story 5.1's job, once `IsActive` exists. Only `Role`-based last-admin protection (demotion) belongs here (AC 11).
6. **`Person.Role`, not Identity role claims, is authoritative for authorization checks.** ASP.NET Core Identity's cookie bakes role claims in at sign-in; if you gate `Admin`-only endpoints with claim-based `[Authorize(Roles=...)]`, a demoted admin keeps working admin access until their session is re-issued — a real security gap, not a theoretical one, given AC 11 exists specifically to keep the admin set correct. Check `Person.Role` fresh from the database per request instead (see Task 2).

### File Structure

Per the architecture spine's Source Tree — this story creates the tree itself:

```text
{repo-root}/
  src/
    Domain/          # Person (Id, Email, Role) — zero dependencies
    Application/     # auth/account use-cases, interfaces
    Infrastructure/  # ApplicationDbContext, ApplicationUser, EF Core/Postgres
    Api/             # ASP.NET Core Web API, composition root, Identity wiring, auth endpoints
    Worker/          # empty BackgroundService host for now (real work starts Epic 2)
  frontend/          # Angular 22 SPA — login page, app frame, language switcher
  deploy/
    docker-compose.yml
    Caddyfile
```

### Testing Standards

- Backend: xUnit. Domain-adjacent business rules (last-admin protection) must be unit-testable without EF Core/DB, consistent with [project-context.md] Testing Rules.
- Backend integration tests run against a real Postgres instance (Testcontainers) — no DB mocking for security-relevant rules (login, role enforcement), per [project-context.md].
- Frontend: Angular's standard Jasmine/Karma setup for component tests.
- A test that only checks the frontend hides something does not satisfy a server-side requirement (AC 2, 3, 5) — assert the actual HTTP responses/cookies too, not just UI state.

### Project Structure Notes

This is the first story in the project — there is no existing code to preserve or extend. Establishing the five-layer solution correctly here (Task 1) is a hard prerequisite for every later epic; a wrong project-reference direction here (violating AD-1) would need to be unwound before Epic 2 can add `Worker`/`Infrastructure.GoogleCalendarProvider` cleanly.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.1] — full AC set, epic framing.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md#AD-1, AD-9, AD-10, AD-13, AD-17, Consistency Conventions, Structural Seed] — layering, auth, error format, role enforcement, source tree, stack versions.
- [Source: _bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md#FR-1, FR-3, FR-4, FR-10, §7 Zugriffsschutz] — must-have scope this story establishes the foundation for.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md#Colors, Typography, Spacing, Shapes, components.language-switcher] — design tokens.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md#Information Architecture (Login), Accessibility Floor, UX-DR16] — login-page deferral rationale, focus/reduced-motion/i18n-sizing rules.
- [Source: _bmad-output/project-context.md] — cross-cutting stack/testing/critical rules (loaded as persistent facts for this workflow).
- Microsoft Learn, "Use Identity to secure a Web API backend for SPAs" (aspnetcore-10.0) — `AddIdentityApiEndpoints`/`MapIdentityApi` endpoint surface and cookie vs. bearer behavior (informs Critical Guardrail #1).
- Transloco project docs/GitHub (`jsverse/transloco`) — current package scope and runtime-switching capability (informs Critical Guardrail #2).

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- Postgres 18's official image changed its expected volume mount point from `/var/lib/postgresql/data` to `/var/lib/postgresql` (pg_ctlcluster-style layout) — first `docker compose up` failed until `deploy/docker-compose.yml` was corrected.
- Host port 80 (and then 8080) were already bound by an unrelated, pre-existing project running on this machine (`calendar-neu-web-1`/`calendar-neu-api-1`/`calendar-neu-db-1`, untouched) — Caddy's host port is `${CADDY_HTTP_PORT:-8090}` to avoid the collision; not itself an AC concern, just a local-environment fact worth recording.
- `System.Text.Json` serializes enums as numbers by default — `role: "Member"` in a JSON body failed model binding with a bare 400 until a global `JsonStringEnumConverter` was registered. Fixed, and also closed the related gap where framework-generated problem+json (e.g. this kind of binding failure) had no `code` extension, via `AddProblemDetails().CustomizeProblemDetails`.
- The `.NET` Worker Service template's default `Worker` class name collides with the project's own `Worker` root namespace (`AddHostedService<Worker>()` resolves `Worker` as the namespace, not the type) — renamed the class to `SyncBackgroundService`.
- Angular 22's `ng new` scaffold now uses the `@angular/build:unit-test` builder (Vitest + jsdom), not Jasmine/Karma as this story's Dev Notes assumed — adapted test syntax (`vi.spyOn`) accordingly. Worth carrying forward: later stories' Dev Notes should say Vitest, not Karma.
- An independent fresh-context review (before implementation) caught and fixed 4 real gaps ahead of time: Person.Role-vs-Identity-claims staleness, EF migration/Postgres-readiness startup race, bootstrap password policy, and the `RequireConfirmedAccount` lockout trap — all incorporated into Task 2 as implemented (see Critical Guardrails #1–6 in Dev Notes).
- **Code review round** (Blind Hunter + Edge Case Hunter + Acceptance Auditor, run against a synthetic full-content diff bundle since no git history exists yet): enabling `EnableRetryOnFailure()` on the DbContext (needed for the last-admin-race fix's Serializable-transaction retry) broke `IdentityAccountProvisioningService`'s pre-existing manual `BeginTransactionAsync` — EF Core requires every manual transaction to run through `CreateExecutionStrategy()` once retry-on-failure is enabled anywhere on the context. Fixed by wrapping that transaction the same way.
- The first version of the last-admin TOCTOU fix (Serializable transaction + automatic retry) looked correct but the concurrency test (`Concurrent_demote_requests_against_a_two_admin_system_never_leave_zero_admins`) caught a real bug: on a retried attempt, EF Core's change tracker still held the `Person` entity mutated (but rolled back) from the failed first attempt, so the retry's "successful" commit persisted nothing — `dbContext.ChangeTracker.Clear()` at the start of each attempt fixed it. Confirmed stable across 4 consecutive runs before trusting it.

### Completion Notes List

- All 13 acceptance criteria implemented and verified: AC 1 (solution scaffold + compose stack), AC 2/12/13 (login/logout, generic error, mid-session 401 redirect), AC 3 (route guard + 401 → login), AC 4/11 (`Person.Role`, last-admin protection), AC 5 (RFC-7807 everywhere), AC 6 (Transloco instant DE/EN switch), AC 7 (DESIGN.md tokens), AC 8 (focus-visible ring), AC 9 (`prefers-reduced-motion`), AC 10 (admin bootstrap).
- **Deviation from the story's literal Dev Notes** (both improvements, not shortcuts, and both explained inline in Tasks/Subtasks and Dev Notes): used `IdentityUserContext<ApplicationUser, Guid>` instead of the full `IdentityDbContext<..., IdentityRole<Guid>, ...>` (no unused Identity role tables, since Guardrail #6 already rules out Identity claims for authorization); added `GET /api/auth/me` (not in the original task list) because the HttpOnly cookie is invisible to client JS and the Angular route guard has no other way to know whether a session is valid — a necessary, minimal addition, not scope creep.
- **Verified live, not just by code review:** a demoted admin's still-valid, already-issued session cookie is rejected (`403`) on the very next request with no re-login — both manually via curl against a running `docker compose` stack and via an automated integration test (`Demoted_admins_existing_session_loses_admin_access_immediately_without_relogin`). This is the single most security-critical behavior in this story and it was proven end-to-end twice.
- Not built in this story (intentionally, per scope): `IsActive`/deactivation (Epic 5), the actual calendar UI (Story 1.2 — `pages/home` is only the authenticated-shell placeholder), OAuth/Google/Outlook (Epic 2). The `Person`/`ApplicationUser` split and the fresh-per-request role-check pattern established here are the load-bearing foundation those later stories build on — do not reintroduce Identity-role-claim-based authorization or merge the two entities when extending them.
- **Post-review state:** all 16 `patch` findings from the code review applied and verified; 1 `defer` (CSRF defense-in-depth) recorded in `deferred-work.md`; 1 `decision-needed` (Cookie `SecurePolicy=Always` vs. no-TLS Caddy) resolved by explicit user decision and also recorded in `deferred-work.md`, pending a future deployment/TLS story. The Postgres schema now genuinely uses snake_case columns (`EFCore.NamingConventions`), matching Dev Notes' Consistency Conventions, which the pre-review implementation did not (migration was regenerated — no real data existed yet, so this was free).
- **Genuinely proved, not just patched:** the last-admin TOCTOU race fix (Serializable transaction + retry) is backed by a real concurrency test that fires two simultaneous demote requests against a two-admin system and asserts exactly one succeeds — confirmed stable across 4 runs. The role-omission privilege-escalation gap (missing `role` JSON field defaulting to `PersonRole.Admin`) is closed via C# `required` properties and confirmed live: a request without `role` now gets 400, and the database shows no accidental admin was created.
- Backend: 20/20 tests pass post-review (7 unit, 13 integration — added a concurrency test and a Set-Cookie-attributes test). Frontend: 14/14 tests pass across 6 spec files (Vitest — added a groupLabel/aria-label check implicitly via the existing language-switcher tests, and reworked the login-error test to assert via the translation key rather than a hardcoded string).
- Full stack manually re-verified via a from-scratch `docker compose up` (fresh volumes) after all patches: health check, snake_case schema (`\d people` shows `id`/`email`/`role`), login through Caddy (proving the DNS-based `ForwardedHeadersOptions.KnownProxies` fix resolves "caddy" correctly), role-omission correctly rejected with no accidental admin created, empty-email correctly rejected with a clean `invalid-request` code.

### File List

_Files below are as of initial implementation; the **Code Review Round** subsection lists what the review pass additionally touched._

**Solution / deployment**
- `calendar-neu-bmad.slnx` (new)
- `.gitignore`, `.dockerignore` (new)
- `deploy/docker-compose.yml`, `deploy/Caddyfile`, `deploy/.env.example` (new)

**Backend — `src/Domain`**
- `Person.cs`, `PersonRole.cs`, `AdminPolicy.cs`, `Domain.csproj` (new)

**Backend — `src/Application`**
- `Accounts/IPersonRepository.cs`, `Accounts/IAccountProvisioningService.cs`, `Accounts/RoleChangeService.cs`, `Application.csproj` (new)

**Backend — `src/Infrastructure`**
- `Identity/ApplicationUser.cs`, `Persistence/ApplicationDbContext.cs`, `Accounts/PersonRepository.cs`, `Accounts/IdentityAccountProvisioningService.cs`, `Persistence/Migrations/*` (InitialCreate), `Infrastructure.csproj` (new)

**Backend — `src/Api`**
- `Program.cs`, `Api.csproj`, `Dockerfile` (new)
- `Authorization/AdminOnlyRequirement.cs`, `Authorization/AdminOnlyAuthorizationHandler.cs` (new)
- `Errors/ApiProblemException.cs`, `Errors/ProblemResults.cs`, `Errors/ProblemDetailsExceptionHandler.cs` (new)
- `Endpoints/AuthEndpoints.cs`, `Endpoints/AdminEndpoints.cs` (new)
- `Contracts/AuthContracts.cs`, `Contracts/AdminContracts.cs` (new)
- `Startup/AdminBootstrap.cs` (new)

**Backend — `src/Worker`**
- `SyncBackgroundService.cs` (new; template's `Worker.cs` renamed), `Program.cs` (modified), `Worker.csproj` (new)

**Backend tests**
- `tests/UnitTests/Domain/AdminPolicyTests.cs`, `tests/UnitTests/Application/{FakePersonRepository,RoleChangeServiceTests}.cs`, `UnitTests.csproj` (new)
- `tests/IntegrationTests/{AuthEndpointsTests,AdminEndpointsTests,AdminBootstrapTests}.cs`, `Infrastructure/{PostgresContainerFixture,TestApiFactory}.cs`, `IntegrationTests.csproj` (new)

**Frontend — `frontend/src`**
- `styles.css` (modified), `styles/tokens.css` (new)
- `app/app.ts`, `app/app.html` (modified — root shell now just `<router-outlet>`), `app/app.spec.ts` (modified), `app/app.routes.ts`, `app/app.config.ts` (modified)
- `app/core/auth/{auth.service,auth.guard,with-credentials.interceptor,unauthorized.interceptor}.ts` + `.spec.ts` for guard/interceptors (new)
- `app/core/i18n/transloco-loader.ts` (new)
- `app/shared/language-switcher/{language-switcher.ts,.html,.css,.spec.ts}` (new)
- `app/pages/login/{login.ts,.html,.css,.spec.ts}` (new)
- `app/pages/home/{home.ts,.html,.css}` (new)
- `app/testing/transloco-testing.ts` (new)
- `public/i18n/{de,en}.json` (new)

**Frontend — deployment/config**
- `Dockerfile`, `nginx.conf`, `proxy.conf.json` (new)
- `angular.json` (modified — added `serve.options.proxyConfig`)

**Code Review Round**
- `deferred-work.md` (new — CSRF defense-in-depth and the Cookie/TLS decision)
- `src/Api/Program.cs` (modified — snake_case naming convention + `EnableRetryOnFailure`, DNS-resolved `ForwardedHeadersOptions.KnownProxies` instead of clearing trust)
- `src/Application/Accounts/IPersonRepository.cs`, `RoleChangeService.cs` (modified — `ExecuteAtomicallyAsync` for the last-admin TOCTOU fix)
- `src/Infrastructure/Accounts/PersonRepository.cs` (modified — `ExecuteAtomicallyAsync` implementation with Serializable transaction + change-tracker clearing), `IdentityAccountProvisioningService.cs` (modified — execution-strategy-wrapped transaction, duplicate-email race caught and mapped to a stable code)
- `src/Infrastructure/Persistence/Migrations/*` (regenerated — snake_case columns)
- `src/Api/Contracts/AdminContracts.cs` (modified — `required` properties, closing the role-omission privilege-escalation gap)
- `src/Api/Endpoints/AdminEndpoints.cs` (modified — email/password validation, duplicate-email → 409)
- `src/Api/Startup/AdminBootstrap.cs` (modified — treats a concurrent-replica duplicate-email race as benign, not fatal)
- `src/Api/Errors/ProblemResults.cs`, `ProblemDetailsExceptionHandler.cs` (modified — `about:blank` type, `HasStarted`/`OperationCanceledException` handling)
- `frontend/src/index.html` (modified — loads IBM Plex Sans)
- `frontend/src/app/pages/home/home.ts` (modified — error handlers on `me()`/`logout()` subscriptions)
- `frontend/src/app/core/auth/unauthorized.interceptor.ts` + `.spec.ts` (modified — excludes `/api/auth/me` from the global redirect)
- `frontend/src/app/shared/language-switcher/language-switcher.html` (modified — translated `aria-label`)
- `frontend/src/app/testing/transloco-testing.ts`, `frontend/public/i18n/{de,en}.json` (modified — added `languageSwitcher.groupLabel`)
- `frontend/src/app/pages/login/login.spec.ts` (modified — asserts via translation key, not a hardcoded string)
- `tests/UnitTests/Application/FakePersonRepository.cs` (modified — implements `ExecuteAtomicallyAsync`)
- `tests/IntegrationTests/AuthEndpointsTests.cs` (modified — added the Set-Cookie-attributes test)
- `tests/IntegrationTests/AdminEndpointsTests.cs` (modified — added the concurrent-demote race test)

### Change Log

- 2026-07-03: Story 1.1 fully implemented (Tasks 1–5) — five-layer solution scaffold, Docker Compose stack, cookie-based auth with admin bootstrap and last-admin protection, RFC-7807 error convention, Angular app shell with design tokens and Transloco i18n, and full backend/frontend test coverage. Status → review.
- 2026-07-03: Code review (Blind Hunter + Edge Case Hunter + Acceptance Auditor) — 2 decision-needed resolved (snake_case columns → fixed; Cookie/TLS → deferred), 16 patches applied and verified (last-admin TOCTOU race, admin-bootstrap race, duplicate-email race, role-omission privilege escalation, missing input validation, exception-handler edge cases, frontend error handling and redirect race, RFC-7807 `type` field, untranslated aria-label, unloaded font, brittle test, missing cookie-attribute test), 1 deferred (CSRF defense-in-depth). Status → done.
