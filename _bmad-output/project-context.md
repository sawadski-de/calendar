---
project_name: 'calendar-neu-bmad'
user_name: 'Dennis'
date: '2026-07-03'
sections_completed: ['technology_stack', 'language_rules', 'architecture_rules', 'frontend_ux_rules', 'testing_rules', 'critical_rules']
existing_patterns_found: 0
status: 'complete'
rule_count: 28
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

_Greenfield project — no starter template, no existing codebase at time of writing. Rules below are derived from the Architecture Spine (`_bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md`) and UX Spine (`DESIGN.md`/`EXPERIENCE.md`), not from discovered code._

---

## Technology Stack & Versions

- .NET 10 (LTS) — Backend (Api, Worker), C#
- ASP.NET Core Web API 10 + ASP.NET Core Identity 10 (Cookie-Auth)
- Entity Framework Core 10 — ORM gegen Postgres
- Angular 22 — Frontend SPA
- PostgreSQL 18
- Docker Compose v2 — Deployment (ein Host, ein Stack: Caddy, Angular, Api, Worker, Postgres, Backup-Sidecar)
- Caddy v2 — Reverse Proxy/TLS-Terminierung, Pfadrouting (`/` → Angular, `/api` → Api)
- Serilog v4 — strukturiertes Logging nach stdout in allen Prozessen

## Critical Implementation Rules

### Sprach-/Daten-Regeln (C#/Postgres)

- Alle IDs sind GUID (`uuid`) — keine sequentiellen Integer-IDs.
- Zeit wird ausschließlich in UTC gespeichert; Umrechnung in lokale Zeit geschieht NUR im Angular-Frontend, nie im Backend.
- Entity-Naming: PascalCase in C# (`Person`, `CalendarConnection`, `Appointment`, `Attendee`, `StatusOverride`), snake_case in Postgres-Spalten (EF-Core-Default-Mapping) — nicht manuell überschreiben.
- API-Fehlerantworten sind ausschließlich RFC-7807 `application/problem+json` mit stabilem `code`-Feld — niemals lokalisierter Freitext vom Backend. Übersetzung geschieht ausschließlich im Angular-Frontend.
- Secrets (DB-Connection-String, Token-Verschlüsselungsschlüssel, OAuth-Client-Secrets) ausschließlich über Umgebungsvariablen — niemals im Repository oder in `appsettings.json` eingecheckt.

### Architektur-Regeln (Layered/Clean Architecture, AD-1 bis AD-17)

- Abhängigkeitsrichtung ist strikt einwärts: Domain kennt nichts; Application kennt nur Domain; Infrastructure implementiert Application-/Domain-Interfaces; Api und Worker sind reine Composition-Roots und referenzieren beide Application + Infrastructure, nie umgekehrt.
- `ICalendarProvider` (Application-Interface) hat genau eine Methode `FetchAllEvents(connection, window)`, die einen VOLLSTÄNDIGEN Snapshot liefert (kein Delta/Incremental-Sync) — niemals eine Schreibmethode hinzufügen (das würde FR-7 strukturell unterlaufen).
- Jeder Lesezugriff auf einen fremden Termin MUSS durch `AppointmentViewService` laufen (inkl. der Bulk-Methode `GetForViewers` für Mehrpersonen-Ansichten) — kein Repository/Controller darf Termin-Rohdaten für einen Nicht-Eigentümer ohne diesen Service ausliefern. Rein clientseitige Filterung erfüllt die Privatsphäre-Anforderung NICHT.
- `StatusHeuristicService.Compute` (Domain) ist die einzige Implementierung der Status-Heuristik; das Ergebnis wird bei jedem Schreibzugriff vorausberechnet und gespeichert — niemals bei jedem Request neu berechnen.
- `StatusOverride` ist eine eigenständige, personenbezogene Entität (nicht am Termin) ohne Ablaufzeitpunkt; `CurrentStatusService.GetCurrentStatus` ist die einzige Quelle für "aktueller Status einer Person" (inkl. Tie-Break: strengerer Status gewinnt bei Überlappung).
- Worker und Api referenzieren dieselben Domain-/Application-/Infrastructure-Bibliotheken; sie koordinieren AUSSCHLIESSLICH über die gemeinsame Postgres-Datenbank — kein direkter Prozess-zu-Prozess-Aufruf (kein RPC).
- OAuth-Access-/Refresh-Tokens werden nie im Klartext persistiert; Entschlüsselung findet ausschließlich in Infrastructure unmittelbar vor einem Provider-API-Aufruf statt. Token-Refresh-Schreibzugriff gehört ausschließlich dem Worker.
- Partieller Unique-Index `(PersonId, Provider, ProviderEventId)` gilt NUR `WHERE ProviderEventId IS NOT NULL`; native Termine haben beide Felder zwingend `NULL`, niemals einen Platzhalterwert.
- Deaktivierung eines Teammitglieds ist IMMER Soft-Delete (`IsActive = false`) — niemals Hard-Delete von Terminen, Teilnahmen oder Overrides.
- Mindestens ein aktiver Admin-Account muss immer existieren — Herabstufung/Deaktivierung des letzten Admins muss serverseitig verweigert werden.

