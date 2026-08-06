# MicroBots — Progress

## Goal

A Big Hero 6-style feature: many small bots — each built from segments joined by internal elbows, with
exactly 2 open "extremity" joint slots at the ends — snap together at their extremities, and the
resulting assembled structure gains new functionality based on its shape (e.g. a "wheel" formation
rolls, a "claw" formation attacks). No multiplayer required.

Built in DOTS/ECS (`com.unity.entities` + `com.unity.entities.graphics`) rather than plain GameObjects,
for swarm-scale parallelism (Burst-compiled jobs processing hundreds/thousands of bots each frame) once
multi-bot work resumes.

**Current focus, per the owner: get a single microbot's own shape/movement right before touching
anything involving multiple bots.** Flocking, grouping, climbing-over-other-bots, and the multi-bot
spawner are explicitly paused — see "Paused scope" below.

## Single-bot behavior spec

- **Shape**: a bot is a chain of 2 segments joined by 1 internal elbow, with exactly 2 open extremities
  at the ends (the free ends of the two segments). Each segment has a fixed **length**.
- **Joint = extremity, unified**: a joint connects two segments, either pre-baked (an internal elbow,
  part of the bot's own template) or dynamically formed (two extremities mating via docking). All
  joints are **spherical (ball-and-socket)**, never single-axis hinges.
- **No local autonomy**: a bot never decides anything for itself — behavior is always resolved from an
  externally-assigned destination, never a strategic choice made by the bot.

## Paused scope (not deleted — resume after single-bot movement is satisfying)

- Multiple bots, spawner/spawn system.
- Separation/anti-overlap between roaming bots.
- Climbing over stationary/docked bots (ant-bridge style) and the standable-surface spatial-hash query
  that would detect it.
- The "dumb-follower shared-command" controller as a real system (currently just a fixed `destination`
  field set by hand in the Inspector for one bot).

## Roadmap

- [ ] **M1 — Single bot moving well:** current focus.
  - [x] Install `com.unity.entities` + `com.unity.entities.graphics`
  - [x] `MicroBots.asmdef`
  - [x] Components: `MicrobotTag`, `MicrobotMovementTarget` (destination), `MicrobotSegments` (entity
        refs + rest rotation + length per segment)
  - [x] `MicrobotAuthoring` — root entity + `segmentA`/`segmentB` transform references (local rotation
        only, pinned at root) plus explicit `segmentALength`/`segmentBLength`, baked into
        `MicrobotSegments`; segment meshes are plain visual children (Entities Graphics converts them
        automatically)
  - [ ] Locomotion — not yet designed
  - [ ] Manual Editor setup: root + `Segment A`/`Segment B` (pinned at root, rotation-only, plus length
        fields) + `MicrobotAuthoring`, in a `SubScene`
  - [ ] Verify in Play mode
  - [ ] Iterate until satisfied
- [ ] **M1.5 — Resume paused scope:** multiple bots, spawner, separation, climbing-over-stationary-bots
      (see "Paused scope" above) — once single-bot movement is satisfying
- [ ] **M2 — Docking detection:** spatial-hash based extremity proximity queries; `Seeking`/`Docking`
      states; define what turns a foot landing into "docked" (occupied flags, joint formation)
- [ ] **M3 — Assembly:** rigid connections via `Unity.Transforms` `Parent`/`LocalTransform` hierarchy;
      still-articulated connections via `Unity.Physics` spherical/ball-and-socket joint constraints,
      never single-axis hinges (Unity Physics installed at this point, used only for joint constraints)
- [ ] **M4 — Formation recognition:** graph-signature matching over the assembly's adjacency data →
      `FormationType` stamped on the assembly root
- [ ] **M5 — Formation functionality:** implement what a recognized shape actually *does*. **Open
      decision, not yet made:** pure-ECS systems acting on the assembly root vs. a companion GameObject
      bridging into MonoBehaviour-based gameplay systems (input, damage, etc.). Decide when this
      milestone starts.
- [ ] **M6 — Polish/demo:** tuning, visuals, final demo scene

## Decisions log

- **DOTS over plain GameObjects** — swarm-scale parallelism is the long-term payoff once multi-bot work
  resumes, even though a single bot's own behavior is fully deterministic/non-parallel by nature.
- **Joint = extremity unification** — internal elbows and inter-bot connectors are the same underlying
  concept (a joint between two segments); extremities are just joints that haven't been mated yet.
- **All joints are spherical (ball-and-socket), never single-axis hinges.**
- **Multi-bot/flocking/grouping/climbing is paused, not abandoned** — explicitly deferred until a
  single bot's movement is satisfying (owner's request). Design intent for it is preserved in "Paused
  scope" above and stays on the roadmap as M1.5.
- **Own `.asmdef`** (`MicroBots.asmdef`, namespace `HippoLib.MicroBots`) — deviates from the project's
  usual "everything in Assembly-CSharp" convention, chosen deliberately for compiler-enforced isolation
  from other features (per the standalone-feature project rule) and because it's standard practice for
  DOTS code.
- **Unity Physics still deferred** — no physics queries or constraints are used anywhere currently.
- **Standalone feature** — per project rule, MicroBots does not reference or depend on any other
  `Assets/Features/` folder (e.g. Pooling, Conjure, Dependency) unless a dependency is explicitly
  requested later.

## Status

What exists on disk: `MicrobotTag`, `MicrobotMovementTarget`, `MicrobotSegments` components, and
`MicrobotAuthoring` (bakes a root entity + 2 segment entities from `segmentA`/`segmentB` transform
references, lengths, and a destination). No locomotion/movement system exists — the bot currently has
shape but no way to move. Being built step by step from here.
