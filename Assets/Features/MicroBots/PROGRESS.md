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
- **Movement — IK foundation (built, being iterated on)**: one extremity is a fixed **anchor** (its world
  position is cached and doesn't move on its own); root is solved every frame via planar 2-bone IK so
  the other extremity reaches toward a **target** entity's current position. Which extremity is the
  anchor is a **toggle** — either the `T` key, or automatically whenever a step lands (see below) — not
  hardcoded to one segment.
- **Step movement only** — the earlier manual/step dual-mode toggle (`isManualMovement`) was removed
  once step mode was working; direct WASD-drag-the-target movement no longer exists. `W`/`S` trigger a
  step cycle — the target lerps toward `± StepSize` along a fixed world Z axis with a **computed**
  `sin(t·π) * StepHeight` vertical lift layered on top (not an authored `AnimationCurve` — see
  Decisions log), over a duration set by `StepSpeed`; landing (progress reaching 1) automatically
  triggers the anchor toggle.
- **`A`/`D` heading rotation was attempted and reverted** — added `HeadingAngle`/`TurnSpeed` so `A`/`D`
  would rotate the step direction around Y instead of doing nothing. Right after this, the microbot's
  root/segments stopped updating entirely (target kept moving, arm didn't) — reverted fully (including
  the now-unused fields) to isolate the cause before deciding whether it was actually the heading change
  or a coincidental/separate issue. Not yet re-attempted or root-caused.

## Paused scope (not deleted — resume after single-bot movement is satisfying)

- Multiple bots, spawner/spawn system.
- Separation/anti-overlap between roaming bots.
- Climbing over stationary/docked bots (ant-bridge style) and the standable-surface spatial-hash query
  that would detect it.
- The "dumb-follower shared-command" controller as a real system (currently just a fixed `destination`
  field set by hand in the Inspector for one bot — unused by the current IK/step movement).

## Roadmap

- [ ] **M1 — Single bot moving well:** current focus, IK + step-movement working end-to-end — just
      needs tuning/iteration before checking this off.
  - [x] Install `com.unity.entities` + `com.unity.entities.graphics`
  - [x] `MicroBots.asmdef`
  - [x] Shape components: `MicrobotTag`, `MicrobotMovementTarget`, `MicrobotSegments`, `MicrobotAuthoring`
  - [x] IK foundation: `MicrobotIkTarget`, `MicrobotIkState`, `MicrobotIkSystem` (planar 2-bone solve,
        fixed anchor, toggle-swappable base/end roles)
  - [x] Input consolidation: `MicrobotInputState` singleton (holds toggle + raw move input),
        `MicrobotInputSystem` (only system that touches `Keyboard`, not Burst-compiled)
  - [x] ~~Manual movement mode~~ — built, then removed once step mode worked (see Decisions log)
  - [x] Step movement mode: `MicrobotStepSettings` (plain struct, computed `sin(t·π)` lift),
        `MicrobotStepState`, `MicrobotStepMovementSystem` (start/advance a step, auto-toggle on landing)
  - [x] ~~Open bug: step height doesn't visibly stick~~ — fixed, verified working (see Decisions log)
  - [ ] Full Play-mode verification of the whole M1 loop end-to-end (see Status)
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
- **IK is planar 2-bone, solved from anchor→target** — `forward` (the bend-plane direction) must be
  derived from anchor→target, not root→target or any other reference: the distance math only equals the
  true anchor-target distance when `forward` is anchor-based, since it's literally defined as that
  vector's horizontal direction. Two attempts at deriving it differently (to smooth the toggle
  transition) both broke the geometry and were reverted.
- **Anchor/end roles are a toggle, not fixed to one segment** — driven by `MicrobotIkState.BaseIsSegmentB`,
  flippable via `T` or automatically on step landing. Toggling recomputes the anchor from the newly-base
  segment's *live* rotation (not its authored rest pose), so it doesn't snap.
- **Snapping the target to the old anchor position on toggle is necessary, not optional** — without it,
  the newly-freed segment's rotation (which always points straight at wherever the target currently is)
  would visibly pop the instant roles swap, since the target likely isn't anywhere near that segment's
  current position. This was correctly identified after initially (incorrectly) removing it.
