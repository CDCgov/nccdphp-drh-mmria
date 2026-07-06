# Feature Brief: System Offline / Planned Outage Notification

**Date:** June 18, 2026  
**Status:** Implemented (pending ticket / backlog grooming)  
**Area:** Installation Admin → System Offline Configuration  
**Roles affected:** Installation Admins (configuration), all authenticated users (notifications), unauthenticated users (login page blocking)

---

## Summary

A new System Offline Configuration feature allows installation admins to schedule and communicate a planned outage to all users — or to a targeted subset of jurisdictions — without requiring a deployment or code change. Admins set warn and offline dates with configurable messages, and the system automatically shows in-app modals, signs users out, and blocks login during the outage window.

---

## Admin Configuration

**Location:** Admin → System Offline Configuration (route: `/system-offline`)  
**Access:** `installation_admin` role only

Admins can configure the following fields:

| Field | Description |
|---|---|
| **Warn Date** | Date/time when the warning modal starts appearing to users. |
| **Warn Message** | Message shown in the dismissable warning modal. Supports template tokens (see below). |
| **Offline Date** | Date/time when the system is considered offline. Users are signed out; login is blocked. |
| **Estimated Maintenance Duration (hours)** | Integer. How long the outage is expected to last. Drives the `{{outage_duration}}` and `{{estimated_restoration}}` tokens. |
| **Auto Sign-Out Delay (minutes)** | Minutes after the offline modal appears before the user is automatically signed out if they do not click OK. Default: 5 minutes. |
| **Offline Modal Message** | Message shown in the non-dismissable offline modal (with countdown to auto sign-out). Supports template tokens. |
| **Offline Page Message** | Message shown on the login page while the system is offline, replacing the login form. Supports template tokens. |
| **Jurisdiction Targeting** | Radio: **All Jurisdictions** (default) or **Select Jurisdictions**. When "Select" is chosen, a checklist of all registered tenants is shown with Check All / Uncheck All controls. Only selected sites receive the outage window. |

All dates are entered in the admin's local time and stored in UTC.

### Template Tokens

The following tokens can be inserted into any message field:

| Token | Replaced with |
|---|---|
| `{{warn_date}}` | Warn date, formatted in server local time |
| `{{offline_date}}` | Offline date, formatted in server local time |
| `{{outage_duration}}` | Duration from the **Estimated Maintenance Duration** field (e.g. "2 hours") |
| `{{estimated_restoration}}` | Offline date + maintenance duration, formatted in server local time |

**Example message:**  
`The system will be offline on {{offline_date}} for approximately {{outage_duration}}. Estimated restoration: {{estimated_restoration}}.`

Line breaks entered in the message fields are preserved when displayed to users.

---

## User-Facing Behavior

### 1. Warning Modal (before offline date)

- Once the **Warn Date** is reached, a dismissable modal titled **"System Going Offline"** appears.
- The modal shows the configured **Warn Message**.
- It is shown **once per browser tab session** — dismissing it does not show it again until the user opens a new tab or logs in again.
- The modal also appears if the polling check (every 2 minutes) detects the warn date has been crossed.

### 2. Offline Modal (at or after offline date)

- Once the **Offline Date** is reached, a non-dismissable modal titled **"System Going Offline"** appears.
- The modal shows the configured **Offline Modal Message**.
- A **countdown timer** displays: *"Automatically signing out in M:SS."*
- When the countdown reaches zero **or** the user clicks **OK**:
  - If the page has unsaved case changes and a save hook is registered, the case is saved first.
  - The user is then signed out via a POST to `/Account/Logout`.
- The modal is shown **once per login session** (tracked in `localStorage`; cleared on the next login).

### 3. Login Page Blocking

- When a user attempts to access the login page while the **Offline Date** is in the past:
  - The login form is hidden.
  - The configured **Offline Page Message** is displayed in its place.
- Login form POST submissions are also rejected during the offline window.
- Once the offline configuration is cleared or the dates are updated, login resumes normally.

### 4. Jurisdiction Targeting

- If **Select Jurisdictions** is configured, only users on the selected tenant sites see any warning or offline behavior.
- All other tenants continue to operate normally with no modals, no login blocking.

---

## Technical Notes (for story acceptance criteria)

- Configuration is stored in CouchDB (`metadata` database, document ID `system-offline-config`) on the CDC instance.
- The `/api/system-offline/status` endpoint (authenticated) returns the resolved config for the current tenant. Non-affected tenants receive null dates.
- The client polls `/api/system-offline/status` every **2 minutes** while a page is open.
- No changes are required to existing cases, forms, or data models.
- The feature degrades gracefully: if the config document does not exist or the service is unreachable, no modals are shown.

---

## Suggested Ticket / Story

**Title:** System Offline / Planned Outage Notification — Retroactive Story  
**Epic:** System Administration  
**Type:** Feature (implemented)

**As an** installation admin,  
**I want to** configure a planned outage window with warn and offline dates, messages, and jurisdiction targeting,  
**So that** users receive timely in-app notification, are automatically signed out, and cannot log in during the outage window.

**Acceptance Criteria:**
1. Admin can set warn date, offline date, messages, maintenance duration, auto sign-out delay, and jurisdiction scope.
2. Warning modal appears for affected users once the warn date is reached; dismissed per tab session.
3. Offline modal appears for affected users once the offline date is reached, with a countdown to auto sign-out.
4. Auto sign-out fires after the configured delay (default 5 min); saving in-progress case work first if applicable.
5. Login form is blocked for affected tenants while offline; custom message is displayed.
6. Unaffected jurisdictions (when "Select Jurisdictions" is configured) see no warnings and can log in normally.
7. Message fields support template tokens: `{{warn_date}}`, `{{offline_date}}`, `{{outage_duration}}`, `{{estimated_restoration}}`.
8. Line breaks in admin-entered messages are preserved in the UI.
9. All dates are stored and compared in UTC; admin UI converts to/from local time transparently.
