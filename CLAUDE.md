# CLAUDE.md — Working Agreement for ArmyVArmy

This file orients an AI coding agent (Claude Code) working in this repository. Read it together with
`Docs/PLAN.md` (design + architecture) and `Docs/ROADMAP.md` (ordered build phases) before writing
code.

## What this project is

A 2D, mobile-first (Android-first, **landscape**) roguelite **auto-battler**. Each map node is a
**real-time battle** between the player's army and an AI army of **100–300 units per side**. Battles
are automated; the player intervenes with **commander abilities** (spells) and **unit commands**
(Charge / Brace), plus speed/pause. Losing one battle ends the run (**permadeath**). Between battles,
the player picks units, arranges formation, buys upgrades, applies buffs, and heals. **Unit wounds
persist between battles.**

## Engine & conventions

- **Unity 6 LTS**, **2D URP (2D Renderer)**. Input System (touch-first), TextMeshPro,
  Cinemachine. C# scripting.
- **Performance is a first-class constraint.** The battle is a **fixed-tick simulation decoupled from
  rendering**. Units live as **plain data in flat arrays** inside `BattleSimulation` — do **not**
  give each unit its own MonoBehaviour `Update`, Rigidbody2D, or Collider2D. Pooled `UnitView`s only
  interpolate toward sim state.
- Use a **spatial uniform grid** for neighbor/target queries (no O(n²) scans). Pool units,
  projectiles, and VFX. Aim for **~0 per-frame GC allocations** during battle.
- **Content is data-driven** via ScriptableObjects (`UnitDef`, `UpgradeDef`, `AbilityDef`,
  `CommandDef`, `BuffDef`, `EncounterDef`, `RunConfig`). New units/upgrades/abilities should be
  authoring work, not new code paths.
- Keep new code consistent with surrounding style; small, reviewable commits per roadmap step.

## Folder layout (target)

```
Assets/Scripts/
  Core/   Sim/   Combat/   Units/   Meta/   Data/   UI/   Audio/   Save/
Assets/Scenes/        # MainMenu, MetaHub, Battle
Assets/ScriptableObjects/  Art/  Audio/  Prefabs/  Fonts/
```

## How to work: one phase at a time

`Docs/ROADMAP.md` defines ordered phases. **Do exactly one phase per session**, then stop so a human
can open the Unity editor and verify before continuing. Each phase lists a concrete "provable"
outcome (e.g. "app launches and volume sliders work", "300v300 holds target FPS").

Suggested loop:
1. Restate the phase's goal and acceptance check.
2. Implement only that phase's scope.
3. Summarize what to verify in the Unity editor (what to press Play on, what to look for).
4. Stop.

## Acceptance gates (don't regress these)

- **Performance:** ~60 FPS (stable 30 minimum) at **200v200 on a mid-range Android device**; low draw
  calls (shared atlas batching); ~0 per-frame GC in battle.
- **Permadeath:** losing a battle deletes the run save; "Continue" is disabled afterward.
- **Settings persistence:** Music/SFX volume survive an app relaunch.
- **Full slice loop:** Main Menu → Meta Hub → Battle → Resolution → Meta Hub / Game Over.

## Testing

- Prefer **EditMode/PlayMode tests** for deterministic sim logic (damage, targeting, win/lose) using
  fixed seeds — fast and rendering-free.
- Manual verification happens in the Unity editor Play Mode and via on-device profiling. This repo's
  CI cannot run the Unity editor.

## Git

- Develop on the branch you were given for the task. Small, descriptive commits. **Do not open a PR
  unless explicitly asked.**
- Art/audio binaries go through **Git LFS** (see `.gitattributes`). Scenes/prefabs use force-text
  serialization for mergeability.
