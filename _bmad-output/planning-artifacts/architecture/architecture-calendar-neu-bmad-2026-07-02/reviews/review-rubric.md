# Architecture Spine Quality Review — Team-Terminkalender mit Synchronisation

Reviewed: `ARCHITECTURE-SPINE.md` (status: final, 2026-07-02) against the BMad good-spine checklist.

## Overall verdict

The spine is well-executed for the domain logic it does cover — the fourteen ADs are crisp, mostly enforceable, and three of them (AD-5, AD-6, AD-14) visibly resolve PRD open questions with concrete numbers rather than punting. The critical problem is coverage, not quality: the spine's own bound sources (EXPERIENCE.md, DESIGN.md) specify a Must-Have Sync-Transparency surface and a real, permission-checked Admin/Member role system as load-bearing functionality, and the spine's data model, ADs, and Capability Map are completely silent on both — no sync-status field, no role field, no authorization-enforcement rule, no mention of the Admin → Sync overview surface anywhere. That is exactly the kind of whole-dimension silence the checklist calls out as a finding, and it is large enough that two independently-built units (e.g., the Settings-connections page and the Admin overview page) would almost certainly invent incompatible sync-status models.

## Findings

### Critical — Sync-Transparency (Must-Have NFR) and the Admin/Member role model have no architectural backing

PRD §7 names "Sync-Transparenz" as a **Must-Have** NFR: the UI must show when each calendar last synced successfully, and a repeated/silent sync failure must become visible to the operator. The spine's own bound UX sources make this concrete and non-optional:

- `EXPERIENCE.md` line 24: "a real role system — Admin vs. Member — with actual permission checks, not just an unguarded extra page"
- `EXPERIENCE.md` line 40 / `DESIGN.md` line 248: a dedicated **Admin → Sync overview** surface, role-gated, showing person/provider/status/last-sync for every account
- `EXPERIENCE.md` line 84: "Sync failure (repeated/silent) ... Must be visible, not silent (Sync-Transparenz NFR — this is a Must-Have, not a nice-to-have...)"

None of this appears in `ARCHITECTURE-SPINE.md`:

- The ERD has no sync-status/timestamp/error field on `CalendarConnection` (compare: AD-7 explicitly names `ProviderEventId`, AD-12 explicitly names `Person.IsActive` in prose even though the ERD diagram doesn't show attributes — so the document's own convention is to call out load-bearing fields in AD text, and it does so everywhere except here).
- AD-9 mentions "Rollenmodell (Admin/Member)" only as a `Binds` label and states that "Accounts werden ausschließlich von einem Admin angelegt" — it never establishes a `Person.Role` field, an authorization mechanism (e.g., ASP.NET Core Identity Roles/claims/policies), or a rule preventing a non-Admin from reaching the Admin surface. There is no AD equivalent to AD-3 ("no read path bypasses the view service") for "no route/controller serves the Admin surface without a role check."
- The Capability → Architecture Map has no row for the Admin → Sync overview surface, and the Source Tree has no corresponding endpoint/module.
- The Deferred section doesn't list this as an intentionally-punted item either — it's simply absent, not decided and not deferred.

**Why this matters:** this is precisely the divergence the spine exists to prevent. Without a named field/entity for sync status and a stated enforcement rule for the Admin role, whoever builds the Worker's sync-failure detection, the Settings→Connections per-provider indicator, and the Admin overview page will each have to invent (a) what "failed" means and how it's stored, and (b) how the role check is wired — with no shared vocabulary to converge on. Recommend adding an AD in the shape of the existing ones, e.g.: "`CalendarConnection` carries `LastSyncedAt`/`LastSyncStatus`/`LastError` fields, written by the Worker after every sync attempt; `Person.Role` (`Admin`|`Member`) is enforced via ASP.NET Core Identity role-based authorization on all Admin-surface endpoints; no other mechanism (client-side hiding, nav-item omission alone) satisfies the role gate."

### High — AD-14 answers "backup" but not "restore," even though the PRD open question it resolves asked for both

PRD Offene Frage #4 (which AD-14 is explicitly tagged `[ADOPTED]` for resolving) reads: "Konkrete Backup-**Frequenz und Restore-Prozess** für native Termine ... sind noch nicht spezifiziert — Teil der Architekturphase." AD-14's Rule specifies frequency, mechanism, and retention (daily `pg_dump` cron sidecar, 14-day rolling retention on a Docker volume) but says nothing about the restore side — no statement of who runs a restore, how (`pg_restore` command, target, downtime expectation), or that it is a deliberately manual/undocumented-for-now procedure. The underlying NFR (PRD §7) explicitly requires "Backup-/**Wiederherstellungs**fähigkeit," not backup alone. As written, the spine only half-resolves the open question it claims to close. A one-sentence addition (e.g., "Restore ist ein manueller `pg_restore` durch den Betreiber gegen das gesicherte Dump-Volume; keine automatisierte Restore-Pipeline in diesem Betriebsmodell") would close this cleanly and is consistent with the Bus-Faktor-1 posture already established elsewhere.

