---
baseline_commit: NO_VCS
---

# Story 1.2: Eigene Kalenderansicht (Monat/Woche/Tag)

Status: review

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a team member,
I want to see my own calendar in month, week, and day view,
so that I have a familiar overview of my appointments regardless of their source.

## Acceptance Criteria

1. **Default landing view.** Given I am logged in, when I open my calendar for the first time, then I land in the Week view by default `[ASSUMPTION carried from epics.md: the default view is not explicitly fixed in PRD/UX]`, with the view-switcher (Month/Week/Day) visible.
2. **Instant client-side view switching.** Given I am in one of the three views, when I select a different view via the view-switcher, then the view changes immediately client-side with no page reload; the active option shows a full accent fill, inactive options show muted text on a transparent background.
3. **Empty state.** Given I have no appointments yet, when I open my calendar, then the view shows an inviting empty message instead of a blank or broken-looking area.
4. **View-switcher accessibility.** Given the view-switcher, when it is operated via keyboard, then it is reachable and operable via standard tab/radio-group semantics for assistive technology.
5. **Mobile usability.** Given I am using a mobile device (browser), when I open any of the three views, then it is fully usable, not degraded to read-only.
6. **Overlapping appointments render side-by-side.** Given I have two or more own appointments with overlapping times on the same day, when the Day or Week view renders them, then both are shown visibly, with neither hiding the other `[OPEN QUESTION carried from epics.md: the exact stacking/side-by-side layout was not specified in PRD/UX — this story fixes a concrete layout, see Dev Notes, rather than silently dropping an appointment]`.

## Tasks / Subtasks

- [x] **Task 1: `Appointment` read-side domain & persistence** (AC: 3, 6)
  - [x] Add `Domain/Appointment.cs`: `Id (Guid)`, `PersonId (Guid, owner)`, `Title (string)`, `StartUtc (DateTimeOffset)`, `EndUtc (DateTimeOffset)`, `Provider (string?)`, `ProviderEventId (string?)` — establish AD-7's partial-unique-index shape now (`Provider`/`ProviderEventId` both `NULL` for native appointments) even though nothing writes native appointments until Story 1.3; retrofitting the constraint later is more expensive than adding it alongside this story's own migration. **Do not add** `AvailabilityStatus`, `Attendees`, or `Location` yet — those belong to Stories 1.3/1.4/1.10 and adding them now would be speculative, unused schema.
  - [x] Map `Appointment` in `ApplicationDbContext` (same file Story 1.1 touched): primary key `Id`, required `Title`/`StartUtc`/`EndUtc`, a foreign key `PersonId → Person.Id` (referential integrity — Story 1.1's `Person` entity already exists), and a **partial unique index** on `(PersonId, Provider, ProviderEventId)` filtered `WHERE "provider_event_id" IS NOT NULL` (AD-7) — use `HasFilter` with the actual **post-snake_case** column name, not the C# property name, since `EFCore.NamingConventions` (added in Story 1.1's review round) rewrites column names before the filter SQL is emitted.
  - [x] Add an EF Core migration (`AddAppointments` or similar) — do **not** touch the existing `people`/Identity tables from Story 1.1's migration.
  - [x] Add `Application/Appointments/IAppointmentViewService.cs` with `GetOwnAppointmentsAsync(Guid personId, DateTimeOffset rangeStartUtc, DateTimeOffset rangeEndUtc, CancellationToken)` — name and shape this so Epic 3's `GetForViewers(personIds, range, viewerId)` (AD-3) can be added alongside it later without restructuring; **do not** build the Privat-Default filtering logic itself yet (FR-9 is Epic 3's job) — for now this method only ever returns the caller's own data, so there is nothing to filter.
  - [x] Implement it in `Infrastructure/Appointments/AppointmentViewService.cs` against `ApplicationDbContext`. **Range query semantics:** an appointment is "in range" when it *overlaps* `[rangeStartUtc, rangeEndUtc)`, i.e. `StartUtc < rangeEndUtc && EndUtc > rangeStartUtc` — not a `BETWEEN`/containment check, which would wrongly drop an appointment that starts before the range but is still active inside it (e.g. one spanning midnight into a Day view). Order results by `StartUtc`.
  - [x] Add `GET /api/appointments?from={utc}&to={utc}` (`Api/Endpoints/AppointmentEndpoints.cs`, `Api/Contracts/AppointmentContracts.cs`) — `RequireAuthorization()` (any authenticated user, not admin-gated), resolves the caller's `PersonId` via `Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!)` (same claim `/api/auth/me` already reads — same GUID as `Person.Id`), calls `IAppointmentViewService.GetOwnAppointmentsAsync`, returns `[{ id, title, startUtc, endUtc }]`. Reject `from > to` and missing/unparseable query params with `ProblemResults.Problem(400, "invalid-request", ...)` — reuse Story 1.1's error infrastructure, don't build a new one.

