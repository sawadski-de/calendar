---
stepsCompleted: ["step-01", "step-02", "step-03", "step-04"]
inputDocuments:
  - "_bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md"
  - "_bmad-output/planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/addendum.md"
  - "_bmad-output/planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md"
  - "_bmad-output/planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md"
  - "_bmad-output/planning-artifacts/briefs/brief-calendar-neu-bmad-2026-07-02/brief.md"
  - "_bmad-output/planning-artifacts/briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md"
---

# calendar-neu-bmad - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for calendar-neu-bmad (Team-Terminkalender mit Synchronisation), decomposing the requirements from the PRD, UX Design Spine, and Architecture Spine into implementable stories.

## Requirements Inventory

### Functional Requirements

**Must-Have (MVP-Scope, PRD §6.1)**

- **FR-1: Eigene Kalenderansicht** — Ein Teammitglied kann seinen eigenen Kalender in Monats-, Wochen- und Tagesansicht anzeigen. Alle drei Ansichten zeigen native und synchronisierte Termine; Wechsel zwischen Ansichten ohne Neuladen.
- **FR-2: Mehrpersonen-Ansicht** — Ein Teammitglied wählt eine Teilmenge der übrigen Teammitglieder explizit aus und sieht deren Kalender nebeneinander in Spalten (Tages- und Wochenansicht). Fremde Termine zeigen ausschließlich Privat-Default (FR-9). Auswahl bleibt bei Wechsel zwischen Tages-/Wochenansicht erhalten. Out of Scope: überlagerte Darstellung mehrerer Kalender in einer Spalte.
- **FR-3: Termin anlegen** — Ein Teammitglied kann einen neuen nativen Termin mit Titel, Datum/Uhrzeit, Dauer und optionalen Teilnehmern anlegen. Erscheint sofort in eigener Ansicht; erhält automatisch einen Verfügbarkeits-Status nach FR-10.
- **FR-4: Termin-Detailansicht** — Klick auf eigenen Termin zeigt volle Details (Titel, Zeit, Teilnehmer) unabhängig vom Privat-Default. Klick auf fremden Termin zeigt nur Privat-Default — außer der Anfragende ist selbst als Teilnehmer eingetragen (dann volle Details). Out of Scope: Ortsanzeige vor FR-15.
- **FR-5: Outlook-Import** — Regelmäßiger Import aller Kalendertermine eines verbundenen Outlook-Kontos (volle Termindaten intern). Dedup/Update über stabile, providerseitige Event-ID. Löschung/Absage an der Quelle entfernt den Eintrag spätestens im nächsten Sync-Zyklus. Out of Scope: paralleles Doppelkonto pro Person; Ausnahmen in importierten Serien.
- **FR-6: Google-Calendar-Import** — Analog zu FR-5 für ein verbundenes Google-Konto (gleiche Frequenz, gleicher Datenumfang, gleiches Dedup-/Lösch-Verhalten).
- **FR-7: Kein Zurückschreiben** — Das System schreibt zu keinem Zeitpunkt Termindaten zurück nach Outlook oder Google.
- **FR-8: Interner Vollzugriff** — Das System liest und speichert für jeden Termin (nativ wie synchronisiert) die vollen Daten: Titel, Teilnehmer, Dauer, Ort.
- **FR-9: Privat-Default gegenüber anderen** — Für jeden fremden Termin zeigt das System anderen Teammitgliedern ausschließlich "privat/beschäftigt" plus Verfügbarkeits-Status — nie Titel, Teilnehmer oder Ort. Ausnahme: ist der Anfragende selbst als Teilnehmer eingetragen, sieht er volle Details. Serverseitige Durchsetzung ist zwingend — rein clientseitige Ausblendung erfüllt die Anforderung nicht.
- **FR-10: Automatische Status-Ableitung** — Für jeden Termin (nativ wie synchronisiert) wird automatisch ein Verfügbarkeits-Status aus Dauer, Teilnehmerzahl und (nachrangig) Tageszeit abgeleitet. Kalibrierte Regeln (Architecture AD-5): kein Teilnehmer → immer `Unterbrechbar`; mit ≥1 Teilnehmer und Dauer ≤45 Min → `Unterbrechbar`; ganztägig ODER (Dauer ≥90 Min UND ≥3 Teilnehmer) → `BitteNichtStoeren`; alle übrigen Fälle → `Unterbrechbar`; kein aktiver Termin zum aktuellen Zeitpunkt → `Unterbrechbar` (Default).
- **FR-11: Manueller Status-Override** — Ein Teammitglied kann seinen eigenen, aktuell angezeigten Verfügbarkeits-Status jederzeit manuell übersteuern. Bleibt bestehen bis aktiv geändert (kein automatisches Überschreiben durch Sync/Heuristik, kein automatisches Ende mit dem ursprünglich aktiven Termin). Kein visueller Unterschied zwischen automatisch abgeleitetem und manuell gesetztem Status.

**Should-Have (PRD §6.2, nicht MVP)**

- **FR-12: Gemeinsamer Slot-Finder** — Mehrere Teammitglieder auswählen; System schlägt automatisch gemeinsame freie Zeitfenster vor (reines Frei/Busy, ignoriert den abgeleiteten Verfügbarkeits-Status).
- **FR-13: Termin-Erinnerungen** — Erinnerung an einen bevorstehenden eigenen Termin (nativ oder synchronisiert), nie für fremde Termine.
- **FR-14: Serientermine** — Wiederkehrende native Termine (täglich/wöchentlich/monatlich) anlegen; jede erzeugte Instanz erhält einzeln einen Status nach FR-10.
- **FR-15: Ortsangabe mit Kartenanzeige** — Ort mit Autovervollständigung für native Termine; Kartendarstellung in der Detailansicht; unterliegt demselben Privat-Default wie andere Termindetails.

### NonFunctional Requirements

- **NFR-1: Sync-Aktualität** — Alle Sync-Zyklen (Outlook, Google, Status-Ableitung) laufen im Bereich weniger Minuten. Kein Echtzeit-Anspruch.
- **NFR-2: Sync-Transparenz (Must-Have)** — Die Oberfläche zeigt erkennbar an, wann ein Kalender zuletzt erfolgreich synchronisiert wurde. Ein wiederholt ausbleibender Sync-Zyklus für ein Konto muss für den Betreiber sichtbar werden — kein stiller Sync-Ausfall.
- **NFR-3: Datenhaltung nativer Termine (Must-Have)** — Native Termine existieren ausschließlich in der Tool-Datenbank und sind bei Datenverlust unwiederbringlich. Das System muss eine grundlegende Backup-/Wiederherstellungsfähigkeit für native Termine bereitstellen.
- **NFR-4: Zugriffsschutz (Must-Have)** — Zugriff auf das Tool erfordert eine Anmeldung. Gespeicherte OAuth-Tokens (Zugriff auf Outlook/Google) müssen verschlüsselt abgelegt werden — ein Datenbankzugriff allein darf keinen direkten Zugriff auf die verbundenen Kalenderkonten ermöglichen.
- **NFR-5: Mobil/Responsiv** — Alle Kernfunktionen (Ansichten, Termin anlegen, Verfügbarkeits-Status, Detailansicht) sind auf mobilen Endgeräten per Browser nutzbar.
- **NFR-6: Betrieb** — Selbst gehostet, ein Betreiber (Bus-Faktor 1) für Wartung und OAuth-Token-Pflege. Kein Anspruch auf Hochverfügbarkeit oder definierte Recovery-Zeiten über NFR-3 hinaus.

### Additional Requirements

_Aus dem Architecture Spine (architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md) — technische Vorgaben, die Epics/Stories und ihre Akzeptanzkriterien binden:_

