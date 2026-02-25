# Meowdex Desktop UI/UX Decisions (Current)

## Purpose
Track implemented UI/UX direction and key behavior constraints so future changes stay consistent.

## Implemented Decisions

### 1) Identity Iconography
- Gender and sexuality in roster-style tables are icon-only.
- Add/Edit management flows show text plus right-aligned icon in selectors.
- Icon assets live under `Meowdex.Desktop/Assets/Icons/GenderSexuality`.
- Current files in use:
  - `GenderMale.png`
  - `GenderFemale.png`
  - `GenderFluid.png`
  - `SexualityBisexual.png`
  - `SexualityGay.png`

### 2) Selector Presentation Rules
- ComboBox item templates for gender/sexuality are explicit (not enum default text-only).
- Display format:
  - Left: readable label (`Male`, `Female`, `Fluid`, etc.)
  - Right: scaled icon (`18x18` style baseline)

### 3) Runtime Stability Hardening
- `async void` UI event paths are wrapped with exception guards to avoid hard crashes.
- Overlay flows are serialized through an app-level gate to avoid unresolved overlay tasks.
- Edit-row lock (`_openingEdit`) is reset in `finally` to prevent stuck state after exceptions.

## Breeding UX/Logic Constraint

### Novel OR Mask Rule (Implemented)
- A cat is only added to diversity/backfill breeding pool if its mask adds new bit coverage.
- Formal check: `(coveredMask | candidateMask) != coveredMask`.
- Effect:
  - If a superior mask already covers all bits of an inferior mask, inferior mask cats are excluded.
  - Example: top cat with `STR+DEX+CHA` excludes `STR+DEX` candidates from backfill pool.

## Known Asset Gap
- No straight-specific sexuality icon is currently present.
- `Straight` currently maps to the bisexual icon as temporary fallback until a dedicated icon is added.

## Breeding Pool Table Layout
- The old single binary mask column is replaced by seven per-stat columns:
  - `STR`, `DEX`, `STA`, `INT`, `SPD`, `CHA`, `LUK`
- Each cell shows a checkmark when the corresponding mask bit is `1`; blank when `0`.
- Name column format:
  - Primary text: cat name
  - Secondary text: muted/smaller `(#ID)`
- Current header set:
  - `Name (#{ID})`
  - `STR`, `DEX`, `STA`, `INT`, `SPD`, `CHA`, `LUK`
  - `#7s`
  - `#Partners`
  - `Base μ`
  - `Current μ`
  - `Reason`
- Removed the separate diversity-backfill name spill list under the table.

## Breeding Sort Rule
- Sorting by any stat-bit column uses this priority stack:
1. Bit presence (`✓` as 1, blank as 0; checked rows rank first)
2. Higher base average
3. Higher current average
4. Name (ascending)

## Open Follow-Ups
1. Add dedicated straight icon and switch fallback mapping.
2. Verify icon contrast/legibility at 100%, 125%, and 150% scale on Windows.
3. Optionally standardize icon dimensions if new icon packs are introduced.
