---
name: review-reconcile-ux
type: reconciliation-review
target: architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md
against:
  - ux-calendar-neu-bmad-2026-07-02/DESIGN.md
  - ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md
date: 2026-07-02
---

# Reconciliation Review — Architecture Spine vs. UX Spine

## Overall verdict: minor-to-real gaps (one significant, load-bearing gap; two smaller ones). No outright contradictions found — everything the architecture spine does say is consistent with the UX spine. The problems are omissions, not conflicts.

---

## Finding 1 (real gap) — Sync-Transparenz is a named Must-Have NFR with zero architectural backing

**UX passage(s):**
- EXPERIENCE.md, Component Patterns table (Sync-timestamp indicator row): "a silent failure is a direct NFR violation (Sync-Transparenz, PRD §7)."
- EXPERIENCE.md, State Patterns ("Sync failure (repeated/silent)"): "Must be visible, not silent (Sync-Transparenz NFR — this is a Must-Have, not a nice-to-have, precisely because Dennis is bus-factor-1 and won't notice a quiet failure otherwise)... Surfaced per-account to the affected user *and* aggregated for all accounts on the Admin surface."
- EXPERIENCE.md, Information Architecture: "Admin → Sync overview | Admin-only nav item (role-gated) | All team members' sync health at a glance: person | provider | status | last sync... A distinct surface from per-user Settings, serving the bus-factor-1 operator need."
- EXPERIENCE.md, Flow 3 (Dennis checks team-wide sync health) — an entire named flow built around this surface and its failure-flagging behavior ("failure state, not just an old timestamp").

**Architecture spine gap:**
- The spine explicitly binds several NFRs by name to specific ADs: `Zugriffsschutz-NFR` → AD-8/AD-9/AD-10; `Datenhaltung-nativer-Termine-NFR` → AD-14. **`Sync-Transparenz` is never mentioned anywhere in ARCHITECTURE-SPINE.md** — not in an AD's `Binds` line, not in the Capability → Architecture Map, not in the Deferred section (where it would at least be a flagged, deliberate deferral rather than a silent one).
- AD-7 ("Sync-Idempotenz über Provider-Event-ID") governs *deduplication* of synced appointments, not tracking of sync *attempts*, *failures*, or *last-successful-sync timestamps* — a different concern. No AD, entity, or ERD attribute establishes that a sync outcome (success/failure, timestamp, consecutive-failure count) is persisted per `CalendarConnection`, even though the UX's per-person sync-timestamp indicator, the per-account failure state, and the Admin aggregate table all depend on exactly that data existing somewhere queryable.
- The Capability → Architecture Map (spine lines 204-219) has no row at all for the "Admin → Sync overview" surface — every other named UX surface/FR has a row; this one is simply absent, which is the "silently ignored" failure mode this review is checking for.

**Why it matters:** Without an invariant comparable to AD-7 (e.g., "every sync attempt records outcome + timestamp on `CalendarConnection`; the Worker never fails silently"), a developer implementing the Worker could satisfy every stated AD (idempotent upsert, encrypted tokens, shared Worker/API codebase) while never persisting the failure/last-sync state the entire Admin Sync overview surface and the per-column sync-indicator failure state are built on. This is precisely the "silent ignore" pattern the reconciliation check is meant to catch — a Must-Have NFR with a dedicated, described UX surface and flow, absent from the architecture's invariants, entities, and capability map alike.

---

## Finding 2 (real gap) — No invariant enforces server-side authorization for the Admin-only Sync overview surface

**UX passage(s):**
- EXPERIENCE.md, Foundation: "**Roles:** a real role system — Admin vs. Member — with actual permission checks, **not just an unguarded extra page**... The Admin → Sync overview surface is gated behind the Admin role."
- EXPERIENCE.md, State Patterns ("Permission denied — Admin surface"): "Nav item is simply absent for non-Admins, not shown-then-blocked... consistent with a real role gate rather than a decorative one."

