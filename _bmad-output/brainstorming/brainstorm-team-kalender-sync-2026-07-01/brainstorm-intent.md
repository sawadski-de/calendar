# Brainstorm Intent: Team Calendar Sync

## Core Problem / Goal

Each team member keeps their own calendar (mix of Outlook and Google Calendar), and there is no shared view. This makes team scheduling harder than it should be:

- Double-bookings and scheduling collisions happen because availability data exists but is scattered across individual calendars.
- Finding a common free slot for multiple people takes too much back-and-forth.
- There is no visibility into who is available, busy, or in what kind of state right now — without leaving your current context to go ask or check.

The real problem is not missing data — availability already exists somewhere for everyone. The problem is access and aggregation: nobody has a merged, at-a-glance view across the team's distributed calendars.

Goal: build a shared team calendar that aggregates everyone's existing calendars and makes team availability easy to see and easy to act on, without demanding people change how or where they already keep their calendar.

Team shape: one fixed, small/overviewable team — no multi-project or multi-group support needed.

## Key Insights & Grounding Analogies

**1. Unified Inbox (primary product analogy).** Just as email clients merge multiple accounts (Gmail, Outlook, etc.) into one inbox, this product merges multiple calendar sources (Outlook, Google) into one team view. This analogy is the blueprint for the whole product, layered in three steps:
   - Merge accounts/sources together (sync)
   - Distinguish per source (color-coding by source: Outlook / Google / manual)
   - Add a signal layer on top (context-aware availability) that solves the actual triggering problem

**2. Presence status (Slack/Teams analogy).** Rather than exposing a full calendar, show one always-visible status indicator — closer to chat-app presence than to a calendar grid.

**3. Context-aware availability = the killer feature.** Availability should not be binary (free/busy). It should carry context/urgency — e.g. "interruptible" vs. "do not disturb" — so people know not just *whether* someone is busy but *how urgently* or *why*. This status should be derived live from calendar data (not manually set) and visible everywhere (web/mobile, potentially a Slack badge).

**4. Privacy-by-default as a trust feature, not a limitation.** Synced/imported appointments show only "private/busy," never real content. There are no real privacy concerns within the team, but this default should be marketed as a trust-building feature of the product, not merely a restriction.

**5. Sync is intentionally one-directional.** Import/read-only sync from Outlook and Google — no write-back — to keep the system simple and avoid conflict-resolution complexity.

**6. The originating trigger.** The concrete real-world moment that sparked this: not being able to see whether a colleague was available, leading to wasted coordination effort. This trigger plus the Unified Inbox analogy are the two core elements the whole concept is built on. In short: the product is "a Unified Inbox for the calendar's availability signal."

## Constraints

- Must integrate with both Outlook Calendar and Google Calendar (mixed use across the team).
- Must be usable as a mobile/responsive web application.
- Sync is one-way (read/import only), no write-back to source calendars.
- Single fixed team, no multi-group/multi-project structure required.

## MoSCoW-Prioritized Feature Scope

### Must Have
- Month, week, and day calendar views
- Create own appointments directly in the calendar, with a detail view on click
- One-way sync (import) from both Outlook and Google Calendar
- Private-by-default display for synced appointments (shows "private/busy" only, never real content)
- Context-aware availability signal (not just free/busy, but why/how urgent), derived live from calendar data
- Mobile/responsive usability

### Should Have
- Shared free-slot finder: select team members, tool auto-suggests common free time windows
- Notifications (e.g., appointment reminders, alert when an awaited colleague becomes free)
- Recurring appointments (series)
- Location field with autocomplete, plus map display in the appointment detail view

### Could Have
- Color-coding by source (Outlook / Google / manual)
- Note field per appointment for team-internal information
- Quick search across appointments/locations/people
- Location or video-link field with automatic travel-time estimation and warning for overly tight scheduling
- Absence/vacation as its own calendar category

### Won't Have (this round)
- Shared/delegated calendar access (entering appointments on someone else's behalf)