- Kein Starter-Template — Architektur wird from-scratch nach Layered/Clean Architecture (Domain → Application → Infrastructure → API/Worker) mit Ports & Adapters für Kalender-Provider aufgebaut (AD-1, AD-2).
- `ICalendarProvider` definiert genau eine Methode `FetchAllEvents(connection, window)`, die einen vollständigen aktuellen Snapshot liefert (kein Delta/Incremental-Sync) und bewusst keine Schreibmethode besitzt — erzwingt FR-7 strukturell (AD-2).
- Jeder Lesezugriff auf einen fremden Termin läuft durch genau einen Application-Service (`AppointmentViewService`), inkl. einer Bulk-Methode für die Mehrpersonen-Ansicht — kein zweiter Codepfad dupliziert die Privat-Default-Regel (AD-3).
- `StatusHeuristicService.Compute(appointment)` (Domain) ist die einzige Implementierung der Status-Heuristik; Ergebnis wird bei jedem Schreibzugriff vorausberechnet und als Feld gespeichert, nie pro Request neu berechnet (AD-4, AD-5).
- `StatusOverride` ist eine eigene, personenbezogene Entität (kein Feld am Termin, kein Ablaufzeitpunkt); `CurrentStatusService.GetCurrentStatus(personId, now)` ist die einzige erlaubte Quelle für den aktuellen Status; bei sich überlappenden Terminen mit unterschiedlichem Status gilt der strengere Wert (AD-6).
- Sync-Idempotenz: partieller Unique-Index `(PersonId, Provider, ProviderEventId)` NUR `WHERE ProviderEventId IS NOT NULL`; native Termine haben `Provider`/`ProviderEventId` zwingend `NULL`, nie einen Platzhalter. Fehlt ein zuvor importierter Schlüssel im aktuellen Abruf, wird der Termin entfernt (AD-7).
- OAuth-Access-/Refresh-Tokens werden nie im Klartext persistiert; Verschlüsselung anwendungsseitig, Entschlüsselung nur in Infrastructure unmittelbar vor einem Provider-Aufruf; Token-Refresh-Schreibzugriff gehört ausschließlich dem Worker (AD-8).
- Login läuft über ASP.NET Core Identity (E-Mail + Passwort); Accounts werden ausschließlich von einem Admin angelegt (kein Self-Signup); Login ist vollständig unabhängig von den Kalender-OAuth-Flows (AD-9).
- Auth über HttpOnly-/Secure-/SameSite=Lax-Cookie, same-origin via Reverse-Proxy-Pfadrouting (`/` → Angular, `/api` → Backend); kein Access-Token im Client (AD-10).
- Worker und API referenzieren dieselben Domain-/Application-/Infrastructure-Bibliotheken; einzige Kopplung ist die gemeinsame Postgres-Datenbank, kein direkter Prozess-zu-Prozess-Aufruf (AD-11).
- Deprovisionierung ist Soft-Deaktivierung: `Person.IsActive = false` sperrt Login sofort, widerruft OAuth-Tokens, stoppt Sync, entfernt aus der Personenauswahl (FR-2) — native Termine, Teilnahmen, Attendee-Einträge und `StatusOverride` bleiben unverändert erhalten (kein Hard-Delete). Worker prüft `IsActive` zu Beginn jedes Sync-Zyklus (AD-12).
- Alle Fehlerantworten der API sind maschinenlesbare Codes/Keys (RFC-7807 `problem+json`), nie lokalisierter Text — Übersetzung geschieht ausschließlich im Angular-Frontend (AD-13).
- Backup: Cron-Sidecar-Container erstellt täglich ein `pg_dump`-Backup auf ein Docker-Volume, rollierend 14 Tage aufbewahrt. Restore ist ein dokumentierter, manueller Vorgang (Runbook in `deploy/`) (AD-14).
- Jeder `ICalendarProvider`-Adapter expandiert Serientermine serverseitig zu einer `Appointment`-Zeile pro Instanz mit providerseitiger Instanz-Event-ID — betrifft den MVP direkt (reale importierte Serientermine), unabhängig davon, dass natives FR-14 Should-Have ist (AD-15).
- `CalendarConnection` trägt `LastSuccessfulSyncAt`, `LastAttemptAt`, `ConsecutiveFailureCount` (optional `LastErrorCode`); der Worker aktualisiert diese Felder bei jedem Sync-Versuch, ein Fehlschlag für ein Konto blockiert nicht den Sync anderer Konten (AD-16).
- `Person.Role` (`Admin` | `Member`) ist serverseitig gesetzt und wird von jedem Endpoint mit aggregierten Daten über mehr als die eigene Person serverseitig geprüft — ein fehlender Nav-Punkt im Frontend ersetzt diese Prüfung nicht (AD-17).
- Konsistenzkonventionen: GUID-IDs für alle Entitäten; Speicherung ausschließlich in UTC (Umrechnung nur im Frontend); Serilog strukturiertes Logging nach stdout in allen Prozessen; Konfiguration via `appsettings.json` + Umgebungsvariablen-Override; Secrets ausschließlich über Umgebungsvariablen.
- Deployment: ein Docker-Compose-Stack (Caddy, Angular, API, Worker, Postgres, Backup-Sidecar) auf einem Host, keine Hochverfügbarkeits-Topologie; genau eine Umgebung (Produktion), kein separates Staging.
- Stack-Vorgabe: .NET 10 (LTS), ASP.NET Core Web API 10, EF Core 10, ASP.NET Core Identity 10, Angular 22, PostgreSQL 18, Docker Compose v2, Caddy v2, Serilog v4.

### UX Design Requirements

_Aus dem UX-Spine-Paar (ux-calendar-neu-bmad-2026-07-02/DESIGN.md + EXPERIENCE.md)._

**Design-Tokens & visuelles System**

- **UX-DR1:** Vollständiges dark-native Farb-Token-System implementieren (`bg`, `surface`, `surface-2`, `text`, `muted`, `border`, `border-interactive`, `accent`, `accent-on`, `interruptible`, `interruptible-fill`, `interruptible-text`, `dnd`, `dnd-fill`, `dnd-text`) plus das vollständige Light-Mode-Pendant (`-light`-Suffix) mit den in DESIGN.md spezifizierten Hex-Werten.
- **UX-DR2:** Typografie-Skala mit 5 Rollen implementieren (`heading`, `label`, `body`, `caption`, `label-caps`), Schrift IBM Plex Sans mit Fallback-Stack.
- **UX-DR3:** Spacing-Skala (`spacing.1`–`spacing.8`, 4px-Basis) plus den separaten `time-gutter`-Token (64px, bewusst nicht Teil der Rhythmus-Skala) implementieren.
- **UX-DR4:** Radius-Tokens implementieren — `sm` (8px), `md` (12px), `lg` (16px), `full` (Pill) — je Komponente gemäß DESIGN.md.Shapes.
- **UX-DR5:** Elevation-System implementieren — keine Schatten auf der Ruhe-UI; weicher Schatten nur für Popovers/Flyouts; der stärkste Schatten ausschließlich für den äußeren App-Rahmen.

**Komponenten**

- **UX-DR6:** Status-Badge/Status-Block-Komponente bauen — Glow-Fill-Hintergrund + 3px linker Rand + Icon-Glyph + Text-Label, nie Vollfläche, nie Farbe allein; kein visueller/textueller Unterschied zwischen automatisch abgeleitetem und manuell übersteuertem Status (FR-11).
- **UX-DR7:** Kalenderspalten-Komponente bauen — zwei Inhaltsmodi: eigene Spalte zeigt volle Terminblöcke (Titel+Zeit, Accent-Rand), Kollegen-Spalten zeigen nur Status-Blöcke; nebeneinander, nie überlagert; Spaltenauswahl bleibt bei Tages-/Wochenwechsel erhalten.
- **UX-DR8:** Monats-Tageszelle + Popover bauen — standardmäßig sauberer Zustand mit nur einem dezenten Aggregat-Marker bei ≥1 ausgewähltem Teammitglied in "Bitte nicht stören"; Klick/Tap (nicht Hover) öffnet Popover mit Name + Status-Badge je ausgewähltem Teammitglied für diesen Tag (Option C); identisches Verhalten auf Touch- und Zeigegeräten.
- **UX-DR9:** Status-Override-Control bauen — Icon-Button ("Status ändern") stets sichtbar auf der eigenen Spaltenkopfzeile unabhängig vom aktuellen Status; öffnet Flyout mit genau drei Optionen (Unterbrechbar / Bitte nicht stören / Automatisch), aktive Wahl mit "AKTIV" markiert; Flyout-Breite intrinsisch (min-width ~180px, nicht fix).
- **UX-DR10:** Sync-Zeitstempel-Anzeige bauen — stets sichtbarer Caption-Text+Icon pro Person (Spaltenkopf), pro Provider (Einstellungen) und aggregiert (Admin-Übersicht); wechselt bei wiederholtem Fehlschlag in expliziten Fehlerzustand (`dnd-text`, nicht rohes `dnd`); Zustandswechsel wird via `aria-live="polite"` angekündigt.
- **UX-DR11:** Termin-Erstellungs-Einstiegspunkte bauen — Doppelklick (oder `Enter`/`Space` auf fokussiertem leerem Slot) öffnet vorausgefülltes Erstellformular; eigenständiger "+ Neuer Termin"-Ghost-Pill-Button öffnet identisches leeres Formular; beide Pfade erzeugen dasselbe Terminobjekt und erhalten beim Speichern automatisch einen Status.
- **UX-DR12:** Termin-Detail-Popover bauen — zwei Modi auf einer Form: Volldetail (eigener Termin oder fremder Termin mit Teilnahme des Anfragenden) vs. Status-only + expliziter Datenschutzhinweis ("Details sind privat — nur der Status ist sichtbar.") für alle anderen fremden Termine; Modus wird serverseitig entschieden.
- **UX-DR13:** Personen-Selektor-Komponente bauen — Avatar-Chip-Reihe (je ausgewähltem Teammitglied, mit "×"-Entfernen) + abschließender "+"-Chip, der ein durchsuchbares Dropdown öffnet (Tippen filtert, Pfeiltasten navigieren, `Enter` fügt hinzu, `Esc` schließt); keine Obergrenze für Auswahl — Zeile scrollt horizontal.
- **UX-DR14:** View-Switcher bauen — Monat/Woche/Tag-Pill-Umschalter, stets sichtbar, sofortiger clientseitiger Wechsel (kein Ladezustand), aktive Option mit Vollflächen-Accent-Fill, Standard-Tab-/Radio-Group-Semantik für Assistive Technologie.
- **UX-DR15:** Sprachumschalter bauen — minimale Pill-Kontrolle Deutsch (kanonisch) ↔ Englisch, sofortiger clientseitiger Wechsel ohne Verlust des aktuellen Ansichts-/Auswahlzustands (z. B. Personen-Selektor-Chips, aktuelle Monat/Woche/Tag-Ansicht bleiben erhalten).

