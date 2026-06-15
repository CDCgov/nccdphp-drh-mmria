---
title: "Product Brief: MMRIA V4.1"
status: final
created: 2026-06-12
updated: 2026-06-12
---

# Product Brief: MMRIA V4.1

## Executive Summary

MMRIA V4.1 is a focused quality and operational resilience release addressing four areas of known friction: case narrative editor reliability, vitals data integrity, configuration brittleness, and an unintended capability exposure in the print interface. No new features are introduced. The changes directly reduce reviewer friction, improve downstream data quality for CDC analysts, and eliminate an impending operational risk — an OMB expiration date change is expected in the near term, and this release must ship before that date changes to avoid an emergency deployment.

## The Problem

**Case reviewers struggle with a narrative editor that does not behave predictably.** Copy/paste operations corrupt content, and formatting choices — underline, horizontal rules, font size — do not survive the transition to print or PDF output. Reviewers lose time reformatting work they have already done, and the final printed record does not reflect the care taken during entry.

**CDC analysts receive vitals data they cannot trust.** Because MMRIA does not validate vitals values at entry time, out-of-range values reach the analysis stage. Analysts must either discard affected records or contact state reviewers to correct them — waste at both ends of a process that depends on data quality.

**Two system values are hardcoded that should not be.** The MMRIA version string and the OMB expiration date are embedded in application source code. Updating either requires a full code deployment. An OMB expiration date change is expected in the near term; without this fix, that change forces an emergency release.

**An unauthorized option is visible in the print interface.** The "core elements" option appears in all print dropdowns but should not be accessible to users. Its presence is a security concern — it exposes system output that users are not authorized to access.

## The Solution

**Editor and print fidelity.** Fix copy/paste behavior in the case narrative editor and align formatting rendering (underline, horizontal rules, font size) across the editor, print view, and PDF output.

**Vitals field validation.** Enforce valid range constraints on vitals fields across four forms: "ER Visits & Hospitalization," "Prenatal Care Record," "Other Medical Office Visits," and "Transport Vital Signs." Validation fires at the field level — entry of an out-of-range value is prevented and an inline message displays the valid range. Auto-save behavior is not affected. Valid ranges are stored in the MMRIA CouchDB configuration document, making them adjustable without a code release.

**Configuration-driven system values.** Move the OMB expiration date and MMRIA version number from source code into the CouchDB configuration document. Developers update values and propagate them to production via script — no deployment required.

**Remove core elements from print dropdowns.** Remove the "core elements" option from all print dropdowns across all user roles.

## A Common Thread

Three of the four solution areas share a common pattern: values that change on an administrative or policy schedule belong in configuration, not code. This release applies that principle to the OMB date, version number, and vitals ranges — reducing operational risk and eliminating the need for emergency deployments when these values change.

## Who This Serves

**MMRIA case reviewers** are the primary beneficiaries. They experience the editor friction directly, will encounter the vitals validation prompts, and are protected from the unauthorized print option.

**CDC analysts** benefit indirectly but materially — cleaner vitals data at the source means less remediation work downstream.

## Success Criteria

- Case reviewers can copy/paste content into the narrative editor without corruption, and underline, horizontal rules, and font size render correctly in both print and PDF output.
- It is not possible to enter a vitals field value outside the configured range on the four targeted forms; an inline message identifies the valid range when an out-of-range value is entered.
- The OMB expiration date, MMRIA version, and vitals valid ranges can each be updated by a developer editing the CouchDB configuration document and running the production update script — no code deployment required.
- The "core elements" option does not appear in any print dropdown for any user role.

## Scope

**In scope for V4.1:**
- Case narrative copy/paste fix
- Editor, print view, and PDF formatting fixes (underline, horizontal rule, font size)
- Vitals field-level input validation on 4 forms with configurable ranges stored in CouchDB
- OMB expiration date moved to CouchDB configuration
- MMRIA version moved to CouchDB configuration
- Remove "core elements" from all print dropdowns

**Explicitly out of scope:**
- Retroactive identification or correction of existing out-of-range vitals data
- Admin UI for managing configuration values
- Any changes to auto-save behavior

**Open prerequisite:**
- Vitals valid ranges must be defined by the program team before implementation of the validation feature can begin.

## Urgency

An OMB expiration date change is expected after this release. The configuration-driven change for the OMB date must ship before that date changes, or the team faces an emergency deployment. This is the primary scheduling driver for V4.1.