- [x] **Task 2: Calendar view shell — Month/Week/Day rendering** (AC: 1, 3, 6)
  - [x] Replace `frontend/src/app/pages/home/*` (currently Story 1.1's authenticated-shell placeholder — read it before changing it) with the real calendar view: keep its header (language switcher, logout — do not regress those), replace the placeholder `<p>` body with the calendar.
  - [x] Build a `CalendarService` (`core/calendar` or `pages/home` local service) wrapping `GET /api/appointments` via `HttpClient` (remember `withCredentialsInterceptor` already applies globally — no extra config needed). **Data-loading strategy (resolves the "does switching views refetch?" question):** always fetch the full calendar month containing the current "focus date" (first day of month 00:00 UTC → first day of next month 00:00 UTC), regardless of which of the three views is active. A full month comfortably covers any Week or Day view within it, so **switching view type never triggers a new fetch** (AC 2) — only navigating the focus date to a month outside the currently-loaded range does. Keep the loaded appointments and the `(viewType, focusDate)` UI state independent of each other.
  - [x] Build Week and Day view components rendering a time-gridded column (`{spacing.time-gutter}` = 64px gutter, `components.calendar-column` tokens: `{colors.surface}` background, `{colors.border}` hairlines, `{rounded.lg}`) with appointment blocks using `ownAppointmentBg: {colors.surface-2}` / `ownAppointmentBorder: {colors.accent}` (per DESIGN.md — this is the **own-column** visual only; the colleague-column status-block variant is Epic 3's job, don't build it now). Appointment blocks show **title + time only** — no status badge (that's Story 1.3's `StatusHeuristicService` output, which doesn't exist yet) and are **not clickable** yet (detail popover is Story 1.4).
  - [x] Build a Month view component using `components.month-day-cell` tokens (`{colors.surface}` background, `{rounded.md}`) — for this story (no multi-person selection exists until Epic 3) each day cell just needs to render *something* indicating the day has appointments; keep it minimal (e.g., a count or compact title list) since the aggregate-marker/popover mechanism (UX-DR8) is explicitly an Epic 3 feature for *other people's* Bitte-nicht-stören days, not applicable to one's own month view here.
  - [x] **Overlap layout algorithm (resolves the epics.md open question):** for Day/Week views, group mutually-overlapping appointments on a given day, and within each group render them side-by-side, each taking `100% / groupSize` of the column width (the standard side-by-side calendar layout, e.g. two overlapping appointments each take half the column). Implement as a pure, unit-testable function (e.g. `computeOverlapLayout(appointments): { appointment, columnIndex, columnCount }[]`) — do not couple it to the rendering component so it can be tested directly.
  - [x] Empty state (AC 3): when `GET /api/appointments` returns an empty array for the visible range, show an inviting message via Transloco (e.g. a new `home.emptyCalendar` key, both `de.json`/`en.json` — **no hardcoded string**, same convention as every other UI text since Story 1.1) instead of a blank grid. **Do not** use Story 2.1's calendar-connection-CTA wording ("verbinde deinen Kalender in den Einstellungen...") — that message links to Settings → Calendar-connections, which does not exist until Epic 2; this story's empty state is simpler and gets upgraded in Story 2.1, not built prematurely here.
  - [x] **Period navigation** (implicit requirement — a calendar view is not usable without it, even though it isn't spelled out as its own AC in epics.md): add previous/next controls that move the focus date by one Month/Week/Day depending on the active view type, plus a "today" control that resets the focus date to today. Moving to a focus date outside the currently-loaded month triggers a new `CalendarService` fetch for the new month (see the data-loading strategy above); moving within the same loaded month does not.

- [x] **Task 3: View-switcher** (AC: 1, 2, 4)
  - [x] Build a `view-switcher` component (`components.view-switcher` — `{colors.surface-2}` track, `{rounded.full}`, active option `activeBg: {colors.accent}` / `activeText: {colors.accent-on}`, inactive `{colors.muted}` text on transparent) with exactly three options: Month/Week/Day, translated via Transloco (no hardcoded strings, matching Story 1.1's i18n convention).
  - [x] Use native radio-group semantics for accessibility (AC 4) — e.g. `role="radiogroup"` on the container and `role="radio"`/`aria-checked` on each option (or a `<fieldset>`/native radio inputs styled as pills), not a generic button group with only visual state — assistive tech must be able to tell it is a single-choice group.
  - [x] Switching views is a pure client-side signal/state change (no route change, no HTTP call) — instant, no loading state (AC 2).

- [x] **Task 4: Routing & mobile responsiveness** (AC: 1, 5)
  - [x] Default view on first load is Week (AC 1) — a local component-level default is sufficient; no need to persist the choice across sessions for this story (not an AC here).
  - [x] **Verify** — done via an automated test instead of manual inspection (`home.spec.ts`, "defaults to the Week view"), which is more repeatable and regression-proof than a one-time manual check.
  - [x] Verify all three views render usably at a mobile viewport width (e.g. 375px) — Day view is the easiest to make work by default (single column); Week view needs horizontal scroll for its multi-day grid (this story has no multi-person columns yet, so "horizontal scroll for many columns" from EXPERIENCE.md's Responsive & Platform section is Epic 3's concern, not this story's — for one's own calendar, Week view's 7 day-columns may still need horizontal scroll on narrow viewports, verify and add `overflow-x: auto` if so); Month view's grid must not overflow or become unusable at 375px width. **Verified structurally, not in a real browser** (no browser/screenshot tool available in this environment — same limitation noted in Story 1.1 for focus-ring testing): `.calendar-grid` has `overflow-x: auto` so Week/Day views scroll horizontally rather than overflow the page; `.month-view` uses `grid-template-columns: repeat(7, minmax(0, 1fr))` (the `minmax(0, ...)` is what prevents a CSS grid from forcing page-level overflow) combined with `text-overflow: ellipsis` on cell titles, so narrow cells truncate text instead of breaking layout.

- [x] **Task 5: Tests** (AC: all)
  - [x] Backend integration test (`AppointmentEndpointsTests.cs`, Story 1.1's `TestApiFactory` pattern): seed appointments directly via `ApplicationDbContext` (no create endpoint exists yet), assert both are returned in range; assert an appointment starting before `from` but overlapping it is still included; assert one fully outside the range is excluded; assert `from > to` returns 400 with code `invalid-request`.
  - [x] Backend integration test for AD-7's partial unique index (`Two_native_appointments_for_the_same_person_can_coexist_under_the_partial_unique_index`): two native appointments (`Provider`/`ProviderEventId` both `NULL`) for the same person both save successfully.
  - [x] Frontend unit test for `computeOverlapLayout` (`overlap-layout.spec.ts`): overlapping appointments get `columnCount = 2` with distinct `columnIndex`; non-overlapping get `columnCount = 1`; a later appointment reuses a freed column; input array is not mutated.
  - [x] Frontend component test (`home.spec.ts`): empty appointment list renders the empty-state message, a non-empty list does not; **also covers AC 1** (defaults to Week) and **AC 2** (`setViewType` triggers no new HTTP request) at this same component level, which is a more direct/reliable place to assert them than the isolated `ViewSwitcher` component (which has no default of its own — its active state is driven entirely by its parent).
  - [x] Frontend component test for the view-switcher (`view-switcher.spec.ts`): `role="radiogroup"`/`role="radio"` semantics present, exactly one option `aria-checked`, clicking a different option emits the new value, clicking the already-active option emits nothing.

## Dev Notes

### Architecture Compliance

- **AD-3 (central read service):** `IAppointmentViewService` is introduced by *this* story as the single read path for appointment data — even though Epic 3's Privat-Default filtering and bulk `GetForViewers` don't exist yet, name/shape this service now so Epic 3 extends it rather than replacing it. No controller/endpoint should query `ApplicationDbContext.Appointments` directly — always through this service.
- **AD-4/AD-5 (status heuristic):** explicitly **not** in scope for this story. Appointment blocks render title + time only; do not add a status field or badge now — Story 1.3 adds `StatusHeuristicService` and the precomputed status field together, and retrofitting a badge onto blocks already built is trivial, so there's no reason to build it early with fake data.
- **AD-7 (sync idempotency):** even though no sync exists until Epic 2 and no native-create exists until Story 1.3, the `(PersonId, Provider, ProviderEventId)` partial unique index (`WHERE ProviderEventId IS NOT NULL`) is a schema-level invariant — add it in this story's migration so it's never missing when Epic 2/1.3 start writing to this table.
- **Consistency Conventions (binding, from Story 1.1):** GUID ids; UTC storage only, timezone conversion happens exclusively in Angular (never send/expect local time from the Api); snake_case Postgres columns via `EFCore.NamingConventions` (already configured in `Program.cs` — don't reconfigure it, just be aware column names differ from C# property names when writing raw SQL/migration filters).

### Critical Guardrails (read before writing code)

1. **Don't build FR-9 (Privat-Default) or FR-2 (multi-person view) now.** Both are Epic 3. `IAppointmentViewService.GetOwnAppointmentsAsync` only ever returns the caller's own data — there is no "other person's calendar" concept yet, so there's nothing to filter or redact. Building it now would be speculative and would likely need rework once Epic 3's actual filtering rule is specified.
2. **Don't build the status badge, detail popover, or appointment-creation entry points.** Status (Story 1.3's `StatusHeuristicService`), the detail popover (Story 1.4), and double-click/`+ Neuer Termin` creation (Story 1.3) all render or trigger against data/services this story does not introduce. Appointment blocks in this story are inert (title + time, no click handler).
3. **`EFCore.NamingConventions` rewrites column names.** If you write a raw SQL fragment (e.g. a migration's `HasFilter` for the partial unique index), use the actual snake_case column name (`provider_event_id`), not the C# property name (`ProviderEventId`) — check the generated migration output rather than assuming.
4. **The overlap layout is a genuine, specified decision, not an open question anymore.** Side-by-side, equal-width columns per overlap group (see Task 2) — do not silently drop or fully overlap a second appointment, and don't invent a different algorithm (e.g. z-index stacking) without checking with the user first, since that would be a visible UX change from what's specified here. **Known, accepted limitation:** grouping by mutual overlap can over-narrow columns for a chain (A overlaps B, B overlaps C, A does not overlap C) — all three still render fully visible, which is what AC 6 requires; a tighter layout for chained-but-non-mutual overlaps is a polish item, not a defect, and isn't worth the added complexity for this story.
5. **`pages/home` is being repurposed, not created fresh.** Story 1.1 built it as a bare authenticated-shell placeholder specifically so this story would fill it in — read the current file before editing (header with language switcher + logout must survive; only the placeholder body changes).

### File Structure

New/changed since Story 1.1:

```text
src/
  Domain/Appointment.cs                                    # new
  Application/Appointments/IAppointmentViewService.cs       # new
  Infrastructure/Appointments/AppointmentViewService.cs      # new
  Infrastructure/Persistence/Migrations/*AddAppointments*    # new
  Api/Endpoints/AppointmentEndpoints.cs                       # new
  Api/Contracts/AppointmentContracts.cs                       # new
  Api/Program.cs                                              # modified — app.MapAppointmentEndpoints()
frontend/src/app/
  pages/home/*                                                # modified — real calendar view replaces placeholder
  shared/view-switcher/*                                      # new
  (calendar view components — Month/Week/Day, overlap layout) # new, organize under pages/home or a calendar/ folder
tests/
  IntegrationTests/AppointmentEndpointsTests.cs                # new
  (frontend) computeOverlapLayout.spec.ts, view-switcher.spec.ts, plus home/calendar component specs
```

### Testing Standards

- Backend: xUnit + Testcontainers Postgres (Story 1.1's `PostgresContainerFixture`/`TestApiFactory` pattern) — seed test data directly via `ApplicationDbContext`, there is no create-appointment endpoint to seed through yet.
- Frontend: Vitest + `TestBed` (Story 1.1's actual runner — **not** Jasmine/Karma, despite what older BMad guidance assumes; see Story 1.1's Dev Agent Record Debug Log for why).
- The overlap layout algorithm must have direct unit tests as a pure function — don't only test it indirectly through a rendered component, since visual/DOM assertions about column widths are far more brittle than asserting the layout function's output data.

### Previous Story Intelligence (from Story 1.1)

- **Established conventions to reuse, not reinvent:** `ProblemResults.Problem(...)` / `ApiProblemException` for every error response (RFC-7807, `about:blank` type, stable `code`); Minimal API endpoints as `IEndpointRouteBuilder` extension methods in `Api/Endpoints/*.cs`, request/response records in `Api/Contracts/*.cs`; Application-layer interfaces implemented in Infrastructure (see `IPersonRepository`/`PersonRepository`); `withCredentialsInterceptor` and `unauthorizedInterceptor` are already registered globally — new `HttpClient` calls need no extra auth wiring.
- **Snake_case columns** were retrofitted into Story 1.1 late (during code review) via `EFCore.NamingConventions` + `.UseSnakeCaseNamingConvention()` — this is now permanent project convention, apply it from the start this time.
- **Testing pattern:** `PostgresContainerFixture` (one shared container, `[Collection("Postgres")]`) + `TestApiFactory` (fresh database per test class via `CREATE DATABASE`, `CreateHttpsClient()` for the `CookieSecurePolicy.Always` requirement) — reuse both as-is, don't build a second container-fixture pattern.
- **A real concurrency bug was found and fixed in Story 1.1's review** (`IPersonRepository.ExecuteAtomicallyAsync`, Serializable transaction + `ChangeTracker.Clear()` on retry) for the last-admin-protection rule. This story's read-only `GetOwnAppointmentsAsync` has no analogous write-race — noted here only so the pattern is known to exist if a future story (1.3's appointment creation) needs similar protection for a concurrent-write scenario.
- **`pages/home`** currently renders: header (app title, language switcher, logout button) + a placeholder body (`home.welcome`/`home.placeholder` i18n keys). This story's Task 2 replaces the body only.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.2] — the 6 ACs, both `[ASSUMPTION]`/`[OPEN QUESTION]` markers resolved above.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md#AD-3, AD-4, AD-5, AD-7, Consistency Conventions, Capability → Architecture Map (FR-1 row)] — read-service ownership, status-heuristic boundary, sync-idempotency schema shape.
- [Source: _bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md#FR-1] — "Eigene Kalenderansicht" consequences (all three views show native+synced appointments; instant client-side switch).
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md#components.calendar-column, components.month-day-cell, components.view-switcher] — visual tokens for the column shape, month cell, and switcher.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md#Information Architecture (Own Calendar row), Component Patterns (View-switcher, Calendar column rows), Responsive & Platform] — landing-surface framing, switcher accessibility contract, mobile behavior baseline.
- [Source: _bmad-output/project-context.md] — cross-cutting stack/testing/critical rules (persistent facts for this workflow).
- [Source: _bmad-output/implementation-artifacts/1-1-projekt-grundgeruest-anmeldung-sprache.md] — established conventions and the `pages/home` placeholder this story fills in (see Previous Story Intelligence above).

## Dev Agent Record

### Agent Model Used

_To be filled in by the dev-story workflow._

### Debug Log References

- The independent review pass (before implementation) caught a real gap: the story didn't specify a data-loading strategy for view switching, which could have caused AC 2 ("no HTTP call on switch") to be violated or silently satisfied by loading too little data. Resolved by always loading the whole focus month regardless of active view type — added explicitly to Task 2 and implemented exactly as specified in `home.ts`'s `loadAppointmentsForFocusMonth`.
- The review also caught that a naive `BETWEEN`-style range query would drop appointments overlapping a range boundary (e.g. spanning midnight into a Day view) — implemented as an overlap test (`StartUtc < to && EndUtc > from`) instead, with a dedicated integration test (`Includes_an_appointment_that_starts_before_the_range_but_overlaps_it`) proving it.
- **Found during end-to-end verification, not caused by this story's own code:** `deploy/.env` (last touched outside this session — presumably Dennis configuring real deployment credentials) has `INITIAL_ADMIN_PASSWORD=datext1210`, which fails ASP.NET Core Identity's default password policy (no uppercase, no non-alphanumeric character). This crashes the Api container on a fresh boot with a clear, loud error (`Failed to bootstrap initial admin account: PasswordRequiresNonAlphanumeric, PasswordRequiresUpper`) — Story 1.1's AC 10 guardrail is working exactly as designed (fail loud, not silently), but the *current* real `.env` cannot actually bootstrap. **Did not modify `deploy/.env`** — that file holds the user's own values, not a file this story should touch; verification instead used a separate temporary env file (`$CLAUDE_JOB_DIR/tmp/story12-verify.env`, a copy of `.env.example`) so the real environment file was never touched or overwritten. Flagging this for Dennis to fix before any real deployment attempt: `INITIAL_ADMIN_PASSWORD` needs an uppercase letter and a non-alphanumeric character.
- EF Core's constructor-binding requires exactly one constructor whose parameters match the mapped properties; `Appointment`'s single public constructor (no parameterless ctor, matching `Person`'s established pattern from Story 1.1) bound cleanly with no extra configuration needed.

### Completion Notes List

- All 6 acceptance criteria implemented and verified: AC 1 (Week default — asserted in `home.spec.ts`), AC 2 (instant switch, no refetch — asserted in `home.spec.ts` via `httpMock.expectNone`), AC 3 (empty state — asserted in `home.spec.ts`), AC 4 (view-switcher `radiogroup`/`radio` ARIA — asserted in `view-switcher.spec.ts`), AC 5 (mobile — verified structurally via CSS, no browser tool available; see Task 4), AC 6 (overlap layout — asserted directly in `overlap-layout.spec.ts` and indirectly proven end-to-end via seeded overlapping appointments through the real API).
- Both epics.md markers are resolved concretely, not left open: the default-view **[ASSUMPTION]** is Week (tested), and the overlap-layout **[OPEN QUESTION]** is a specified greedy side-by-side column algorithm (`computeOverlapLayout`), not a placeholder.
- **Implicit requirement added beyond the literal 6 ACs, per the "leave the system working end-to-end" principle:** period navigation (previous/next/today). A calendar view with no way to move between periods would not be a usable calendar — epics.md's own ACs assume a "the view I'm looking at" concept that doesn't exist without it. Scoped minimally: three buttons, refetches only when navigation crosses a month boundary (reusing the same month-load strategy AC 2 depends on).
- **Verified live against the real stack, not just tests:** rebuilt the full `docker compose` stack from scratch (temporary env file — see Debug Log re: not touching the user's real `.env`), seeded two overlapping native appointments directly via SQL (no create-endpoint exists yet, as scoped), and confirmed via `curl`: both overlapping appointments returned together for a day query, a month-range query correctly excludes an appointment in the following month, and an inverted `from`/`to` range returns a clean 400 with code `invalid-request`.
- **Discovered a real deployment-readiness gap unrelated to this story's own code** (see Debug Log): the currently configured real admin password in `deploy/.env` doesn't meet Identity's password policy and would crash a fresh deployment. Not fixed here (out of scope — it's the user's credential choice, not a code defect), but flagged clearly so it isn't missed before an actual deployment.
- Not built in this story (intentionally, per scope, per Dev Notes Critical Guardrails): status badges/heuristic (Story 1.3), appointment creation (Story 1.3), detail popover (Story 1.4), multi-person/Privat-Default views (Epic 3). `IAppointmentViewService.GetOwnAppointmentsAsync` is shaped so Epic 3's `GetForViewers` can be added alongside it without restructuring — do not merge or replace this interface when extending it.
- Backend: 25/25 tests pass post-story (7 unit + 18 integration — 13 from Story 1.1, 5 new for Appointments). Frontend: 26/26 tests pass across 9 spec files (up from 14/6 after Story 1.1).

### File List

**Backend — `src/Domain`**
- `Appointment.cs` (new)

**Backend — `src/Application`**
- `Appointments/IAppointmentViewService.cs` (new)

**Backend — `src/Infrastructure`**
- `Appointments/AppointmentViewService.cs` (new)
- `Persistence/ApplicationDbContext.cs` (modified — `Appointment` mapping, FK to `Person`, AD-7 partial unique index)
- `Persistence/Migrations/*AddAppointments*` (new)

**Backend — `src/Api`**
- `Contracts/AppointmentContracts.cs`, `Endpoints/AppointmentEndpoints.cs` (new)
- `Program.cs` (modified — registers `IAppointmentViewService`, maps appointment endpoints)

**Backend tests**
- `tests/IntegrationTests/AppointmentEndpointsTests.cs` (new — 5 tests: range inclusion, range exclusion, overlap-at-boundary inclusion, invalid-range 400, AD-7 partial-index coexistence)

**Frontend — `frontend/src/app`**
- `pages/home/calendar/appointment.model.ts` (new)
- `pages/home/calendar/date-utils.ts` (new)
- `pages/home/calendar/overlap-layout.ts` + `.spec.ts` (new)
- `pages/home/calendar/calendar.service.ts` (new)
- `pages/home/calendar/calendar-column/{calendar-column.ts,.html,.css}` (new)
- `pages/home/calendar/month-view/{month-view.ts,.html,.css}` (new)
- `pages/home/home.ts`, `home.html`, `home.css` (modified — placeholder body replaced with the real calendar; removed the now-unused `email`/`me()` welcome display)
- `pages/home/home.spec.ts` (new)
- `shared/view-switcher/{view-switcher.ts,.html,.css,.spec.ts}` (new)
- `testing/transloco-testing.ts` (modified — replaced unused `home.*` test keys with `calendar.*`)
- `public/i18n/{de,en}.json` (modified — same key replacement, plus new `calendar.*` strings)

### Change Log

- 2026-07-03: Story 1.2 fully implemented (Tasks 1–5) — read-side `Appointment` domain/persistence with AD-7's partial unique index, `GET /api/appointments` with correct overlap-range semantics, full Month/Week/Day calendar view with a specified overlap-layout algorithm, accessible view-switcher, empty state, and period navigation. Both epics.md open markers (default view, overlap layout) resolved concretely. Status → review.
