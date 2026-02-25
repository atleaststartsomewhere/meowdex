# UI/UX Canon

## Navigation Model
- The app behaves like a single‑page experience.
- Navigation is driven by in‑context controls on the page.
- There is no persistent side navigation.

## Page Types
### Dashboard
- The dashboard is broken into screen-like sections.
- Sections are navigated with tab trackers that show active state and allow direct navigation.
- Section order:
  1. Snapshot
  2. Adventuring Team
  3. Breeding Pool Roster
  4. General Population Roster

### Manage Cats
- Primary content is a dense, full‑width roster.
- Inline edit expands the row with a full edit panel and Save/Cancel.
- Add Cat opens in an overlay (modal) and refreshes the roster on save.
- Destructive actions require confirmation.

## Overlay/Modal Behavior
- Overlays are full‑screen layers above content.
- The overlay includes a close “X” in the top‑right.
- Opening an overlay triggers a quick fade‑in; closing fades out.
- Overlays are used for:
  - Config
  - Add Cat
  - Destructive confirmations

## Visual Language
- Soft dark mode is the default.
- Background uses a warm gray (Discord‑like).
- Emphasis states use clear accent color for primary actions.
- Layout favors clarity, density, and rapid scanning.
