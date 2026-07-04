# Epic 2 — Offene Fragen / Anmerkungen für Dennis

Gesammelt während der autonomen Umsetzung von Epic 2 (Kalender-Synchronisation & Sync-Transparenz:
Story 2.1 Google, Story 2.2 Outlook, Story 2.3 Admin-Sync-Übersicht).
Format: Frage/Anmerkung, getroffene Annahme (falls weitergearbeitet wurde), Kontext.

**Status:** Alle Punkte unten wurden mit Dennis durchgesprochen (2026-07-04). Entscheidungen und
Umsetzung sind vermerkt.

---

## 1. Externe OAuth-App-Registrierungen fehlen real — ✅ Anleitung erstellt

- **Google**: `GOOGLE_OAUTH_CLIENT_ID`/`SECRET`/`REDIRECT_URI` in `deploy/.env.example` sind Platzhalter. Erfordert eine echte OAuth-2.0-Client-Registrierung ("Web application") in der Google Cloud Console mit Scope `https://www.googleapis.com/auth/calendar.readonly`.
- **Microsoft/Outlook**: `MICROSOFT_OAUTH_CLIENT_ID`/`SECRET`/`REDIRECT_URI`/`TENANT_ID` ebenfalls Platzhalter. Erfordert eine App-Registrierung in Azure AD / Microsoft Entra admin center mit der delegierten Berechtigung `Calendars.Read`.
- **Entscheidung**: Dennis macht die Registrierung selbst, wollte aber eine Schritt-für-Schritt-Anleitung. → **`deploy/oauth-setup.md`** erstellt (Google Cloud Console + Azure AD, inkl. Troubleshooting-Abschnitt für die häufigsten Fehler).
- Der komplette OAuth-Handshake- und Sync-Code ist implementiert und durch Unit-/Integrationstests abgedeckt (ohne echte Netzwerkaufrufe), aber noch nicht gegen echte Google-/Microsoft-Konten verifiziert — das bleibt ein manueller Schritt nach der Registrierung.

## 2. Externes Attendee-Schema (Story 2.1) — ✅ Matching gegen Team-Roster ergänzt

Die bestehende `Attendee`-Entität (Story 1.3) war zwingend an `Person` gebunden (nur interne Teammitglieder-Auswahl bei nativen Terminen). Importierte Google-/Outlook-Termine haben aber beliebige externe E-Mail-Teilnehmer ohne `Person`-Zeile.

**Entscheidung**: Matching ergänzt. `CalendarSyncService` gleicht jeden synchronisierten Teilnehmer gegen die Personen-Tabelle ab (case-insensitiv per E-Mail); bei Treffer wird der Teilnehmer als echtes Teammitglied verknüpft (`Attendee.PersonId`), sonst weiterhin als extern (`ExternalEmail`/`ExternalDisplayName`) gespeichert. Ein Teammitglied, das nach dem ersten Sync zum Roster hinzukommt, wird beim nächsten Sync-Zyklus automatisch nachträglich verknüpft (kein manueller Nachzieh-Schritt nötig). Relevant für Epic 3 (Mehrpersonen-Ansicht), das jetzt echte Personen-Referenzen für gematchte externe Termine bekommt.

## 3. CalendarConnection existiert schon vor erfolgreicher Verbindung (Story 2.1) — Architekturentscheidung, keine offene Frage

Um AC 10 zu erfüllen (persistenter Fehlerzustand für "nie erfolgreich verbunden, Consent abgelehnt"), wird die `CalendarConnection`-Zeile bereits beim ersten "Verbinden"-Klick angelegt — unabhängig vom Ergebnis. `EncryptedAccessToken`/`EncryptedRefreshToken` bleiben `null`, bis der erste Handshake erfolgreich war (`IsConnected` = `EncryptedAccessToken != null`).

## 4. Nicht spezifizierte Zahlenwerte — ✅ Defaults bestätigt, keine Änderung

- **Sync-Intervall**: 5 Minuten (`SYNC_INTERVAL_SECONDS`, konfigurierbar).
- **Sync-Zeitfenster**: 30 Tage zurück / 180 Tage voraus.
- **Schwellenwert für "wiederholt fehlgeschlagen"**: 3 aufeinanderfolgende Fehlversuche.
- Dennis hat die Defaults bestätigt — keine Änderung nötig. Alle drei Werte sind zentral an einer Stelle im Code definiert, falls sich das später ändern soll.

## 5. Microsoft-spezifische Details (Story 2.2) — ✅ "common" bestätigt

