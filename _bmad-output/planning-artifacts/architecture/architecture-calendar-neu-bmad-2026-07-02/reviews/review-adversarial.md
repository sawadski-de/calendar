---
name: 'Adversarial Review — ARCHITECTURE-SPINE.md'
type: review
target: '_bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md'
created: '2026-07-02'
---

# Adversarial Review — Architecture Spine (Team-Terminkalender mit Synchronisation)

## Method

Read every AD as a contract two independent implementers (human or agent) could each satisfy to the letter. For each, constructed a concrete pair of implementation choices — both textually compliant — and asked whether the two artifacts they produce (schema, enum, DTO shape, write path, timing assumption) actually compose. Findings below are only cases with a demonstrable integration break, silent data corruption, or FR-9 privacy leak — not naming/style bikeshedding.

## Overall Verdict

**Not yet safe to hand to two parallel teams/agents.** The spine is strong on layering (AD-1) and on the two invariants it clearly anticipated needing central enforcement for (AD-3 privacy, AD-4 status compute). But it has one **provable internal inconsistency** (a status enum spelled two different ways in two ADs that must compose), one **fetch-semantics contradiction** that can cause mass false-deletion of synced appointments, one **schema-level gap** that can make native-appointment creation fail outright depending on a reasonable-but-unlucky nullability choice, and a **live (non-deferred) gap** around recurring events that the Deferred section doesn't actually cover. AD-3's centralization mandate also has no answer for the one place performance pressure will most tempt a bypass: the multi-person view's bulk query — which is exactly the FR-9 high-stakes surface the review was asked to scrutinize hardest.

8 findings, ranked by severity below.

---

## Finding 1 — Status enum spelled two different ways across AD-5 and AD-6 (will not compose)

