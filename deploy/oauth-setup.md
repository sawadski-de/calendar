# OAuth-Setup: Google Calendar & Outlook/Microsoft 365

Anleitung für die einmalige Einrichtung der beiden OAuth-Apps, die Epic 2 (Kalender-Synchronisation)
zum Laufen braucht. Ohne diese Registrierungen funktionieren die "Verbinden"-Buttons in
Einstellungen → Kalenderverbindungen nicht (die Platzhalterwerte in `.env.example` sind absichtlich
ungültig). Reine Admin-Aufgabe, kein Code nötig — am Ende hast du vier Werte pro Provider, die in
`deploy/.env` eingetragen werden.

## Vorher: TOKEN_ENCRYPTION_KEY erzeugen

Unabhängig vom Provider — einmalig, bevor der Stack das erste Mal mit echten Zugangsdaten läuft:

```bash
openssl rand -base64 32
```

Ergebnis in `deploy/.env` als `TOKEN_ENCRYPTION_KEY` eintragen. Ohne einen gültigen 32-Byte-Base64-Wert
brechen Api und Worker beim Start mit einer klaren Fehlermeldung ab (bewusst so, kein stilles
Fehlverhalten).

## 1. Google Cloud Console

**Voraussetzung:** Ein Google-Cloud-Projekt (falls noch keins existiert: [console.cloud.google.com](https://console.cloud.google.com) → Projekt erstellen).

1. **OAuth-Consent-Screen konfigurieren** (falls noch nicht getan): `APIs & Services` → `OAuth consent screen`.
   - User Type: **Internal** wählen, falls ihr ein Google Workspace (Firmen-)Konto habt — dann kann sich nur euer eigenes Workspace verbinden, keine Google-Verifizierung nötig. Falls ihr private Gmail-Konten nutzt, bleibt nur **External** übrig; dann ist die App standardmäßig im "Testing"-Modus und nur explizit hinzugefügte Test-User können sich verbinden (siehe Schritt 4) — das reicht für ein internes Team-Tool völlig aus, eine Google-Verifizierung ist nicht nötig.
   - App-Name, Support-E-Mail ausfüllen (frei wählbar, wird nur euch selbst angezeigt).
2. **Calendar API aktivieren**: `APIs & Services` → `Library` → "Google Calendar API" suchen → **Enable**.
3. **OAuth-Client-ID erstellen**: `APIs & Services` → `Credentials` → `Create Credentials` → `OAuth client ID`.
   - Application type: **Web application**.
   - Name: frei wählbar (z. B. "Team-Terminkalender").
   - **Authorized redirect URIs**: exakt der Wert, den ihr als `GOOGLE_OAUTH_REDIRECT_URI` verwendet, z. B.:
     - Lokal/Test: `http://localhost:8090/api/calendar-connections/google/callback`
     - Produktion: `https://<eure-domain>/api/calendar-connections/google/callback`
   - Nach dem Erstellen: **Client ID** und **Client secret** notieren.
4. **Falls User Type = External und Status "Testing"**: `OAuth consent screen` → `Test users` → jede E-Mail-Adresse eintragen, die sich verbinden soll (jedes Teammitglied). Ohne das schlägt der Consent-Flow für alle außer dir fehl.
5. **Scope**: Der Code fordert automatisch `https://www.googleapis.com/auth/calendar.readonly` an — dafür ist in der Consent-Screen-Konfiguration keine zusätzliche Freischaltung nötig (es ist ein "non-sensitive" Scope).

**Eintragen in `deploy/.env`:**

```
GOOGLE_OAUTH_CLIENT_ID=<Client ID aus Schritt 3>
GOOGLE_OAUTH_CLIENT_SECRET=<Client secret aus Schritt 3>
GOOGLE_OAUTH_REDIRECT_URI=<exakt die im Google-Client eingetragene Redirect-URI>
```

## 2. Azure AD / Microsoft Entra admin center

**Voraussetzung:** Ein Microsoft-Entra-Tenant (jeder Microsoft-365-Account hat automatisch einen; Zugriff auf [entra.microsoft.com](https://entra.microsoft.com) mit Admin-Rechten für App-Registrierungen nötig — oder du bittest euren IT-Admin, die folgenden Schritte für dich auszuführen).

1. **App registrieren**: `Identity` → `Applications` → `App registrations` → `New registration`.
   - Name: frei wählbar (z. B. "Team-Terminkalender").
   - Supported account types: **"Accounts in this organizational directory only"**, wenn nur euer eigener Tenant sich verbinden soll (empfohlen, siehe Story-2.2-Notiz zu `MICROSOFT_OAUTH_TENANT_ID=common` vs. fester Tenant — mit dieser Wahl bleibt `common` im Code trotzdem funktionsfähig, es lehnt lediglich fremde Tenants beim Consent selbst ab). "Accounts in any organizational directory" nur wählen, wenn Teammitglieder aus verschiedenen Firmen-Tenants teilnehmen sollen.
   - Redirect URI: Type **Web**, Wert exakt wie `MICROSOFT_OAUTH_REDIRECT_URI`, z. B.:
     - Lokal/Test: `http://localhost:8090/api/calendar-connections/outlook/callback`
     - Produktion: `https://<eure-domain>/api/calendar-connections/outlook/callback`
2. **Client Secret erstellen**: In der neu erstellten App → `Certificates & secrets` → `Client secrets` → `New client secret`. Beschreibung frei wählbar, Ablaufdatum wählen (z. B. 24 Monate — **Secrets laufen ab und müssen dann erneuert werden**, das ist eine Microsoft-Eigenheit ohne Gegenstück bei Google; im Kalender notieren). **Den Secret-Wert sofort kopieren** — er wird nach dem Verlassen der Seite nie wieder angezeigt.
3. **API-Berechtigung hinzufügen**: `API permissions` → `Add a permission` → `Microsoft Graph` → `Delegated permissions` → `Calendars.Read` suchen und hinzufügen.
   - Falls euer Tenant "Admin consent" für neue Apps verlangt: `Grant admin consent for <Tenant>` klicken (Admin-Rechte nötig) — sonst bekommt jeder Nutzer beim ersten Verbinden einen Zustimmungsdialog, was ebenfalls funktioniert, aber ggf. von der Tenant-Policy blockiert wird (siehe App die im Code bereits vorgesehene "tenant blocked"-Fehlermeldung).
4. **Client ID und Tenant ID notieren**: `Overview`-Seite der App → **Application (client) ID** kopieren. Falls ihr in Schritt 1 einen festen Tenant statt "common" gewählt habt, auch die **Directory (tenant) ID** kopieren.

**Eintragen in `deploy/.env`:**

```
MICROSOFT_OAUTH_CLIENT_ID=<Application (client) ID aus Schritt 4>
MICROSOFT_OAUTH_CLIENT_SECRET=<Client secret aus Schritt 2>
MICROSOFT_OAUTH_REDIRECT_URI=<exakt die in der App registrierte Redirect-URI>
MICROSOFT_OAUTH_TENANT_ID=common
```

Nur falls ihr euch für einen festen Tenant statt "common" entschieden habt, `MICROSOFT_OAUTH_TENANT_ID`
auf die in Schritt 4 notierte Tenant-ID setzen.

## Danach: Stack neu starten

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d --build
```

Beide `api`- und `worker`-Container lesen die neuen Werte beim Start. Danach in
Einstellungen → Kalenderverbindungen testen: "Verbinden" bei Google bzw. Outlook klicken, den
Consent-Flow durchlaufen, Status sollte auf "Verbunden" wechseln. Die ersten importierten Termine
erscheinen erst nach dem nächsten Worker-Sync-Zyklus (Standard: alle 5 Minuten, `SYNC_INTERVAL_SECONDS`
in `.env`), nicht sofort nach dem Verbinden.

## Troubleshooting

- **"redirect_uri_mismatch" (Google) / "AADSTS50011" (Microsoft)**: Die Redirect-URI im `.env` stimmt
  nicht exakt (inkl. `http`/`https`, Port, kein/mit Trailing Slash) mit der in der Konsole
  registrierten überein.
- **"Verbindung von deinem Unternehmen blockiert"**: Der Tenant-Admin hat Self-Consent für
  unverifizierte Apps gesperrt — entweder Admin-Consent erteilen (Schritt 3 oben) oder den IT-Admin
  bitten, die App freizugeben.
- **Google zeigt "This app isn't verified"**: Normal für eine interne, unverifizierte App im
  Testing-Modus — auf "Advanced" → "Go to \<App-Name\> (unsafe)" klicken. Für ein privates Team-Tool
  ist eine Google-Verifizierung nicht nötig und auch nicht sinnvoll erreichbar (die ist für öffentliche
  Apps mit vielen Nutzern gedacht).