**Oberflächen (Information Architecture)**

- **UX-DR16:** Login-Oberfläche als unauthentifizierter Einstiegspunkt bauen — Detailzustände (Laden, ungültige Anmeldedaten, Sperre) sind bewusst aufgeschoben bis zur Architektur-Entscheidung zum Auth-Mechanismus; die Seite selbst muss existieren und jeglichen Kalenderzugriff vor Anmeldung blockieren.
- **UX-DR17:** Eigener Kalender (Monat/Woche/Tag) als Standard-Landing-Oberfläche nach Login bauen.
- **UX-DR18:** Mehrpersonen-Ansicht für Tag/Woche (FR-2) bauen, Auswahl bleibt bei Tages-/Wochenwechsel erhalten.
- **UX-DR19:** Mehrpersonen-Ansicht für Monat bauen — eine UX-seitige Scope-Erweiterung über PRD FR-2 hinaus (Memlog-Entscheidung) — unter Nutzung des Monats-Tageszelle+Popover-Mechanismus (UX-DR8).
- **UX-DR20:** Oberfläche Einstellungen → Kalenderverbindungen bauen — pro Provider (Outlook/Google) eine Zeile mit Verbunden/Nicht-verbunden, "Verbinden"-Button, letztem Sync-Zeitstempel, Fehleranzeige; optional erreichbar über das Einstellungsmenü, nie ein erzwungener Onboarding-Schritt (neue Nutzer landen direkt im eigenen leeren Kalender).
- **UX-DR21:** Oberfläche Admin → Sync-Übersicht bauen — rollen-gesperrter (nur Admin) Navigationspunkt; listet für jedes Teammitglied Provider, Sync-Status, letzten erfolgreichen Sync (Person | Provider | Status | letzter Sync).

**Zustände**

- **UX-DR22:** Leerzustand für neuen Nutzer ohne verbundenen Kalender implementieren — einladende, nicht blockierende Nachricht mit Link zu Einstellungen → Kalenderverbindungen, kein Fehlerzustand.
- **UX-DR23:** Fehlerzustand Kalenderverbindung (Consent verweigert / durch Tenant-Admin blockiert) implementieren — expliziter, providerspezifischer, handlungsleitender Text (z. B. "Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren"), nie stillschweigend wiederholt ohne Fehleranzeige.
- **UX-DR24:** Sync-Fehlerzustand (wiederholt/still) implementieren — sichtbar pro Konto für den betroffenen Nutzer und aggregiert in der Admin-Übersicht; Text nutzt `dnd-text` (nicht rohes `dnd`), Ankündigung via `aria-live="polite"`.
- **UX-DR25:** Zustand "keine Teammitglieder ausgewählt" in der Mehrpersonen-Ansicht implementieren — eigene Spalte rendert weiterhin; verbleibender Raum zeigt den "+"-Chip mit einladendem Text ("Wähle Teammitglieder, um ihre Verfügbarkeit zu sehen."), keine leere Fläche.
- **UX-DR26:** Zugriffsverweigerung für die Admin-Oberfläche implementieren — Navigationspunkt ist für Nicht-Admins schlicht nicht vorhanden, keine "Kein Zugriff"-Sackgasse.
- **UX-DR27:** Zustand "fremder Termin ohne Teilnahme" als designter Standardfall implementieren (kein Fehlerzustand) — Status-Badge + einzeiliger Datenschutzhinweis.
- **UX-DR28:** Validierungsfehler-Zustand bei Terminerstellung implementieren — fehlender Titel oder ungültiges/unvollständiges Datum/Uhrzeit blockiert das Speichern mit inline-Feldfehler; Formular bleibt offen mit erhaltenen Eingabewerten; Fokus springt auf das erste ungültige Feld.

**Interaktion & Barrierefreiheit**

- **UX-DR29:** Vollständige Tastaturbedienbarkeit für alle Floating-Layer implementieren (Status-Override-Flyout, beide Popover-Typen, Personen-Selektor-Dropdown) — per `Tab` erreichbar, per `Enter`/`Space` öffenbar, interne Pfeiltasten-Navigation, per `Esc` schließbar, mit Fokus-Trap während geöffnet und Fokus-Rückgabe an das auslösende Element beim Schließen.
- **UX-DR30:** Sichtbare Fokus-Ringe (Accent-Farbe) auf jedem interaktiven Element implementieren (Status-Override-Button, Flyout-Items, Kalender-Slots, Monatszellen, View-Switcher, Personen-Selektor-Chips/Dropdown-Items, "+ Neuer Termin", Sprachumschalter).
- **UX-DR31:** `prefers-reduced-motion`-Unterstützung implementieren — alle Hover-/Öffnen-/Schließen-Übergänge werden unter `prefers-reduced-motion: reduce` nahezu augenblicklich oder deaktiviert.
- **UX-DR32:** Status-Barrierefreiheits-Floor implementieren — Status darf nie allein auf Farbe beruhen (Icon + Textlabel + visuell unterscheidbare Glyphen-Form je Status); Status-Textlabels dürfen nie abgeschnitten/ellipsiert werden, auch nicht bei horizontalem Scroll oder schmalen Spalten.
- **UX-DR33:** Screenreader-Beschriftung für Status-Blöcke implementieren — korrekte Rolle+Label in beide Richtungen (volles Titel+Zeit für eigenen Slot; "Termin, Status: Bitte nicht stören" für fremden Slot, ohne den eigenen Slot zu über-redigieren).
- **UX-DR34:** i18n-bewusste Komponentengrößen implementieren — Komponenten mit übersetzten Strings (Status-Override-Flyout, Status-Badges, Personen-Selektor-Chips/Dropdown) skalieren mit min-/max-width statt fixer Pixel-Sperre (deutsche Strings laufen länger als englische).
- **UX-DR35:** Alle in DESIGN.md spezifizierten Kontrastverhältnisse exakt umsetzen und verifizieren (Basis-Text/Hintergrund-Paare, Status-Farb-Text-Paare, Nicht-Text-UI-Grenzen via `border-interactive` statt `border`, Light-Mode-Linktext via `accent-text-light`) — Implementierung muss den tatsächlich gerenderten Kontrast prüfen, nicht die Token-Werte als gegeben annehmen.

**i18n & Rollen (Foundation)**

- **UX-DR36:** Echtes i18n-Fundament implementieren — Deutsch (kanonisch/erstautorisiert) + Englisch als vollwertige Parallelsprache, mit Sprachumschalter (UX-DR15); keine hartkodierten UI-Strings.
- **UX-DR37:** Echtes Admin/Member-Rollensystem mit serverseitigen Berechtigungsprüfungen implementieren (nicht nur eine ungeschützte Zusatzseite oder ein versteckter Nav-Punkt) — siehe auch Additional Requirement AD-17.

**Responsive & Plattform**

- **UX-DR38:** Sicherstellen, dass alle Kernfunktionen (Ansichtswechsel, Terminerstellung, Statusanzeige, Detailansicht) auf mobilen Browsern identisch funktionieren — nicht auf reinen Lesezugriff degradiert.
- **UX-DR39:** Horizontales-Scroll-Verhalten für Mehrpersonen-Ansicht und Personen-Selektor-Chip-Reihe auf kleinen Bildschirmen implementieren — keine Obergrenze für ausgewählte Teammitglieder, eigene Spalte angeheftet/zuerst.

### FR Coverage Map

