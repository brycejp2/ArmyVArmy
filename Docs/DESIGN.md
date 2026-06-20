# ArmyVArmy — Living Design Doc

A running log of design decisions, open questions, and balance notes. Update this as the game
evolves. `PLAN.md` is the stable reference; this file captures the moving parts.

## Decisions log

| Date | Decision | Notes |
|------|----------|-------|
| 2026-06-20 | Real-time auto-battler combat | Presented real-time; runs on a fixed-tick sim under the hood. |
| 2026-06-20 | In-battle input = commander abilities + unit commands (Charge/Brace) + speed/pause | Data-driven so more stances/spells are cheap. |
| 2026-06-20 | Army scale 100–300/side (~600 agents worst case) | Drives the data-oriented sim + pooling + LOD approach. |
| 2026-06-20 | Landscape orientation throughout | Menus and battle. |
| 2026-06-20 | Unit wounds persist between battles | Makes healing/rest a meaningful spend; reinforces permadeath. |
| 2026-06-20 | Vertical slice first | Full loop with 3 units before breadth. |
| 2026-06-20 | Unity 2022.3 LTS + 2D URP (default) | Unity 6 LTS acceptable alternative. |

## Open questions (non-blocking)
- Exact tick rate (20 vs 30 Hz) — decide during Phase 2 profiling.
- Mana pool vs pure cooldowns for commander abilities — prototype both in Phase 4.
- Supply/capacity formula for roster size per battle.
- Whether rout threshold ends a battle early, and at what fraction.
- Damage model specifics (flat armor subtraction vs % mitigation vs armor-vs-pierce types).

## Balance notes
- Slice triad intent: **Archer** kites and out-ranges, **Swordsman** is the durable line, **Spearman**
  hard-counters charges (and later Cavalry) especially on **Brace**.
- Target a readable rock-paper-scissors before adding more units, so synergies layer onto a solid base.

## Content checklist (slice)
- [ ] UnitDef: Swordsman, Archer, Spearman
- [ ] AbilityDef: 1 damage (e.g. Volley), 1 support (e.g. Heal)
- [ ] CommandDef: Charge, Brace
- [ ] BuffDef: at least one pre-battle buff
- [ ] UpgradeDef: armor / weapon / ability tier 1 for each unit
- [ ] EncounterDef: one linear sequence of enemy armies