- **Suspected tension between the anchor-snap and step continuity turned out not to be a real problem
  in practice** — theorized that the snap (overwriting the shared target's position on every automatic
  toggle) would conflict with the step system's need to know where its last step landed, and drafted a
  fix (step system tracks its own last-landed position instead of re-reading the shared target) to
  address it. That fix was reverted and never re-applied, and the owner has since confirmed walking
  works fine as-is — this was a real risk worth identifying, but not an actual bug. No further action
  needed here.
- **Debugged further: even watching mid-step, the target's height genuinely never changed** — ruling
  out the anchor-snap as the (sole) explanation. Leading theory: `MicrobotStepSettings` was a *managed*
  component (`class`, to hold an `AnimationCurve`), baked once via `AddComponentObject` — suspected the
  baked curve wasn't actually carrying the authored keyframes (a managed-component/baking risk), so
  `Evaluate(t)` was silently returning `0` for every `t` regardless of what the Inspector showed.
  **Fix (confirmed working)**: removed `StepCurve`/`AnimationCurve` entirely — `MicrobotStepSettings` is
  a plain struct again, and the lift is computed directly as `math.sin(t * math.PI) * StepHeight` (same
  0→peak→0 shape as the authored curve, no baked-asset risk). Bonus: this also let
  `MicrobotStepMovementSystem` regain `[BurstCompile]`, since the `AnimationCurve` call was the only
  thing blocking it. Height now rises correctly during a step — verified by the owner in Play mode.
- **Manual movement mode was removed once step mode worked** — `MicrobotIkState.IsManualMovement`,
  `MicrobotAuthoring.isManualMovement`, and the direct-WASD-drag logic in `MicrobotIkSystem`
  (`HandleManualMovement`/`TargetMoveSpeed`/`moveDelta`) are all deleted. Every bot is step-mode only
  now — it was a useful A/B comparison while building the step system, not worth keeping as a
  permanent dual-mode toggle.
- **Input reading is centralized** in `MicrobotInputState` (a singleton) + `MicrobotInputSystem` (the
  only system touching `Keyboard`, necessarily not Burst-compiled) — other systems read the singleton
  instead of polling input themselves, keeping `MicrobotIkSystem`'s actual math Burst-compiled.
- **`state.CompleteDependency()` is required** at the top of any system here that directly writes
  `ComponentLookup<LocalTransform>` on the main thread (both `MicrobotIkSystem` and
  `MicrobotStepMovementSystem` do) — without it, Unity's built-in `LocalToWorldSystem` job can still be
  in flight, throwing `InvalidOperationException` on write. This was latent from early on but only
  surfaced once the step system's write path actually started executing.
- **Multi-bot/flocking/grouping/climbing is paused, not abandoned** — explicitly deferred until a
  single bot's movement is satisfying (owner's request). Design intent for it is preserved in "Paused
  scope" above and stays on the roadmap as M1.5.
- **Own `.asmdef`** (`MicroBots.asmdef`, namespace `HippoLib.MicroBots`) — deviates from the project's
  usual "everything in Assembly-CSharp" convention, chosen deliberately for compiler-enforced isolation
  from other features (per the standalone-feature project rule) and because it's standard practice for
  DOTS code.
- **Unity Physics still deferred** — no physics queries or constraints are used anywhere currently; the
  IK solve is pure analytic math.
- **Standalone feature** — per project rule, MicroBots does not reference or depend on any other
  `Assets/Features/` folder (e.g. Pooling, Conjure, Dependency) unless a dependency is explicitly
  requested later.

## Status

Working end-to-end, confirmed by the owner in Play mode: IK foundation, anchor toggle (manual `T` +
automatic on step landing), and step movement (horizontal progress, height lift, alternating anchors)
all function correctly together. Manual movement mode was built, verified, then removed once step mode
was ready — every bot is step-mode only now. What's left for M1 is tuning/iteration, not bug-fixing.
