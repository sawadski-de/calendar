---
title: Reconciliation Review — PRD vs. Architecture Spine
subject: architecture-calendar-neu-bmad-2026-07-02 / ARCHITECTURE-SPINE.md
against: prd-calendar-neu-bmad-2026-07-02 (prd.md + addendum.md)
created: 2026-07-02
---

# Reconciliation Review: PRD → Architecture Spine

## Overall Verdict: Real gaps

Most of the PRD's FR-1…FR-15, the Must-Have NFRs, and the explicit "architecture phase decides this" open questions are honestly and traceably resolved in the spine (AD-5 correctly closes Offene Frage #3, AD-14 correctly closes most of Offene Frage #4, AD-12 correctly closes Offene Frage #5, the mobile-breakpoint question is legitimately delegated to EXPERIENCE.md which does resolve it). However, one Must-Have Cross-Cutting NFR is dropped entirely, one open question is left completely unaddressed (not even flagged as deferred), one Non-Goal/FR has no protecting invariant, one explicitly-flagged "architecture must define this" behavior is left undefined, and one FR's exception clause has an unresolved scope ambiguity the spine silently picked a side on.

---

## Finding 1 (Severity: High) — "Sync-Transparenz" Must-Have NFR is completely absent from the spine

**PRD requirement:** Section 7, Cross-Cutting NFRs:
> **Sync-Transparenz (Must-Have):** Die Oberfläche zeigt erkennbar an, wann ein Kalender zuletzt erfolgreich synchronisiert wurde. Bleibt ein Sync-Zyklus für ein Konto wiederholt aus (z. B. wegen abgelaufenem/widerrufenem Token), muss das für den Betreiber sichtbar werden — ein stiller, unbemerkter Sync-Ausfall ist bei Bus-Faktor-1-Betrieb sonst nicht erkennbar.

This is one of only five Cross-Cutting NFRs and is explicitly marked Must-Have — on par with the backup NFR (which the spine does address via AD-14) and the access-control NFR (addressed via AD-8/AD-9/AD-10).

**Spine gap:** There is no AD, no entity field (the ERD lists `Person ||--o{ CalendarConnection`, `Person ||--o{ Appointment`, etc. with no attributes shown, and no prose anywhere else names a `LastSyncedAt`/`SyncStatus`/failure-count concept), no mention in the Capability→Architecture Map, and — critically — no entry in the **Deferred** section either. Every other PRD item the spine chooses not to fully resolve (FR-12…FR-15, timezone axis, external-attendee retention, heuristic recalibration, host redundancy) is explicitly listed under `## Deferred` with a rationale and revisit trigger. Sync-Transparenz has none of that: it isn't resolved, and it isn't acknowledged as unresolved. A builder reading only the spine would not know this Must-Have requirement exists.

This also silently drops **Offene Frage #6** (PRD §10): "Widerruft ein ausgewähltes Teammitglied seinen Kalenderzugriff (OAuth) ... Soll das Tool das für Betrachter erkennbar machen ... oder reicht die allgemeine Sync-Transparenz-Anforderung?" — a question that presupposes the Sync-Transparenz mechanism exists and asks the architecture to decide how it interacts with viewers. Since the underlying mechanism itself is missing, this open question is also nowhere in the spine (not resolved, not deferred).

**Where it should live:** Likely a new AD near AD-2/AD-7 (Worker/sync invariants), e.g. a `CalendarConnection.LastSuccessfulSyncAt` / failure-streak field updated by the Worker, surfaced via an API endpoint the Angular UI reads — or at minimum a `## Deferred` entry naming this as an open architectural decision.

---

## Finding 2 (Severity: Medium-High) — Restore-Prozess half of Offene Frage #4 is left undefined

**PRD requirement:** Section 7 NFR "Datenhaltung nativer Termine (Must-Have)": "Konkrete Umsetzung (**Frequenz, Aufbewahrungsdauer, Restore-Prozess**) folgt in der Architekturphase." Offene Frage #4 (§10) repeats this explicitly: "Konkrete Backup-**Frequenz und Restore-Prozess** für native Termine ... sind noch nicht spezifiziert — Teil der Architekturphase."

**Spine gap:** AD-14 ("Backup ohne Host-Redundanz [ADOPTED]") specifies frequency (daily) and retention (14 days rolling) — two of the three asked-for items — but says nothing about the **restore process**: no restore script/runbook, no stated RTO, no mention of who/how a `pg_dump` file gets turned back into a running database, no mention of testing restores. AD-14's own "Prevents" bullet talks only about backup existing at all ("Kein Backup-Mechanismus existiert..."), not about restore capability being exercised. The PRD explicitly asked for both halves; only one half is closed out, and the gap isn't flagged (AD-14 is tagged `[ADOPTED]` as if fully resolved).

---

## Finding 3 (Severity: Medium) — FR-7 "Kein Zurückschreiben" has no explicit protecting invariant

**PRD requirement:** FR-7: "Das System schreibt zu keinem Zeitpunkt Termindaten zurück nach Outlook oder Google," reinforced as an explicit Non-Goal (§5: "Kein Zurückschreiben nach Outlook/Google — Sync bleibt dauerhaft einseitig").

