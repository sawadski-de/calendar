---
baseline_commit: e13032eb838a8426bd2d64d9b93d15ae20badb94
---

# Story 1.4: Termin-Detailansicht (eigene Termine)

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Teammitglied,
I want die vollen Details eines eigenen Termins per Klick öffnen können,
so that ich Titel, Zeit und Teilnehmer jederzeit einsehen kann.

## Acceptance Criteria

1. **Full detail on click, any view.** Given I click on an own appointment in any view (Month/Week/Day), when the detail popover opens, then it shows title, time, attendees, and the current availability status in full — independent of the Privat-Default, which only applies to other viewers.
2. **No location field.** Given an appointment without a location (location is FR-15, Should-Have, not MVP), when the detail popover is shown, then no location field is displayed.
3. **Escape/outside-click closes.** Given the detail popover is open, when I press `Esc` or click outside it, then it closes and focus returns to the element that triggered it.
4. **Keyboard-opened popover traps focus.** Given the popover is opened via keyboard (`Enter`/`Space` on the focused appointment), when it is visible, then focus is trapped inside it (Tab cycle stays within) until it closes.

## Tasks / Subtasks

- [x] **Task 1: Backend — single-appointment detail read** (AC: 1, 2)
  - [x] Extend `Application/Appointments/IAppointmentViewService.cs` with `Task<Appointment?> GetOwnAppointmentByIdAsync(Guid personId, Guid appointmentId, CancellationToken)` — stays inside the existing central read service (AD-3), returning `null` when the appointment doesn't exist **or** isn't owned by `personId` (don't distinguish the two cases to the caller — same 404 either way, no existence leak).
  - [x] Implement in `Infrastructure/Appointments/AppointmentViewService.cs`: `dbContext.Appointments.Include(a => a.Attendees).FirstOrDefaultAsync(a => a.Id == appointmentId && a.PersonId == personId, cancellationToken)` — `Include` works against the public `Attendees` property regardless of its private backing-field storage (same pattern EF already uses for materializing it, per Story 1.3).
  - [x] Add `Api/Contracts/AppointmentContracts.cs`: `AttendeeSummaryResponse(Guid PersonId, string Email)` and `AppointmentDetailResponse(Guid Id, string Title, DateTimeOffset StartUtc, DateTimeOffset EndUtc, AvailabilityStatus Status, IReadOnlyList<AttendeeSummaryResponse> Attendees)`. **No location property** — `Appointment` has no location field at all (FR-15 not built), so AC 2 is satisfied by omission, not a conditional hide; don't add a stubbed/always-null location field.
  - [x] Add `GET /api/appointments/{id:guid}` to `AppointmentEndpoints.cs` (same file, alongside the existing `GET`/`POST`): `RequireAuthorization()`, resolve `personId` the same way the other two handlers already do, call `GetOwnAppointmentByIdAsync`; `null` → `ProblemResults.Problem(404, "appointment-not-found", ...)`. On success, resolve attendee emails via `IPersonRepository.GetAllAsync()` (already added in Story 1.3 for the same "small team, one roster fetch" reasoning — do not add a second bulk-lookup method or loop `GetByIdAsync` per attendee, that N+1 pattern was just fixed in Story 1.3's code review) filtered to `appointment.Attendees.Select(a => a.PersonId)`, map to `AttendeeSummaryResponse`, return `AppointmentDetailResponse`.

- [x] **Task 2: Frontend — detail popover component** (AC: 1, 2, 3, 4)
  - [x] Extend `calendar.service.ts`: `getAppointmentDetail(id: string): Observable<AppointmentDetail>` (`GET /api/appointments/{id}`); add the `AppointmentDetail`/`AttendeeSummary` interfaces (mirror the backend contracts — `status` typed as the existing `AvailabilityStatus` union from `appointment.model.ts`).
  - [x] Add `pages/home/calendar/appointment-detail/{appointment-detail.ts,.html,.css,.spec.ts}` — shares the exact popover shape/behavior as `appointment-create` (backdrop + `appFocusTrap` panel, `surface-2`/`border`/`rounded.md`/drop-shadow per `DESIGN.md components.appointment-detail-popover`): on init, calls `getAppointmentDetail(appointmentId)` and renders title (`text-label`), date/time range, attendee list (email per row — same "email, not a name" scope decision Story 1.3 made for `Person`, carry it forward, don't reopen it), and the status via the existing `<app-status-badge>` (built in Story 1.3 — reuse directly, do not rebuild). **No location field anywhere in the template** (see Task 1). Backdrop click / `Esc` closes via the same pattern `appointment-create` already established (click on backdrop emits close; `(keydown.escape)` on the panel; `$event.stopPropagation()` on the panel's own click so backdrop-close doesn't fire for clicks inside).
  - [x] Input: `appointmentId: input.required<string>()`. Output: `closed = output<void>()`. No `saved`/edit capability — this story is read-only detail, not an edit form (editing appointments is not in any AC here).

- [x] **Task 3: Frontend — click/keyboard entry points on existing appointment renderings** (AC: 1, 4)
  - [x] `calendar-column.ts`/`.html`: the existing `.calendar-column__appointment` block (Week/Day views) currently has no interactivity (Story 1.2 built it inert on purpose, deferring this to this story). Add `tabindex="0"`, `role="button"`, and `(click)`/`(keydown.enter)`/`(keydown.space)` handlers emitting a new `appointmentActivated = output<string>()` with the appointment's `id`. **Emit the id, not the full `Appointment` object** — the popover always fetches fresh detail from the API (server-authoritative, consistent with AD-3), so passing local list data would be redundant and could go stale. Appointment blocks are rendered *after* the slot grid in the same template (existing DOM order) — this already means a click lands on the block, not the underlying empty-slot element, at any point where they visually overlap; don't reorder the two `@for` blocks or reintroduce a z-index without re-verifying this.
  - [x] `month-view.ts`/`.html`: the rendered title spans (`.month-view__title`, up to `maxTitlesPerCell` — see Story 1.2) get the same `tabindex="0"`/`role="button"`/click+keyboard handlers, emitting the identical `appointmentActivated` output. **Scope note:** the "+N more" overflow indicator stays inert — no AC requires expanding it to reveal the remaining appointments, and building that affordance now would be speculative scope beyond this story's read-one-appointment-at-a-time popover.
  - [x] Wire both outputs in `Home` (`home.ts`/`.html`): own the detail-popover's open/closed state and the currently-selected appointment id; on `appointmentActivated` from either component, open `<app-appointment-detail>`; on its `closed` output, close it. Follows the exact same open/close pattern `home.ts` already has for `appointment-create` (`createFormOpen`/`onAppointmentCreateCancelled`) — mirror that shape for consistency, don't invent a different one.

- [x] **Task 4: Tests** (AC: all)
  - [x] Extend `tests/IntegrationTests/AppointmentEndpointsTests.cs`: `GET /api/appointments/{id}` for an owned appointment with attendees returns title/time/status/attendee emails (seed via the existing `POST` or direct `DbContext` insert + `AddAttendee`); `GET` for a non-existent id → 404 `appointment-not-found`; `GET` for another person's appointment (owned by someone else) → 404 `appointment-not-found` (not 403 — no existence leak, see Task 1); response has no location-related property (assert the JSON shape, not just individual field values, so a future accidental re-add of a location field is caught).
  - [x] New `appointment-detail.spec.ts` (TestBed + `HttpTestingController`, mirror `appointment-create.spec.ts`'s structure from Story 1.3): renders title/time/attendee emails/status badge from a flushed `GET /api/appointments/{id}` response; renders no location text anywhere; `Esc` emits `closed` with no further HTTP calls; a backdrop click emits `closed`; a click inside the panel does not emit `closed`.
  - [x] New `calendar-column.spec.ts` additions (extend the Story 1.3 file): clicking an appointment block emits `appointmentActivated` with its id; `Enter`/`Space` on a focused appointment block emits the same event; clicking an empty slot still emits `slotActivated`, not `appointmentActivated` (regression guard for the two features sharing one component).
  - [x] New `month-view.spec.ts` (this component had no tests before this story): clicking a rendered appointment title emits `appointmentActivated` with its id; the "+N more" indicator has no click handler attached (assert clicking it does not emit).
  - [x] Extend `home.spec.ts`: clicking a rendered appointment opens the detail popover with the right `appointmentId`; the popover's `closed` output closes it.

## Dev Notes

### Architecture Compliance

- **AD-3 (central read service):** the new single-appointment read stays on `IAppointmentViewService` — this is still "read my own appointment," the same service that already handles "read my own appointments in a range." Do not create a second, parallel read interface for the detail case.
- **Consistency Conventions:** GUID ids; RFC-7807 `problem+json` with a stable `code` (AD-13) for the 404 case — never localized text; the frontend maps `appointment-not-found` to a translated message if it ever surfaces (in practice a rare race — appointment deleted between render and click — not an explicit AC, handle gracefully rather than crash, but no dedicated UI copy is mandated here).

### Critical Guardrails (read before writing code)

1. **Reuse `shared/focus-trap` and `app-status-badge` as-is.** Both were built in Story 1.3 specifically so this story wouldn't need to rebuild them — `appointment-detail`'s popover shell should look structurally identical to `appointment-create`'s (backdrop, `appFocusTrap` panel, Esc/outside-click-to-close), just read-only content instead of a form.
2. **No location field, anywhere** — not in the backend contract, not in the frontend template. `Appointment` has no location property; inventing one (even as an always-empty placeholder) would be speculative schema growth for a Should-Have (FR-15) this story doesn't implement.
3. **Attendee display is email, not a name** — same `Person`-has-no-display-name scope decision from Story 1.3, carried forward. Don't add a name field now either.
4. **Fetch detail by id from the API on open, don't reuse the list's local `Appointment` data.** The list view (`home.ts`'s `appointments` signal) doesn't carry attendee data at all — only the detail endpoint does — so there's no shortcut here anyway, but the principle also matters for future stories: server-authoritative reads per AD-3.
5. **Don't touch `AppointmentResponse`** (the list/create response shape) — it stays exactly as Story 1.3 left it. The new `AppointmentDetailResponse` is a separate, additive contract for the single-appointment endpoint only.
6. **Appointment-block click vs. slot double-click:** both live in `calendar-column` now. Verify (via the regression test in Task 4) that clicking an appointment never fires `slotActivated` and vice versa — they're separate DOM elements, not a shared handler with a branch, so this should hold naturally, but the test exists because Story 1.3 already showed this component's interaction surface can get subtle.

### File Structure

New/changed since Story 1.3:

```text
src/Application/Appointments/IAppointmentViewService.cs   # modified — GetOwnAppointmentByIdAsync
src/Infrastructure/Appointments/AppointmentViewService.cs  # modified — implementation
src/Api/Contracts/AppointmentContracts.cs                   # modified — AttendeeSummaryResponse, AppointmentDetailResponse
src/Api/Endpoints/AppointmentEndpoints.cs                   # modified — GET /api/appointments/{id}
tests/IntegrationTests/AppointmentEndpointsTests.cs          # modified — detail endpoint tests
frontend/src/app/pages/home/calendar/
  calendar.service.ts                                        # modified — getAppointmentDetail
  appointment-detail/{appointment-detail.ts,.html,.css,.spec.ts}  # new
  calendar-column/{calendar-column.ts,.html}                 # modified — clickable/focusable appointment blocks
  calendar-column/calendar-column.spec.ts                    # modified — new activation tests
  month-view/{month-view.ts,.html}                           # modified — clickable/focusable titles
  month-view/month-view.spec.ts                              # new
frontend/src/app/pages/home/{home.ts,home.html}              # modified — detail-popover wiring
frontend/src/app/pages/home/home.spec.ts                     # modified — new tests
```

### Testing Standards

- Backend: xUnit + Testcontainers Postgres (existing `PostgresContainerFixture`/`TestApiFactory`), same as Stories 1.1–1.3.
- Frontend: Vitest + `TestBed` (not Jasmine/Karma).
- Assert the 404 response's JSON shape/code explicitly for both "doesn't exist" and "exists but isn't mine" — the point of collapsing them to one response is exactly that a test (and an attacker) can't tell them apart from the outside.

### Previous Story Intelligence (from Story 1.3)

- **Reuse-first checklist for this story:** `shared/focus-trap` (built specifically anticipating this story), `pages/home/calendar/status-badge` (status rendering), `IPersonRepository.GetAllAsync` (attendee-email resolution, same "one roster fetch, no N+1" fix already applied in Story 1.3's code review). Don't rebuild any of these.
- **`calendar-column` and `month-view` had zero interactivity before Story 1.3/1.4** — Story 1.3 added the first interactive element (empty-slot creation); this story adds the second (appointment click). Keep the two interaction surfaces cleanly separate (see Critical Guardrail 6).
- **Code-review lesson from Story 1.3:** a migration that adds a required column needs an explicit, valid default for pre-existing rows — not relevant to this story's schema-free scope (no migration needed here), but worth remembering if a future task in this story turns out to need one.
- **`home.ts`'s existing open/close pattern for `appointment-create`** (`createFormOpen` signal + `cancelled`/`saved` outputs) is the template to mirror for the detail popover's `open`/`closed` wiring — don't invent a structurally different pattern for what is functionally the same kind of modal lifecycle.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 1.4] — all 4 ACs as numbered above.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md#AD-3, AD-13, Capability → Architecture Map (FR-4 row)] — read-service ownership, error-format rule.
- [Source: _bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md#FR-4] — own-appointment full detail, no-location-in-MVP.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md#components.appointment-detail-popover] — popover visual spec (full variant only — the status-only variant is Epic 3's job, not built here).
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md#Component Patterns (Appointment detail popover), Accessibility Floor (focus trap/restore)] — full-vs-status-only mode split (only "full" applies to this story), focus/keyboard contract.
- [Source: _bmad-output/project-context.md] — cross-cutting stack/testing/critical rules.
- [Source: _bmad-output/implementation-artifacts/1-3-termin-anlegen-nativ-mit-automatischem-verfuegbarkeits-status.md] — `shared/focus-trap` and `status-badge` built specifically for this story to reuse; the N+1/roster-fetch pattern for attendee resolution; `home.ts`'s modal open/close convention.

## Dev Agent Record

### Agent Model Used

Claude Sonnet 5 (claude-sonnet-5)

### Debug Log References

- `Include(a => a.Attendees)` worked against `Appointment.Attendees`'s private backing field with zero extra `OnModelCreating` configuration, confirming Story 1.3's note that EF Core's default convention handles this transparently.
- ASP.NET Core's default JSON options (already configured globally) serialize record property names as camelCase (`PersonId` → `personId`), matching the frontend's `AttendeeSummary`/`AppointmentDetail` interfaces without any explicit naming-policy work.

### Completion Notes List

- All 4 acceptance criteria implemented and verified. Backend: 45/45 tests passing (14 unit + 31 integration, incl. 4 new `AppointmentEndpointsTests` cases: full detail with attendees, 404-not-found, 404-not-mine, no-location-property). Frontend: 58/58 tests passing across 15 spec files (up from 44/13 after Story 1.3).
- **Reused, not rebuilt:** `shared/focus-trap` and `status-badge` (both from Story 1.3) plug directly into `appointment-detail` with no modification — confirms the "build it generically, reuse it next story" bet paid off.
- **404 collapses two cases deliberately:** "appointment doesn't exist" and "appointment exists but isn't mine" return the identical `appointment-not-found` 404 — verified with a dedicated test using a second, freshly-provisioned person so there's no existence leak an attacker (or a curious teammate) could exploit to enumerate other people's appointment ids.
- **Interaction-surface regression guard:** added explicit tests proving appointment-block clicks and empty-slot double-clicks (both live in `calendar-column` now) never cross-fire each other's output — this was called out in Dev Notes precisely because Story 1.3 already showed this component's interaction surface can get subtle.
- Not built in this story (intentionally, per scope): editing or deleting appointments (read-only detail only, no AC covers it), a location field (FR-15, Should-Have), expanding Month view's "+N more" overflow indicator (no AC requires it).
- Verified via full test suites: `dotnet test` (45/45) and `ng test`/`ng build` (58/58 tests, clean build) both run clean at the end of implementation.

### File List

**Backend — `src/Application`**
- `Appointments/IAppointmentViewService.cs` (modified — `GetOwnAppointmentByIdAsync`)

**Backend — `src/Infrastructure`**
- `Appointments/AppointmentViewService.cs` (modified — implementation, `Include(a => a.Attendees)`)
- `Appointments/AppointmentCreationService.cs` (modified; code-review fix — null-guard `attendeePersonIds` before `Distinct()`)

**Backend — `src/Api`**
- `Contracts/AppointmentContracts.cs` (modified — `AttendeeSummaryResponse`, `AppointmentDetailResponse`)
- `Endpoints/AppointmentEndpoints.cs` (modified — `GET /api/appointments/{id:guid}`)

**Backend tests**
- `tests/IntegrationTests/AppointmentEndpointsTests.cs` (modified — 4 new detail-endpoint tests; +1 code-review regression test for a missing `attendeePersonIds` field)

**Frontend — `frontend/src/app`**
- `pages/home/calendar/calendar.service.ts` (modified — `getAppointmentDetail`, `AppointmentDetail`/`AttendeeSummary`)
- `pages/home/calendar/date-utils.ts` (modified; code-review fix — shared `formatTime`, deduplicated from `calendar-column`/`appointment-detail`)
- `pages/home/calendar/appointment-detail/{appointment-detail.ts,.html,.css,.spec.ts}` (new; code-review fix — 404 error state instead of a blank overlay, +2 regression tests)
- `pages/home/calendar/appointment-create/{appointment-create.ts,.html}` (modified; code-review fix — handles `attendee-not-found`, +1 regression test)
- `pages/home/calendar/calendar-column/{calendar-column.ts,.html,.css}` (modified — clickable/focusable appointment blocks; code-review fix — uses shared `Activatable` directive); `calendar-column.spec.ts` (modified — new activation + regression tests)
- `pages/home/calendar/month-view/{month-view.ts,.html,.css}` (modified — clickable/focusable titles; code-review fix — uses shared `Activatable` directive); `month-view.spec.ts` (new)
- `shared/activatable/{activatable.ts,.spec.ts}` (new; code-review fix — extracted from three copy-pasted tabindex/role/click/keydown blocks, fixes missing `preventDefault` on Space)
- `pages/home/home.ts`, `home.html` (modified — detail-popover wiring)
- `pages/home/home.spec.ts` (modified — new tests)
- `testing/transloco-testing.ts` (modified — new i18n test keys)
- `public/i18n/{de,en}.json` (modified — new `calendar.detail*`/`calendar.createAttendeeNotFound` keys)

### Change Log

- 2026-07-03: Story 1.4 fully implemented (Tasks 1–4) — `GET /api/appointments/{id}` (own-appointment detail with attendee emails, 404 collapsing "doesn't exist" and "isn't mine" into one response), and the full frontend flow: clickable/focusable appointment renderings in both `calendar-column` (Week/Day) and `month-view` (Month), a read-only detail popover reusing Story 1.3's `focus-trap` and `status-badge`. All 4 ACs verified; 45 backend + 58 frontend tests passing, both builds clean. Status → review.
- 2026-07-03: Addressed workflow-backed code-review findings (high effort, 7 verified). Fixed: (1) `CreateAppointmentRequest.AttendeePersonIds` could deserialize to `null` despite its non-nullable type (System.Text.Json ignores the annotation for a missing JSON property), crashing `Distinct()` with an unhandled 500 — null-guarded, regression test added. (2) `appointment-create` silently swallowed the `attendee-not-found` error code with no user feedback — now shows an inline message. (3) `appointment-detail` had no error handler at all for its 404, leaving a blank overlay — now shows an error message with a close button. (4/5) three near-identical `tabindex`/`role="button"`/click+keydown blocks (two in `calendar-column`, one in `month-view`) were extracted into a shared `shared/activatable` directive, which also fixes a missing `event.preventDefault()` on Space that caused the page to scroll on keyboard activation. (6) a duplicated `formatTime` helper was moved into `date-utils.ts`. One finding — `GET /api/appointments/{id}` loading the full roster via `GetAllAsync` to resolve a handful of attendee emails — was investigated and left as-is: it's the same "small team, one roster fetch, no N+1" pattern this codebase deliberately adopted in Story 1.3's own code review (see that story's Dev Notes), not a new defect; introducing a second, differently-shaped lookup method here would be inconsistent rather than an improvement at this app's explicit 5–10 person team scale. 46/46 backend tests and 67/67 frontend tests passing after fixes (up from 45/58).