FR-1: Epic 1 - Eigene Kalenderansicht (Monat/Woche/Tag)
FR-2: Epic 3 - Mehrpersonen-Ansicht
FR-3: Epic 1 - Termin anlegen
FR-4: Epic 1 - Termin-Detailansicht (eigene Termine); Status-only-Variante ergänzt in Epic 3
FR-5: Epic 2 - Outlook-Import
FR-6: Epic 2 - Google-Calendar-Import
FR-7: Epic 2 - Kein Zurückschreiben (strukturell durch ICalendarProvider ohne Schreibmethode)
FR-8: Epic 2 - Interner Vollzugriff (für native Termine bereits in Epic 1 trivial erfüllt, vollständig relevant erst mit Sync)
FR-9: Epic 3 - Privat-Default gegenüber anderen
FR-10: Epic 1 - Automatische Status-Ableitung (Voraussetzung für FR-3; gilt ab Epic 2 auch für synchronisierte Termine ohne weitere Story, da derselbe Domain-Service verwendet wird)
FR-11: Epic 4 - Manueller Status-Override
FR-12 (Should-Have): Nicht im MVP-Scope — kein Epic in diesem Breakdown (PRD §6.2)
FR-13 (Should-Have): Nicht im MVP-Scope — kein Epic in diesem Breakdown (PRD §6.2)
FR-14 (Should-Have): Nicht im MVP-Scope — kein Epic in diesem Breakdown (PRD §6.2)
FR-15 (Should-Have): Nicht im MVP-Scope — kein Epic in diesem Breakdown (PRD §6.2)

NFR-1 (Sync-Aktualität): Epic 2
NFR-2 (Sync-Transparenz): Epic 2
NFR-3 (Datenhaltung/Backup): Epic 5
NFR-4 (Zugriffsschutz): Epic 1
NFR-5 (Mobil/Responsiv): Cross-cutting — Basisverhalten in Epic 1, Mehrpersonen-spezifisches Verhalten (horizontales Scrollen) in Epic 3
NFR-6 (Betrieb, Bus-Faktor-1): Epic 5

## Epic List

### Epic 1: Anmeldung, Grundgerüst & Eigener Kalender
Ein Teammitglied kann sich anmelden und seinen eigenen Kalender im Tool nutzen: Monats-/Wochen-/Tagesansicht, native Termine anlegen und per Klick einsehen, jeweils mit automatisch abgeleitetem Verfügbarkeits-Status. Dies etabliert außerdem das Projekt-Grundgerüst (Layered-Architecture-Solution, Docker-Compose-Basisstack mit Postgres/Api/Angular/Caddy, Design-Tokens, i18n-Fundament, Rollenmodell-Feld) als Voraussetzung für alle folgenden Epics.
**FRs covered:** FR-1, FR-3, FR-4, FR-10
**NFRs covered:** NFR-4
**Enthält zusätzlich:** Projekt-/Deployment-Grundgerüst (AD-1, Stack, Docker-Compose-Basis), Login/Identity (AD-9, AD-10), Rollenmodell-Feld (Person.Role, AD-17-Grundlage), Fehlerformat-Konvention (AD-13), Design-Tokens (UX-DR1–UX-DR5), Status-Badge (UX-DR6), View-Switcher (UX-DR14), Sprachumschalter (UX-DR15), Login-Oberfläche (UX-DR16), eigener Kalender als Landing (UX-DR17), Termin-Erstellung prefilled/blank (UX-DR11), Termin-Detail-Popover Volldetail-Modus (UX-DR12, Teil 1), Validierungsfehler-Zustand (UX-DR28), i18n-Fundament (UX-DR36), Rollensystem-Grundlage (UX-DR37, Teil 1).

### Epic 2: Kalender-Synchronisation & Sync-Transparenz
Termine aus verbundenen Outlook- und Google-Konten erscheinen automatisch im eigenen Kalender (einseitiger Import, kein Zurückschreiben). Jedes Teammitglied sieht in den Einstellungen, wann zuletzt synchronisiert wurde; Dennis als Admin sieht den Sync-Zustand aller Konten gebündelt und erkennt Ausfälle, bevor sie unbemerkt bleiben.
**FRs covered:** FR-5, FR-6, FR-7, FR-8
**NFRs covered:** NFR-1, NFR-2
**Enthält zusätzlich:** Worker-Container im Deployment-Stack, `ICalendarProvider`-Port ohne Schreibmethode (AD-2), Sync-Idempotenz über Provider-Event-ID (AD-7), Token-Verschlüsselung (AD-8), gemeinsame Codebasis Worker/API (AD-11), Serientermin-Normalisierung pro Instanz (AD-15), sichtbarer Sync-Zustand (AD-16), erste rollen-durchgesetzte Oberfläche (AD-17 in der Praxis), Sync-Zeitstempel-Anzeige (UX-DR10), Einstellungen → Kalenderverbindungen (UX-DR20), Admin → Sync-Übersicht (UX-DR21), Leerzustand ohne Verbindung (UX-DR22), Verbindungsfehler-Zustand (UX-DR23), Sync-Fehlerzustand (UX-DR24), Zugriffsverweigerung Admin-Nav (UX-DR26).

### Epic 3: Team-Sichtbarkeit & Privat-Default (Mehrpersonen-Ansicht)
Ein Teammitglied kann Kollegen auswählen und deren Kalender nebeneinander sehen — mit "privat/beschäftigt" + Verfügbarkeits-Status statt echtem Titel, außer es ist selbst als Teilnehmer eingetragen. Das realisiert den Kernnutzen des Tools (UJ-1: auf einen Blick sehen, ob jemand ansprechbar ist).
**FRs covered:** FR-2, FR-9
**Ergänzt:** FR-4 (Status-only-Variante der Detailansicht für fremde Termine)
**Enthält zusätzlich:** Zentrale Privat-Default-Durchsetzung inkl. Bulk-Methode (AD-3), Kalenderspalte im Kollegen-Modus (UX-DR7), Monats-Tageszelle + Popover (UX-DR8), Personen-Selektor (UX-DR13), Detail-Popover Status-only-Modus (UX-DR12, Teil 2), Mehrpersonen-Ansicht Tag/Woche (UX-DR18) und Monat (UX-DR19), Zustand "keine Auswahl" (UX-DR25), Zustand "fremder Termin ohne Teilnahme" (UX-DR27), horizontales Scrollen ohne Obergrenze (UX-DR39).

### Epic 4: Eigenen Verfügbarkeits-Status manuell steuern
Ein Teammitglied kann seinen aktuell angezeigten Verfügbarkeits-Status jederzeit manuell übersteuern (Unterbrechbar / Bitte nicht stören / Automatisch) — sichtbar für Kollegen ohne Unterscheidung zu einem automatisch abgeleiteten Status.
**FRs covered:** FR-11
**Enthält zusätzlich:** `StatusOverride`-Entität und `CurrentStatusService` inkl. Tie-Break-Regel bei überlappenden Terminen (AD-6), Status-Override-Control mit Flyout (UX-DR9).

### Epic 5: Team-Verwaltung & Betrieb (Deprovisionierung, Backup)
Dennis kann als Admin ein ausscheidendes Teammitglied deaktivieren (Login/Sync/Personenauswahl sofort gesperrt, Daten bleiben erhalten) und verlässt sich auf ein tägliches automatisiertes Backup nativer Termine mit dokumentiertem, manuellem Restore-Prozess.
**NFRs covered:** NFR-3, NFR-6
**Enthält zusätzlich:** Soft-Deaktivierung eines Teammitglieds (AD-12), Backup-Sidecar-Container mit täglichem `pg_dump` und 14-Tage-Rollierung plus Restore-Runbook (AD-14).
**Voraussetzungen:** Setzt Epic 1 (Rollenfeld, Admin-Bootstrapping), Epic 2 (OAuth-Tokens/`CalendarConnection`, die bei Deaktivierung widerrufen werden) UND Epic 3 (Personen-Selektor, aus dem die Person entfernt wird) voraus — nicht nur Epic 1.

**Nicht im MVP abgedeckt (Should-Have, PRD §6.2):** FR-12 (Slot-Finder), FR-13 (Erinnerungen), FR-14 (Serientermine), FR-15 (Ortsangabe mit Karte) — bewusst kein Epic in diesem Breakdown, gemäß Architecture-Spine-Abschnitt "Deferred".

## Epic 1: Anmeldung, Grundgerüst & Eigener Kalender

Ein Teammitglied kann sich anmelden und seinen eigenen Kalender im Tool nutzen: Monats-/Wochen-/Tagesansicht, native Termine anlegen und per Klick einsehen, jeweils mit automatisch abgeleitetem Verfügbarkeits-Status. Dies etabliert außerdem das Projekt-Grundgerüst als Voraussetzung für alle folgenden Epics.

### Story 1.1: Projekt-Grundgerüst, Anmeldung & Sprache

As a Teammitglied,
I want mich mit E-Mail und Passwort anmelden und die Oberfläche auf Deutsch oder Englisch nutzen können,
So that ich sicher auf meinen persönlichen Kalender zugreifen kann, ohne dass Unautorisierte Zugriff haben.

**Acceptance Criteria:**

