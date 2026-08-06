# MicroBots — Progress

## Goal

A Big Hero 6-style feature: many small single- or double-joint bots roam/flock as a swarm, snap
together at defined sockets, and the resulting assembled structure gains new functionality based on
its shape (e.g. a "wheel" formation rolls, a "claw" formation attacks). No multiplayer required.

Built in DOTS/ECS (`com.unity.entities` + `com.unity.entities.graphics`) rather than plain GameObjects,
specifically because the roaming/flocking swarm is the part that benefits from Burst-compiled parallel
jobs at scale. The docking/assembly logic itself doesn't strictly need DOTS but rides on the same
entities once the swarm exists.

## Roadmap

- [ ] **M1 — Bare swarm proof:** packages installed, feature scaffolded, bots wander/separate and render
      via GPU instancing. No docking/joints/state yet.
  - [x] Install `com.unity.entities` + `com.unity.entities.graphics`
  - [ ] Scaffold code: `MicroBots.asmdef`, `MicrobotTag`/`MicrobotMovement` components,
        `MicrobotAuthoring` + `MicrobotSpawnerAuthoring` (Bakers), spawn system, wander/separation system
  - [ ] Create a microbot prefab (mesh + `MicrobotAuthoring` + `MeshFilter`/`MeshRenderer`)
  - [ ] Create `Demo/DemoScene_MicroBots.unity` with a `SubScene` containing a
        `MicrobotSpawnerAuthoring` GameObject
  - [ ] Verify in Editor: no Console errors, bots spawn/wander/separate/render correctly in Play mode
- [ ] **M2 — Docking detection:** spatial-hash based socket proximity queries; `Seeking`/`Docking` states
- [ ] **M3 — Assembly:** rigid connections via `Unity.Transforms` `Parent`/`LocalTransform` hierarchy;
      still-articulated connections via `Unity.Physics` hinge `PhysicsJoint` (Unity Physics gets
      installed at this point — deliberately deferred from M1)
- [ ] **M4 — Formation recognition:** graph-signature matching over the assembly's adjacency data →
      `FormationType` stamped on the assembly root
- [ ] **M5 — Formation functionality:** implement what a recognized shape actually *does*. **Open
      decision, not yet made:** pure-ECS systems acting on the assembly root vs. a companion GameObject
      bridging into MonoBehaviour-based gameplay systems (input, damage, etc.). Decide when this
      milestone starts.
- [ ] **M6 — Polish/demo:** tuning, visuals, final demo scene

## Decisions log

- **DOTS over plain GameObjects** — the swarm-scale roaming/flocking is what benefits from Burst/Job
  System parallelism; a small number of joint-based bots snapping together would not, on its own,
  justify DOTS.
- **State-gated systems, not a bespoke two-tier architecture** — a `Roaming` bot is driven by the
  flocking job; an `Assembled` bot is driven by transform-hierarchy propagation or a physics joint.
  This is normal ECS query gating (systems only picking up entities matching their query), not extra
  machinery to build.
- **Own `.asmdef`** (`MicroBots.asmdef`, namespace `HippoLib.MicroBots`) — deviates from the project's
  usual "everything in Assembly-CSharp" convention, chosen deliberately for compiler-enforced isolation
  from other features (per the standalone-feature project rule) and because it's standard practice for
  DOTS code.
- **Unity Physics deferred to M3** — M1 only needs `com.unity.entities` + `com.unity.entities.graphics`;
  Unity Physics is only needed once docking/joint constraints are being built.
- **Standalone feature** — per project rule, MicroBots does not reference or depend on any other
  `Assets/Features/` folder (e.g. Pooling, Conjure, Dependency) unless a dependency is explicitly
  requested later.

## Status

Design-only right now. M1 code was scaffolded once and then reverted at the owner's request — nothing
beyond this document currently exists on disk under `Assets/Features/MicroBots/`. The package install
checkbox above is checked because `com.unity.entities`/`com.unity.entities.graphics` were added
independently to `Packages/manifest.json` and were not reverted.
