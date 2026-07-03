---
id: SPEC-calendar-neu-bmad
companions:
  - functional-requirements.md
  - glossary.md
  - ../../planning-artifacts/architecture/architecture-calendar-neu-bmad-2026-07-02/ARCHITECTURE-SPINE.md
  - ../../planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/DESIGN.md
  - ../../planning-artifacts/ux-designs/ux-calendar-neu-bmad-2026-07-02/EXPERIENCE.md
sources:
  - ../../planning-artifacts/briefs/brief-calendar-neu-bmad-2026-07-02/brief.md
  - ../../planning-artifacts/briefs/brief-calendar-neu-bmad-2026-07-02/addendum.md
  - ../../planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/prd.md
  - ../../planning-artifacts/prds/prd-calendar-neu-bmad-2026-07-02/addendum.md
---

> **Kanonischer Vertrag.** Dieses SPEC und die in `companions:` gelisteten Dateien sind der vollständige, preservation-validierte Vertrag für das, was gebaut, getestet und abgenommen wird. Die in `sources:` gelisteten Dokumente dienen nur der Nachvollziehbarkeit — bei Bedarf für erzählerischen Kontext, den dieser Vertrag bewusst weglässt.

# Team-Terminkalender mit Synchronisation

## Why

Im Team (5-10 feste Personen, Mix aus Outlook- und Google-Kalendern) fehlt eine gemeinsame Sicht auf Verfügbarkeit: Kollegen müssen fremde Kalender öffnen oder direkt nachfragen, ob gerade ein guter Moment ist, und ein gemeinsamer freier Slot für mehrere Personen bedeutet, mehrere Kalender von Hand zu vergleichen. Dennis (Product Owner, Entwickler und Betreiber in Personalunion) baut dafür einen selbst gehosteten Prototyp: eine vereinte Kalenderansicht plus einen Status, der mehr sagt als frei/beschäftigt — unterbrechbar vs. bitte-nicht-stören — automatisch aus echten Kalenderdaten abgeleitet. Erfolg für diesen ersten Prototyp ist verhaltensbezogen: Das Tool wird täglich statt Outlook direkt geöffnet, und neue Termine entstehen ab jetzt dort statt in Outlook/Google.

## Capabilities

- **CAP-1**
  - **intent:** Ein Teammitglied kann den eigenen Kalender (native + synchronisierte Termine) in Monats-, Wochen- und Tagesansicht sehen und verlustfrei zwischen den Ansichten wechseln (FR-1).
  - **success:** Alle drei Ansichten zeigen beide Terminarten des eingeloggten Nutzers; der Wechsel erfolgt ohne Neuladen der Seite.

- **CAP-2**
  - **intent:** Ein Teammitglied wählt eine Teilmenge der übrigen Teammitglieder aus und sieht deren Verfügbarkeit nebeneinander — als Spalten in Tages-/Wochenansicht, als Tages-Aggregat-Marker mit Detail-Popover in der Monatsansicht — nie überlagert. FR-2 selbst nennt nur Tag/Woche; die Erweiterung auf die Monatsansicht ist eine spätere, bereits gemockte UX-Entscheidung (siehe `EXPERIENCE.md`, `DESIGN.md`) und fester Bestandteil dieser Capability.
  - **success:** Fremde Spalten/Marker zeigen ausschließlich "privat/beschäftigt" + Status (siehe CAP-5), nie den echten Titel; die Personenauswahl bleibt über Tages-, Wochen- und Monatsansicht hinweg erhalten.

- **CAP-3**
  - **intent:** Ein Teammitglied legt einen nativen Termin (Titel, Zeit, Dauer, optionale Teilnehmer) an und öffnet eigene Termine per Klick zur vollen Detailansicht (FR-3, FR-4).
  - **success:** Ein gespeicherter Termin erscheint sofort in der eigenen Ansicht und erhält automatisch einen Status (siehe CAP-6); die eigene Detailansicht zeigt immer volle Daten, unabhängig vom Privat-Default.

