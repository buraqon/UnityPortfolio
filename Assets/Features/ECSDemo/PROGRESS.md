# ECSDemo — Progress

## Goal

Learn Unity DOTS/ECS hands-on by building the smallest possible working example, one guided step
at a time. Not trying to demo a "real" gameplay system yet — just get a minimal ECS pipeline
running end-to-end and understand each piece before adding more.

## Basic implementation plan: spinning cubes ("Hello ECS")

- [x] 1. Set up feature folder + this progress doc.
- [x] 2. Install the `com.unity.entities` package (and `com.unity.entities.graphics`, needed to
      actually render entities) via `Packages/manifest.json`.
- [x] 3. Write a `RotationSpeedAuthoring` MonoBehaviour + Baker that converts a normal GameObject
      into an Entity carrying a `RotationSpeed` `IComponentData`.
- [x] 4. Write a `RotationSpeedSystem` (`ISystem`, Burst-compiled) that queries all entities with
      `RotationSpeed` + `LocalTransform` and rotates them every frame.
- [x] 5. Build a demo scene (`ECSDemo.unity`, matching the feature name — same convention as
      `Centipede/Centipede.unity`) containing a `SubScene` with a few cubes using the authoring
      component, so pressing Play shows them spinning.
- [x] 6. Press Play, verify in the Entities Hierarchy / Systems windows, note observations.

## Status

Steps 5-6 done: `ECSDemo.unity` + `ECSDemo_SubScene.unity` built through the Editor (owner-driven,
guided step by step) with 3 cubes in the SubScene, each carrying `RotationSpeedAuthoring` at 45/90/180
deg/sec. Pressed Play: all three spin at the correct differentiated speeds, no console errors —
confirms the full pipeline (Baker → `RotationSpeed` → `RotationSpeedSystem` job → `LocalTransform` →
Entities Graphics rendering) works end-to-end. "Hello ECS" checklist complete.

## Observations

- The Scene view does not visibly animate the spinning cubes during Play Mode by default — only the
  Game view updates every frame. This is standard Editor behavior (Scene view doesn't auto-repaint
  each frame unless "Always Refresh" is enabled in its toolbar), not an ECS/Entities quirk.

## Notes

- Working one step at a time on purpose — each step gets applied and explained before moving to
  the next, so the owner can read and understand what happened before continuing. This is a
  learning exercise, not a race to a finished feature.
- Step 5 (scene + SubScene) was done by the owner directly in the Editor, guided turn-by-turn in
  chat, rather than hand-authored as YAML — SubScene setup involves Editor-generated linkage that's
  fragile to write blind with no way to see the Console for errors.

## Stage 2 plan: spawn cubes at runtime

- [x] 1. Create a `Cube.prefab` asset (drag a cube out into the Project window) — the template entity
      we'll clone at runtime.
- [x] 2. Write a `CubeSpawnerAuthoring` MonoBehaviour + Baker that bakes a referenced prefab
      GameObject into an `Entity` reference, stored on a `CubeSpawner` `IComponentData`
      (`Entity Prefab`, `int Count`, `float Spacing`).
- [x] 3. Write a `CubeSpawnerSystem` (`ISystem`) that, on its first update, calls
      `EntityManager.Instantiate(prefab)` `Count` times and positions each clone.
- [x] 4. In the Editor: add an empty GameObject inside the SubScene, attach `CubeSpawnerAuthoring`,
      assign the prefab, set a count.
- [x] 5. Press Play, verify cubes spawn (in addition to the 3 baked ones) and spin.

Note on step 3: `CubeSpawnerSystem` calls `EntityManager.Instantiate`/`SetComponentData` directly on
the main thread rather than through `IJobEntity`, because these are *structural changes* (they change
an entity's archetype) — not job-safe, can't be called from inside `Execute`. If we ever need to
trigger spawning from inside parallelized per-entity job logic, that's what `EntityCommandBuffer` is
for (jobs record structural-change "commands" into it, played back on the main thread afterward) —
see TODO below.

Bug found during step 4/5 testing: cubes kept spawning every frame instead of once. Root cause was
almost certainly the SubScene being open for Live Baking while in Play Mode, which kept re-syncing the
authoring data and re-adding the `CubeSpawner` component right after we removed it (the original
design removed `CubeSpawner` after spawning, as a one-shot guard). Superseded by the stage 3 redesign
below, which makes the spawner persistent and trigger-driven instead of one-shot, so this is no longer
a concern. `ISystem.OnCreate` was considered as a "run once" hook but rejected: it fires at
World/system creation, which can be before the SubScene has finished streaming entities in, so the
spawner entity may not exist yet when it runs.

## Stage 3: spawn on input or timer, arrange in a 3D lattice

- [x] 1. Add `Interval`/`NextSpawnTime` to `CubeSpawner` + `CubeSpawnerAuthoring` (timer trigger;
      `Interval <= 0` disables it).
- [x] 2. Rewrite `CubeSpawnerSystem.OnUpdate` to be persistent (no longer removes `CubeSpawner` after
      one run) and spawn a batch whenever Space is pressed (`Keyboard.current`, new Input System) or
      the timer interval elapses.
- [x] 3. Rewrite the spawn-position math from a straight line to a 3D lattice: linear spawn index `i`
      decomposed into `x/y/z` grid coordinates via an integer `side` search (`side` grows until
      `side³ >= count`; avoids floating-point `pow`/`cbrt` rounding landing exact cubes like 8/27/64
      one bucket too high).
- [x] 4. In the Editor: set `Interval` on the `CubeSpawner` GameObject (or leave `0` and just press
      Space during Play) and verify.

Redesign: `Count` isn't the size of an independent lattice per trigger — each trigger *adds* `Count`
more cubes to one continuously growing lattice, and the whole lattice (existing cubes included) must
reflow to the size that fits the new total (5 cubes → 2x2x2 partially filled, 9th cube → the entire
thing becomes a 3x3x3). That requires remembering every cube spawned so far, so a `DynamicBuffer`
(`SpawnedCube`, wrapping an `Entity`) was added to the spawner entity via `AddBuffer<SpawnedCube>` in
the baker. `CubeSpawnerSystem` now appends new instances' entities to that buffer, then repositions
*every* buffered cube (old and new) each trigger via `RelayoutLattice`, so old cubes visibly shift when
the lattice grows into a bigger cube shape.

Bug found while testing: relayout was resetting every cube's rotation back to identity on every
trigger. Cause: `RelayoutLattice` wrote `LocalTransform.FromPosition(position)`, which constructs a
brand new `LocalTransform` (identity rotation, scale 1) rather than updating position in place —
overwriting whatever rotation `RotationSpeedSystem` had already accumulated. Fixed by reading the
existing `LocalTransform` via `GetComponentData`, mutating only `.Position`, and writing that back.
Confirmed fixed by the owner — rotation now persists across relayouts. Stage 3 checklist complete.

## What's next (not started)

Session paused here — likely to be revisited for more ECS/DOTS tutorials/stages rather than treated
as finished. Candidate directions, not yet decided:

- A second component/system to see how multiple systems and job dependencies interact.
- A query with more interesting filtering (e.g. `WithChangeFilter`, enableable components).
- **TODO: look into `EntityCommandBuffer`** — the job-safe way to perform structural changes
  (spawn/destroy/add/remove) from inside parallelized per-entity job logic, instead of the main-thread
  `EntityManager` calls used so far.