**Severity: Critical**
**ADs:** AD-5, AD-6 (composition explicitly required by AD-6's own rule)

**The text, verbatim:**
- AD-5: "→ `unterbrechbar`" / "→ `bitte-nicht-stören`" — lowercase, hyphenated, German literal.
- AD-6: `Status` (`Unterbrechbar` | `BitteNichtStören` | null = automatisch) — PascalCase, no hyphen.

**Scenario:** Dev/Agent A implements `Domain.StatusHeuristicService` (AD-4/AD-5) and, following AD-5's rule text literally, defines the computed field's values as the lowercase-hyphenated strings shown there (or a C# enum with `[EnumMember]`/JSON attributes producing that exact casing). Dev/Agent B implements `StatusOverride` (AD-6) and, following AD-6's rule text literally, defines `StatusOverride.Status` as a C# enum `Unterbrechbar | BitteNichtStören`. Both are 100% faithful to "their" AD.

**The incompatibility:** AD-6's own rule requires composing these two values ("Override, falls gesetzt; sonst der vorausberechnete Status (AD-4) des gerade aktiven Termins") into one "current status" result. There is no canonical shared enum — whoever writes the composition code (and whoever writes the DTO that serializes it to the Angular frontend) must guess which casing is authoritative. Two features that each surface "current status" (e.g., the FR-2 multi-person status badge vs. an override-setting control) can each pick a different one of the two spellings, or normalize inconsistently, so the same logical value renders differently depending on whether it came from a stored computed field or from an override — breaking any frontend switch/lookup keyed on the string, and breaking equality checks (e.g., "is the override redundant with the computed status" logic) anywhere both are compared.

**Fix:** Add one canonical `Status` value set (name + exact casing) at the spine level (e.g. in Consistency Conventions or a tightened AD-4/AD-6), and require both `Appointment.Status` and `StatusOverride.Status` to use the same enum type/serialization, not two independently-declared enums that happen to mean the same thing.

---

## Finding 2 — AD-2's `FetchEvents(connection, since)` is ambiguous between delta and full-snapshot semantics; AD-7's deletion rule only works under one of them

**Severity: Critical**
**ADs:** AD-2, AD-7

**Scenario:** Dev/Agent A implements `GoogleCalendarProvider`/`OutlookCalendarProvider.FetchEvents(connection, since)`. The parameter name `since` and the general efficiency goal (don't re-pull a person's whole calendar every poll) naturally reads as "give me a delta since the last successful sync" — which is exactly what Google's `syncToken` and Microsoft Graph's `delta` query are optimized for, and what a competent implementer would reach for. Dev/Agent B implements the Worker's upsert loop per AD-7: "fehlt ein zuvor importierter Schlüssel **im aktuellen Abruf**, wird der Termin ... entfernt" — this deletion rule is only correct if "der aktuelle Abruf" is a complete snapshot of all currently-active provider events in the sync window, so that "missing" reliably means "the organizer deleted/moved it," not "it just didn't change this cycle."

**The incompatibility:** If A's adapter returns only changed events (true delta semantics — the natural reading of "since"), then every appointment that simply didn't change this cycle is "missing" from B's "current fetch" and gets deleted by AD-7's rule. Result: mass false-deletion of unchanged synced appointments on every poll cycle after the first. Both implementers followed their AD to the letter; the two ADs contradict each other operationally.

**Fix:** AD-2 or AD-7 must state explicitly whether `FetchEvents` returns (a) a full current snapshot of the sync window (in which case AD-7's diffing is correct and delta/sync-token optimizations are out of scope for this prototype), or (b) a true delta (in which case AD-7 needs a different deletion signal — e.g., explicit tombstone/cancelled-status events from the provider — not "absence from the current fetch").

---

## Finding 3 — AD-7's uniqueness key has no specified null/sentinel convention for native appointments; one reasonable schema choice breaks native appointment creation outright

**Severity: Critical**
**ADs:** AD-7 (scoped to "synchronisierte Termine"), interacting with FR-3/FR-4 native creation (governed by AD-3/AD-4, which say nothing about the Provider/ProviderEventId columns)

**Scenario:** Dev/Agent A builds the `Appointment` schema/migration while implementing FR-3 (native create). Dev/Agent B builds the Worker upsert path per AD-7. Neither AD specifies what `Provider`/`ProviderEventId` should contain for a natively-created appointment — AD-7 only says the key is unique "für synchronisierte Termine," implying native rows are outside its scope, but not how a native row is supposed to satisfy (or be exempted from) a composite-unique-index defined on those same three columns.

**The incompatibility:** If Provider/ProviderEventId are left nullable and actually stored as `NULL` for native rows, Postgres's default unique-index semantics (each `NULL` is distinct) happen to make this work — but that is an implementation detail nobody was told to rely on, and it is equally reasonable (arguably more idiomatic given the "GUID/uuid for all entities" no-surprises convention already documented) for a developer to instead give `Provider` a non-nullable enum with a `Native`/`None` member and give `ProviderEventId` a non-nullable string defaulting to an empty sentinel (`""`) for native rows — a very ordinary EF Core migration choice. Under that (equally spec-compliant) choice, `(PersonId, Provider='Native', ProviderEventId='')` collides for every native appointment a person creates after their first — **a user's second native appointment fails to insert**, a hard functional break discovered only in integration, not in either developer's unit tests (each tests their own slice in isolation).

**Fix:** AD-7 (or the Consistency Conventions table) should state explicitly: native appointments must have `Provider = NULL` and `ProviderEventId = NULL`, and the unique constraint must be declared as a partial/filtered index (`WHERE ProviderEventId IS NOT NULL`) — not a plain composite unique constraint relied on implicitly.

---

## Finding 4 — AD-3 centralizes single-appointment reads but gives no contract for the bulk/multi-person case, inviting a duplicate (and divergent) FR-9 privacy predicate

**Severity: High (direct FR-9 risk — the highest-stakes invariant per the brief)**
**ADs:** AD-3

**Scenario:** Dev/Agent A implements FR-1 (single calendar view) by calling `AppointmentViewService` per-appointment or per-owner, exactly as AD-3 describes ("Jeder Lesezugriff auf einen fremden Termin läuft durch genau einen Application-Service"). Dev/Agent B implements FR-2, the multi-person view — by construction this endpoint must fetch N people × a date range in one request. Faced with the obvious N+1-call performance problem of invoking a single-appointment-shaped service N×M times, Dev/Agent B writes one bulk SQL projection (join `Appointment`+`Attendee`+`Person`, apply the ownership/attendance predicate inline in the query) so the endpoint is a single round trip. Nothing in AD-3's rule speaks to bulk reads, and the rule's literal prohibition is on "Repository- oder Controller-Code" reading raw data "ohne durch diesen Service zu gehen" — B can argue the bulk query *is* the privacy-aware path, just not literally a call to the named class.

**The incompatibility:** The privacy predicate now exists in two independently-authored places — the C# `AppointmentViewService` and B's hand-rolled SQL — with no shared implementation. Any edge case the two get right differently (e.g., how the "Teilnehmer-Ausnahme" attendee-match is joined for internal vs. external attendees, inclusive/exclusive date-range boundaries, handling of a currently-active `StatusOverride`) becomes a silent FR-9 leak or a silent over-redaction in exactly one of the two views, and nothing in code review would flag it as a violation of AD-3 because B never touched a Repository or Controller directly reading "for a viewer" in the sense AD-3's prose seems to have had in mind (single appointment, single viewer).

**Fix:** AD-3 should either (a) mandate that `AppointmentViewService` expose a set-oriented method (e.g., `GetVisibleAppointments(viewerId, personIds[], range)`) that is the *only* privacy-aware entry point, explicitly covering FR-2's bulk case, or (b) explicitly forbid re-deriving the ownership/attendance predicate in any SQL/view outside Application, even for performance.

---

## Finding 5 — Recurring events are already live via FR-5/6 sync today; Deferred only covers the FR-14 *editing* question, not the *ingestion representation* the two provider adapters must decide now

**Severity: High (not actually deferred — will surface on day one against real calendars)**
**ADs:** AD-2, AD-7, AD-4/AD-5; Deferred note for FR-14 covers a different question

**Scenario:** Real Google and Outlook accounts used for FR-5/6 sync will contain recurring meetings today, independent of whether the in-app "create a recurring appointment" feature (FR-14) exists yet. Dev/Agent A builds `GoogleCalendarProvider` and — following normal Google Calendar API practice — expands recurring series into individual instances (`singleEvents=true`), yielding one `ProviderEventId` and one `Appointment` row per occurrence, each with a concrete start/end (which AD-5's duration-based heuristic needs to function at all). Dev/Agent B builds `OutlookCalendarProvider` and, working from Microsoft Graph's series-master model, ingests the recurring series as a single event with a `seriesMasterId` (one `Appointment` row for the whole series, no single concrete duration) unless specifically told to expand via `calendarView`. Both satisfy AD-7's idempotency-key rule literally (each still has a stable `(PersonId, Provider, ProviderEventId)`).

**The incompatibility:** A Google-synced person's weekly recurring meeting becomes N separate, correctly-classified Appointment rows; an Outlook-synced person's identical weekly meeting becomes a single row with an ambiguous/unusable duration for AD-5's heuristic and no per-occurrence times for the FR-2 grid. Status heuristic results and multi-person rendering diverge by provider for the same real-world meeting shape, and this happens now, not when FR-14 is eventually built — the Deferred entry for FR-14 explicitly scopes itself to "Bearbeitungs-/Löschgranularität ... und Interaktion mit Status-Override," which is a UI/edit-semantics question, not this ingestion-cardinality question.

**Fix:** Add an AD (or extend AD-2/AD-7) requiring both adapters to expand recurring events into per-occurrence rows with concrete start/end before they ever reach the Worker's upsert step, so `ICalendarProvider.FetchEvents` has one normalized contract regardless of provider.

---

## Finding 6 — AD-6 has no "single service" mandate for computing a person's *current* status, and doesn't define the tie-break when multiple appointments are simultaneously active

**Severity: Medium**
**ADs:** AD-6 (compare to AD-3/AD-4, which do centralize their respective concerns)

**Scenario:** AD-4 centralizes status *computation* in one Domain service, and AD-3 centralizes privacy-aware *reads* in one Application service — but AD-6's "current status of a person" composition (override, else active-appointment's precomputed status, else default) is stated only as prose, assigned to no single service. Dev/Agent A builds the FR-2 multi-person view's live status indicator and implements the override-then-lookup composition inline. Dev/Agent B later builds any second feature that needs "is this person currently interruptible" (e.g., a notification-suppression check for FR-13 when it's eventually built, or an admin dashboard) and reimplements the same composition independently. AD-6's rule also literally says "der ... Status **des** gerade aktiven Termins" (singular) — it has no answer for a double-booked person (two overlapping synced/native appointments both active "now," entirely plausible with independent Google+Outlook connections or a native appointment overlapping a synced one).

**The incompatibility:** Two independently-written "current status" computations can pick different appointments to defer to when more than one is active (e.g., A picks "most restrictive wins," B picks "first row returned by an unordered query," which is non-deterministic run to run) — producing different displayed statuses for the same person at the same instant in different parts of the UI, with no AD violation either implementer could point to.

**Fix:** Extend AD-6 (or add a new AD) to (a) name a single service responsible for computing "current status of a person," and (b) define the overlap tie-break explicitly (e.g., most-restrictive-wins).

---

## Finding 7 — Token-refresh write path for `CalendarConnection` has no assigned owner or concurrency guard

**Severity: Medium**
**ADs:** AD-2, AD-8 (encryption-at-rest is specified; the write path that produces the ciphertext in the first place is not)

**Scenario:** AD-2's interface is illustrated only with `FetchEvents(connection, since)`; nothing states who persists a refreshed access/refresh token pair when the current one has expired. Dev/Agent A, writing `GoogleCalendarProvider`, has the adapter silently refresh-and-persist the token itself inside Infrastructure (consistent with AD-8's "Entschlüsselung ausschließlich innerhalb von Infrastructure, unmittelbar vor einem Provider-API-Aufruf" — refresh-and-reencrypt feels like the same category of operation). Dev/Agent B, writing `OutlookCalendarProvider`, instead returns the refreshed token pair up to the Application-layer caller to persist via a separate repository call, assuming that's "the pattern." Both are defensible readings of an interface that was only ever shown one method signature.

**The incompatibility:** Beyond the inconsistency itself (one provider's tokens get persisted, the other's don't, unless the Application-layer caller happens to also handle it — a real risk of B's refreshed token being used once and then discarded, forcing re-auth every cycle), there's a live race: a user hitting "reconnect" in Settings (API-driven OAuth flow) at the same moment the Worker's background poll independently detects an expired token and refreshes it can produce a lost update on the same `CalendarConnection` row — whichever write lands second wins, potentially overwriting a just-established valid token with a stale one.

**Fix:** Assign token-refresh persistence to one specific layer/interface method in AD-2 or AD-8, and require optimistic concurrency (e.g., a row version) on `CalendarConnection` writes.

---

## Finding 8 — AD-12's "stoppt den Sync" is not given a transactional guard against an in-flight Worker cycle

**Severity: Medium**
**ADs:** AD-12, AD-11

**Scenario:** Admin deactivates Person P mid-way through a Worker poll cycle that already fetched P's events before `Person.IsActive` flipped to `false`. AD-12's rule states deactivation "stoppt den Sync für dieses Konto," phrased as an immediate effect, but does not require the Worker's upsert step to re-check `IsActive` transactionally at write time (as opposed to only at the start of a cycle/connection-selection query). A reasonable implementer builds the "which connections to poll" query filtered by `IsActive`, but the actual row-level upsert loop, once started, doesn't repeat that check per appointment write.

**The incompatibility:** Appointment rows for a just-deactivated person can still be written after deactivation, briefly (or, depending on batch size/poll duration, not-so-briefly) contradicting AD-12's "sync stopped" guarantee, and reintroducing data for a person meant to be immediately excluded from FR-2's selection — a small but real privacy/consistency gap right at the moment access is supposed to be revoked.

**Fix:** AD-12 should require the upsert step itself (not just the poll-scheduling query) to check `IsActive` immediately before each write, inside the same transaction, or accept and document this as a bounded/acceptable race with a maximum staleness window.

---

## Minor / not scored separately

- **Attendee FK cascade on AD-7's "missing key → delete Appointment" path** is unspecified. If the Attendee schema uses a default (restrictive) foreign key and the Worker's delete-on-missing logic issues a plain `DELETE`, the first cancelled synced appointment that has attendee rows will throw a FK violation in production. Worth a one-line clarification (cascade delete `Attendee` with its parent `Appointment`) rather than a full AD.
- **StatusOverride survival across AD-12 deactivation/reactivation** is unaddressed — AD-12's reset list (login, tokens, sync, FR-2 selection) doesn't mention `StatusOverride`, so a stale manual override (which by AD-6 design never expires) could resurface unexpectedly if a person is reactivated later. Low likelihood, worth a Deferred note rather than a blocking finding.