- **CAP-4**
  - **intent:** Das System importiert regelmäßig, einseitig und nie zurückschreibend alle Kalendertermine jedes verbundenen Outlook- und Google-Kontos (FR-5, FR-6, FR-7).
  - **success:** Ein neuer/geänderter/gelöschter Termin an der Quelle spiegelt sich spätestens im nächsten Sync-Zyklus (Zielgröße: wenige Minuten) im Tool, dedupliziert über eine stabile Provider-Event-ID; keine Schreiboperation geht je gegen die Outlook- oder Google-API.

- **CAP-5**
  - **intent:** Das System liest intern volle Termindaten, zeigt anderen Teammitgliedern gegenüber aber für jeden fremden Termin standardmäßig nur "privat/beschäftigt" + Status (FR-8, FR-9).
  - **success:** Kein API-Response liefert Titel, Teilnehmer oder Ort eines fremden Termins an einen Betrachter, der weder Eigentümer noch eingetragener Teilnehmer ist; ein eingetragener Teilnehmer sieht volle Details auch ohne Eigentümer zu sein.

- **CAP-6**
  - **intent:** Jeder Termin erhält automatisch einen Verfügbarkeits-Status (unterbrechbar/bitte-nicht-stören) aus Dauer, Teilnehmerzahl und Tageszeit; ein Teammitglied kann seinen eigenen aktuellen Status jederzeit manuell übersteuern (FR-10, FR-11).
  - **success:** Die Klassifikation folgt der in `functional-requirements.md` (FR-10) fixierten Heuristik; ein Override bleibt bestehen, bis er aktiv geändert wird, übersteht Sync-Zyklen und Terminende, und ist optisch nicht von einem automatischen Status unterscheidbar.

- **CAP-7**
  - **intent:** Die Oberfläche zeigt pro Kalenderverbindung sichtbar, wann zuletzt erfolgreich synchronisiert wurde, und macht einen wiederholt fehlschlagenden Sync für Betroffene und Betreiber sichtbar statt still zu scheitern.
  - **success:** Jede Verbindung zeigt einen Zeitstempel des letzten erfolgreichen Syncs; nach wiederholtem Fehlschlag wechselt die Anzeige in einen expliziten Fehlerzustand, sichtbar sowohl für den betroffenen Nutzer als auch in der Admin-Sync-Übersicht.

- **CAP-8**
  - **intent:** Nur angemeldete Teammitglieder erreichen Kalenderdaten; Konten werden ausschließlich von einem Admin angelegt (kein Self-Signup).
  - **success:** Keine Kalenderdaten sind ohne aktive Sitzung erreichbar; gespeicherte OAuth-Tokens liegen verschlüsselt vor — ein reiner Datenbankzugriff genügt nicht für Kalenderzugriff.

- **CAP-9**
  - **intent:** Native Termine — die einzigen Daten, die bei Verlust nicht aus Outlook/Google wiederherstellbar sind — sind durch eine grundlegende Backup-/Wiederherstellungsfähigkeit geschützt.
  - **success:** Ein tägliches Backup existiert; ein dokumentierter, ausführbarer Restore-Vorgang kann native Termindaten nach Verlust zurückbringen.

## Constraints

