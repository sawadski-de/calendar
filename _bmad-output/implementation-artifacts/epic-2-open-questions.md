# Epic 2 — Offene Fragen / Anmerkungen für Dennis

Gesammelt während der autonomen Umsetzung von Epic 2 (Kalender-Synchronisation & Sync-Transparenz:
Story 2.1 Google, Story 2.2 Outlook, Story 2.3 Admin-Sync-Übersicht).
Format: Frage/Anmerkung, getroffene Annahme (falls weitergearbeitet wurde), Kontext.

---

## 1. Externe OAuth-App-Registrierungen fehlen real (blockiert nur echtes End-to-End-Testen, nicht den Code)

- **Google**: `GOOGLE_OAUTH_CLIENT_ID`/`SECRET`/`REDIRECT_URI` in `deploy/.env.example` sind Platzhalter. Erfordert eine echte OAuth-2.0-Client-Registrierung ("Web application") in der Google Cloud Console mit Scope `https://www.googleapis.com/auth/calendar.readonly`.
- **Microsoft/Outlook**: `MICROSOFT_OAUTH_CLIENT_ID`/`SECRET`/`REDIRECT_URI`/`TENANT_ID` ebenfalls Platzhalter. Erfordert eine App-Registrierung in Azure AD / Microsoft Entra admin center mit der delegierten Berechtigung `Calendars.Read`.
- Beide Provider brauchen zusätzlich einen echten `TOKEN_ENCRYPTION_KEY` (32-Byte-Base64, z. B. via `openssl rand -base64 32`) — der Platzhalterwert ist absichtlich ungültig, damit Api/Worker beim Start mit einer klaren Fehlermeldung abbrechen statt still falsch zu funktionieren.
- **Auswirkung**: Der komplette OAuth-Handshake- und Sync-Code ist implementiert und durch Unit-/Integrationstests abgedeckt (ohne echte Netzwerkaufrufe), aber nicht gegen echte Google-/Microsoft-Konten verifiziert. Das ist ein manueller Schritt für dich nach dem Merge.

## 2. `[ASSUMPTION]` Externes Attendee-Schema (Story 2.1)

Die bestehende `Attendee`-Entität (Story 1.3) war zwingend an `Person` gebunden (nur interne Teammitglieder-Auswahl bei nativen Terminen). Importierte Google-/Outlook-Termine haben aber beliebige externe E-Mail-Teilnehmer ohne `Person`-Zeile.

**Getroffene Entscheidung**: `Attendee.PersonId` ist jetzt nullable; neue Felder `ExternalEmail`/`ExternalDisplayName` für synchronisierte Teilnehmer. Ein DB-Check-Constraint erzwingt: genau eines von beidem ist gesetzt. Synchronisierte Teilnehmer werden **immer** als extern gespeichert, auch wenn die E-Mail zufällig mit einem Teammitglied übereinstimmt (kein automatisches Matching gegen die Personen-Tabelle).

**Bitte bestätigen**, bevor Epic 3 (Mehrpersonen-Ansicht) auf dieser Struktur aufbaut — falls du stattdessen ein Matching gegen bekannte Teammitglieder-E-Mails möchtest, ist das ein Nachtrag, kein Bug.

## 3. `[ASSUMPTION]` CalendarConnection existiert schon vor erfolgreicher Verbindung (Story 2.1)

Um AC 10 zu erfüllen (persistenter Fehlerzustand für "nie erfolgreich verbunden, Consent abgelehnt"), wird die `CalendarConnection`-Zeile bereits beim ersten "Verbinden"-Klick angelegt — unabhängig vom Ergebnis. `EncryptedAccessToken`/`EncryptedRefreshToken` bleiben `null`, bis der erste Handshake erfolgreich war (`IsConnected` = `EncryptedAccessToken != null`). Das ist eine Architekturentscheidung, die von der ursprünglichen Story-Skizze abweicht (die davon ausging, die Zeile entstehe erst bei Erfolg) — technisch notwendig, um den Fehlerzustand überhaupt speichern zu können.

## 4. Nicht spezifizierte Zahlenwerte — reasonable defaults, keine Vorgabe aus PRD/Architecture

- **Sync-Intervall**: 5 Minuten (`SYNC_INTERVAL_SECONDS`, konfigurierbar). NFR-1 sagt nur "im Bereich weniger Minuten".
- **Sync-Zeitfenster**: 30 Tage zurück / 180 Tage voraus. Nirgends spezifiziert.
- **Schwellenwert für "wiederholt fehlgeschlagen"**: 3 aufeinanderfolgende Fehlversuche, bevor die Settings-Zeile / Admin-Übersicht in den Fehlerzustand wechselt (AD-16 sagt nur "ein Schwellenwert", keine Zahl).
- Alle drei sind zentral an einer Stelle im Code definiert (nicht dupliziert), falls du andere Werte möchtest, sind das kleine, risikoarme Änderungen.

