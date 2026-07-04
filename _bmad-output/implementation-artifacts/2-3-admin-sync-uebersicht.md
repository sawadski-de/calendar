# Story 2.3: Admin → Sync-Übersicht

Status: done

## Story

As a Admin (Dennis),
I want den Sync-Zustand aller Teammitglieder-Konten an einer Stelle sehen,
so that ich als Bus-Faktor-1-Betreiber einen ausbleibenden Sync bemerke, bevor jemand einen Termin verpasst.

## Acceptance Criteria

1. **Given** ich bin als Admin angemeldet, **when** ich auf den Navigationspunkt "Admin → Sync-Übersicht" klicke, **then** sehe ich eine Tabelle mit einer Zeile pro Person/Provider-Kombination: Person, Provider, Status, letzter erfolgreicher Sync.
2. **Given** ich bin als Member (nicht Admin) angemeldet, **when** ich die Navigation betrachte, **then** ist der Punkt "Admin → Sync-Übersicht" schlicht nicht vorhanden — kein sichtbarer, aber gesperrter Link.
3. **Given** ich bin als Member angemeldet, **when** ich den API-Endpoint der Sync-Übersicht direkt aufrufe (z. B. über die Browser-Konsole), **then** verweigert das Backend den Zugriff serverseitig (403) — die Rollenprüfung erfolgt serverseitig, ein fehlender Frontend-Nav-Punkt ersetzt sie nicht (AD-17).
4. **Given** ein Konto mit wiederholt fehlgeschlagenem Sync, **when** die Übersicht gerendert wird, **then** hebt sich diese Zeile mit dem expliziten Fehlerzustand ab (nicht nur ein alter Zeitstempel, der als "ruhige Woche" missverstanden werden könnte).
5. **Given** ein Teammitglied hat gar kein Kalenderkonto verbunden, **when** die Übersicht gerendert wird, **then** erscheint die Person mit einem klar erkennbaren "nicht verbunden"-Zustand statt einer Fehlerzeile.

## Tasks / Subtasks

- [x] **Task 1 — Expose the current user's role so the frontend can gate the nav link** (AC: 2)
  - [x] `src/Api/Endpoints/AuthEndpoints.cs`'s `GET /api/auth/me` currently returns only `{ id, email }` — extend it to also return `role`, fetched fresh via `IPersonRepository.GetByIdAsync(personId)` (**do not** read role from a claim/cookie — this codebase's established rule, per `AdminOnlyAuthorizationHandler`'s own doc comment, is to never trust a cached claim for authorization-relevant role checks; `/api/auth/me` is read on every app load so a live DB read here is cheap and consistent with that rule). This is a small, additive change to an existing endpoint — do not create a parallel endpoint.
