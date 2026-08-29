# Story Dev Queue

> **Maintenance policy — read before editing this file.**
>
> This file is a **work queue**. It lists only stories that still need to be dev'd. Each entry is a copy-paste `dev this story …` prompt for the dev agent.
>
> **What belongs here**
> - An epic header: `## Epic N — Title`
> - Per story: a short story ID line followed by a fenced code block containing exactly one `dev this story …` command.
>
> **What does NOT belong here**
> - Any story that is `done` (or `superseded`). Remove the bullet the moment the story flips to `done` in [sprint-status.yaml](sprint-status.yaml).
> - Fine-grained statuses (`ready-for-dev`, `in-progress`, `review`). Those live in [sprint-status.yaml](sprint-status.yaml).
> - Acceptance criteria, sequencing notes, dependency notes, engineering rationale, background, ADRs, OI callouts. Those live in the story file itself.
> - `create story …` prompts. Author new stories with the `bmad-create-story` skill; drop the resulting story file into this folder and add its bullet here.
>
> **Update rules**
> - When a new story file is authored, add its bullet here immediately.
> - When a story flips to `done` (or `superseded`) in sprint-status.yaml, delete its bullet from this file in the same change.
> - If an epic empties out (every story done), delete the epic header too.
> - Historical / shipped work is discoverable via [sprint-status.yaml](sprint-status.yaml) and the story files in this folder — do not preserve shipped prompts here.

---

## Epic 41 — Per-Tenant Authentication Mode (SAMS + Password)

**41.3**

```
dev this story _bmad-output/implementation-artifacts/41-3-tenant-config-hot-reload.md
```

**41.4**

```
dev this story _bmad-output/implementation-artifacts/41-4-account-controller-partial-cross-reference-comment.md
```

---

## Epic 46 — Case Route Migration — Numeric Index to Case `_id`

**46.1**

```
dev this story _bmad-output/implementation-artifacts/46-1-case-route-index-to-case-id.md
```