### Frontend-/UX-Regeln (Angular, aus DESIGN.md/EXPERIENCE.md)

- Auth läuft über HttpOnly/Secure/SameSite=Lax-Cookie, same-origin via Caddy-Pfadrouting — niemals ein Access-/Bearer-Token im Client-JS speichern.
- Design-Tokens aus DESIGN.md sind verbindlich (Farben, Typografie, Spacing, Radius) — dark-native ist das Standard-Token-Set, `-light`-Suffix für Light-Mode, nicht umgekehrt annehmen.
- Status (Unterbrechbar/Bitte-nicht-stören) darf NIE allein über Farbe kommuniziert werden — immer Icon-Glyph + Textlabel kombinieren; Status-Text niemals truncaten/ellipsizen.
- Automatisch abgeleiteter und manuell übersteuerter Status sehen für andere Betrachter IDENTISCH aus — kein "Auto"/"Manuell"-Kennzeichen einbauen.
- WCAG 2.1 AA ist verbindliches Ziel: Fokus-Trap + Fokus-Restore für alle Flyouts/Popovers/Dropdowns, sichtbare Fokusringe, `aria-live="polite"` für asynchrone Sync-Fehler-Übergänge, `prefers-reduced-motion` respektieren.
- Echtes i18n (Deutsch kanonisch + Englisch vollwertig) ist Pflicht ab der ersten Zeile UI-Text — keine hartkodierten Strings; Komponenten mit übersetztem Text nutzen min-/max-width statt fixer Pixel-Breite.

### Testing Rules

- Backend: xUnit für Unit-/Integrationstests; Domain-Services (`StatusHeuristicService`, `CurrentStatusService`) müssen isoliert unit-testbar sein ohne EF-Core/DB-Abhängigkeit (Domain hat laut AD-1 keine Abhängigkeiten).
- Integrationstests gegen eine echte Postgres-Instanz (z. B. Testcontainers) für `AppointmentViewService`/Privat-Default-Regeln und den partiellen Unique-Index (AD-7) — kein DB-Mocking für sicherheitskritische Filterregeln.
- Frontend: Angular-Standard-Setup (Jasmine/Karma) für Komponenten-Unit-Tests.
- Privat-Default (FR-9) muss auf API-Ebene getestet werden, nicht nur im UI — ein Test, der nur prüft, dass das Frontend etwas ausblendet, erfüllt die Anforderung nicht.

### Kritische Don't-Miss-Regeln

- Niemals einen Schreibpfad gegen die Google-/Microsoft-Graph-API implementieren, auch nicht testweise — FR-7 ist absolut, strukturell durch `ICalendarProvider` erzwungen.
- Niemals die Privat-Default-Filterung im Frontend "zur Vereinfachung" nachbauen — sie muss serverseitig in `AppointmentViewService` erfolgen; ein Leak echter Titel/Teilnehmer an unautorisierte Betrachter ist ein Sicherheitsvorfall, kein Bug.
- Niemals zwei unabhängige Implementierungen der Status-Heuristik oder des Tie-Breaks bei überlappenden Terminen entstehen lassen — beides läuft ausschließlich über die in AD-4/AD-5/AD-6 benannten Services.
- Niemals synchronisierte (nicht-native) Termine im Tool bearbeitbar machen — sie sind rein lesend; jede Änderung erfolgt in der Quelle (Outlook/Google).

**Development Workflow Rules:** bewusst ausgelassen — Solo-Betrieb (Dennis, Bus-Faktor 1) ohne Team-Review-Prozess; keine erzwungenen Branch-/Commit-Konventionen nötig.

---

## Usage Guidelines

**Für KI-Agenten:**

- Diese Datei vor jeder Implementierung lesen.
- ALLE Regeln exakt befolgen wie dokumentiert.
- Im Zweifel die restriktivere Option wählen (insbesondere bei Privatsphäre-/Sicherheitsregeln).
- Diese Datei aktualisieren, wenn neue Muster entstehen.

**Für Menschen:**

- Diese Datei schlank und agenten-fokussiert halten.
- Bei Änderungen am Technologie-Stack aktualisieren.
- Regelmäßig überprüfen und veraltete Regeln entfernen, sobald sie selbstverständlich geworden sind.

Last Updated: 2026-07-03
