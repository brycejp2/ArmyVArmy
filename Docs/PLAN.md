# ArmyVArmy — Design & Architecture Plan

## 1. Concept

A 2D, mobile-first roguelite in the spirit of *Slay the Spire*, where each encounter is a
**real-time auto-battle between the player's army and an AI army**. Win to advance through a map;
**lose a single battle and the run ends (permadeath)**. Between battles, the player manages a roster:
choose which units deploy, arrange the starting formation, buy upgrades, apply buffs, and heal.

### Confirmed pillars
- **Real-time auto-battler** — armies march and fight live on screen.
- **Limited live interaction** — commander **abilities/spells** plus **unit commands** (Charge,
  Brace), plus speed/pause.
- **Hundreds per side** — target **100–300 units/side** (~600 agents worst case).
- **Permadeath roguelite** — one lost battle ends the run.
- **Persistent wounds** — unit damage **carries between battles**; healing/rest is a real spend.
- **Landscape** orientation throughout.
- **Vertical slice first** — one full playable loop before breadth.

---

## 2. Tech stack

- **Unity 6 LTS**, **2D URP (2D Renderer)** — sprite batching, 2D lights, strong mobile perf.
  (Unity 2022.3 LTS is an acceptable fallback; only the editor version changes.)
- **Packages:** Input System (touch-first), TextMeshPro, Cinemachine (battle camera pan/zoom),
  Addressables (optional, content streaming later).
- **No Unity physics for unit-vs-unit combat** — movement, separation, and targeting run in a custom
  lightweight simulation (details below).
- **Platforms:** Android first, iOS second. **Landscape** orientation.
- **Source control:** Unity `.gitignore`, **force-text serialization** (mergeable scenes/prefabs),
  **Git LFS** for art/audio binaries.

### Project layout
```
Assets/
  Scenes/              # MainMenu, MetaHub, Battle
  Scripts/
    Core/              # GameManager, RunState, SceneRouter, ServiceLocator
    Sim/               # tick loop, unit data arrays, spatial grid, AI, separation
    Combat/            # abilities, commands, damage model
    Units/             # unit view (pooled sprite/anim), LOD/culling
    Meta/              # roster, formation editor, upgrades, buffs, heal, shop
    Data/              # ScriptableObject defs (see §5)
    UI/                # menus, HUD, settings, formation UI
    Audio/             # AudioManager + AudioMixer wiring
    Save/              # run + settings + meta-progression persistence
  ScriptableObjects/   # authored content assets
  Art/  Audio/  Prefabs/  Fonts/
Docs/                  # this folder
```

---

## 3. Core architecture

### 3.1 Simulation / View separation (the load-bearing decision)
The battle is a **fixed-tick simulation (~20–30 Hz) decoupled from rendering**. It looks real-time,
but the deterministic tick buys us: stable balance, trivial pause/speed-up, replayability, and a
clean place to optimize.

- **`BattleSimulation`** owns all units as **plain C# data in flat arrays** (data-oriented). It does
  **not** create one MonoBehaviour per unit running `Update`. This is the single biggest reason
  300/side is feasible on a phone.
- **`UnitView`** MonoBehaviours are **pooled** and only interpolate their transform/animation toward
  the latest sim state. Views are **culled when off-screen** and **LOD'd** (distant units drop to a
  cheap 1–2 frame "blob" sprite). Rendering is throttled; the sim is not.
- A **spatial uniform grid (buckets)** answers neighbor/target queries so each unit scans only its
  cell and neighbors — no O(n²) target search.

### 3.2 Performance budget (hundreds of units)
- **Shared sprite atlas + single material per faction/type** → few draw calls (URP 2D / SRP batcher).
- **Object pooling** for units, projectiles (archers/longbowmen), and hit VFX → zero per-frame
  instantiation.
- **No per-unit Rigidbody2D/Collider2D.** A grid-based boid-style **separation** step keeps ranks
  from collapsing into a single blob.
- **Sprite-sheet flipbook** animation driven from the sim; reserve Mecanim Animators for the rare
  hero/elite unit only.
- **Time-slicing:** target acquisition and AI re-evaluation run on a staggered schedule (each unit
  re-targets every N ticks, not every tick).
- **Acceptance gate:** ~60 FPS (stable 30 minimum) at **200v200 on a mid-range Android device**, with
  low draw calls and ~0 per-frame GC in battle.

### 3.3 Combat resolution
- Units have: HP, armor, damage, attack speed, range, move speed, ammo (ranged), class tags, current
  stance, current target, position/velocity.
- Melee: when within range of target, apply damage on attack cooldown; armor mitigates per the damage
  model. Ranged: spawn a pooled projectile toward target's predicted position; on hit, apply damage.
- **Stances** (from unit commands) modify speed/damage/defense/cohesion — read by the sim each tick.
- Win/lose: a side is defeated when it has no living, non-routed units (or a rout threshold is met).

