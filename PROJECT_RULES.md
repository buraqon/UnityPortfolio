# Project Rules

Working rules for this repo, agreed with the project owner. Claude Code should follow these without being
asked again, and should proactively remind the owner when a rule applies (see each rule's note).

## 1. Keep a progress document per feature

Whenever work starts on a new feature (a new folder under `Assets/Features/<Name>/`, or a substantial new
piece of work inside an existing one), keep a progress document for it: `Assets/Features/<Name>/PROGRESS.md`.

- Create it at the start of the work, before or alongside the first code changes.
- Update it as work progresses — what's done, what's in progress, what's left, and any decisions/tradeoffs
  worth remembering later.
- **Reminder trigger:** at the start of a new session, if the conversation is about starting or continuing
  work on a feature and no `PROGRESS.md` exists for it yet, remind the owner to create one before diving in.

## 2. No large explanatory comments in scripts

Don't write comments (or comment blocks) in scripts that explain what a feature or piece of code does.
Rely on clear class and method/function naming instead — if the names are good, the comment isn't needed.

- A short comment is still fine when it captures a genuinely non-obvious *why* (a hidden constraint, a
  workaround, a subtle invariant) — not a restatement of *what* the code does.
- If a class/function name isn't self-explanatory enough to skip a comment, the fix is a better name, not a
  comment.

## 3. Treat each feature as standalone

Unless explicitly told otherwise, treat each folder under `Assets/Features/<Name>/` as self-contained.
Don't read, reference, or reuse code from other feature folders, and don't assume a dependency between
features, when working on one of them.

- Only cross into another feature's folder if the owner explicitly says there's a dependency on it (e.g.
  "this reuses the Pooling system" or "wire this into Conjure").
- This applies to research/exploration too — don't go searching other `Assets/Features/` folders for
  patterns to copy unless asked.
