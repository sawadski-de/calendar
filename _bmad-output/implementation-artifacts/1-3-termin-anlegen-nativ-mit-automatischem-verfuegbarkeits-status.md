---
baseline_commit: 460391e23fdc5d057cb226e15d08700e6770fdac
---

# Story 1.3: Termin anlegen (nativ) mit automatischem Verfügbarkeits-Status

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Teammitglied,
I want einen neuen Termin direkt im Tool anlegen, der automatisch einen Verfügbarkeits-Status erhält,
so that mein Termin sofort sichtbar ist und Kollegen später einschätzen können, ob ich ansprechbar bin.

## Acceptance Criteria

1. **Pre-filled create via slot.** Given I am in Week or Day view on an empty time slot, when I double-click it (or press `Enter`/`Space` while it is focused), then the create form opens with that slot's date/time pre-filled.
2. **Blank create via button.** Given I click "+ Neuer Termin" instead, when the form opens, then no date/time is pre-filled and I choose both manually.
3. **Save creates a native appointment.** Given the create form is open, when I enter title, date/time, duration and optionally attendees and save, then a native appointment is created (`Provider` and `ProviderEventId` both `NULL`) and appears immediately in my own calendar view.
4. **Automatic status on save.** Given a new native appointment is saved, when `StatusHeuristicService.Compute` runs, then it gets an automatic status per: no attendee → `Unterbrechbar`; ≥1 attendee and duration ≤45 min → `Unterbrechbar`; all-day OR (duration ≥90 min AND ≥3 attendees) → `BitteNichtStoeren`; all other cases → `Unterbrechbar` — the status is stored as a field on the appointment, never recomputed on read.
5. **Status badge rendering.** Given the stored status of an own appointment, when it renders in my calendar column, then it carries the status-badge treatment (glow-fill background, 3px full-color border, icon glyph, text label) — never color alone.
6. **Boundary: exactly 45 min.** Given an appointment of exactly 45 minutes with ≥1 attendee (lower bound of the "short" rule), when status is computed, then the result is `Unterbrechbar`.
7. **Boundary: exactly 90 min / 3 attendees.** Given an appointment of exactly 90 minutes with exactly 3 attendees (lower bound of the "long" rule), when status is computed, then the result is `BitteNichtStoeren`.
8. **No-attendee rule outranks all-day.** Given an all-day appointment with no attendees at all (e.g. a self-blocked focus day), when status is computed, then the result is `Unterbrechbar` — the no-attendee rule takes precedence over the all-day rule regardless of duration.
9. **Invalid time range rejected.** Given I try to save an appointment with 0 minutes duration or an end time before the start time, when validation runs, then the appointment is not created, an inline error about the time span appears, and my inputs are preserved.
10. **Missing title rejected.** Given I leave the title empty and click Save, when validation runs, then the appointment is not created, an inline error ("Bitte gib einen Titel ein.") appears under the title field, the form stays open with my previous inputs, and focus moves to the title field.
11. **Escape/outside-click cancels.** Given the create form is open, when I press `Esc` or click outside it, then it closes without saving and focus returns to the element that triggered it.

## Tasks / Subtasks

