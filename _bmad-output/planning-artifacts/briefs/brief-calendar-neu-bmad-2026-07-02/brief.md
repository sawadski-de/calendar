---
title: Team-Terminkalender mit Synchronisation
status: final
created: 2026-07-02
updated: 2026-07-02
---

# Product Brief: Team-Terminkalender mit Synchronisation

## Executive Summary

Ein Prototyp für ein eigenes, kleines Team-Kalender-Tool: Es führt die verteilten Kalender eines 5-10-köpfigen Teams (Mix aus Outlook und Google Calendar) in einer gemeinsamen Ansicht zusammen — nach dem Vorbild einer Unified Inbox für E-Mail-Konten. Statt in mehreren Kalendern nachzuschauen oder Kollegen zu fragen "hast du gerade Zeit?", zeigt das Tool auf einen Blick, wer verfügbar ist — nicht nur binär frei/beschäftigt, sondern mit Kontext (unterbrechbar vs. bitte-nicht-stören). Neue Termine werden künftig direkt im Tool angelegt; Outlook und Google bleiben über einseitigen Sync eingebunden, weil extern ausgelöste Termine (z. B. eine Kundeneinladung) immer dort zuerst entstehen. Erfolg für diesen ersten Prototyp: Das Tool wird täglich genutzt, statt Outlook direkt zu öffnen.

## The Problem

Im Team hat jede Person ihren eigenen Kalender — mal Outlook, mal Google Calendar — ohne gemeinsame Sicht. Die Verfügbarkeitsdaten existieren also längst, sind aber verstreut und nicht zusammengeführt. Das führt zu:

- Doppelbuchungen und Terminkollisionen, obwohl die Information zur Vermeidung eigentlich vorhanden wäre
- Zeitaufwändiger Suche nach einem gemeinsamen freien Slot für mehrere Personen
- Fehlende Sichtbarkeit, ob ein Kollege gerade verfügbar, beschäftigt oder in einem Zustand ist, der besser nicht unterbrochen wird — ohne den eigenen Kontext zu verlassen, um das erst nachzufragen

Der auslösende Moment: nicht sehen zu können, ob ein Kollege gerade verfügbar ist, und dadurch unnötig Koordinationsaufwand zu verursachen.

## The Solution

Ein Web-Tool (mobil/responsiv nutzbar, selbst gehostet), das die Kalender aller Teammitglieder per einseitigem Sync (nur Import, kein Zurückschreiben) aus Outlook und Google zusammenführt — Monats-, Wochen- und Tagesansicht wie ein gewohnter Kalender. Neue Termine werden ab jetzt direkt im Tool angelegt (mit Detailansicht per Klick); Outlook/Google bleiben die Quelle für alles, was von außen hereinkommt (z. B. Kundeneinladungen) oder versehentlich woanders eingetragen wird.

Das Kernstück: eine Verfügbarkeitsanzeige, die nicht nur frei/beschäftigt zeigt, sondern Kontext — unterbrechbar oder bitte-nicht-stören —, automatisch aus den Kalenderdaten abgeleitet über eine einfache Heuristik (Dauer, Tageszeit, Teilnehmerzahl), mit derselben Sync-Verzögerung von wenigen Minuten wie der übrige Kalender (kein Echtzeit-Signal). Das funktioniert für importierte wie für selbst angelegte Termine gleichermaßen, weil das Tool im Hintergrund die vollen Kalenderdaten liest, inklusive Titel und Teilnehmer — anderen Teammitgliedern gegenüber zeigt es aber standardmäßig nur "privat/beschäftigt" plus den abgeleiteten Kontext, nie den echten Titel oder Inhalt. Diese Trennung zwischen dem, was das Tool intern liest, und dem, was es anzeigt, ist die Grundlage des Privat-Defaults.

## What Makes This Different

Kein Wettbewerbsvorteil im Marktsinn nötig — das hier ist ein interner Prototyp, kein Produkt für den Markt. Der Unterschied zum Status quo: Weder Outlook noch Google Calendar bieten eine zusammengeführte Sicht über mehrere Konten/Anbieter hinweg, und keines der beiden leitet aus Kalenderdaten eine Dringlichkeits-/Kontext-Aussage ab — beide zeigen bestenfalls binäres Frei/Beschäftigt. Slack/Teams-Präsenz löst nur die Sichtbarkeit, nicht die Zusammenführung der Kalenderquellen. Die Kombination aus beidem (Unified View + abgeleiteter Kontext) ist das, was es gegenüber vorhandenen Tools rechtfertigt, statt einfach eine geteilte Google/Outlook-Kalenderfreigabe zu nutzen.

## Who This Serves

Ein festes, überschaubares Team von 5-10 Personen (inklusive Dennis selbst), das heute eine Mischung aus Outlook- und Google-Kalendern nutzt. Erfolg für die einzelne Person: schneller sehen, ob ein Kollege gerade ansprechbar ist, ohne nachzufragen oder einen fremden Kalender zu öffnen.

