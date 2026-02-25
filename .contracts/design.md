# Meowdex Contract

## Purpose
This document is the canonical shared understanding between product and implementation for the Meowdex app.

## Product Goal
Meowdex is a fast decision-support tool for Mewgenics cat management after each day cycle, focused on:
1. Entering and maintaining current cat population data.
2. Producing a recommended breeding pool.

## Design & Interaction Conventions
- The app behaves like an SPA: navigation happens through in-context controls, not a persistent side nav.
- The dashboard is organized into screen-like sections, with tab trackers to move between sections and indicate the active view.
- Global configuration is accessed via a single top-level control and edited in a modal dialog.
- Manage Cats prioritizes speed and density: full-width roster, inline editing, and modal-driven add flow.
- Destructive actions require confirmation.

## Core Cat Data Model
Each cat has immutable identity + mutable gameplay data.

### Identity
- `id` (immutable, ascending integer assigned at insert time)
- `name`

### Demographics
- `gender`: `male`, `female`, `fluid`
- `sexuality`: `gay_lesbian`, `bi`, `straight`

### Stats
Each stat has `base` and `current` values:
- `strength`
- `dexterity`
- `stamina`
- `intellect`
- `speed`
- `charisma`
- `luck`

Rules:
- Base values are inheritable/inherited values.
- Current values are post-adjustment values (mutations, etc).
- Base values cap at `7`.

## Breeding Selection Canon
Primary optimization target is maximizing natural base 7s across the 7 base stats.

### 7-Mask
- Compute a 7-bit mask from base stats where each bit is `1` when that base stat equals `7`.
- This mask is the primary signature for breeding value.

### Configurable Top Cat Count
- Setting: `top_cat_count`.
- Default: `3`.
- Breeding pool initially selects the top `top_cat_count` cats under ranking rules below.
- Remaining cats are general population.

### Ranking Within Same 7-Mask (Breeding Population)
For cats sharing the same 7-mask, rank in this order:
1. Number of compatible partners (higher first)
2. Base stat average (higher first)
3. Age proxy using `id DESC` (higher id = younger, ranked first)

### General Population Ranking
- Rank by average of current stat values (higher first).

### Compatible Partner Rule
- Two cats with the same gender cannot breed.
- Sexuality and gender must be represented and available for compatibility checks.

## Diversity Backfill Rule for Breeding Pool
After selecting top-mask cats, allow additional cats into breeding consideration when their 7-mask contributes natural 7 positions not already represented in the current breeding mask coverage.

Intent:
- Preserve opportunities to create kittens with better stat combinations by introducing complementary masks.

## Cat Lifecycle and Management
- Cats may mutate over time.
- Player needs fast lookup and update for a specific cat.
- Age is not tracked directly; `id` is the age proxy and remains immutable.
- Cats can be culled/demoted from breeding once desired stats are passed on.

## Data Entry Constraints
- Data entry speed is critical.
- Workflow should support bursts (e.g., ~10 cats in one night).
- Minimum practical ingress should focus on immutable identity + statline + simple breeding compatibility fields.

## Manage Cats UX Canon
- Primary roster is a full-width table with sortable columns, including all core stats.
- Gender and sexuality are rendered as compact icon-like badges.
- Edit happens inline as an expanded row panel with its own Save/Cancel.
- Add Cat is a modal dialog that refreshes the roster on save.

## Dashboard UX Canon
- Sections are treated as canvases with tab trackers for navigation.
- Snapshot is the first section.
- Additional sections: Adventuring Team, Breeding Pool Roster, General Population Roster.
- Config is a global control that opens a modal; closing applies updates and refreshes data.

## Abilities Tracking Scope
- Full per-cat ability tracking is considered too costly.
- App should support logging player-favorite abilities with collar source metadata.

### Favorite Ability Log (Planned Scope)
- ability name
- ability type: `active` or `passive`
- collar source (e.g., `collarless`, `cleric`, `necromancer`, etc)

## Mewgenics System Qualities To Respect
- At `32` stimulation, an active skill will always pass down.
- At `95` stimulation, a passive skill will always pass down.
- Cats should not be kept if they do not have a natural base `7`.
- Room `MUTATION` increases mutation chance; these mutations are usually net positive.
- Room `HEALTH` affects lifespan, disease rate, and chances to develop/cure injuries and hereditary disorders.
- Room `STIMULATION` increases inheritance quality for best stats and active/passive abilities.
- Room `COMFORT` affects breeding-vs-fighting outcomes.
- House `APPEAL` attracts better strays (supports inbreeding mitigation).

## Room Topology Assumptions
- 1 room (default, no expansions): one breeding room.
- 2 rooms: breeding room + safe room.
- 3 rooms: breeding room + safe room + fight club.
- 4 rooms: breeding room + safe room + fight club + second breeding room.

## Operational Loop Assumption
Primary loop:
1. Day ends.
2. Overnight: cats spawn/fight/die.
3. New day starts with changed population.
4. Player enters updated population and uses recommendations to decide breeding/culling/demotion.

## Non-Goals (Current Phase)
- Full simulation of all combat/breeding outcomes.
- Full per-cat ability database.
- Manual age tracking.

## Contract Update Rule
Any behavior that conflicts with this file should be treated as out-of-contract until this file is updated and agreed.