**Given** die Solution-Struktur gemäß Architecture-Spine (Domain/Application/Infrastructure/Api/Worker/frontend/deploy) existiert noch nicht
**When** das Projekt aufgesetzt wird
**Then** existieren die fünf Schichten als separate Projekte mit der AD-1-Abhängigkeitsrichtung (Domain kennt nichts; Application kennt nur Domain; Infrastructure implementiert Application/Domain-Interfaces; Api und Worker sind reine Composition-Roots) und ein Docker-Compose-Stack mit mindestens Postgres, Api, Angular und Caddy (Pfadrouting `/` → Angular, `/api` → Api) startet lokal erfolgreich

**Given** ein Admin hat einen Account (E-Mail + Passwort) für ein Teammitglied angelegt — kein Self-Signup möglich
**When** das Teammitglied auf der Login-Seite gültige Zugangsdaten eingibt
**Then** wird es angemeldet und über ein HttpOnly-, Secure-, SameSite=Lax-Cookie authentifiziert; kein Access-Token wird an clientseitigen JavaScript-Code ausgeliefert

**Given** ein nicht angemeldeter Nutzer
**When** er versucht, eine Kalender-Route direkt aufzurufen
**Then** wird er zur Login-Seite umgeleitet und erhält keinerlei Kalenderdaten

**Given** die `Person`-Entität
**When** sie angelegt wird
**Then** besitzt sie ein `Role`-Feld (`Admin` \| `Member`), das ausschließlich von einem bestehenden Admin änderbar ist

**Given** ein API-Endpoint liefert einen Fehler
**When** die Antwort ausgeliefert wird
**Then** folgt sie dem RFC-7807-`problem+json`-Format mit stabilem `code`-Feld, nie lokalisiertem Freitext

**Given** die Angular-Oberfläche wird geladen
**When** ein Nutzer den Sprachumschalter betätigt
**Then** wechselt die UI sofort zwischen Deutsch (kanonisch) und Englisch, ohne Neuladen; alle vorhandenen Texte (Login, Navigation) liegen in beiden Sprachen vor — keine hartkodierten Strings

**Given** das Design-Token-System aus DESIGN.md (Farben, Typografie, Spacing, Radius, dark-native als Standard mit vollständigem `-light`-Set)
**When** die Login-Seite und der App-Rahmen gerendert werden
**Then** nutzen sie ausschließlich diese Tokens, inkl. korrekter Kontrastwerte für Text/Hintergrund-Paare

**Given** ein interaktives Element (Login-Button, Sprachumschalter)
**When** es per Tastatur fokussiert wird
**Then** zeigt es einen sichtbaren Fokusring in der Accent-Farbe

**Given** `prefers-reduced-motion: reduce` ist aktiv
**When** UI-Übergänge auftreten (z. B. Formular-Feedback)
**Then** sind sie nahezu augenblicklich oder deaktiviert

**Given** noch kein Admin-Account existiert (allererster Start des Systems)
**When** der Api-Container zum ersten Mal hochfährt
**Then** wird genau ein initialer Admin-Account aus Umgebungsvariablen (E-Mail + Passwort) angelegt, sodass ein Henne-Ei-Problem (kein Admin kann Accounts anlegen, weil kein Admin existiert) nicht entsteht

**Given** es existiert genau ein Admin-Account
**When** ein Admin versucht, sich selbst oder den letzten verbleibenden Admin auf `Member` herabzustufen
**Then** verweigert das System die Aktion mit einem RFC-7807-Fehlercode — es muss immer mindestens ein aktiver Admin-Account bestehen bleiben (Bus-Faktor-1-Schutz). Die analoge Regel für Deaktivierung (`IsActive`) wird in Epic 5, Story 5.1 durchgesetzt, sobald dieses Feld eingeführt ist

**Given** ein Nutzer gibt ungültige Zugangsdaten ein
**When** der Login-Versuch fehlschlägt
**Then** ist das konkrete Fehlerverhalten (Meldungstext, Lockout nach N Versuchen) bewusst nicht Teil dieser Story — laut EXPERIENCE.md (UX-DR16) ist dies eine explizite Aufschiebung bis zur Festlegung des endgültigen Auth-Mechanismus, kein vergessenes Akzeptanzkriterium; ein einfacher generischer Fehlerhinweis genügt für diese Story

**Given** ein Nutzer ist angemeldet und sein Session-Cookie läuft während der Nutzung ab
**When** er die nächste Aktion ausführt, die eine Authentifizierung erfordert
**Then** wird er kontrolliert zur Login-Seite umgeleitet (kein unbehandelter Fehler, kein stiller Datenverlust in einem offenen Formular)

### Story 1.2: Eigene Kalenderansicht (Monat/Woche/Tag)

As a Teammitglied,
I want meinen eigenen Kalender in Monats-, Wochen- und Tagesansicht sehen können,
So that ich einen gewohnten Überblick über meine Termine habe, unabhängig von deren Quelle.

**Acceptance Criteria:**

**Given** ich bin angemeldet
**When** ich meinen Kalender zum ersten Mal öffne
**Then** lande ich standardmäßig in der Wochenansicht `[ASSUMPTION: Standardansicht ist in PRD/UX nicht explizit festgelegt]` mit sichtbarem View-Switcher (Monat/Woche/Tag)

**Given** ich befinde mich in einer der drei Ansichten
**When** ich über den View-Switcher eine andere Ansicht wähle
**Then** wechselt die Ansicht sofort clientseitig ohne Neuladen der Seite; die aktive Option zeigt Vollflächen-Accent-Fill, die inaktiven zeigen gedämpften Text auf transparentem Hintergrund

**Given** ich habe noch keine Termine
**When** ich meinen Kalender öffne
**Then** zeigt die Ansicht eine einladende Leer-Nachricht statt einer leeren oder fehlerhaft wirkenden Fläche

**Given** der View-Switcher
**When** er per Tastatur bedient wird
**Then** ist er über Standard-Tab-/Radio-Group-Semantik für Assistive Technologie erreichbar und bedienbar

**Given** ich nutze ein mobiles Endgerät (Browser)
**When** ich eine der drei Ansichten öffne
**Then** ist sie vollständig nutzbar, nicht auf Lesezugriff beschränkt

**Given** ich habe zwei oder mehr eigene Termine mit überlappender Zeit am selben Tag
**When** die Tages- oder Wochenansicht sie rendert
**Then** werden beide sichtbar dargestellt, ohne dass einer den anderen verdeckt `[OPEN QUESTION: konkretes Stapel-/Nebeneinander-Layout für überlappende eigene Termine ist in PRD/UX nicht spezifiziert — Layout-Entscheidung bei Umsetzung dieser Story treffen, nicht stillschweigend einen Termin verschwinden lassen]`

### Story 1.3: Termin anlegen (nativ) mit automatischem Verfügbarkeits-Status

As a Teammitglied,
I want einen neuen Termin direkt im Tool anlegen, der automatisch einen Verfügbarkeits-Status erhält,
So that mein Termin sofort sichtbar ist und Kollegen später einschätzen können, ob ich ansprechbar bin.

**Acceptance Criteria:**

**Given** ich bin in der Wochen- oder Tagesansicht auf einem leeren Zeitslot
**When** ich doppelklicke (oder `Enter`/`Space` auf dem fokussierten leeren Slot drücke)
**Then** öffnet sich das Termin-Erstellungsformular mit Datum und Uhrzeit dieses Slots vorausgefüllt

**Given** ich klicke stattdessen auf den Button "+ Neuer Termin"
**When** sich das Formular öffnet
**Then** ist kein Datum/keine Uhrzeit vorausgefüllt und ich wähle beides manuell

**Given** das Termin-Erstellungsformular ist offen
**When** ich Titel, Datum/Uhrzeit, Dauer und optional Teilnehmer eingebe und speichere
**Then** wird ein nativer Termin angelegt (`Provider` und `ProviderEventId` beide `NULL`) und erscheint sofort in meiner eigenen Kalenderansicht

**Given** ein neuer nativer Termin wird gespeichert
**When** `StatusHeuristicService.Compute` ausgeführt wird
**Then** erhält der Termin automatisch einen Verfügbarkeits-Status nach der Regel: kein Teilnehmer → `Unterbrechbar`; ≥1 Teilnehmer und Dauer ≤45 Min → `Unterbrechbar`; ganztägig ODER (Dauer ≥90 Min UND ≥3 Teilnehmer) → `BitteNichtStoeren`; alle übrigen Fälle → `Unterbrechbar` — der Status wird als Feld am Termin gespeichert, nicht bei jedem Lesezugriff neu berechnet

**Given** der gespeicherte Status eines eigenen Termins
**When** er in meiner Kalenderspalte angezeigt wird
**Then** trägt er die Status-Badge-Darstellung (Glow-Fill-Hintergrund, 3px-Rand in Vollfarbe, Icon-Glyph, Textlabel) — niemals Farbe allein