- [x] **Task 2 — Application/Infrastructure: bulk read across all people** (AC: 1, 4, 5)
  - [x] Add `Task<IReadOnlyList<CalendarConnection>> GetAllAsync(CancellationToken ct = default)` to `src/Application/Sync/ICalendarConnectionRepository.cs` — **distinct from the existing `GetAllActiveAsync`**: this one is for the Admin overview and must include connections belonging to every person regardless of `Person.IsActive` (an admin needs to see a deactivated person's last-known state too, per AD-12's "Daten bleiben erhalten" spirit — `GetAllActiveAsync` is specifically for the Worker's polling loop, which must skip inactive people; do not merge these two methods or make one call the other with a flag, they serve genuinely different callers with different filtering needs).
  - [x] Implement in `src/Infrastructure/Sync/CalendarConnectionRepository.cs` — plain `dbContext.CalendarConnections.ToListAsync(ct)`, no join needed here (the endpoint layer joins against the person roster, see Task 3 — keeps this repository method a simple, reusable primitive rather than baking an admin-specific shape into Infrastructure).
- [x] **Task 3 — Api: Admin sync-overview endpoint** (AC: 1, 3, 4, 5)
  - [x] `src/Api/Contracts/CalendarConnectionContracts.cs`: add `AdminCalendarConnectionRow(Guid PersonId, string PersonEmail, string Provider, bool Connected, DateTimeOffset? LastSuccessfulSyncAt, bool HasError, string? ErrorCode)` — same `HasError`/`ErrorCode` semantics as the existing per-user `CalendarConnectionResponse` (reuse the exact same threshold logic, see next bullet).
  - [x] Extend `src/Api/Endpoints/CalendarConnectionEndpoints.cs` with `GET /api/admin/calendar-connections` under `app.MapGroup("/api/admin").RequireAuthorization("Admin")` (reuse the **existing** `/api/admin` group pattern from `AdminEndpoints.cs` — either add this route inside `AdminEndpoints.MapAdminEndpoints` or keep it in `CalendarConnectionEndpoints` under its own `app.MapGroup("/api/admin").RequireAuthorization("Admin")` call; either is fine, but **do not** duplicate the `"Admin"` policy wiring or invent a second admin-check mechanism — `AddAuthorizationBuilder().AddPolicy("Admin", ...)` in `Program.cs` already exists and is suficient). Build the response by joining `IPersonRepository.GetAllAsync()` (full roster) × `CalendarProviders.All` (`Google`, `Outlook`) against `ICalendarConnectionRepository.GetAllAsync()` (Task 2) keyed by `(PersonId, Provider)` — every person gets exactly one row per provider, `Connected: false`/nulls for a person with no `CalendarConnection` row at all (AC 5's "nicht verbunden" case is the *absence* of a row, not a special flag).
  - [x] Extract the `HasError` threshold logic (`RepeatedFailureThreshold`, currently a private constant + private `ToResponse` method in `CalendarConnectionEndpoints.cs`) into a small shared helper both the per-user `GET /api/calendar-connections` and this new admin endpoint call — **do not copy-paste the threshold check**, that would let the two surfaces drift out of sync silently (e.g. AC 4 here must show the identical error state as Story 2.1/2.2's per-user Settings row for the same connection, not a second interpretation of "repeatedly failed").
- [x] **Task 4 — Frontend: role-aware nav + Admin sync-overview page** (AC: 1, 2)
  - [x] `frontend/src/app/core/auth/auth.service.ts` (or wherever `/api/auth/me` is currently consumed — check the existing `AuthService`/`auth.guard.ts`): the `me()` call's response shape gains `role` — thread it through to wherever `Home`'s template can read it. **Do not** create a second call to `/api/auth/me`; reuse whatever the app already fetches on load / after login for the auth guard.
  - [x] `frontend/src/app/pages/home/home.html`: add an "Admin → Sync-Übersicht" nav link next to the existing Settings link (added in Story 2.1), rendered only `@if (isAdmin())` — a **frontend convenience only**, the real enforcement is Task 3's server-side 403 (AD-17, AC 3). A Member who guesses the URL and navigates directly must still get a 403 from the API when the page tries to load data — do not add a client-side route guard that pretends to be the security boundary (a guard is fine as an additional layer, e.g. to avoid flashing an empty table before the 403 comes back, but the AC is satisfied by the API check, not the guard).
  - [x] New feature folder `frontend/src/app/pages/admin/sync-overview/` (mirrors `pages/settings/connections/` structure): `sync-overview.service.ts` (`getOverview(): Observable<AdminCalendarConnectionRow[]>` hitting `/api/admin/calendar-connections`), `sync-overview.model.ts`, `sync-overview.ts`/`.html`/`.css` — a table grouped/sorted by person, each row showing Person (email), Provider, Status (reuse the same `.status-tag`/`.sync-line` visual language already built in `pages/settings/connections/` — **do not invent a new status-badge visual style for this table**, EXPERIENCE.md's Information Architecture section describes this as the same sync-indicator component reused in a third location), last successful sync.
  - [x] Route: `path: 'admin/sync-overview'` in `app.routes.ts`, under `authGuard` (not a new admin-specific guard needed for this story per the previous bullet — keep it simple, the API is the real gate).
- [x] **Task 5 — i18n**
  - [x] `de.json`/`en.json`: new `admin.syncOverview` namespace — nav label ("Sync-Übersicht"), page heading, table column headers (Person, Provider, Status, Letzter Sync), "Nicht verbunden" (reuse `settings.connections.statusNotConnected` if identical rather than duplicating the string — check before adding a new key).
- [x] **Task 6 — Tests**
  - [x] Integration (`tests/IntegrationTests/`, extend `CalendarConnectionEndpointsTests.cs` or add `AdminSyncOverviewEndpointsTests.cs`): Admin sees one row per (person, provider) including people with zero connections (AC 5) and a row with the repeated-failure error state (AC 4); a Member calling `GET /api/admin/calendar-connections` directly gets 403 (AC 3, mirrors the existing pattern in `AdminEndpointsTests.cs` for other `/api/admin/*` routes — check that file for the exact "create a Member, log in as them, assert 403" pattern already established there); `GET /api/auth/me` now includes the correct `role` for both an Admin and a Member session (Task 1 regression coverage).
  - [x] Frontend: `sync-overview.spec.ts` covering the table rendering (connected/error/not-connected row states) and a `home.spec.ts` addition confirming the Admin nav link is absent when `/api/auth/me` returns `role: "Member"`.

## Dev Notes

### This story is almost entirely additive — no changes to Story 2.1/2.2's sync pipeline

Unlike Story 2.2 (which required a real structural change to `CalendarSyncService` for the second provider), Story 2.3 only *reads* what Story 2.1/2.2 already write (`CalendarConnection` rows, `LastSuccessfulSyncAt`, `ConsecutiveFailureCount`, `LastErrorCode`). No changes to `CalendarSyncService`, `ICalendarProvider`, `GoogleCalendarProvider`, `OutlookCalendarProvider`, or the Worker.

### Architecture compliance

- **AD-17** is this story's central rule: "`Person.Role` ... wird von jedem Endpoint mit aggregierten Daten über mehr als die eigene Person serverseitig geprüft — ein fehlender Nav-Punkt im Frontend ersetzt diese Prüfung nicht." This endpoint is the textbook case (aggregated data across every person) — it must use the existing `"Admin"` authorization policy, exactly like `AdminEndpoints.cs` already does for `/api/admin/persons` and `/api/admin/persons/{id}/role`.
- Reuse `ProblemResults.Problem(...)` for any error responses (AD-13) — though this endpoint likely has no expected-error branches beyond the 403 the authorization policy already produces automatically.
- `AdminOnlyAuthorizationHandler` (`src/Api/Authorization/`) re-fetches `Person.Role` fresh from the DB per request — already correct, no change needed, just confirm this story's new endpoint sits behind the same `"Admin"` policy rather than a hand-rolled role check.

### Existing code this story touches (read before changing)

- `src/Api/Endpoints/AuthEndpoints.cs` — `/api/auth/me` currently returns `{ id, email }`. Adding `role` here is Task 1; check `frontend`'s consumers of this endpoint (likely `core/auth/auth.service.ts`) don't destructure the response in a way that would break on an added field (unlikely in TS, but confirm the response interface/type is updated too).
- `src/Api/Endpoints/AdminEndpoints.cs` — the existing `/api/admin` group + `"Admin"` policy pattern to reuse exactly.
- `src/Api/Endpoints/CalendarConnectionEndpoints.cs` (Story 2.1) — contains the `RepeatedFailureThreshold` constant and `ToResponse` private method this story's Task 3 must share rather than duplicate.
- `tests/IntegrationTests/AdminEndpointsTests.cs` — the established "log in as Member, hit an admin route, assert 403" test pattern to mirror.
- `frontend/src/app/pages/home/home.html`/`.ts` — already has the Settings nav link (Story 2.1); this story adds a second, conditional one.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Epic 2, Story 2.3] — full AC text.
- [Source: _bmad-output/implementation-artifacts/2-1-google-kalender-verbinden-importieren.md] — `CalendarConnection` entity/fields, `ICalendarConnectionRepository`, `CalendarConnectionEndpoints.cs`'s threshold/mapping logic this story extends.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md#AD-17]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md#Information Architecture] — "Admin → Sync overview" row: "role-gated ... nav item; lists per team member: provider, sync status, last successful sync (Person | Provider | Status | last sync)."
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/mockups/key-admin-sync-overview.html] — not yet reviewed in detail for this story; check its markup/class conventions the same way Story 2.1 did for `key-settings-connections.html` before building the table markup, to stay visually consistent.
- [Source: src/Api/Endpoints/AdminEndpoints.cs, src/Api/Authorization/AdminOnlyAuthorizationHandler.cs] — existing Admin-gating pattern.
- [Source: tests/IntegrationTests/AdminEndpointsTests.cs] — existing 403-for-Member test pattern.

### Previous Story Intelligence (2.1/2.2 → 2.3)

- Story 2.1's dev-agent record: eager `IConfiguration` reads at Program.cs top-level-statement time broke `WebApplicationFactory` test config overrides. Not directly relevant here (this story adds no new options-bound config), but if any new DI registration is needed, register lazily (factory/`Configure<T>`), not via an eagerly-evaluated `AddSingleton(new Foo(...))`.
- Story 2.1 established the fake-over-mock convention for Application-layer unit tests and the Testcontainers-based integration pattern (`TestApiFactory`, `PostgresContainerFixture`) — reuse both, no new test infrastructure needed for this story.

## Dev Agent Record

### Agent Model Used

claude-sonnet-5

### Debug Log References

- No real surprises — this story was as additive as the Dev Notes predicted. The one design decision made during implementation: kept the admin endpoint in `CalendarConnectionEndpoints.cs` (new `AdminSyncOverviewEndpoints.cs` file, same namespace) rather than inside `AdminEndpoints.cs`, calling the shared `CalendarConnectionEndpoints.HasVisibleError` (made `internal`) so the per-user Settings row and the Admin overview can never silently disagree on what counts as an error.
- A combined code review (run against Story 2.2+2.3 together) flagged `AdminSyncOverviewEndpoints`'s two sequential repository reads as a latency cleanup opportunity and suggested `Task.WhenAll`. Applying it caused an intermittent 500 (confirmed via a full integration-suite run, reproducible): `IPersonRepository` and `ICalendarConnectionRepository` share one scoped `ApplicationDbContext` per HTTP request, and EF Core throws when two queries run concurrently against the same context instance. Reverted to sequential awaits with a comment recording why — verified findings are only trustworthy after they're actually run, not just applied.

### Completion Notes List

- All 6 tasks complete; all 5 ACs implemented and covered by tests.
- Full backend solution builds clean. Final counts after the combined review round: Unit tests 37/37, Integration tests 48/48, Frontend 86/86, `ng build` clean.
- `/api/auth/me` now returns `role` (previously `{ id, email }` only) — a backwards-compatible additive field, no existing consumer broke.
- The Admin nav link and the sync-overview page's own component both fail safe: a Member somehow reaching the page directly gets a 403 from the API, which the page renders as an explicit "Admins only" notice rather than a blank/broken table.

### File List

**New files**
- `src/Api/Endpoints/AdminSyncOverviewEndpoints.cs`
- `tests/IntegrationTests/AdminSyncOverviewEndpointsTests.cs`
- `frontend/src/app/pages/admin/sync-overview/sync-overview.model.ts`
- `frontend/src/app/pages/admin/sync-overview/sync-overview.service.ts`
- `frontend/src/app/pages/admin/sync-overview/sync-overview.ts`
- `frontend/src/app/pages/admin/sync-overview/sync-overview.html`
- `frontend/src/app/pages/admin/sync-overview/sync-overview.css`
- `frontend/src/app/pages/admin/sync-overview/sync-overview.spec.ts`

**Modified files**
- `src/Api/Endpoints/AuthEndpoints.cs` (`/api/auth/me` now returns `role`)
- `src/Api/Contracts/CalendarConnectionContracts.cs` (`AdminCalendarConnectionRow`)
- `src/Api/Program.cs` (`app.MapAdminSyncOverviewEndpoints()`)
- `src/Application/Sync/ICalendarConnectionRepository.cs` (`GetAllAsync`)
- `src/Infrastructure/Sync/CalendarConnectionRepository.cs` (`GetAllAsync` implementation)
- `tests/UnitTests/Application/Sync/FakeCalendarConnectionRepository.cs` (`GetAllAsync` implementation)
- `frontend/src/app/core/auth/auth.service.ts` (`CurrentPerson.role`)
- `frontend/src/app/pages/home/home.ts`, `.html` (`isAdmin` signal, conditional nav link)
- `frontend/src/app/pages/home/home.spec.ts` (flush `/api/auth/me` in every test; 2 new Admin-nav-gating tests)
- `frontend/src/app/app.routes.ts` (`admin/sync-overview` route)
- `frontend/public/i18n/de.json`, `en.json`, `frontend/src/app/testing/transloco-testing.ts` (`nav.syncOverview`, `admin.syncOverview.*`)
