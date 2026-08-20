# MMRIA Form Designer Removal
### Leadership Brief

---

## The Bottom Line

MMRIA contains a built-in tool that allows administrators to visually reposition form fields on a canvas. That tool has never been used in practice, because the form layout is locked by an OMB certification process that governs how the data collection form must appear. Removing this unused tool now simplifies the system, reduces maintenance burden, and prevents it from becoming a complicating factor in the planned SQL database migration.

---

## What the Form Designer Is

When MMRIA was originally built, the form designer was intended to give administrators a graphical, drag-and-drop interface for controlling the layout of the case data entry form — where labels appear, how wide input fields are, how sections are positioned on the page.

To make this work, the system stores a database document describing the exact position and size of every field on every form — over **1,700 individual layout entries**. Each time a case is opened, the application downloads this document and uses it to dynamically assemble the form on the screen in the user's browser.

This is a meaningful amount of infrastructure for a configuration that is never changed.

---

## Why It Has Never Been Used

MMRIA's case data collection form is subject to **OMB (Office of Management and Budget) certification**. This federal process certifies the form's content and layout. Any material change to how the form appears — including repositioning a field — requires going back through the OMB process.

In practical terms, this means the form layout is fixed for the life of a certified version. The ability to drag and rearrange fields in real time provides no operational value under those constraints.

---

## What This Costs Today

Carrying the form designer as an active part of the system has ongoing costs:

| Cost Area | Description |
|---|---|
| **Maintenance burden** | The form designer is built on a custom JavaScript rendering engine — over 300 KB of specialized code across more than 20 files — that must be understood, maintained, and tested alongside the rest of the application. |
| **Configuration distribution** | The layout database document is stored in each jurisdiction's individual CouchDB database. Across 72 jurisdictions, that is 72 copies of a document that is never modified. Every update or deployment must account for keeping all 72 in sync. |
| **Security surface** | The form designer requires a dedicated administrator role. This access control point must be maintained, audited, and documented even though no one logs in to use it. |
| **Developer onboarding complexity** | New developers working on the case form must learn two systems: the form's business logic *and* the form designer's layout engine. The layout engine adds significant learning time with no corresponding benefit. |

---

## The Risk It Poses to SQL Migration

The planned SQL database migration will move MMRIA's data storage from 72 separate per-jurisdiction databases to a single, centralized SQL database. This is a major simplification of the system's infrastructure.

The form designer creates a complication for that migration. Because form layout is stored in the database — not in the application's source code — the migration must explicitly account for moving, transforming, and validating those 1,700+ layout entries into the SQL data model. If the form designer remains in the system, every architectural decision about the SQL data model must include a solution for where and how layout data lives.

Removing the form designer before the SQL migration eliminates that complication entirely. The layout moves from the database into the application itself — as standard HTML — where it belongs for a fixed, certified form.

---

## The Proposed Approach

The replacement is deliberately simple:

1. The current form layout is captured from the browser as plain HTML (the form already renders correctly today — we are simply preserving that output).
2. That HTML becomes static view files in the application — the standard, well-understood technology the rest of the server already uses for all other pages.
3. The form designer tool, its supporting code, and its database documents are removed.

The result is a case data entry form that works identically to today, built on straightforward HTML instead of a specialized rendering engine.

No visual change to the form. No change to how data is entered or saved. No change to any certified layout.

---

## Summary

| Dimension | Today | After Removal |
|---|---|---|
| Form layout storage | Database document (72 copies) | Application source code (1 copy, version-controlled) |
| Rendering mechanism | 300+ KB custom JavaScript engine | Standard HTML |
| SQL migration scope | Must design data model for layout data | Layout is out of scope entirely |
| Maintenance surface | Custom layout engine + admin tool | Removed |
| Administrator role | Required (never exercised) | Removed |
| Functional change to users | None | None |

Removing the form designer is a contained, low-risk cleanup that simplifies the codebase, reduces the scope of the SQL migration, and eliminates a maintenance burden with no active use justifying it.

---

*Prepared by: MMRIA Engineering Team*
*Date: August 2026*