## 5. `[ASSUMPTION]` Microsoft-spezifische Details (Story 2.2), unverifiziert gegen echten Tenant

- **Tenant-Wert `"common"`**: erlaubt Consent von jedem Microsoft-Entra-Tenant (Multi-Tenant). Falls euer Team auf einem einzelnen bekannten Microsoft-365-Tenant sitzt, wäre ein fester `MICROSOFT_OAUTH_TENANT_ID` die restriktivere, ggf. passendere Wahl.
- **Fehlercode-Mapping für Tenant-Blockierung**: Microsofts Fehler-Rückgabe für "vom Tenant-Admin blockiert" ist weniger eindeutig als Googles `admin_policy_enforced` (kommt oft als `AADSTS`-Code im Freitext, nicht als knapper `error`-Wert). Aktuell fällt das auf einen generischen `oauth_error`-Code zurück statt auf die spezifische "von deinem Unternehmen blockiert"-Meldung wie bei Google. Sollte mit einem echten blockierten Test-Tenant verifiziert werden, sobald verfügbar.

## 6. Bewusst nicht gebaut (kein AC verlangt es, aber im Mockup sichtbar)

- **"Verbindung trennen" (Disconnect)**: Der UX-Mockup (`key-settings-connections.html`) zeigt einen Trennen-Button, aber keine Story-AC verlangt die Funktion. Zurückgestellt — falls gewünscht, ein kleiner eigener Nachtrag (Endpoint + UI, kein architektonisches Risiko).
- **Eager-Sync beim Verbinden**: Nach dem OAuth-Handshake erscheinen Termine erst nach dem nächsten Worker-Zyklus (bis zu 5 Min.), nicht sofort. Bewusste Vereinfachung, um den Api-Request synchron/einfach zu halten.

## 7. Code-Review-Historie (zur Transparenz, bereits behoben)

Zwei automatisierte High-Effort-Code-Reviews liefen während der Umsetzung. Alle als CONFIRMED eingestuften Findings wurden behoben und durch neue Tests abgesichert, bevor die jeweilige Story auf "review"/"done" gesetzt wurde:

- **Story 2.1**: Datenverlust-Risiko bei nicht-transaktionalem Sync-Diff (behoben: DB-Transaktion), zwei Silent-Failure-Lücken in der Fehlerbehandlung (behoben: umfassenderes Exception-Handling), Case-sensitive E-Mail-Vergleich (behoben), sequenzieller statt paralleler Worker-Sync-Loop (behoben), toter Code im Frontend (entfernt).
- **Story 2.2/2.3 (zweiter Durchlauf)**: Ein echter Bug — Token-Refresh mitten im Sync-Zyklus hat den Fehler-Zähler zurückgesetzt, bevor der eigentliche Kalenderabruf überhaupt gelaufen war, wodurch eine andauernde Störung zeitweise als "gesund" angezeigt wurde (behoben: neue `UpdateTokensAfterRefresh`-Methode, die den Fehler-Zähler unangetastet lässt). Ein zweiter echter Bug — fehlender Vergleich des Teilnehmer-Anzeigenamens beim Sync-Diff, wodurch Namensänderungen bei externen Teilnehmern verschluckt wurden (behoben). Ein durch mich selbst eingeführter Frontend-Regressions-Bug — Angular-Listen-Tracking über eine nicht garantiert eindeutige E-Mail statt über einen stabilen Index (behoben). Mehrere Code-Duplizierungen bereinigt (OAuth-Token-Refresh-Logik, OAuth-POST-Logik, "Minuten seit Sync"-Hilfsfunktion im Frontend). **Wichtige Erkenntnis**: Ein vom Review vorgeschlagener "Parallelisierung"-Fix (`Task.WhenAll` für zwei DB-Abfragen im Admin-Endpoint) wurde testgetrieben als selbst fehlerhaft erkannt und zurückgenommen — `IPersonRepository`/`ICalendarConnectionRepository` teilen sich einen EF-Core-DbContext pro Request, der keine parallelen Abfragen erlaubt (führte zu einem intermittierenden 500er). Sequenziell bleibt hier die korrekte Lösung.

---

## Prozess-Notiz (kein inhaltliches Thema, nur für zukünftige Sessions relevant)

- Das Worktree für diese Session hatte sich zunächst versehentlich von `main`/Initial-Commit statt von `dev` abgezweigt (fehlte dadurch der komplette Epic-1-Code: `Attendee`, `AvailabilityStatus`, `StatusHeuristicService` etc.). Korrigiert per `git reset --hard origin/dev` bevor Code geschrieben wurde — keine Auswirkung auf den finalen Stand, aber als Hinweis für künftige Worktree-Sessions in diesem Repo festgehalten.