### 3.4 Roguelite run / meta loop
- **`RunState`** (saved): roster, gold, **per-unit HP (damage persists)**, upgrades owned, map
  position, RNG seed. Permadeath = delete the save on loss.
- **Map:** branching node graph (battle / elite / shop / rest / event). The slice uses a **linear**
  sequence; branching comes later.
- **Meta Hub scene** hosts: roster select, formation editor, upgrade shop, buff application, heal/rest.

---

## 4. Player interaction

### 4.1 During battle (the "limited interaction")
- **Commander abilities:** thumb-reachable HUD buttons — e.g. **Heal** (support), **Volley/Fireball**
  (AoE damage), **Rally** (buff aura). Gated by **mana or cooldown**. Targeted spells use
  **tap-to-place** on the battlefield.
- **Unit commands (stances):** **Charge** (temporary speed + damage, reduced defense) and **Brace**
  (temporary defense + hold position, reduced movement). Issued army-wide or per selected group/lane.
  Implemented as data (`CommandDef`) so **Hold / Retreat / Flank** are cheap future adds.
- **Battle speed:** 1× / 2× / pause — nearly free thanks to the fixed-tick sim.

### 4.2 Between battles (Meta Hub)
- **Roster:** choose which owned units deploy, within a supply/capacity cap.
- **Formation editor:** grid drag-and-drop placement in the deployment zone; **save/load presets**.
- **Upgrades:** per-unit-type **armor / weapon / ability** tracks bought with gold.
- **Buffs:** apply consumable/temporary buffs to units or the whole formation before a battle.
- **Healing / Rest:** spend gold or use rest nodes to restore wounded survivors (wounds persist).

---

## 5. Data-driven content (ScriptableObjects)

Everything balance-related lives in ScriptableObjects so adding content is authoring, not coding:

- **`UnitDef`** — name, sprite/anim set, base stats (HP, armor, damage, attack speed, move speed,
  range, ammo), class tags, costs.
- **`UpgradeDef`** — tiered armor / weapon / ability upgrades with stat deltas + cost.
- **`AbilityDef`** — commander spells: cost/cooldown, target shape, effect.
- **`CommandDef`** — unit stances (Charge, Brace, …): stat/behavior modifiers + duration.
- **`BuffDef`** — pre-battle buffs.
- **`EncounterDef`** — enemy army composition + formation + reward for a node.
- **`RunConfig`** — map length, node mix, difficulty scaling curve.

### Starting unit set
Slice ships **Swordsman, Archer, Spearman** (proves melee / ranged / anti-charge interplay).
Post-slice content: **Longbowman, Armored Swordsman, Cavalry**, then expand.

---

## 6. Menus & audio

- **Main Menu:** New Run, Continue, Settings, Quit.
- **Settings:** **Music** and **SFX** volume sliders wired to an **AudioMixer** (exposed
  `MusicVol` / `SfxVol`, dB-mapped), master mute, and stubs for haptics/language. Persisted via
  PlayerPrefs / the Save layer.
- **`AudioManager`** singleton: pooled SFX sources, music crossfade.
- **Pause menu** in battle: resume / settings / abandon run.

---

## 7. Vertical slice — definition of done

A complete, shippable-feeling loop on device (landscape):

1. **Main Menu → New Run**, with working **Settings** (music/SFX persist across relaunch).
2. **Meta Hub:** small starting roster; pick units, arrange formation, buy ≥1 upgrade, apply a buff,
   heal a wounded unit.
3. **One real-time battle**, ~**150–200 units/side**, with auto melee+ranged combat via the tick sim,
   **≥2 commander abilities** (1 damage, 1 support), **Charge + Brace**, and speed/pause.
4. **Resolution:** win → Meta Hub with gold + surviving (damaged) units → next node; lose →
   **Game Over** and the run save is deleted.
5. **≥3 unit types** implemented (Swordsman, Archer, Spearman).
6. **Performance gate met** (200v200 on a mid-range Android device).

Everything else is post-slice content (see `ROADMAP.md`) and is intentionally cheap to add because of
the data-driven design.

---

## 8. Verification

Verification happens in the user's Unity editor and on-device (this repo's CI cannot run Unity):

- **Editor Play Mode:** walk the full flow Main Menu → Meta Hub → Battle → Resolution → Meta Hub /
  Game Over.
- **EditMode/PlayMode tests:** deterministic checks on fixed seeds — damage, target acquisition, and
  win/lose resolution (e.g. archers beat fewer swordsmen at range; spearmen on Brace out-trade
  charging cavalry). Fast, no rendering.
- **Profiling:** Unity Profiler on a mid-range Android device — confirm 200v200 holds the FPS target,
  draw calls stay low (atlas batching), and per-frame GC is ~0 (proves pooling works).
- **Settings persistence:** change sliders, relaunch, confirm restored.
- **Permadeath:** lose a battle, confirm the save is deleted and Continue is disabled.
- **Acceptance run:** complete the full vertical-slice loop end-to-end on device.