- Privat-Default ist eine reine Anzeigeregel, serverseitig pro Anfrage anhand der Anfrager-Identität durchgesetzt — rein clientseitige Filterung erfüllt sie nicht (FR-9).
- Beide Provider werden über die volle Kalender-API (nicht nur Frei/Busy) angebunden, damit die Status-Heuristik auch für synchronisierte Termine funktioniert — Kehrseite: Namen/E-Mails externer Teilnehmer aus importierten Kundenterminen landen ungeschützt im Backend (akzeptiertes Risiko, siehe Open Questions).
- Sync bleibt dauerhaft einseitig (nur Import) mit einem Intervall im Minutenbereich — kein Echtzeit-Anspruch, kein Zurückschreiben nach Outlook/Google, unter keinen Umständen.
- Weil native Termine nie zurückgeschrieben werden, sind sie außerhalb des Tools unsichtbar: Wer die Verfügbarkeit eines Teammitglieds direkt in dessen Outlook/Google prüft (ein Kollege ohne Tool-Zugang, ein externer Buchungslink, eine Kundin), kann in einen bereits belegten nativen Termin hineinbuchen. Eigenständiges, akzeptiertes Restrisiko — zu unterscheiden vom Non-Goal "keine aktive Kollisionswarnung im Tool", das nur Kollisionen zwischen im Tool sichtbaren Terminen betrifft.
- Google-OAuth läuft als "Internal"-App innerhalb einer gemeinsamen Workspace-Organisation (umgeht Verifizierung/CASA); Microsoft Graph `Calendars.Read` kann je nach Tenant Admin-Consent erfordern, den ein Tenant-Admin organisationsweit sperren kann — beide Punkte liegen außerhalb der Kontrolle des Teams.
- Alle Kernfunktionen (Ansichten, Termin anlegen, Status, Override) müssen im mobilen Browser nutzbar sein — kein natives App, nur Responsive-Web.
- Selbst gehostet, ein Betreiber (Bus-Faktor 1), eine Umgebung (kein Staging) — kein Hochverfügbarkeits- oder Recovery-Zeit-Anspruch über die native-Termine-Backup-Pflicht (CAP-9) hinaus.
- Erfolg bemisst sich am tatsächlichen Verhalten (Termine entstehen im Tool, tägliche Nutzung), nicht an reinen Lesezugriffen — hohe Nutzung bei weiterhin extern angelegten Terminen ist ein Warnsignal, kein Erfolg (Counter-Metric, siehe Success signal).

## Non-goals

- Kein delegierter/stellvertretender Zugriff — niemand legt Termine im Namen eines anderen Teammitglieds an.
- Keine Unterstützung mehrerer Teams oder Projektgruppen — das Tool ist für genau ein festes Team gebaut.
- Kein Echtzeit-Sync und kein Zurückschreiben nach Outlook/Google (dauerhaft, siehe Constraints).
- Keine aktive Kollisionswarnung beim Anlegen eines Termins — gemeinsame Sichtbarkeit macht Kollisionen sichtbar, das Tool warnt aber nicht proaktiv davor.
- Kein Mechanismus, der erzwingt oder incentiviert, dass Termine tatsächlich im Tool statt weiter in Outlook/Google angelegt werden — Fragmentierung ist ein akzeptiertes Verhaltensrisiko, beobachtet über die Counter-Metric (siehe Success signal).
- Kein Benachrichtigungs- oder Einladungsversand an externe Teilnehmer eines nativen Termins.
- Keine Bearbeitung synchronisierter (nicht-nativer) Termine im Tool — sie sind nur lesend dargestellt; jede Änderung erfolgt weiterhin an der Quelle.
- Verschoben auf nach dem MVP (Should-Have, bereits UX-/Architektur-seitig skizziert, siehe Companions und `functional-requirements.md`): gemeinsamer Slot-Finder (FR-12), Termin-Erinnerungen (FR-13), native Serientermine (FR-14), Ortsangabe mit Karte (FR-15).
- Nicht in dieser Runde geplant (Could-Have, unscoped): Farbcodierung nach Quelle, Notizfeld pro Termin, Schnellsuche über Termine/Orte/Personen, Anfahrtszeit-Schätzung, Abwesenheiten/Urlaub als eigene Kalender-Kategorie.

## Success signal