## Success Criteria

- **Primär:** Das Tool wird im Alltag tatsächlich genutzt — Termine werden dort angelegt statt in Outlook, und es wird regelmäßig statt Outlook direkt geöffnet, um Verfügbarkeit zu checken.
- Doppelbuchungen werden seltener, weil Kollisionen durch die gemeinsame Sichtbarkeit vor dem Anlegen auffallen — das Tool warnt nicht aktiv davor, macht die Kollision aber sichtbar.
- Der Verfügbarkeits-Check dauert spürbar kürzer als vorher (kein Nachfragen/Kalender-Wechseln mehr nötig).
- Sync-Aktualisierung innerhalb weniger Minuten ist für den Alltag ausreichend — kein Echtzeit-Anspruch.

## Scope

**Must Have**
- Monats-, Wochen- und Tagesansicht
- Eigene Termine anlegen, mit Detailansicht per Klick
- Einseitiger Sync (nur Import) von Outlook und Google Calendar, Aktualisierung alle paar Minuten
- Privat-Default für synchronisierte Termine: zeigt anderen nur "privat/beschäftigt" plus abgeleiteten Kontext, nie den echten Titel/Inhalt
- Kontext-bewusste Verfügbarkeit (unterbrechbar vs. bitte-nicht-stören), automatisch per Heuristik (Dauer/Tageszeit/Teilnehmerzahl) abgeleitet, mit derselben Sync-Verzögerung wie der übrige Kalender — gilt gleichermaßen für importierte wie selbst angelegte Termine
- Mobil/responsiv nutzbar
- Selbst gehostet

**Should Have**
- Gemeinsamer Slot-Finder: Teammitglieder auswählen, Tool schlägt automatisch freie Zeitfenster vor
- Benachrichtigungen (z. B. Termin-Erinnerung, Hinweis wenn ein wartender Kollege frei wird)
- Serientermine (wiederkehrende Termine)
- Ortsangabe mit Autovervollständigung, Kartenanzeige in der Detailansicht

**Could Have**
- Farbcodierung nach Quelle (Outlook/Google/manuell)
- Notizfeld pro Termin für team-interne Infos
- Schnellsuche über Termine/Orte/Personen
- Ort-oder-Video-Link mit automatischer Anfahrtszeit-Schätzung und Warnung bei knapper Taktung
- Abwesenheiten/Urlaub als eigene Kalender-Kategorie

**Won't Have (diese Runde)**
- Shared/Delegated-Zugriff (stellvertretend Termine für andere eintragen)
- Unterstützung mehrerer Teams/Projektgruppen
- Echtzeit-Sync
- Zurückschreiben nach Outlook/Google (Sync bleibt einseitig)

## Bekannte Risiken

Punkte, die dieser Brief bewusst nicht löst, aber für die nächste Phase (UX/Architektur) im Blick behalten werden sollten:

- **Fragmentierung ist eine Verhaltensabsicht, kein Feature:** Nichts im Scope erzwingt oder unterstützt, dass neue Termine tatsächlich im neuen Tool statt weiter in Outlook/Google angelegt werden.
- **Mehrpersonen-Ansicht ungeklärt:** Wie 5-10 Kalender gleichzeitig in Wochen-/Tagesansicht dargestellt werden (nebeneinander, überlagert, filterbar), ist offen — für den Kern-Use-Case "Team-Überblick" aber zentral.
- **Betrieb ohne Resilienz-Konzept:** Selbst gehostet, ohne Aussage zu Backup oder Ausfallverhalten; ein Betreiber (Bus-Faktor 1) für Wartung und OAuth-Token-Pflege.
- **Abhängigkeit von Google-/Microsoft-Berechtigungsmodellen:** Admin-Consent-Pflicht bei Firmenkonten und API-Rate-Limits sind nicht geprüft, obwohl der volle Calendar-API-Zugriff (siehe Addendum) technisch zwingend ist.
- **Scope-Spannung Benachrichtigungen vs. Sync-Intervall:** Ein "Hinweis, wenn ein wartender Kollege frei wird" (Should Have) verträgt sich schlecht mit einem Sync-Intervall von mehreren Minuten statt Echtzeit.
- **Datenschutz bei externen Terminteilnehmern:** Importierte Kundentermine bringen Namen/E-Mail-Adressen Externer ins Backend — bei Firmenkonten ungeprüft.

## Vision

Bewährt sich der Prototyp im Alltag, sind naheliegende nächste Schritte die Should-Have-Features (Slot-Finder, Benachrichtigungen) sowie ggf. eine Ausweitung auf weitere Teams oder eine Kontext-Anzeige außerhalb des Tools selbst (z. B. als Slack-Badge). Für den ersten Wurf zählt aber vor allem eines: ob es den täglichen Griff zu Outlook tatsächlich ersetzt.