**Spine gap:** The Capability→Architecture Map lists FR-7 as governed by "AD-2, AD-4, AD-7, AD-11," but none of those ADs actually state a rule preventing write-back. AD-2's Rule only describes `ICalendarProvider.FetchEvents(connection, since)` as the sole method the Worker knows about, and its "Prevents" bullet is about provider-specific logic leaking into sync orchestration — not about write operations. Unlike every other significant guardrail in the spine (e.g. AD-3's explicit "Rein clientseitige Filterung erfüllt diese Regel nicht," AD-7's explicit dedup rule), there is no "Prevents: ein Adapter/Endpoint bekommt eine Update/Push-Methode gegen Google/Microsoft" style statement. The absence of a write method on today's interface is a weak, incidental protection — nothing in the spine would flag it as an invariant violation if a future `ICalendarProvider.PushEvent(...)` were added for some other feature (e.g., a future two-way-sync experiment).

---

## Finding 4 (Severity: Medium) — Overlapping-appointments status tie-break, explicitly flagged for architecture, is not defined

**PRD requirement:** FR-10 "Out of Scope": "Nachträgliche Teilnehmer-Änderungen an einem bereits laufenden/eingestuften Termin sowie **sich überschneidende Termine mit unterschiedlichem Status lösen keine spezifizierte Neubewertung aus — Verhalten in diesen Fällen ist architekturseitig zu definieren**."

**Spine gap:** AD-6 defines "der aktuelle Status einer Person" as: "Override, falls gesetzt; sonst der vorausberechnete Status (AD-4) des gerade aktiven Termins, sonst unterbrechbar" — using the singular "des gerade aktiven Termins" (**the** currently active appointment). This implicitly assumes exactly one active appointment per person at any moment and never states what happens when two appointments overlap for the same person with different derived statuses (e.g., a 1:1 marked `unterbrechbar` overlapping a large meeting marked `bitte-nicht-stören`). This is precisely the scenario the PRD calls out by name as needing an architectural decision, and the spine does not decide it (nor list it in Deferred).

The related sub-case — recompute on later participant edits — is arguably covered implicitly by AD-4's "wird bei jedem Schreibzugriff ... aufgerufen" (recompute on every write), so only the overlap tie-break is a genuine open gap.

---

## Finding 5 (Severity: Low-Medium) — FR-9 attendee-exception scope (native-only vs. all appointments) is ambiguous in the PRD and the spine silently picks one reading without flagging it

**PRD tension:** FR-9's main clause restricts the attendee exception to native appointments: "Ist das anfragende Teammitglied selbst als Teilnehmer in einem **fremden nativen Termin** eingetragen, sieht es dessen volle Details." But FR-9's own "Consequences" bullet generalizes the whole rule (default-hide + exception) to apply "unabhängig davon, ob der Termin nativ oder synchronisiert ist" — i.e., seemingly including synced (imported Outlook/Google) appointments too.

**Spine gap:** AD-3's Rule adopts the broader reading without comment: "der anhand von Eigentümerschaft/**Teilnahme** entscheidet" (participation generally, no native/synced qualifier), and the Capability→Architecture Map cites AD-3 for both FR-8/FR-9 without noting a resolved ambiguity. Elsewhere the spine is careful to flag when it is making an explicit interpretive call ("[ADOPTED]" tags on AD-5/AD-6/AD-12/AD-14, each naming which PRD open question it resolves). Here, a real textual inconsistency in the source PRD is resolved silently and without a stated rationale — worth at minimum a one-line note, since it affects whether an attendee of a synced (possibly customer-containing) meeting gets to see full imported details, which interacts with the already-accepted external-attendee-data risk (§8/Offene Frage #8).

---

## Items checked and found consistent (no gap)

- FR-1…FR-6, FR-8, FR-11: correctly bound to AD-3/AD-4/AD-6/AD-7 with no weakening found.
- FR-10 heuristic thresholds: AD-5 correctly and completely closes Offene Frage #3, including the no-participant-always-interruptible precedence rule.
- FR-12…FR-15 (Should-Haves): correctly left unbuilt and listed in Deferred, consistent with PRD's explicit "bei Umsetzung festlegen" language.
- Backup existence/frequency/retention (Offene Frage #4, partial): AD-14 correctly resolves the "should we back up at all, how often, how long" part (see Finding 2 for the unresolved restore-process part).
- Deprovisioning (Offene Frage #5): AD-12 answers all four sub-questions PRD raised (native appointments retained, participations retained, tokens revoked, removed from person-selector), just without an explicit "[löst Offene Frage #5]" annotation like AD-5/AD-14 use — cosmetic only, not a content gap.
- Mobile breakpoint behavior for Mehrpersonen-Ansicht (§7 NFR, "architekturseitig zu lösen"): spine's Deferred entry claims this was already resolved in EXPERIENCE.md — verified true (EXPERIENCE.md line 85: "No hard cap on selections ... the chip row and columns scroll horizontally instead"), so this is a legitimate resolution, not a drop.
- Timezone axis, external-attendee data retention, host redundancy: all correctly carried into Deferred with revisit triggers matching the PRD's own framing.
- Access control / token encryption (Zugriffsschutz NFR): fully covered by AD-8/AD-9/AD-10.