Primär: Das Tool wird im Alltag tatsächlich täglich geöffnet, um Verfügbarkeit zu prüfen, statt Outlook direkt zu öffnen — und neue Termine entstehen dort statt in Outlook/Google. Sekundär, als unterstützende Signale: der Verfügbarkeits-Check fühlt sich spürbar schneller an als vorheriges Nachfragen, und Doppelbuchungen werden seltener, weil Kollisionen durch die gemeinsame Sichtbarkeit vor dem Anlegen auffallen. Counter-Signal: ein hoher Anteil weiterhin extern (in Outlook/Google) angelegter Termine trotz täglicher Tool-Nutzung zeigt, dass das Tool nur zum Lesen dient — das zählt nicht als Erfolg. Revisit-Anlass: dieses Counter-Signal nach den ersten Wochen echter Alltagsnutzung aktiv gegenprüfen — ohne einen definierten Prüfzeitpunkt könnte ein hoher SM-1-Wert (Nutzungshäufigkeit) monatelang unbemerkt einen falschen Erfolgseindruck erzeugen, während der eigentliche Verhaltenswechsel ausbleibt.

## Assumptions

- Das Team nutzt eine gemeinsame Google-Workspace-Organisation (Voraussetzung für den Internal-App-Typ ohne Verifizierung/CASA-Prüfung).
- Die numerischen Heuristik-Schwellenwerte sind final (Architektur AD-5: ≤45 Min. mit ≥1 Teilnehmer → unterbrechbar; ganztägig ODER ≥90 Min. mit ≥3 Teilnehmern → bitte-nicht-stören; sonst unterbrechbar) — ursprünglich im PRD nur als Richtwert markiert, in der Architekturphase fixiert.
- Alle Teammitglieder befinden sich in derselben Zeitzone; die Mehrpersonen-Ansicht hat keine Zeitzonen-Achse (Revisit-Anlass: geografisch verteiltes Team).
- Jede Person nutzt genau ein Kalenderkonto (nicht parallel ein privates Google- und ein dienstliches Outlook-Konto).
- Zeitumstellungen (Sommer-/Winterzeit) werden von der Kalender-Infrastruktur der Provider korrekt gehandhabt.
- Deprovisionierung ausscheidender Teammitglieder ist architekturseitig gelöst (AD-12: Soft-Deaktivierung — Login/Sync sofort gestoppt, native Termine und Teilnahmen bleiben erhalten).
- Sichtbarkeit eines vom Nutzer selbst widerrufenen Kalenderzugriffs läuft über denselben Sync-Fehler-Mechanismus wie jeder andere Sync-Ausfall (Architektur AD-16) — kein separates Signal nötig.

## Open Questions

- Nutzt das Team tatsächlich eine gemeinsame Google-Workspace-Organisation? Falls nicht, wird bei Google eine zusätzliche Verifizierungsphase nötig.
- Lässt die Microsoft-Tenant-Konfiguration der Teammitglieder Admin-Consent für `Calendars.Read` zu, oder ist Self-Consent organisationsweit gesperrt? Betrifft die Onboarding-Fähigkeit neuer Teammitglieder.
- Externe Teilnehmerdaten (Namen/E-Mails aus importierten Kundenterminen) haben keine Aufbewahrungs-/Löschregel — zu klären, sobald ein Kundentermin mit sensiblen Daten regelmäßig vorkommt, ein weniger vertrauenswürdiges Teammitglied hinzukommt, oder das Hosting-Modell wechselt.
- Der manuelle Status-Override (FR-11) hat kein Ablaufdatum und ist für andere nicht von einem automatischen Status unterscheidbar — ein vergessener Override (z. B. vor einem Urlaub gesetzt) kann das Vertrauenssignal unbemerkt verfälschen. Ist eine spätere Staleness-Absicherung (z. B. ein nur für den Nutzer selbst sichtbarer "seit wann gesetzt"-Hinweis) sinnvoll, ohne FR-11s Vorgabe zu brechen, dass andere keinen Unterschied zwischen Auto und Override sehen dürfen?