**Given** ein Termin mit genau 45 Minuten Dauer und ≥1 Teilnehmer (untere Grenze der "kurz"-Regel)
**When** der Status berechnet wird
**Then** ist das Ergebnis `Unterbrechbar` (45 Minuten liegt inklusive in der ≤45-Regel)

**Given** ein Termin mit genau 90 Minuten Dauer und genau 3 Teilnehmern (untere Grenze der "lang"-Regel)
**When** der Status berechnet wird
**Then** ist das Ergebnis `BitteNichtStoeren` (90 Minuten UND 3 Teilnehmer liegen inklusive in der ≥90-Regel)

**Given** ein ganztägiger Termin ganz ohne Teilnehmer (z. B. ein selbst geblockter Fokustag)
**When** der Status berechnet wird
**Then** ist das Ergebnis `Unterbrechbar` — die Kein-Teilnehmer-Regel hat Vorrang vor der Ganztägig-Regel, unabhängig von der Dauer

**Given** ich versuche, einen Termin mit 0 Minuten Dauer oder mit Endzeit vor Startzeit zu speichern
**When** die Validierung läuft
**Then** wird der Termin nicht angelegt, ein Inline-Fehler zur Zeitspanne erscheint, und meine Eingaben bleiben erhalten

**Given** ich lasse den Titel leer und klicke Speichern
**When** die Validierung läuft
**Then** wird der Termin nicht angelegt, ein Inline-Fehler ("Bitte gib einen Titel ein.") erscheint unter dem Titelfeld, das Formular bleibt mit meinen bisherigen Eingaben geöffnet, und der Fokus springt auf das Titelfeld

**Given** das Erstellungsformular ist geöffnet
**When** ich `Esc` drücke oder außerhalb klicke
**Then** schließt es sich ohne zu speichern und der Fokus kehrt zum auslösenden Element zurück

### Story 1.4: Termin-Detailansicht (eigene Termine)

As a Teammitglied,
I want die vollen Details eines eigenen Termins per Klick öffnen können,
So that ich Titel, Zeit und Teilnehmer jederzeit einsehen kann.

**Acceptance Criteria:**

**Given** ich klicke auf einen eigenen Termin in beliebiger Ansicht
**When** das Detail-Popover sich öffnet
**Then** zeigt es Titel, Zeit, Teilnehmer und den aktuellen Verfügbarkeits-Status vollständig — unabhängig vom Privat-Default, der nur für andere Betrachter gilt

**Given** ein Termin ohne Ort (Ortsangabe ist FR-15, Should-Have, nicht MVP)
**When** das Detail-Popover angezeigt wird
**Then** wird kein Ort-Feld angezeigt

**Given** das Detail-Popover ist geöffnet
**When** ich `Esc` drücke oder außerhalb klicke
**Then** schließt es sich und der Fokus kehrt zum auslösenden Element zurück

**Given** das Popover wird per Tastatur geöffnet (`Enter`/`Space` auf dem fokussierten Termin)
**When** es sichtbar ist
**Then** ist der Fokus im Popover gefangen (Tab-Zyklus bleibt innerhalb), bis es geschlossen wird

## Epic 2: Kalender-Synchronisation & Sync-Transparenz

Termine aus verbundenen Outlook- und Google-Konten erscheinen automatisch im eigenen Kalender (einseitiger Import, kein Zurückschreiben). Jedes Teammitglied sieht in den Einstellungen, wann zuletzt synchronisiert wurde; Dennis als Admin sieht den Sync-Zustand aller Konten gebündelt und erkennt Ausfälle, bevor sie unbemerkt bleiben.

### Story 2.1: Google-Kalender verbinden & importieren

As a Teammitglied,
I want mein Google-Konto verbinden können, damit meine Google-Termine automatisch im Tool erscheinen,
So that ich nicht mehr zwischen Google Calendar und dem Tool wechseln muss, um vollständig zu sein.

**Acceptance Criteria:**

**Given** ich bin angemeldet und öffne Einstellungen → Kalenderverbindungen
**When** ich auf "Verbinden" bei Google klicke
**Then** durchlaufe ich den OAuth-Consent-Flow (Scope `calendar.readonly`, Internal-App ohne Verifizierung `[ASSUMPTION: gemeinsames Google-Workspace, siehe PRD Offene Frage #1]`) und lande danach zurück in den Einstellungen mit dem Status "Verbunden"

**Given** der OAuth-Handshake ist abgeschlossen
**When** die Access-/Refresh-Tokens gespeichert werden
**Then** werden sie nie im Klartext persistiert — Verschlüsselung erfolgt anwendungsseitig, Entschlüsselung nur unmittelbar vor einem Google-API-Aufruf innerhalb von Infrastructure

**Given** ein verbundenes Google-Konto
**When** der Worker seinen Sync-Zyklus ausführt
**Then** ruft er `GoogleCalendarProvider.FetchAllEvents` auf, das einen vollständigen aktuellen Snapshot aller Termine im betrachteten Zeitfenster liefert (kein Delta/Incremental-Sync), und upsertet Termine anhand von `(PersonId, Provider, ProviderEventId)`

**Given** ein bereits importierter Termin wird an der Quelle verschoben oder inhaltlich geändert
**When** der nächste Sync-Zyklus läuft
**Then** wird der bestehende Eintrag aktualisiert, kein zusätzlicher Termin angelegt

**Given** ein bereits importierter Termin wird an der Quelle gelöscht oder abgesagt
**When** der nächste Sync-Zyklus läuft und der zugehörige Schlüssel im aktuellen Abruf fehlt
**Then** wird der Termin (inkl. eines davon abgeleiteten Status) aus dem Tool entfernt

**Given** ein importierter Serientermin
**When** der `GoogleCalendarProvider` ihn verarbeitet
**Then** wird serverseitig pro Instanz expandiert (nicht der Serien-Master) — jede Instanz erhält eine eigene `Appointment`-Zeile mit der providerseitigen Instanz-Event-ID als `ProviderEventId`

**Given** ein importierter Google-Termin
**When** er gespeichert wird
**Then** liest und speichert das System die vollen Daten (Titel, Teilnehmer, Dauer, Ort) und `StatusHeuristicService.Compute` weist ihm denselben Verfügbarkeits-Status nach derselben Regel wie native Termine zu (kein zweiter Heuristik-Codepfad)

**Given** kein Schreibpfad existiert gegen die Google-API
**When** ein nativer Termin im Tool angelegt, geändert oder gelöscht wird
**Then** erzeugt dies keine Schreiboperation gegen Google — `ICalendarProvider` definiert strukturell keine Schreibmethode

**Given** ich habe mein Google-Konto verbunden
**When** ich in Einstellungen → Kalenderverbindungen nachsehe
**Then** zeigt die Google-Zeile den Zeitpunkt des letzten erfolgreichen Syncs ("Zuletzt synchronisiert vor N Min.") in Caption-Text mit Icon

**Given** der Consent wird verweigert oder von der Google-Workspace-Organisation blockiert
**When** der Verbindungsversuch fehlschlägt
**Then** zeigt die Zeile einen expliziten, providerspezifischen Fehlerhinweis statt eines generischen "Verbindung fehlgeschlagen", und der Versuch wird nicht stillschweigend wiederholt

**Given** der Sync für ein Google-Konto schlägt wiederholt fehl (z. B. abgelaufenes Token)
**When** `ConsecutiveFailureCount` einen Schwellenwert übersteigt
**Then** wechselt die Sync-Anzeige in den expliziten Fehlerzustand (`dnd-text`-Farbe, nicht rohes `dnd`), die Änderung wird via `aria-live="polite"` angekündigt, und der Sync anderer Konten wird davon nicht blockiert

**Given** noch kein Kalender verbunden ist
**When** ich meinen (leeren) Kalender öffne
**Then** zeigt die Ansicht die einladende Nachricht "Noch keine Termine — verbinde deinen Kalender in den Einstellungen, wann immer du bereit bist." mit einem Link zu Einstellungen → Kalenderverbindungen — das Verbinden ist optional, kein erzwungener Onboarding-Schritt

### Story 2.2: Outlook-Kalender verbinden & importieren

As a Teammitglied,
I want mein Outlook-Konto verbinden können, damit meine Outlook-Termine automatisch im Tool erscheinen,
So that Kundeneinladungen und andere extern ausgelöste Termine nicht in einem separaten Kalender untergehen.

**Acceptance Criteria:**

**Given** ich öffne Einstellungen → Kalenderverbindungen und klicke bei Outlook auf "Verbinden"
**When** ich den Microsoft-Graph-OAuth-Consent-Flow durchlaufe (Scope `Calendars.Read`)
**Then** lande ich danach zurück in den Einstellungen mit dem Status "Verbunden"