- [ ] **Task 1: Domain — status enum, attendees, heuristic** (AC: 4, 6, 7, 8)
  - [ ] Add `Domain/AvailabilityStatus.cs`: `enum AvailabilityStatus { Unterbrechbar, BitteNichtStoeren }` (AD-5) — this is the **one and only** status enum; Epic 4's `StatusOverride` reuses it later, do not create a second value list.
  - [ ] Add `Domain/Attendee.cs`: `Id (Guid)`, `AppointmentId (Guid)`, `PersonId (Guid)` — internal team-member attendees only. Native creation in this story has no free-text/external-invite path (the mockup and FR-3's field list only show picking from the team roster), so every `PersonId` here refers to a real `Person` row.
  - [ ] Modify `Domain/Appointment.cs`: add `Status (AvailabilityStatus, private set)`, `IsAllDay (bool, private set)`, and a read-only `Attendees` collection backed by a private `List<Attendee> _attendees = []` (EF Core's default convention binds `IReadOnlyCollection<Attendee> Attendees => _attendees;` to that backing field automatically — no extra `OnModelCreating` wiring needed beyond `HasMany`, matching how this codebase already treats other read-only properties). Constructor gains `bool isAllDay = false`; do **not** add attendees via the constructor (unbounded params list, and EF's single-constructor binding must stay clean) — add a domain method `AddAttendee(Guid personId)` instead, called once per selected attendee right after construction. Add `AssignStatus(AvailabilityStatus status)` as the only way `Status` is ever set post-construction (mirrors AD-4: computed once at write time, stored, never recomputed on read) — called exactly once by the creation flow, after all attendees are attached (the heuristic needs the final attendee count).
  - [ ] Add `Domain/StatusHeuristicService.cs`: `static AvailabilityStatus Compute(Appointment appointment)` — pure, stateless (AD-4's single implementation). Reads `appointment.Attendees.Count`, `appointment.EndUtc - appointment.StartUtc`, `appointment.IsAllDay`. **Rule order matters — evaluate top to bottom, first match wins:**
    1. `Attendees.Count == 0` → `Unterbrechbar` (outranks every other rule — this is what AC 8 tests).
    2. `duration <= TimeSpan.FromMinutes(45)` → `Unterbrechbar`.
    3. `IsAllDay || (duration >= TimeSpan.FromMinutes(90) && Attendees.Count >= 3)` → `BitteNichtStoeren`.
    4. Otherwise → `Unterbrechbar`.
  - [ ] **`IsAllDay` scope note (read before building the create form):** FR-3's field list (Titel/Datum-Uhrzeit/Dauer/Teilnehmer) and `mockups/key-appointment-create.html` show no "ganztägig" toggle. **Do not add one to this story's form** — every appointment created through it has `IsAllDay = false`. The field and the Domain branch are still implemented in full (not stubbed), because AD-4/AD-5 require `StatusHeuristicService.Compute` to be the single, complete, permanent implementation — Epic 2's synced-appointment upsert *does* receive an all-day flag from Google/Outlook and must be able to set `IsAllDay = true` later without reopening this service. Verify AC 8 and the all-day branch of AC 7 via direct unit tests that construct an `Appointment` with `isAllDay: true` directly — there is no UI path to exercise in this story.

- [ ] **Task 2: Application/Infrastructure — write path** (AC: 3, 4, 9, 10)
  - [ ] Add `Application/Appointments/IAppointmentCreationService.cs`: `Task<AppointmentCreationResult> CreateNativeAppointmentAsync(Guid ownerId, string title, DateTimeOffset startUtc, DateTimeOffset endUtc, IReadOnlyCollection<Guid> attendeePersonIds, CancellationToken)`. Return a result object carrying either the created `Appointment` or a stable error code — mirror `AccountProvisioningResult`'s shape (Story 1.1), don't throw for expected validation failures.
    - Validate in order: `title` blank (after trim) → `"title-required"`; `endUtc <= startUtc` → `"invalid-time-range"` (this one check covers both the "0 minutes" and "end before start" cases in AC 9); every id in `attendeePersonIds` resolves via `IPersonRepository.GetByIdAsync` → else `"attendee-not-found"` (defense in depth — the frontend only offers real roster entries, but the API must not trust client input, consistent with this codebase's existing server-authoritative posture, e.g. AD-3).
    - On success: construct `Appointment` with `provider`/`providerEventId` both `null` (AD-7), call `AddAttendee` per id, compute status via `StatusHeuristicService.Compute` and `AssignStatus` it, persist, return it.
  - [ ] Add `Application/Appointments/IAppointmentRepository.cs` with `Task AddAsync(Appointment appointment, CancellationToken)` — this is the **first write path** for appointments (Story 1.2's `IAppointmentViewService` is read-only per AD-3's own scoping; do not add a write method to it). Implement in `Infrastructure/Appointments/AppointmentRepository.cs` against `ApplicationDbContext`.
  - [ ] Implement `Infrastructure/Appointments/AppointmentCreationService.cs` for `IAppointmentCreationService`, using `IPersonRepository` + the new `IAppointmentRepository`.
  - [ ] Extend `Application/Accounts/IPersonRepository.cs` with `Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken)` (a genuinely new capability — the attendee-picker roster listing needs it; don't repurpose `GetByIdAsync` in a loop for this). Implement in `Infrastructure/Accounts/PersonRepository.cs`.
  - [ ] Register `IAppointmentCreationService` and `IAppointmentRepository` in `Program.cs` (`AddScoped`), next to the existing `IAppointmentViewService` registration.

- [ ] **Task 3: Persistence — migration** (AC: 3, 4)
  - [ ] Update `ApplicationDbContext.OnModelCreating`: map `Appointment.Status` with `HasConversion<string>().IsRequired()` (same pattern as `Person.Role`); map `Appointment.IsAllDay` as a required bool; add `DbSet<Attendee> Attendees`, map it with FK `AppointmentId → Appointment.Id` (cascade delete — an attendee row is meaningless without its appointment) and FK `PersonId → Person.Id` (restrict/no-action delete — Person rows never hard-delete per AD-12, but don't wire an accidental cascade that would delete unrelated appointments if that ever changed); add a unique index on `(AppointmentId, PersonId)` (defensive guardrail against double-adding the same attendee — not explicitly required by an AC, cheap to add, consistent with this codebase's existing care around indexes e.g. AD-7).
  - [ ] Add one EF Core migration covering all of the above (e.g. `AddAppointmentStatusAndAttendees`). Do not touch the existing `people`/Identity tables or the prior `AddAppointments` migration.

- [ ] **Task 4: API — create endpoint + roster endpoint** (AC: 3, 4, 9, 10)
  - [ ] Extend `Api/Contracts/AppointmentContracts.cs`: add `CreateAppointmentRequest(string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, IReadOnlyList<Guid> AttendeePersonIds)`; extend `AppointmentResponse` to also carry `AvailabilityStatus Status` (the existing `GET /api/appointments` mapping in `AppointmentEndpoints.cs` must include `a.Status` too — AC 5 requires the badge on *every* rendered own appointment, not just newly-created ones).
  - [ ] Add `POST /api/appointments` to `AppointmentEndpoints.cs` (same file, alongside the existing `GET`): `RequireAuthorization()`, resolve the caller's `PersonId` the same way the `GET` handler already does (`ClaimTypes.NameIdentifier`), call `IAppointmentCreationService`, map error codes to `ProblemResults.Problem(400, code, ...)` (reuse the exact codes from Task 2 — do not invent new ones or return free text, AD-13), success → `Results.Created($"/api/appointments/{id}", response)`.
  - [ ] Add new `Api/Contracts/PersonContracts.cs`: `PersonResponse(Guid Id, string Email)`. Add new `Api/Endpoints/PersonEndpoints.cs` with `GET /api/persons` — `RequireAuthorization()` only (**not** `"Admin"`-gated like `/api/admin/*`; every team member needs the roster to pick attendees), returns `IPersonRepository.GetAllAsync` mapped to `PersonResponse[]`. **No display name in the response** — `Person` has only `Email`/`Role` (Story 1.1); adding a name field is out of scope here (see Task 5 note). Map it in `Program.cs` (`app.MapPersonEndpoints()`, next to the other `Map*Endpoints()` calls).

- [ ] **Task 5: Frontend — model, service, status badge** (AC: 4, 5)
  - [ ] Extend `frontend/src/app/pages/home/calendar/appointment.model.ts`: add `status: 'Unterbrechbar' | 'BitteNichtStoeren'` (the API's global `JsonStringEnumConverter`, already registered in `Program.cs`, serializes the enum as its member name string, not an integer — same convention as `role: "Member"` elsewhere in this codebase).
  - [ ] Extend `calendar.service.ts`: `createAppointment(request): Observable<Appointment>` (`POST /api/appointments`); `getPersons(): Observable<{ id: string; email: string }[]>` (`GET /api/persons`).
  - [ ] **Attendee display note (explicit, deliberate deviation from the mock):** `mockups/key-appointment-create.html`'s attendee chip shows a name ("Jonas Keller"). `Person` has no name field and this story does not add one — no AC or PRD requirement needs it, and inventing one would be speculative schema growth. The attendee picker and chips in this story display **email**, not a fabricated name. Flag this in your Completion Notes as a resolved, intentional scope decision — not a silent gap.
  - [ ] Add `pages/home/calendar/status-badge/{status-badge.ts,.html,.css}` per `DESIGN.md components.status-badge`: input `status: Appointment['status']`; glow-fill background (`interruptible-fill` / `dnd-fill`), 3px **left-only** border in the full-strength color, `rounded.md`, an icon glyph + translated text label (`calendar.statusInterruptible` / `calendar.statusDnd`) using two **visually distinct glyph shapes** (not just two colors — Accessibility Floor, defense-in-depth for color-blind users). The text label must **never truncate or ellipsize** — this is a deliberate exception to `month-view`'s cell-title `text-overflow: ellipsis` convention from Story 1.2; do not copy that pattern here.
  - [ ] Render the badge inside `calendar-column.html`'s existing own-appointment block (alongside the title/time already there). **Scope note:** AC 5 says "in my calendar **column**" (Day/Week) — Month view's compact per-day list is out of scope for the badge in this story; Story 1.2 already left Month view deliberately minimal and no AC here asks for a badge there.

- [ ] **Task 6: Frontend — appointment creation form + attendee picker** (AC: 1, 2, 3, 9, 10, 11)
  - [ ] Add `shared/focus-trap/focus-trap.ts` (a directive or small injectable helper) — **this is the app's first popover-like UI**, so build the trap/restore behavior generically now rather than one-off inside the create form: while active, `Tab`/`Shift+Tab` cycle within the container's focusable elements only; on deactivate, focus returns to whatever element was active before it was applied. Story 1.4's detail popover will need the exact same behavior next — reuse this, don't duplicate it (see `_bmad-output/project-context.md`'s "don't reinvent" guardrail).
  - [ ] Add `pages/home/calendar/attendee-picker/{attendee-picker.ts,.html,.css}` — a small, form-scoped picker: fetches the roster via `calendar.service.ts#getPersons`, renders selected attendees as chips (`{rounded.full}`, `surface` bg per `DESIGN.md`'s chip styling) with a "×" remove, plus a "+ Teilnehmer hinzufügen" trigger opening a filterable dropdown (type to filter by email; arrow keys move focus; `Enter` adds and keeps the dropdown open; `Esc` closes it). **Do not build this as Epic 3's full person-selector** (that component is for choosing which teammates' calendars to *view*, has no selection cap, and needs horizontal-scrolling chip rows for a different UI context) — this is a narrower, form-only field. Two-way bind selected `personId[]` to the parent form.
  - [ ] Add `pages/home/calendar/appointment-create/{appointment-create.ts,.html,.css}` sharing the popover visual shape (`surface-2` bg, `border`, `rounded.md`, popover drop-shadow — `DESIGN.md` notes this form is closest to `components.appointment-detail-popover`'s shape). Fields: Title (text), Date (date), Start time + Duration (both user-editable; **End is computed/display-only** = Start + Duration — avoids a third independently-editable field that could disagree with the other two, unlike the 3-field visual in the mock), Attendees (the picker above).
    - Inputs: an optional pre-fill `{ startUtc, endUtc }` (slot-triggered open) — when absent, all fields start empty (button-triggered open), satisfying AC 1 vs. AC 2.
    - Apply the focus trap from above while open. `Esc` or an outside click closes without saving and restores focus to the triggering element (AC 11) — the component needs to know what triggered it (a slot vs. the toolbar button) to restore focus correctly.
    - Validation on Save: title blank (trimmed) → inline error under the title field using `calendar.createTitleRequired`, focus moves to the title input, form stays open with all entered values intact (AC 10). Computed end ≤ start (duration ≤ 0) → inline error near the time fields using `calendar.createInvalidTimeRange`, same "stays open, values preserved" behavior (AC 9). These are **client-side pre-checks** for instant feedback; also handle the same two error codes if the backend still rejects (defense in depth, e.g. a race or a bug in the client check) — map codes to the same translated messages, never show raw backend `title`/`detail` text (AD-13).
    - On successful save: emit the created `Appointment`.
  - [ ] Add a "+ Neuer Termin" ghost-pill button to `home.html`'s toolbar (`components.appointment-create-entry`: transparent bg, `border-interactive` outline, `rounded.full`) opening the form blank (AC 2).
  - [ ] Add empty-time-slot targets to `calendar-column.ts`/`.html`: currently the column renders only the absolutely-positioned appointment overlay with no discrete slot elements. Add a focusable half-hour slot grid (48 slots across the existing `24 * HOUR_HEIGHT_PX` column) underneath the overlay; each empty slot gets `tabindex="0"` and `(dblclick)`/`(keydown.enter)`/`(keydown.space)` handlers emitting a `slotActivated` output with that slot's computed start `Date` (AC 1). **Reuse `positioned()`'s existing occupancy data to know which slots already have an appointment** — do not build a second, separately-maintained occupancy calculation; a slot covered by an appointment is not "empty" and must not fire the event.
  - [ ] Wire it all in `Home`: own the create-form's open/closed state and which element triggered it; on `slotActivated` (from any visible column) or the toolbar button, open the form (pre-filled or blank respectively); on successful creation, **append the new appointment directly to the `appointments` signal** (`this.appointments.update(list => [...list, created])`) rather than forcing a full month refetch — satisfies "erscheint sofort" (AC 3) without an extra round-trip.

- [ ] **Task 7: i18n** (AC: 1, 2, 5, 9, 10)
  - [ ] Add to both `public/i18n/de.json` and `en.json` under `calendar.*`: `createButton`, `createHeading`, `createFieldTitle`, `createFieldDate`, `createFieldStart`, `createFieldDuration`, `createFieldEnd`, `createFieldAttendees`, `createTitleRequired`, `createInvalidTimeRange`, `createCancel`, `createSave`, `addAttendee`, `statusInterruptible`, `statusDnd` — no hardcoded strings anywhere in the new components (Story 1.1/1.2 convention).
  - [ ] Update `testing/transloco-testing.ts` with the new keys so component specs render real (not missing-key) text.

- [ ] **Task 8: Tests** (AC: all)
  - [ ] `tests/UnitTests/Domain/StatusHeuristicServiceTests.cs` (new): one test per rule/boundary — 0 attendees → always `Unterbrechbar` regardless of duration or `IsAllDay` (AC 8, including the all-day+0-attendee case specifically); exactly 45 min + ≥1 attendee → `Unterbrechbar` (AC 6); 46 min + 1 attendee → `Unterbrechbar` (just past the short-rule boundary, still not long enough for the long rule); exactly 90 min + exactly 3 attendees → `BitteNichtStoeren` (AC 7); 90 min + 2 attendees → `Unterbrechbar` (long duration alone isn't enough); all-day + ≥1 attendee → `BitteNichtStoeren`.
  - [ ] Extend `tests/IntegrationTests/AppointmentEndpointsTests.cs`: `POST` creates a native appointment (`Provider`/`ProviderEventId` null) with the computed `Status`; `POST` with blank title → 400 `title-required`; `POST` with `end <= start` (including exactly equal) → 400 `invalid-time-range`; `POST` with a non-existent attendee id → 400 `attendee-not-found`; `POST` with valid attendees → 201, persisted `Attendee` rows verified; existing `GET /api/appointments` response now includes `status`.
  - [ ] New `tests/IntegrationTests/PersonEndpointsTests.cs`: `GET /api/persons` (authenticated non-admin) returns the roster with `id`+`email` only — assert the response shape explicitly (no password hash or other Identity field leaks).
  - [ ] New `status-badge.spec.ts`: renders the correct icon+text per status; asserts the rendered text is the full untruncated label (not just that a CSS class is absent).
  - [ ] New `appointment-create.spec.ts` (TestBed + `HttpTestingController`): pre-filled input sets initial field values; blank open leaves fields empty; blank title + save → inline error, focus on title input, **no HTTP call**; invalid time range + save → inline error, no HTTP call; valid save → `POST` fired with the expected body, success emits the created appointment; `Esc` closes with no HTTP call and restores focus to a stub trigger element; Tab cycling stays within the form while open (simulate a `Tab` sequence, assert `document.activeElement` never leaves the form's boundary).
  - [ ] New `calendar-column.spec.ts` (this component had no interactive behavior before this story): double-click on an empty slot emits `slotActivated` with the correct start `Date`; a slot already covered by an appointment does not emit on double-click; `Enter`/`Space` on a focused empty slot emits the same event as the double-click.
  - [ ] Extend `home.spec.ts`: "+ Neuer Termin" opens the form blank; a successful creation appends to `appointments()` with **no additional `GET /api/appointments` request** (only the `POST`).
  - [ ] New `focus-trap.spec.ts`: `Tab`/`Shift+Tab` wrap within the trapped container; deactivating restores focus to the pre-trap active element.

## Dev Notes

### Architecture Compliance

- **AD-4/AD-5 (status heuristic):** `StatusHeuristicService.Compute(appointment)` is the *only* place this rule is implemented — evaluated once at write time (this story's create flow; Epic 2's sync-upsert will call it the same way later), stored on the entity, never recomputed on read. Rule **order** is load-bearing: no-attendee check first, or AC 8 fails.
- **AD-7 (sync idempotency):** native appointments created here have `Provider`/`ProviderEventId` both `NULL` — same invariant Story 1.2 already established for the read side; this story is the first to actually *write* rows that must satisfy it.
- **AD-3 (central read service) — unaffected:** `IAppointmentViewService` stays read-only; this story's new write path is a separate `IAppointmentCreationService`/`IAppointmentRepository`, not an addition to it.
- **AD-13 (no localized text from backend):** every validation failure returns a stable `code` (`title-required`, `invalid-time-range`, `attendee-not-found`) — never German/English prose. Frontend owns all translation via the codes.
- **Consistency Conventions (binding, from Story 1.1/1.2):** GUID ids; UTC storage only; snake_case Postgres columns via `EFCore.NamingConventions` (already configured, don't reconfigure); RFC-7807 `problem+json` for all errors.

### Critical Guardrails (read before writing code)

1. **Rule order in `StatusHeuristicService.Compute` is not arbitrary** — no-attendee outranks all-day/long-duration. Get this wrong and AC 8 silently fails while everything else still passes.
2. **No "ganztägig" UI control in this story.** Implement the `IsAllDay` field and Domain branch fully; do not add a form checkbox for it (out of scope per FR-3/mock — see Task 1). Test the branch directly at the Domain level.
3. **Don't build Epic 3's Privat-Default filtering, multi-person view, or full person-selector.** The attendee picker here is a small, form-scoped component for internal team members only — not the general person-selector (different cap/scroll/persistence requirements, different surface).
4. **`Person` gets no new fields.** Attendee display uses email, not a name — deliberate, not an oversight (see Task 5).
5. **Reuse `computeOverlapLayout`'s occupancy data** for slot emptiness in `calendar-column` — do not build a second occupancy calculation that can drift out of sync with the one already rendering appointment blocks.
6. **Build the focus trap as a shared utility now**, not inline in the create form — Story 1.4's detail popover needs identical behavior next.
7. **`IAppointmentViewService` stays read-only.** The new write path is a separate interface/service — don't bolt a `CreateAsync` onto it.
8. **EF Core constructor-binding:** `Appointment`'s constructor must keep matching its *scalar* mapped properties (`Status`, `IsAllDay` join `Title`/`StartUtc`/etc.); the `Attendees` collection navigation is populated via the private backing field, not a constructor parameter — adding attendees to an unbounded constructor parameter list would break the "exactly one binding constructor" rule Story 1.2 already hit once.

### File Structure

New/changed since Story 1.2:

```text
src/
  Domain/AvailabilityStatus.cs                                # new
  Domain/Attendee.cs                                          # new
  Domain/Appointment.cs                                       # modified — Status, IsAllDay, Attendees, AssignStatus/AddAttendee
  Domain/StatusHeuristicService.cs                             # new
  Application/Appointments/IAppointmentCreationService.cs     # new
  Application/Appointments/IAppointmentRepository.cs          # new
  Application/Accounts/IPersonRepository.cs                   # modified — GetAllAsync
  Infrastructure/Appointments/AppointmentCreationService.cs   # new
  Infrastructure/Appointments/AppointmentRepository.cs        # new
  Infrastructure/Accounts/PersonRepository.cs                 # modified
  Infrastructure/Persistence/ApplicationDbContext.cs           # modified — Status/IsAllDay/Attendees mapping
  Infrastructure/Persistence/Migrations/*AddAppointmentStatusAndAttendees*  # new
  Api/Contracts/AppointmentContracts.cs                        # modified — CreateAppointmentRequest, Status on response
  Api/Contracts/PersonContracts.cs                             # new
  Api/Endpoints/AppointmentEndpoints.cs                        # modified — POST
  Api/Endpoints/PersonEndpoints.cs                             # new
  Api/Program.cs                                               # modified — DI + MapPersonEndpoints
frontend/src/app/
  pages/home/calendar/appointment.model.ts                    # modified — status field
  pages/home/calendar/calendar.service.ts                      # modified — createAppointment, getPersons
  pages/home/calendar/status-badge/*                            # new
  pages/home/calendar/attendee-picker/*                         # new
  pages/home/calendar/appointment-create/*                      # new
  pages/home/calendar/calendar-column/*                         # modified — slot grid, badge in appointment block
  pages/home/home.ts/.html/.css                                 # modified — create-button, wiring, local append
  shared/focus-trap/*                                           # new
  public/i18n/{de,en}.json                                      # modified
  testing/transloco-testing.ts                                  # modified
tests/
  IntegrationTests/AppointmentEndpointsTests.cs                 # modified
  IntegrationTests/PersonEndpointsTests.cs                      # new
  UnitTests/Domain/StatusHeuristicServiceTests.cs               # new
  (frontend) status-badge.spec.ts, appointment-create.spec.ts, calendar-column.spec.ts, focus-trap.spec.ts — new; home.spec.ts extended
```

### Testing Standards

- Backend: xUnit + Testcontainers Postgres (existing `PostgresContainerFixture`/`TestApiFactory`); Domain unit tests need no container — plain xUnit against `StatusHeuristicService.Compute` with directly-constructed `Appointment` instances.
- Frontend: Vitest + `TestBed` (not Jasmine/Karma — see Story 1.1/1.2).
- The focus-trap and status-heuristic logic must have direct unit tests as pure/isolated units — don't only test them indirectly through rendered components.

### Previous Story Intelligence (from Story 1.2)

- **Reuse, don't reinvent:** `ProblemResults.Problem(...)` for every error; Minimal API endpoints as `IEndpointRouteBuilder` extensions in `Api/Endpoints/*.cs`; request/response records in `Api/Contracts/*.cs`; `withCredentialsInterceptor`/`unauthorizedInterceptor` already global, no extra auth wiring needed for new `HttpClient` calls.
- **EF Core gotcha already hit once:** exactly one constructor must bind to scalar mapped properties — collection navigations go through backing fields, not constructor params (see Critical Guardrail 8 above).
- **Signal-driven local state:** Story 1.2 established fetching the whole visible grid once and deriving views from signals/computed — this story's "append locally on create" follows the same philosophy rather than forcing a refetch.
- **i18n discipline:** every user-visible string goes through Transloco with `de.json`/`en.json` keys — no exceptions, checked in Story 1.1/1.2 test suites via `transloco-testing.ts`.
- **`calendar-column`'s current shape:** pure rendering of `positioned()` appointment blocks over a fixed-height column, no interactivity, no discrete slot DOM — this story is the first to add focusable/clickable targets to it.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.3] — all 11 ACs as numbered above.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md#AD-3, AD-4, AD-5, AD-7, AD-13, Consistency Conventions, Capability → Architecture Map (FR-3, FR-10 rows)] — heuristic ownership/order, sync-idempotency shape, error-format rule.
- [Source: _bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md#FR-3, FR-4, FR-10] — creation fields, no-location-in-MVP, heuristic consequences.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md#components.status-badge, components.appointment-create-entry, components.person-selector] — badge visual spec, ghost-pill entry-point spec, chip styling reused (scaled down) for the attendee picker.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md#Component Patterns (Appointment-create entry points), State Patterns (Appointment creation — validation error), Interaction Primitives, Accessibility Floor (focus trap/restore, status never color-alone)] — entry-point behavior, validation UX, keyboard/focus contract.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/mockups/key-appointment-create.html] — three-state form reference (pre-filled/blank/validation-error); attendee-name display noted as illustrative, not a data requirement (see Task 5).
- [Source: _bmad-output/project-context.md] — cross-cutting stack/testing/critical rules.
- [Source: _bmad-output/implementation-artifacts/1-2-eigene-kalenderansicht-monat-woche-tag.md] — established conventions, `calendar-column`'s pre-existing (non-interactive) shape, the EF Core constructor-binding gotcha.

## Dev Agent Record

### Agent Model Used

_To be filled in by the dev-story workflow._

### Debug Log References

### Completion Notes List

### File List