### Medium — AD-3's attendee exception is broader than the FR-9 rule it's supposed to encode

PRD FR-9 scopes the "you see full details if you're an attendee" exception explicitly to **native** appointments: "Ist das anfragende Teammitglied selbst als Teilnehmer in einem fremden **nativen** Termin eingetragen, sieht es dessen volle Details." AD-3's Rule generalizes this to any appointment via "Eigentümerschaft/**Teilnahme**" without restricting it to native ones: "Jeder Lesezugriff auf einen fremden Termin läuft durch ... `AppointmentViewService`, der anhand von Eigentümerschaft/Teilnahme entscheidet." Read literally, AD-3 would also unlock full details for a teammate merely listed as an attendee on a *synchronized* appointment they don't own — which FR-9's own text does not license (FR-4's back-reference to "siehe FR-9, Ausnahme" doesn't resolve this either, since FR-4 itself only says "fremden Terminen" generically). This may be intentional (per-connection sync could make the native/synced distinction moot in practice, since each attendee who has their own connected calendar would typically get their own owned copy of a shared external meeting) — but the spine should say so explicitly rather than leave the AD's wording looser than the FR it's meant to pin down. As written, this is exactly the kind of ambiguity that lets two implementers of `AppointmentViewService` diverge on a privacy-sensitive rule.

### Low — Stack table mixes pinned major versions with "aktuelle stabile Version" placeholders

.NET/ASP.NET Core/EF Core/Identity are pinned to major version 10; Angular to 22; PostgreSQL to 18 — these read as plausible, deliberate choices, not fabricated numbers (not independently re-verified per instructions). But Docker/Docker Compose, Caddy, and Serilog are all given as "aktuelle stabile Version" with no version pin at all. That's a defensible choice for infra tooling that trails a Dockerfile/NuGet lockfile rather than an architectural decision, but the inconsistency (three "always-latest" entries next to five hard-pinned ones) is worth a one-line note explaining the difference in treatment, so it doesn't read as an oversight during a later doc pass.

### Low — No explicit statement on environments (dev/local vs. the single production stack)

The Structural Seed thoroughly covers the production topology (one Docker Compose stack, one host, Caddy + Angular + Api + Worker + Postgres + backup sidecar) and explicitly owns the "no HA, Bus-Faktor-1" trade-off. It says nothing about how a developer runs/tests the system locally before it reaches that host (e.g., docker-compose override file, local Postgres, disabling the backup sidecar). Given the single-operator, prototype nature of this project this is a minor omission rather than a load-bearing gap, but it is a structural dimension ("deployment & environments") that the checklist asks to confirm is at least addressed, and right now it's silent rather than decided or deferred.

## Checklist coverage notes (non-findings, confirmed clean)

- **AD mechanics:** AD-1 through AD-14, no duplicate IDs, every AD has Binds/Prevents/Rule populated. No leftover template placeholders (`TODO`, `TBD`, `{{...}}`, etc.) found anywhere in the document.
- **Mermaid diagrams:** all three (paradigm dependency graph, container/deployment diagram, ERD) are substantive and specific to this system, not trivial/fake stand-ins.
- **Capability → Architecture Map:** all fifteen FRs (FR-1…FR-15) are represented, either mapped to a concrete owner+AD or explicitly routed to Deferred for the four Should-Have FRs (FR-12–FR-15) — no gaps, no duplicates.
- **Deferred section:** every entry reviewed is a genuine Should-Have/non-MVP item or an explicitly-accepted risk carried over verbatim from the PRD, each with a stated revisit trigger. None of the *currently listed* deferred items look load-bearing for MVP-vs-MVP consistency (the load-bearing gap is the sync-status/role dimension that isn't listed as deferred at all — see Critical finding above).
- **Internal consistency (greenfield, no codebase to ratify against):** Consistency Conventions table (IDs, time/UTC, error format, naming, auth, logging, config) doesn't contradict any AD; entity names in the table match the ERD.
- **i18n-Foundation and Rollenmodell bindings:** though not named as such in the PRD, both trace cleanly to explicit, non-optional decisions in the bound UX sources (`EXPERIENCE.md`/`DESIGN.md`), so referencing them in AD-13/AD-9 `Binds` fields is legitimate, not invented.
- **PRD open questions resolved by this spine:** #3 (heuristic calibration → AD-5), #4 (backup, partially → AD-14, see High finding above), #5 (deprovisioning → AD-12), #7 (timezone → Deferred) are all explicitly and traceably addressed.
