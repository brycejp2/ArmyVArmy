# ArmyVArmy — Roadmap

Build the **vertical slice** first (Phases 1–7), one phase per working session, verifying each in the
Unity editor before moving on. The **feature backlog** below is committed work for after the slice
proves the loop is fun and performant. See `PLAN.md` for design/architecture detail.

---

## Part A — Vertical Slice (ordered build phases)

### Phase 1 — Project bootstrap
- Create the Unity 2022.3 LTS project with **2D URP**; add packages (Input System, TextMeshPro,
  Cinemachine).
- Initialize **Git LFS**; commit `.gitignore` / `.gitattributes` (already in repo root).
- Create the `Assets/Scripts/...` folders and three scenes: **MainMenu**, **MetaHub**, **Battle**.
- Add `GameManager` + `SceneRouter`, an **AudioMixer** with exposed `MusicVol`/`SfxVol`, an
  `AudioManager`, and a **Main Menu** + **Settings** screen (music/SFX sliders, persisted).
- **Set landscape orientation** in Player Settings (Android first).
- **Provable:** app launches into Main Menu; Settings sliders change volume and persist across
  relaunch; New Run loads the Meta Hub scene.

### Phase 2 — Simulation core
- Implement `BattleSimulation`: fixed-tick loop, **unit data in flat arrays**, **spatial grid**,
  movement, **boid separation**, target acquisition, melee damage + armor model.
- Placeholder capsule/quad sprites; no animation yet.
- **Provable:** spawn 300v300, watch them close and fight to a result; **profile FPS** on device or
  in editor and confirm the budget is reachable.

### Phase 3 — Unit content + view
- Author `UnitDef` ScriptableObjects for **Swordsman, Archer, Spearman**.
- Build pooled `UnitView` with **sprite-sheet animation**, **culling + LOD**; add **pooled
  projectiles** for ranged units.
- **Provable:** the three types read distinctly on screen and interact correctly (ranged kite, melee
  close, spears hold).

### Phase 4 — Player interaction
- Commander **abilities** (≥1 damage, ≥1 support) with mana/cooldown and tap-to-place targeting.
- **Charge** and **Brace** unit commands (data-driven `CommandDef`).
- Battle **HUD** + **speed/pause** controls.
- **Provable:** abilities and commands visibly change the battle; pause/2× work.

### Phase 5 — Meta loop
- **Roster select**, **formation editor** (grid drag-and-drop + presets), **upgrade shop**
  (armor/weapon/ability), **buffs**, **healing/rest**.
- `RunState` + **Save** layer; **wounds persist** between battles.
- **Provable:** edit a formation, buy an upgrade, heal a unit, and carry that state into the next
  battle.

### Phase 6 — Roguelite glue
- Node progression (linear for the slice), win → reward + return to Meta Hub, lose → **Game Over** +
  **delete save (permadeath)**.
- First **balance pass**.
- **Provable:** full loop Menu → Hub → Battle → Resolution → Hub / Game Over; Continue disabled after
  a loss.

### Phase 7 — Polish gate
- On-device **profiling** to hit the perf gate; audio pass; **juice** (hit flashes, screen shake,
  death fades); full **slice acceptance run**.
- **Provable:** the whole slice plays end-to-end on a phone at the target frame rate.

---

## Part B — Feature Backlog (committed, post-slice)

Prioritize roughly top-to-bottom; each is content/system work enabled by the data-driven core.

1. **Remaining unit types** — Longbowman, Armored Swordsman, Cavalry (then expand).
2. **Unit synergies / formation bonuses** — spearmen counter cavalry, archers behind a shield wall,
   flanking bonuses; makes formation editing genuinely strategic.
3. **Commander / Hero unit** — its own leveling and the source of commander abilities; the chosen
   archetype defines a run's playstyle.
4. **Relics / blessings** — Slay-the-Spire-style run-altering passives from events/elites.
5. **Branching map** — elites, shops, rest, and random **choice events**.
6. **Morale / routing system** — units flee when morale breaks; adds drama *and reduces compute* as
   routed units leave the field.
7. **Terrain & deployment modifiers** — chokepoints, high ground, hazards per battlefield.
8. **Ascension / difficulty tiers** — unlocked by winning, for long-tail replayability.
9. **Persistent meta-progression** — unlock new units/commanders/relics permanently across runs.
10. **Boss armies** — unique mechanics at map milestones.
11. **Daily challenge + seeded runs + leaderboards** — retention and shareability.
12. **Replay / battle highlights + share** — auto-battlers are highly watchable → organic marketing.
13. **Cosmetics** — banner/color customization; monetization-friendly, non-pay-to-win.
14. **Monetization (if commercial)** — cosmetic IAP, optional ad-for-reward; **no pay-to-win**.

---

## Working rules
- **One phase per session.** Implement only that phase's scope, then stop for editor verification.
- Don't regress the **acceptance gates** in `CLAUDE.md` (performance, permadeath, settings
  persistence, full loop).
- Keep balance data in **ScriptableObjects**, not hard-coded.