**Given** die Tenant-Konfiguration eines Teammitglieds sperrt Self-Consent organisationsweit
**When** der Verbindungsversuch daran scheitert
**Then** zeigt die Zeile einen handlungsleitenden, providerspezifischen Hinweis (z. B. "Verbindung von deinem Unternehmen blockiert — bitte den IT-Admin kontaktieren"), keine generische Fehlermeldung

**Given** ein verbundenes Outlook-Konto
**When** der Worker seinen Sync-Zyklus ausführt
**Then** ruft er `OutlookCalendarProvider.FetchAllEvents` auf (voller Snapshot, kein Delta) und wendet dieselbe Upsert-/Dedup-Logik über `(PersonId, Provider, ProviderEventId)` an wie für Google (Story 2.1)

**Given** ein importierter Outlook-Serientermin
**When** der `OutlookCalendarProvider` ihn verarbeitet
**Then** wird er serverseitig pro Instanz expandiert, analog zu Google (Story 2.1), mit der providerseitigen Instanz-Event-ID als `ProviderEventId`

**Given** ein Outlook-Termin wird an der Quelle verschoben, geändert, gelöscht oder abgesagt
**When** der nächste Sync-Zyklus läuft
**Then** verhält sich das System identisch zu Google (Update statt Duplikat; Entfernen bei fehlendem Schlüssel)

**Given** ein importierter Outlook-Termin
**When** er gespeichert wird
**Then** erhält er denselben Verfügbarkeits-Status nach derselben `StatusHeuristicService`-Regel wie native und Google-Termine

**Given** Access-/Refresh-Tokens für Outlook
**When** sie gespeichert oder für einen API-Aufruf entschlüsselt werden
**Then** gilt dieselbe Verschlüsselungs-/Entschlüsselungsregel wie für Google (Story 2.1) — nie Klartext, Entschlüsselung nur unmittelbar vor dem Graph-API-Aufruf

**Given** der Sync für ein Outlook-Konto schlägt wiederholt fehl
**When** `ConsecutiveFailureCount` den Schwellenwert übersteigt
**Then** verhält sich die Anzeige identisch zu Google (Story 2.1) — Fehlerzustand, `aria-live`-Ankündigung, kein Blockieren anderer Konten

### Story 2.3: Admin → Sync-Übersicht

As a Admin (Dennis),
I want den Sync-Zustand aller Teammitglieder-Konten an einer Stelle sehen,
So that ich als Bus-Faktor-1-Betreiber einen ausbleibenden Sync bemerke, bevor jemand einen Termin verpasst.

**Acceptance Criteria:**

**Given** ich bin als Admin angemeldet
**When** ich auf den Navigationspunkt "Admin → Sync-Übersicht" klicke
**Then** sehe ich eine Tabelle mit einer Zeile pro Person/Provider-Kombination: Person, Provider, Status, letzter erfolgreicher Sync

**Given** ich bin als Member (nicht Admin) angemeldet
**When** ich die Navigation betrachte
**Then** ist der Punkt "Admin → Sync-Übersicht" schlicht nicht vorhanden — kein sichtbarer, aber gesperrter Link

**Given** ich bin als Member angemeldet
**When** ich den API-Endpoint der Sync-Übersicht direkt aufrufe (z. B. über die Browser-Konsole)
**Then** verweigert das Backend den Zugriff serverseitig (403) — die Rollenprüfung erfolgt serverseitig, ein fehlender Frontend-Nav-Punkt ersetzt sie nicht (AD-17)

**Given** ein Konto mit wiederholt fehlgeschlagenem Sync
**When** die Übersicht gerendert wird
**Then** hebt sich diese Zeile mit dem expliziten Fehlerzustand ab (nicht nur ein alter Zeitstempel, der als "ruhige Woche" missverstanden werden könnte)

**Given** ein Teammitglied hat gar kein Kalenderkonto verbunden
**When** die Übersicht gerendert wird
**Then** erscheint die Person mit einem klar erkennbaren "nicht verbunden"-Zustand statt einer Fehlerzeile

## Epic 3: Team-Sichtbarkeit & Privat-Default (Mehrpersonen-Ansicht)

Ein Teammitglied kann Kollegen auswählen und deren Kalender nebeneinander sehen — mit "privat/beschäftigt" + Verfügbarkeits-Status statt echtem Titel, außer es ist selbst als Teilnehmer eingetragen. Das realisiert den Kernnutzen des Tools (UJ-1: auf einen Blick sehen, ob jemand ansprechbar ist).

### Story 3.1: Personen-Selektor & Mehrpersonen-Ansicht Tag/Woche mit Privat-Default

As a Teammitglied,
I want eine Teilmenge meiner Kollegen auswählen und deren Verfügbarkeit nebeneinander sehen, ohne ihre echten Termindetails einzusehen,
So that ich in Sekunden erkenne, ob jemand ansprechbar ist, ohne nachzufragen oder ihre Privatsphäre zu verletzen.

**Acceptance Criteria:**

**Given** ich bin in der Tages- oder Wochenansicht
**When** ich auf den "+"-Chip des Personen-Selektors klicke (oder ihn per `Enter`/`Space` aktiviere)
**Then** öffnet sich ein durchsuchbares Dropdown mit der Team-Roster-Liste; Tippen filtert in Echtzeit, Pfeiltasten navigieren durch die gefilterten Treffer, `Enter` fügt die fokussierte Person als neuen Avatar-Chip hinzu und hält das Dropdown offen, `Esc` schließt es ohne Auswahl und gibt den Fokus an den "+"-Chip zurück

**Given** ich habe mehrere Kollegen ausgewählt
**When** die Mehrpersonen-Ansicht rendert
**Then** erscheinen ihre Kalender nebeneinander in Spalten (nie überlagert), meine eigene Spalte zuerst/angeheftet, mit unbegrenzter Anzahl an Spalten — die Reihe scrollt horizontal statt eine Obergrenze zu erzwingen

**Given** eine fremde Spalte (ich bin weder Eigentümer noch Teilnehmer eines dort gezeigten Termins)
**When** die Spalte rendert
**Then** zeigt jeder Zeitblock ausschließlich einen Status-Block ("privat/beschäftigt" + Verfügbarkeits-Status) — nie Titel, Teilnehmer oder Ort, unabhängig davon ob der Termin nativ oder synchronisiert ist

**Given** ich bin selbst als Teilnehmer in einem fremden nativen oder synchronisierten Termin eingetragen
**When** ich diesen Termin in der Kollegen-Spalte anklicke
**Then** öffnet sich das Detail-Popover im Vollmodus (Titel, Zeit, Teilnehmer, Ort) — die Teilnehmer-Ausnahme aus FR-9 gilt unabhängig von der Terminquelle

**Given** ich bin weder Eigentümer noch Teilnehmer eines fremden Termins
**When** ich ihn anklicke
**Then** öffnet sich das Detail-Popover im Status-only-Modus mit explizitem Datenschutzhinweis ("Details sind privat — nur der Status ist sichtbar."), niemals eine leere oder wie defekt wirkende Karte

**Given** die Filterung nach Eigentümerschaft/Teilnahme
**When** ein beliebiger API-Endpoint Termindaten für einen Betrachter ausliefert, der weder Eigentümer noch Teilnehmer ist
**Then** liefert die API die vollen Felder (Titel, Teilnehmer, Ort) zu keinem Zeitpunkt aus — die Filterung läuft serverseitig durch `AppointmentViewService`/`GetForViewers`, eine rein clientseitige Ausblendung erfüllt die Anforderung nicht

**Given** die Mehrpersonen-Ansicht für mehrere ausgewählte Personen gleichzeitig
**When** Termine für alle Spalten geladen werden
**Then** verwendet das System die Bulk-Methode `AppointmentViewService.GetForViewers(personIds, range, viewerId)` — kein zweiter Codepfad reimplementiert die Privat-Default-Regel eigenständig für "mehrere Personen gleichzeitig"

**Given** ich wähle keine Kollegen aus
**When** ich die Mehrpersonen-Ansicht öffne
**Then** rendert meine eigene Spalte weiterhin vollständig, der verbleibende Raum zeigt den "+"-Chip mit einladendem Text ("Wähle Teammitglieder, um ihre Verfügbarkeit zu sehen."), keine leere Fläche

**Given** ich wechsle zwischen Tages- und Wochenansicht
**When** ich vorher Kollegen ausgewählt hatte
**Then** bleibt die Auswahl erhalten

**Given** ich klicke auf das "×" an einem Avatar-Chip
**When** die Aktion ausgeführt wird
**Then** wird die Person sofort ohne Bestätigungsdialog aus der Auswahl entfernt

**Given** ich nutze ein mobiles Endgerät mit vielen ausgewählten Spalten
**When** ich die Mehrpersonen-Ansicht öffne
**Then** funktioniert horizontales Scrollen identisch zur Desktop-Ansicht, keine Obergrenze für die Auswahl