**Architecture spine gap:**
- AD-9 ("Entkoppelte Authentifizierung") and AD-12 ("Deprovisionierung ist Soft-Deaktivierung") are the only ADs that touch the Admin/Member role model, and both are about something else (login/OAuth decoupling; deactivation semantics). Neither states that Admin-only capabilities (the Sync overview aggregate endpoint spanning every team member's data) must be authorized **server-side**, independent of what the Angular client chooses to render.
- Contrast this with how carefully the spine handles the analogous privacy concern: AD-3 explicitly states "Rein clientseitige Filterung erfüllt diese Regel nicht (PRD FR-9)" — a direct rule against relying on the client alone. No equivalent sentence exists for the role gate, even though the UX Foundation phrases the requirement in exactly the same spirit ("not just an unguarded extra page"). As written, an implementer could satisfy every literal AD by hiding the nav item in Angular (matching the "Permission denied" state pattern's visual description) while leaving the underlying `/api/admin/sync-overview`-style endpoint reachable by any authenticated Member — which would violate the UX's explicit "real permission checks" requirement without violating any stated architecture invariant.

**Why it matters:** This is a real, if narrower, gap: the UX names this as a deliberate, explicit decision ("explicit user decision" per Foundation), and the architecture spine's own pattern elsewhere (AD-3) shows it knows how to write this kind of "server, not client" invariant — it just didn't do so for the role gate.

---

## Finding 3 (minor gap) — No mechanism specified for the async, no-reload sync-status update the `aria-live` behavior depends on

**UX passage(s):**
- EXPERIENCE.md, Component Patterns (Sync-timestamp indicator): "the transition itself is announced via `aria-live=\"polite\"`... since it can happen asynchronously with no page reload."
- EXPERIENCE.md, Accessibility Floor: "Live regions for async state changes: the sync-indicator's failure-state transition is announced via an `aria-live=\"polite\"` region... this is an asynchronous, no-reload state change per the Sync-Transparenz NFR, so a screen-reader user needs an explicit announcement to learn about it at all."

**Architecture spine gap:**
- The spine's system diagram and AD-11 describe the Worker and API coordinating only through Postgres, with no process-to-process call. That's a reasonable backend decision, but nothing in the spine describes **how the Angular frontend learns of a sync-status change without a page reload** (polling interval + endpoint, SSE, WebSocket, etc.). Because this is the mechanism the `aria-live` requirement is actually built on (a value has to change on an already-rendered page for the live region to have something to announce), its complete absence from the spine leaves a UX-committed, accessibility-load-bearing behavior technically ungrounded — it's compatible with several implementations but the spine picks none of them, not even as a named "Deferred" item.
- This is smaller than Findings 1–2 because it's plausibly intended to fall out of ordinary REST polling once Finding 1's missing sync-status data model is fixed, but as it stands neither piece exists.

---

## Areas checked and found consistent (no gap)

- **Server-side privacy enforcement (EXPERIENCE.md Foundation, "server responses to unauthorized viewers must already omit those fields"):** Well reconciled. AD-3 explicitly routes every foreign-appointment read through `AppointmentViewService`, decides "ob volle Felder oder nur 'privat/beschäftigt' + Status zurückgegeben werden," and states in its own words "Rein clientseitige Filterung erfüllt diese Regel nicht (PRD FR-9)" — matching the UX's requirement almost verbatim, including the FR-9 attendee exception ("anhand von Eigentümerschaft/**Teilnahme**").
- **Login/auth surface deliberately unspecified in UX, deferred to architecture:** Well reconciled. EXPERIENCE.md's Information Architecture explicitly defers the auth mechanism and states new users must land directly in an empty calendar with calendar-connection as a later, optional step ("Optional and reached later — not a forced onboarding step"). AD-9 was written to directly satisfy this: it decouples ASP.NET Core Identity login (admin-provisioned accounts, no self-signup) from the Settings→Calendar-Connections OAuth flow, and its own `Prevents` clause names the UX decision it protects ("...unterläuft damit die UX-Entscheidung, dass das Verbinden eines Kalenders ein optionaler, späterer Schritt ist"). The deferred login *states* (loading/invalid-credential/lockout) are UX's own explicit deferral, not something the architecture needed to resolve this round.
- **i18n requirement (German canonical + English parallel; component sizing for translated strings):** No contradiction. AD-13 (backend returns only stable codes, all UI-visible translation lives in Angular) is compatible with and supports the UX's German-canonical/English-parallel model. Component-sizing-for-translated-strings is a DESIGN.md-level concern (intrinsic-width tokens on the status-override flyout etc.) that the architecture spine has no need to restate; nothing in the spine forces fixed-width containers that would conflict with it.
- **Status-override visual/data neutrality (FR-11, "no auto vs. manual indicator"):** AD-6 computes a single unified "current status" value (override-if-set, else heuristic, else default) — consistent with the UX's requirement that no viewer-facing signal reveal which source produced the status.

---

## Recommendation

Add at minimum:
1. An AD (or extension of AD-7/AD-11) binding the `Sync-Transparenz` NFR: define that `CalendarConnection` (or a related entity) persists last-sync outcome/timestamp/failure count, and that the Worker never fails without recording that fact.
2. An explicit server-side authorization rule for Admin-only endpoints (Sync overview and any future admin surface), stated with the same "client-side alone does not satisfy this" force as AD-3.
3. Either a stated mechanism (or an explicit Deferred entry) for how the frontend detects sync-status changes without a page reload, since the Accessibility Floor's `aria-live` requirement depends on it.
