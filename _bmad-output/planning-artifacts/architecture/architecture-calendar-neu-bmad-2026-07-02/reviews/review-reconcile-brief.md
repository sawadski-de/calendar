---
title: Reconciliation Review — Architecture Spine vs. Brief/Addendum
subject: architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md
against:
  - briefs/brief-calendar-neu-bmad-2026-07-02/brief.md
  - briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md
created: 2026-07-02
verdict: clean
---

# Reconciliation Review: Architecture Spine vs. Product Brief + Addendum

## Scope of this check

Per instructions, this is narrower than a full architecture review: it checks only whether
ARCHITECTURE-SPINE.md contradicts one of the following foundational, still-binding decisions
from the brief/addendum (as opposed to detail the PRD/UX have already legitimately superseded):

1. The OAuth full-Calendar-API-scope technical constraint (addendum).
2. The chosen resolution to the Privat-Default vs. live-context conflict, and the two
   discarded alternatives (addendum).
3. The one-way-sync-only principle (brief, Scope → Won't Have, and Solution section).
4. The self-hosted / bus-factor-1 operating reality (brief, "Bekannte Risiken").

## Findings

### 1. OAuth full-scope constraint — consistent, correctly operationalized

Addendum: "muss die App bei der OAuth-Registrierung vollen Lese-Zugriff auf die Kalender-API
beantragen (volle Event-Objekte inkl. Titel, Teilnehmer, Dauer) — nicht nur auf die
Free/Busy-API. Der 'Privat-Default' ist damit bewusst eine Anzeige-Regel der Anwendung
gegenüber anderen Nutzern, keine technische Zugriffsbeschränkung auf Datenebene."

The spine implements exactly this model:
- AD-5's heuristic inputs (duration, all-day flag, attendee count — ARCHITECTURE-SPINE.md
  line 76) require full event objects, not free/busy stubs, for *synchronized* appointments
  too — matching the addendum's rationale for requesting full scope.
- AD-3 (lines 60–65) explicitly locates the privacy boundary at the read/display layer
  (`AppointmentViewService`), not at ingestion/storage: "Rein clientseitige Filterung erfüllt
  diese Regel nicht" — and by construction this implies server-side storage of full raw data
  (title, attendees), consistent with "keine technische Zugriffsbeschränkung auf Datenebene."
- No interface or convention in the spine narrows the provider fetch to a free/busy-only
  call for the main sync path; `ICalendarProvider.FetchEvents` (AD-2, line 58) is the only
  sync entry point and is used to feed the full heuristic.

No contradiction found. (Minor observation, not a contradiction: the spine never states the
OAuth scope requirement itself as an explicit AD/convention — it's implied rather than
codified. Since the PRD/addendum already carry this constraint forward as binding and nothing
in the spine narrows it, this is a non-issue for this reconciliation, not a gap to flag.)

### 2. Privat-Default resolution — chosen option implemented, neither discarded alternative reintroduced

Addendum lists three options and records option 3 as chosen: heuristic-based context
derivation for imported events too, "weil das Backend ohnehin vollen Zugriff auf die
Kalender-Metadaten hat." Discarded:
- Alt. 1 — context derivation only for natively created events.
- Alt. 2 — manual context tag instead of automatic derivation.

Spine cross-check:
- AD-4 (lines 66–70) is explicit that `StatusHeuristicService.Compute` runs "bei jedem
  Schreibzugriff auf einen Termin ... — nativ (API, FR-3) wie synchronisiert (Worker-Upsert,
  FR-5/FR-6)". This directly implements option 3 and forecloses option 1 (native-only).
- AD-11 (lines 108–112) reinforces this by forbidding the Worker from reimplementing the
  heuristic differently from the API — i.e., no parallel, weaker native-vs-imported path can
  silently creep back in.
- AD-5 codifies concrete, non-manual thresholds (duration/attendee-count based), i.e.
  automatic derivation — not a per-event manual tag, so option 2 is not reintroduced.
- AD-6's `StatusOverride` (per-person override entity, FR-11) is a distinct, PRD-approved
  capability layered on top of the automatic heuristic (used only when explicitly set by the
  person themselves for their own current status), not a per-appointment manual context tag
  applied instead of automatic derivation for imported events. It does not reintroduce
  discarded alternative 2.

No contradiction found.

### 3. One-way-sync-only principle — preserved

Brief (Scope → Won't Have): "Zurückschreiben nach Outlook/Google (Sync bleibt einseitig)."

Spine: `ICalendarProvider` (AD-2) exposes only a fetch/read method (`FetchEvents(connection,
since)`); AD-7's idempotency rule and the Worker's role throughout the spine are read/import
only — deletion on missing keys (AD-7) is a local-DB operation, not a write-back to the
provider. No push/update-to-provider method or flow appears anywhere in the spine.

No contradiction found.

### 4. Self-hosted / bus-factor-1 operating reality — preserved and made explicit

Brief risk: "Betrieb ohne Resilienz-Konzept: Selbst gehostet ... ein Betreiber (Bus-Faktor 1)."

Spine: Structural Seed states directly, "Ein Docker-Compose-Stack, ein Host, keine
Hochverfügbarkeits-Topologie (bewusst, Bus-Faktor-1-Betrieb)" (line 175), and AD-14 accepts no
off-host backup target as a deliberate residual risk consistent with this operating model.

No contradiction found.

## Minor note (not a contradiction — informational only)

AD-14's Deferred/revisit note ("Revisit-Anlass: Wechsel zu einem Betriebsmodell mit mehr
Beteiligten oder gestiegenem Datenwert (vgl. Brief-Addendum)" — line 231) borrows phrasing
("Wechsel zu einem ... Modell mit mehr Beteiligten") from the addendum's Privat-Default
revisit trigger ("bei einem Wechsel zu einem Cloud-Hosting-Modell mit mehr Beteiligten"),
which in the addendum is specifically about revisiting the *trust-based Privat-Default
heuristic decision*, not about backup/host-redundancy. The addendum does not itself state a
revisit condition for backups. This is a sourcing/citation looseness in the spine's Deferred
section, not a substantive contradiction — it does not change any binding decision or
reintroduce anything discarded. Flagged for awareness only; no action required for this
reconciliation pass.

## Overall Verdict: Clean

The architecture spine does not contradict any foundational, still-binding decision from the
brief or addendum. It correctly operationalizes the OAuth full-scope constraint, implements
the chosen Privat-Default resolution without reintroducing either discarded alternative,
preserves one-way-sync-only, and explicitly carries forward the self-hosted/bus-factor-1
operating reality (going further than the brief by making it an architectural invariant,
AD-14, rather than leaving it as an unaddressed risk).