### Story 3.2: Mehrpersonen-Monatsansicht mit Aggregat-Popover

As a Teammitglied,
I want in der Monatsansicht auf einen Blick sehen, an welchen Tagen ausgewählte Kollegen "Bitte nicht stören" haben,
So that ich Kollisionen und ungünstige Tage für Anfragen früh erkenne, ohne jeden Tag einzeln zu öffnen.

**Acceptance Criteria:**

**Given** ich habe Kollegen ausgewählt und befinde mich in der Monatsansicht
**When** ein Tag rendert, an dem mindestens ein ausgewählter Kollege irgendwann "Bitte nicht stören" ist
**Then** zeigt die Tageszelle einen dezenten Aggregat-Marker — keine einzelnen Punkte/Balken pro Person, keine überladene Zelle

**Given** eine Tageszelle ohne "Bitte nicht stören"-Kollegen
**When** sie rendert
**Then** bleibt sie visuell sauber ohne jegliche Marker

**Given** eine Tageszelle mit Aggregat-Marker
**When** ich sie anklicke oder per Tastatur (`Enter`/`Space` auf fokussierter Zelle) aktiviere
**Then** öffnet sich ein Popover mit Name + Status-Badge jedes ausgewählten Teammitglieds für diesen Tag

**Given** dasselbe Popover
**When** ich es auf einem Touch-Gerät öffne
**Then** ist das Verhalten identisch zum Pointer-Gerät (Tap statt Klick, kein Hover-Vorschau-Unterschied)

**Given** das Popover ist geöffnet
**When** ich `Esc` drücke oder außerhalb klicke
**Then** schließt es sich und der Fokus kehrt zur auslösenden Tageszelle zurück

## Epic 4: Eigenen Verfügbarkeits-Status manuell steuern

Ein Teammitglied kann seinen aktuell angezeigten Verfügbarkeits-Status jederzeit manuell übersteuern (Unterbrechbar / Bitte nicht stören / Automatisch) — sichtbar für Kollegen ohne Unterscheidung zu einem automatisch abgeleiteten Status.

### Story 4.1: Status-Override setzen und zurücksetzen

As a Teammitglied,
I want meinen aktuellen Verfügbarkeits-Status jederzeit manuell setzen können,
So that ich Kollegen korrekt informiere, auch wenn die automatische Heuristik gerade danebenliegt.

**Acceptance Criteria:**

**Given** ich bin in meiner eigenen Spaltenkopfzeile (beliebige Ansicht)
**When** die Ansicht rendert
**Then** ist das Icon-Button "Status ändern" immer sichtbar, unabhängig vom aktuell abgeleiteten Status

**Given** ich klicke auf "Status ändern" (oder aktiviere es per `Enter`/`Space`)
**When** das Flyout öffnet
**Then** zeigt es genau drei Optionen — Unterbrechbar / Bitte nicht stören / Automatisch — mit der aktiven Wahl als "AKTIV" markiert; der Fokus wird in das Flyout verschoben und beim Tab-Zyklus darin gefangen

**Given** ich wähle "Unterbrechbar" oder "Bitte nicht stören"
**When** die Auswahl gespeichert wird
**Then** wird ein `StatusOverride` mit `PersonId`, `Status`, `SetAt` angelegt/aktualisiert (kein Bezug zu einer Termin-Instanz, kein Ablaufzeitpunkt) und bleibt bestehen, bis ich ihn aktiv ändere — er endet nicht automatisch mit dem Ende eines zum Zeitpunkt der Übersteuerung aktiven Termins und wird nicht vom nächsten Sync-/Heuristik-Zyklus überschrieben

**Given** ich wähle "Automatisch"
**When** die Auswahl gespeichert wird
**Then** wird mein `StatusOverride.Status` auf `null` gesetzt und die Kontrolle kehrt vollständig zur Heuristik (FR-10) zurück

**Given** `CurrentStatusService.GetCurrentStatus(personId, now)` wird aufgerufen
**When** ein Override gesetzt ist
**Then** hat der Override Vorrang vor dem vorausberechneten Status eines gerade aktiven Termins; ist kein Override gesetzt, gilt der Status des aktiven Termins, sonst `Unterbrechbar` als Default — kein UI-Pfad berechnet "der aktuelle Status von Person X" eigenständig

**Given** für dieselbe Person überlappen sich zwei aktive Termine mit unterschiedlichem abgeleitetem Status und kein Override ist gesetzt
**When** der aktuelle Status berechnet wird
**Then** gilt der strengere Wert (`BitteNichtStoeren` sticht `Unterbrechbar`)

**Given** ein automatisch abgeleiteter und ein manuell gesetzter Status
**When** ein Kollege sie in seiner Mehrpersonen-Ansicht betrachtet
**Then** sind beide optisch und textlich absolut identisch — kein "Auto"- oder "Manuell"-Kennzeichen ist jemals sichtbar

**Given** das Status-Override-Flyout
**When** ich `Esc` drücke oder außerhalb klicke
**Then** schließt es sich ohne Änderung und der Fokus kehrt zum "Status ändern"-Button zurück

## Epic 5: Team-Verwaltung & Betrieb (Deprovisionierung, Backup)

Dennis kann als Admin ein ausscheidendes Teammitglied deaktivieren (Login/Sync/Personenauswahl sofort gesperrt, Daten bleiben erhalten) und verlässt sich auf ein tägliches automatisiertes Backup nativer Termine mit dokumentiertem, manuellem Restore-Prozess.

### Story 5.1: Teammitglied deaktivieren (Soft-Deprovisionierung)

As a Admin (Dennis),
I want ein ausscheidendes Teammitglied deaktivieren können,
So that dessen Zugriff und Sync sofort gestoppt werden, ohne Terminhistorien oder Teilnahmen anderer zu beschädigen.

**Acceptance Criteria:**

**Given** ich bin als Admin angemeldet
**When** ich ein Teammitglied in der Team-Verwaltung deaktiviere
**Then** wird `Person.IsActive = false` gesetzt, der Login dieser Person ist ab sofort gesperrt, alle zugehörigen OAuth-Tokens werden gelöscht/widerrufen, und die Person verschwindet aus dem Personen-Selektor (FR-2)

**Given** eine deaktivierte Person hatte native Termine, Teilnahmen an fremden Terminen, Attendee-Einträge oder einen gesetzten `StatusOverride`
**When** die Deaktivierung ausgeführt wird
**Then** bleiben all diese Daten unverändert erhalten — kein Hard-Delete, keine Kaskadenlöschung

**Given** ein laufender Sync-Zyklus für eine Person, die währenddessen deaktiviert wird
**When** der Worker den aktuellen Zyklus abschließt
**Then** darf dieser noch zu Ende laufen; erst der nächste Zyklus prüft `IsActive` erneut und überspringt die Person

**Given** ich versuche, die letzte verbleibende Admin-Person zu deaktivieren
**When** ich die Aktion ausführe
**Then** verweigert das System dies (siehe Story 1.1, Bus-Faktor-1-Schutz)

**Given** eine deaktivierte Person versucht sich anzumelden
**When** sie gültige, aber zu einem deaktivierten Account gehörende Zugangsdaten eingibt
**Then** wird der Login-Versuch abgelehnt

### Story 5.2: Automatisiertes Backup & Restore-Runbook

As a Betreiber (Dennis),
I want native Termine täglich automatisch gesichert wissen und einen dokumentierten Weg haben, sie im Ernstfall wiederherzustellen,
So that ein Datenbankfehler oder ein versehentliches Löschen nicht zum unwiederbringlichen Verlust nativer Termine führt.

**Acceptance Criteria:**

**Given** der Docker-Compose-Stack läuft produktiv
**When** der Backup-Sidecar-Container seinen täglichen Cron-Zyklus ausführt
**Then** erstellt er ein `pg_dump`-Backup der Postgres-Datenbank und legt es auf einem Docker-Volume ab

**Given** mehrere Tage an Backups wurden erstellt
**When** die Aufbewahrungsfrist geprüft wird
**Then** werden Backups älter als 14 Tage rollierend entfernt, maximal 14 Tage bleiben vorrätig

**Given** ein Datenverlust-Ernstfall tritt ein
**When** ich als Betreiber wiederherstellen muss
**Then** existiert ein dokumentiertes Runbook im `deploy/`-Verzeichnis mit den konkreten Schritten (Postgres-Container stoppen, `pg_restore`/`psql` gegen die zuletzt funktionierende Dump-Datei ausführen, Container neu starten) — Restore ist ein manueller, kein automatisierter Vorgang

**Given** der Backup-Sidecar-Container
**When** ein Backup-Lauf fehlschlägt (z. B. Volume voll)
**Then** wird dies über die bestehende strukturierte Logging-Konvention (Serilog nach stdout) sichtbar, kein stiller Fehlschlag ohne jede Spur