- **Tenant-Wert `"common"`**: Dennis bleibt bei "common" (erlaubt Consent von jedem Microsoft-Entra-Tenant) — keine Änderung.
- **Fehlercode-Mapping für Tenant-Blockierung**: Microsofts Fehler-Rückgabe für "vom Tenant-Admin blockiert" ist weniger eindeutig als Googles `admin_policy_enforced` (kommt oft als `AADSTS`-Code im Freitext). Aktuell fällt das auf einen generischen `oauth_error`-Code zurück. Bleibt eine unverifizierte Annahme, bis sie an einem echten blockierten Tenant getestet werden kann (kein Blocker, nur eine etwas weniger spezifische Fehlermeldung im Edge-Case).

## 6. "Verbindung trennen" (Disconnect) — ✅ jetzt gebaut

Ursprünglich zurückgestellt (kein AC verlangte es), auf Wunsch nachträglich ergänzt:
- Backend: `DELETE /api/calendar-connections/{provider}` — setzt die Verbindung zurück (`CalendarConnection.Disconnect()`) und entfernt alle über diese Verbindung importierten Termine (native Termine bleiben unberührt). Idempotent, unbekannter Provider → 400.
- Frontend: "Verbindung trennen"-Button in Einstellungen → Kalenderverbindungen, mit Bestätigungsdialog (da destruktiv — löscht importierte Termine).
- **Eager-Sync beim Verbinden bleibt bewusst nicht gebaut**: Nach dem OAuth-Handshake erscheinen Termine weiterhin erst nach dem nächsten Worker-Zyklus (bis zu 5 Min.), nicht sofort — bewusste Vereinfachung, kein offener Punkt.

## 7. Code-Review-Historie (zur Transparenz, bereits behoben)

Zwei automatisierte High-Effort-Code-Reviews liefen während der ursprünglichen Umsetzung. Alle als CONFIRMED eingestuften Findings wurden behoben und durch neue Tests abgesichert:

- **Story 2.1**: Datenverlust-Risiko bei nicht-transaktionalem Sync-Diff (behoben: DB-Transaktion), zwei Silent-Failure-Lücken in der Fehlerbehandlung (behoben: umfassenderes Exception-Handling), Case-sensitive E-Mail-Vergleich (behoben), sequenzieller statt paralleler Worker-Sync-Loop (behoben), toter Code im Frontend (entfernt).
- **Story 2.2/2.3 (zweiter Durchlauf)**: Ein echter Bug — Token-Refresh mitten im Sync-Zyklus hat den Fehler-Zähler zurückgesetzt, bevor der eigentliche Kalenderabruf überhaupt gelaufen war, wodurch eine andauernde Störung zeitweise als "gesund" angezeigt wurde (behoben: neue `UpdateTokensAfterRefresh`-Methode, die den Fehler-Zähler unangetastet lässt). Ein zweiter echter Bug — fehlender Vergleich des Teilnehmer-Anzeigenamens beim Sync-Diff, wodurch Namensänderungen bei externen Teilnehmern verschluckt wurden (behoben). Ein durch mich selbst eingeführter Frontend-Regressions-Bug — Angular-Listen-Tracking über eine nicht garantiert eindeutige E-Mail statt über einen stabilen Index (behoben). Mehrere Code-Duplizierungen bereinigt (OAuth-Token-Refresh-Logik, OAuth-POST-Logik, "Minuten seit Sync"-Hilfsfunktion im Frontend). **Wichtige Erkenntnis**: Ein vom Review vorgeschlagener "Parallelisierung"-Fix (`Task.WhenAll` für zwei DB-Abfragen im Admin-Endpoint) wurde testgetrieben als selbst fehlerhaft erkannt und zurückgenommen — `IPersonRepository`/`ICalendarConnectionRepository` teilen sich einen EF-Core-DbContext pro Request, der keine parallelen Abfragen erlaubt (führte zu einem intermittierenden 500er). Sequenziell bleibt hier die korrekte Lösung.

---

## Prozess-Notiz (kein inhaltliches Thema, nur für zukünftige Sessions relevant)

- Das Worktree für diese Session hatte sich zunächst versehentlich von `main`/Initial-Commit statt von `dev` abgezweigt (fehlte dadurch der komplette Epic-1-Code: `Attendee`, `AvailabilityStatus`, `StatusHeuristicService` etc.). Korrigiert per `git reset --hard origin/dev` bevor Code geschrieben wurde — keine Auswirkung auf den finalen Stand, aber als Hinweis für künftige Worktree-Sessions in diesem Repo festgehalten.
