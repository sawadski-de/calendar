---
title: Addendum — PRD Team-Terminkalender mit Synchronisation
related_prd: prd.md
created: 2026-07-02
updated: 2026-07-02
---

# Addendum

Ergänzendes Material aus der PRD-Discovery, das für Architektur/UX relevant ist, aber nicht in die PRD selbst gehört.

## Vergleichslandschaft (Recherche-Digest)

Kurze Einordnung, warum die Kombination aus Unified-Calendar-Sicht und abgeleitetem Kontext-Signal (Kernstück der PRD, FR-10) kein etabliertes Marktmuster kopiert:

- **Unified-Calendar-Kategorie ist etabliert**, aber auf Einzelnutzer ausgelegt: Vimcal, Notion Calendar (ex-Cron), Fantastical und Morgen führen Google + Outlook (+ iCloud) in einer Ansicht zusammen — jeweils für die eigene Kalenderübersicht einer einzelnen Person, nicht als geteilte Team-Sicht mit privacy-redigierter Verfügbarkeit anderer.
- **Clockwise** kommt der Status-Heuristik (FR-10) am nächsten: es leitet automatisch "Focus Time"-Blöcke her (heuristisch definiert über Blocklänge, z. B. 2+ Stunden ungestört, sonst 1 Stunde) und setzt bei 3+ Stunden Fokuszeit automatisch den Slack/Teams-Status auf "nicht stören". Der Unterschied: Clockwise optimiert den eigenen Kalender (Selbstplanung); es klassifiziert nicht bestehende Termine für die Anzeige gegenüber Kollegen.
- **Reclaim.ai** redigiert beim Kalender-Sync standardmäßig Termintitel zu einem generischen Label (z. B. "Busy"), um Detailinhalte beim Kopieren über Kalendergrenzen hinweg nicht preiszugeben — ein Präzedenzfall für den in FR-9 beschriebenen Privat-Default, wenn auch technisch anders gelöst (dort Redaktion auf Datenebene, hier eine reine Anzeigeregel bei vollem internen Datenzugriff).
- **Fazit:** Kein gefundenes Tool kombiniert "Cross-Provider-Zusammenführung" mit "an Kollegen ausgespieltem, abgeleitetem Interruptibility-Signal". Das ist die reale, wenn auch schmale Differenzierung des Prototyps — ohne dass daraus ein Marktanspruch abgeleitet werden sollte (dieses Projekt ist ein interner Prototyp, kein Launch).

## Herkunft der Heuristik-Richtwerte (FR-10)

Die in FR-10 genannten numerischen Richtwerte (kurz: bis ca. 30-45 Min.; bitte-nicht-stören: ab ca. 1,5-2h mit 3+ Teilnehmern oder ganztägig) wurden aus einer kleinen Beispiel-Klassifikation abgeleitet, die Dennis im Coaching-Gespräch vorgenommen hat:

| Beispiel | Dauer | Teilnehmer | Tageszeit | Eingestuft als |
|---|---|---|---|---|
| 1:1 | 30 Min | 1 | Kernzeit | unterbrechbar |
| Team-Meeting | 2h | 6 | Kernzeit | bitte-nicht-stören |
| Workshop/Offsite | ganztägig | mehrere | — | bitte-nicht-stören |
| Kurztermin außerhalb Kernzeit | 15 Min | 1 | Randzeit (7 Uhr) | unterbrechbar |
| Fokuszeit-Block ohne Teilnehmer | unspezifiziert, auch mehrstündig | 0 | Kernzeit | unterbrechbar |

Daraus abgeleitete Faustregel: Dauer ist der dominante Faktor; Teilnehmerzahl verstärkt nur bei bereits langer Dauer Richtung bitte-nicht-stören; ein Termin ganz ohne Teilnehmer bleibt unabhängig von der Dauer unterbrechbar; Tageszeit spielt praktisch keine Rolle. Die exakten Zahlenwerte (30-45 Min., 1,5-2h, 3+ Teilnehmer) sind Interpolationen aus diesen fünf Punkten, keine vom Nutzer explizit bestätigten Schwellenwerte — zur Kalibrierung in der Architekturphase erneut mit weiteren Beispielen prüfen.

## OAuth/API-Gotchas (Detail zu PRD Abschnitt 8)

Vollständige Recherche-Grundlage für die in der PRD unter "Integrations-Constraints" zusammengefassten Punkte:

- **Google**: `calendar.readonly` ist ein sensibler Scope; OAuth-Verifizierung + jährliche CASA-Sicherheitsprüfung entfällt, wenn die App als "Internal" innerhalb einer einzigen Google-Workspace-Organisation registriert ist. Push-Kanäle (Webhooks) laufen nach ca. 24h ab und müssen erneuert werden; Zustellung ist nicht garantiert. Abfrage-Kontingente werden pro Minute, pro Projekt und pro Nutzer durchgesetzt.
- **Microsoft Graph**: `Calendars.Read` benötigt je nach Tenant-Konfiguration Admin-Consent; Tenant-Admins können User-seitiges Self-Consent organisationsweit sperren, was das Onboarding neuer Teammitglieder blockieren würde. Change-Notification-Abonnements (Webhooks) haben Limits pro Ressource und keine Zeitgarantie für Zustellung (kann unter Last mehrere Minuten verzögern); GET-Aufrufe werden pro App und Postfach gedrosselt (ca. 4 Anfragen/Sek., Bursts bis 10k/10 Min.).
- Beide Provider erfordern faktisch einen Poll/Reconcile-Fallback neben oder statt Webhooks — das gewählte Sync-Intervall von wenigen Minuten (PRD FR-5/FR-6) ist somit keine vorläufige Krücke, sondern die naheliegende Lösung angesichts der fehlenden Zustellgarantien beider Provider.

Quellen (Stichproben, keine vollständige Zitation): getclockwise.com / support.getclockwise.com (Focus-Time-Heuristik); help.reclaim.ai (Sync-Redaktion); support.google.com/calendar (Frei/Busy-Freigabestufen); developers.google.com/identity/protocols/oauth2 (sensible/eingeschränkte Scopes, CASA); developers.google.com/workspace/calendar/api/guides/push und /quota; learn.microsoft.com/graph (Change-Notifications, Berechtigungen, Rate Limits); Feature-Vergleiche zu Vimcal/Notion Calendar/Fantastical/Morgen.
