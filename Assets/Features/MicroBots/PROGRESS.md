# MicroBots — Progress

## Goal

A Big Hero 6-style feature: many small bots — each built from segments joined by internal elbows, with
exactly 2 open "extremity" joint slots at the ends — snap together at their extremities, and the
resulting assembled structure gains new functionality based on its shape (e.g. a "wheel" formation
rolls, a "claw" formation attacks). No multiplayer required.

Built in DOTS/ECS (`com.unity.entities` + `com.unity.entities.graphics`) rather than plain GameObjects,
for swarm-scale parallelism (Burst-compiled jobs processing hundreds/thousands of bots each frame) once
multi-bot work resumes.

**M1 (single microbot shape/movement) is done, per the owner.** Current focus has moved to M1.5 —
resuming the paused multi-bot scope (spawner, multiple bots, separation, climbing-over-other-bots). See
"Paused scope" below for what that covers.

## Single-bot behavior spec

- **Shape**: a bot is a chain of 2 segments joined by 1 internal elbow, with exactly 2 open extremities
  at the ends (the free ends of the two segments). Each segment has a fixed **length**.
- **Joint = extremity, unified**: a joint connects two segments, either pre-baked (an internal elbow,
  part of the bot's own template) or dynamically formed (two extremities mating via docking). All
  joints are **spherical (ball-and-socket)**, never single-axis hinges.
- **No local autonomy**: a bot never decides anything for itself — behavior is always resolved from an
  externally-assigned destination, never a strategic choice made by the bot.
- **Movement — IK foundation (built, being iterated on)**: each extremity (A and B) has its **own**
  persistent IK target entity (`MicrobotIkTargets.TargetAEntity`/`TargetBEntity` — see Decisions log for
  why this replaced the earlier single shared/snapped target). Whichever extremity is currently the
  **anchor** just has its target left untouched (frozen wherever it last was); root is solved every
  frame via planar 2-bone IK from the anchor's target position toward the free extremity's target
  position, which the step system actively moves. Which extremity is the anchor is a **toggle** —
  either the `T` key, or automatically whenever a step lands (see below) — not hardcoded to one segment.
- **Step movement only** — the earlier manual/step dual-mode toggle (`isManualMovement`) was removed
  once step mode was working; direct WASD-drag-the-target movement no longer exists. `W`/`S` trigger a
  step cycle — the target lerps toward `± StepSize` along a **rotatable heading** (not a fixed world
  axis) with a **computed** `sin(t·π) * StepHeight` vertical lift layered on top (not an authored
  `AnimationCurve` — see Decisions log), over a duration set by `StepSpeed`; landing (progress reaching
  1) automatically triggers the anchor toggle. `A`/`D` continuously rotate `MicrobotStepState
  .HeadingAngle` around Y (rate set by `TurnSpeed`) — each new step's direction is that heading's
  forward vector rotated by the current angle, so turning changes where the *next* step goes rather
  than spinning the currently-planted pose in place.
- **`com.unity.physics` is currently uninstalled — deliberately parked, not a revert-in-progress.**
  Installing it (tried 1.4.7 and 1.4.6, with and without the transitive Burst 1.8.28→1.8.29 bump)
  reliably breaks rendering: `LocalTransform` and `LocalToWorld` both keep updating correctly every
  frame (confirmed live in the Entities Inspector), the microbot mesh is visible in the Scene view, but
  it never visually moves — no Console errors or warnings at all. Ruled out so far: our own code/asmdef
  reference (reproduces even with zero `Unity.Physics` usage anywhere in `MicroBots`, purely from the
  package being present), the Burst version bump specifically (reproduced with Burst unchanged at
  1.8.28), transform-sync staleness (`LocalToWorld` does update), and Companion GameObject rendering (no
  `CompanionLink` component on the entity). Leading unconfirmed theory: Unity Physics's graphics-
  integration/interpolation system intercepts what Entities Graphics actually uploads for rendering,
  independent of the `LocalToWorld` component value shown in the Inspector — there's changelog history
  of similar bugs in past 1.x releases, but nothing confirmed for 1.4.6/1.4.7 on Unity 6000.4.0f1
  specifically. The originally-suspected `BaseIsSegmentB`-flicker theory (from the A/D rotation work)
  was a red herring — never reproduced without `com.unity.physics` installed, and this whole "target
  moves, arm frozen"-shaped symptom turned out to really be "nothing renders live," not an IK-state
  bug. **Next step, if resumed**: try newer `com.unity.physics` patch versions via Package Manager, or
  check `WorldRenderBounds` for staleness (untested). Uneven-ground step landing (the raycast feature)
  stays un-implemented until this is resolved — reverted back to flat anchor-height landing.

## Docking command (single-bot test, in progress)

Before building the spawner, testing whether a single microbot can be commanded to walk to a "dock" (two
world-space points) and be considered arrived once each extremity occupies one of the two points (either
assignment — extremity A on point 1 & B on point 2, or A on point 2 & B on point 1).

- **Dock is a fully standalone prefab** (`DockAuthoring` + `MicrobotDockPoints`: two child marker
  Transforms baked to two world-space `float3`s) — it has no knowledge of any microbot.
- **The command is a third, standalone link**, not baked into either `MicrobotAuthoring` or
  `DockAuthoring`: `MicrobotDockCommandAuthoring` sits on its own GameObject, references a microbot +
  a dock by `GameObject` reference. **Its `MicrobotDockCommand` component lives on the linker's own
  entity, not on the microbot's** — a Baker can only `AddComponent` on an entity it owns (its own
  primary entity, or one it created), never on another authoring component's entity; the first attempt
  tried adding it directly to the microbot's entity and hit a baking `InvalidOperationException`
  ("Entity doesn't belong to the current authoring component"). `MicrobotDockCommand` instead holds both
  `MicrobotEntity` and `DockEntity` references, and `MicrobotNavigationSystem` looks up the microbot's
  components via `ComponentLookup` rather than querying them directly off the command entity.
- **`MicrobotNavigationSystem`** (new, `[UpdateAfter(MicrobotInputSystem)] [UpdateBefore
  (MicrobotStepMovementSystem)]`) drives the *existing* step/turn gait toward the dock instead of
  replacing it: for each undocked `MicrobotDockCommand`, it resolves each extremity's real world
  position from the referenced microbot's `MicrobotIkState` (anchor tip via `AnchorWorldPosition`, free
  tip approximated as the IK target entity's position), decides once which extremity is assigned to
  which dock point (whichever pairing has lower total distance, cached via
  `AssignmentDecided`/`SwapAssignment`), then overwrites the shared `MicrobotInputState.MoveInput`
  singleton each frame: turn toward the free extremity's assigned point until roughly facing it, then
  step forward.
- **Dynamic step size to avoid overshoot** — `MicrobotStepState` gained `HasStepSizeOverride`/
  `StepSizeOverride`; `MicrobotNavigationSystem` writes `min(nominal StepSize, remaining distance to the
  free extremity's assigned point)` every frame while actively steering, and
  `MicrobotStepMovementSystem`'s step-start block uses that override (if set) instead of the nominal
  `StepSize`, consuming/clearing it once the step starts. Far from the dock this equals the normal fixed
  step size (no behavior change); close to it, steps shrink to exactly the remaining distance instead of
  overshooting past the point. This is also what makes docking work when the two dock points aren't a
  "natural" distance apart for the bot's stride — each extremity's *final* step can be sized to land
  exactly where needed, rather than being limited to fixed-size hops. Manual WASD stepping is unaffected
  (override is only ever set by navigation). Not yet tested in Play mode.
- **`MicrobotStepSettings` and `MicrobotStepState` merged into one component** (kept the `StepState`
  name) — every system that touched either one already queried/looked-up both together on the same
  entity every time (`MicrobotStepMovementSystem`, `MicrobotNavigationSystem`, `MicrobotAuthoring`'s
  Baker); the split wasn't earning its keep. The authored fields (`StepSize`, `StepSpeed`, `StepHeight`,
  `TurnSpeed`) and the runtime fields are grouped with a one-line comment separating them, but live in
  the same struct now. `MicrobotStepSettings.cs` was deleted.
- **Don't waste a gait cycle re-stepping an already-satisfied extremity** — the gait always alternates
  (every landed step auto-toggles anchor/free), so once the currently-free extremity reaches its own
  assigned point, its *next* turn would otherwise still fire a (harmless but wasted) near-zero step
  instead of letting the still-unsatisfied extremity move. `MicrobotNavigationSystem` now checks
  `freeReached` (is the currently-free extremity already within tolerance of its own point) each frame:
  if so, it skips the forward step entirely, steers heading toward the *anchor's* target instead (since
  that's who needs to move next), and force-requests a toggle via the shared `ToggleBase` singleton
  rather than waiting for a step to land. Because `MicrobotNavigationSystem` runs before
  `MicrobotStepMovementSystem`/`MicrobotIkSystem` in the same frame, this toggle actually takes effect
  immediately — no wasted frame at all, not just no wasted step.
- **Fixed: heading changes only applied once per step landing instead of continuously mid-step.**
  `HeadingAngle` was already updating every frame unconditionally (even mid-step), but
  `StepTargetPosition` was computed once, at step-start, and never revisited — so a step's direction was
  locked in the instant it began, and any turning that happened while it was still in flight only showed
  up at the *next* step's start. This made turning look like it happened in discrete bursts timed to
  step landings rather than smoothly. Fix: `MicrobotStepState.StepTargetPosition` (a fixed point) became
  `StepSignedDistance` (a fixed scalar — direction × distance, decided once at step-start); the actual
  target position is now recomputed every frame *during* the step from the anchor's current position and
  the *live* `HeadingAngle`, so the free extremity's step continuously curves toward wherever the bot is
  currently turning, converging exactly at landing instead of snapping.
- **Open bug (being re-tested): rapid multi-toggle glitch (segments briefly overlapping) right as the
  first point is reached, then it self-corrects.** The one-frame-settling-guard fix attempt (tracking
  `LastAnchorIsSegmentB`) did not fix it and was reverted. Not yet retested against the target-splitting
  change below, which removes a plausible root cause (see next entry) — status unknown until verified in
  Play mode.
- **Split the single shared/snapped IK target into two persistent per-extremity targets**
  (`MicrobotIkTarget.TargetEntity` → `MicrobotIkTargets.TargetAEntity`/`TargetBEntity`) — this exact
  split was tried once before, early on, and reverted back to a single snapped target; revisited now
  because the old single-target design meant `MicrobotIkState.AnchorWorldPosition` (the anchor's
  position, used by the dock/reached checks) was *recomputed via forward-kinematics*
  (`ComputeTipWorldPosition`, based on segment rotation) rather than read directly from an authoritative
  source — a genuinely different calculation than the free extremity's raw target-entity position, so
  the two could disagree by a hair right at a tolerance boundary. With two persistent targets, an
  extremity's position **is** its target entity's position, full stop, for both anchor and free roles —
  one consistent source, no more FK-vs-target mismatch. This also let `MicrobotIkSystem` drop its whole
  "needsAnchorRefresh / snap-target-on-toggle / continue-without-solving" branch entirely — the anchor's
  target simply stays wherever it was, so there's no pop to guard against, and the real IK solve now
  runs every frame unconditionally. `MicrobotIkState` shrank to just `BaseIsSegmentB`.
  **Requires a scene change**: `MicrobotAuthoring.target` (one Transform) is now `targetA`/`targetB`
  (two Transforms) — existing microbot prefabs/instances need a second target object assigned, or they
  won't bake correctly.
- **Turn-gate/heading-epsilon angles moved from hardcoded constants to authored fields** —
  `MicrobotAuthoring.turnGate`/`headingEpsilon` (Inspector-tunable) are plain degrees end-to-end, same as
  `turnSpeed` — no `Degrees`/`Radians` suffixes anywhere, and no conversion at bake time. They're stored
  in degrees on `MicrobotStepState.TurnGate`/`HeadingEpsilon`, and `MicrobotNavigationSystem` converts to
  radians with `math.radians(...)` only at the point of use, matching how `TurnSpeed` was already
  handled in `MicrobotStepMovementSystem`.
- **Known limitation, deliberately not solved yet**: `MicrobotInputState` is still one shared singleton
  across all bots (pre-existing issue, not introduced here) — this only behaves correctly with exactly
  one commanded bot. Multi-bot commanding needs per-bot input, which is out of scope until M1.5's
  spawner work makes it unavoidable.

## Paused scope (not deleted — resume after single-bot movement is satisfying)

- Multiple bots, spawner/spawn system.
- Separation/anti-overlap between roaming bots.
- Climbing over stationary/docked bots (ant-bridge style) and the standable-surface spatial-hash query
  that would detect it.
- The "dumb-follower shared-command" controller as a real system (currently just a fixed `destination`
  field set by hand in the Inspector for one bot — unused by the current IK/step movement).

## Roadmap

- [x] **M1 — Single bot moving well:** owner has called this done on flat ground. IK + step-movement
      (heading, height lift, auto-toggle-on-landing) all work end-to-end.
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
  - [~] Uneven-ground step landing via Unity Physics raycast — **deferred out of M1, not required to
        call M1 done.** `com.unity.physics` breaks rendering on install (see Decisions log); revisit
        later, possibly without Unity.Physics.
  - [x] Full Play-mode verification of the whole M1 loop end-to-end (see Status)
- [ ] **M1.5 — Resume paused scope:** multiple bots, spawner, separation, climbing-over-stationary-bots
      (see "Paused scope" above) — **current focus**, now that single-bot movement is satisfying
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
- **Unity Physics pulled in early for uneven-ground step landing** — originally deferred to M3 (joint
  constraints), but landing-height detection needed real colliders sooner. `MicrobotStepMovementSystem`
  now does **one downward raycast at step-start** (`SampleGroundHeight`, via
  `PhysicsWorldSingleton.CollisionWorld.CastRay`) against the intended X/Z landing point, using the hit
  height for `StepTargetPosition.y` (falls back to the anchor's current height if nothing is hit). Step
  *completion* stays purely time-based (`StepProgress >= 1`), unchanged — only the landing *height*
  comes from physics. Mid-arc obstacle clearance (making sure the arc's peak clears a hill mid-stride)
  is explicitly deferred until bots are observed clipping through terrain. Both `PhysicsWorldSingleton`
  and `CollisionWorld.CastRay` are Burst-compatible, so `[BurstCompile]` is unaffected.
  **Requires manual scene setup**: terrain/obstacle GameObjects need real Unity Physics collider
  components (not just visual meshes) in the SubScene for this to have anything to hit — not yet
  verified in Play mode.
- **Standalone feature** — per project rule, MicroBots does not reference or depend on any other
  `Assets/Features/` folder (e.g. Pooling, Conjure, Dependency) unless a dependency is explicitly
  requested later.

## Status

M1 is done: IK foundation, anchor toggle (manual `T` + automatic on step landing), and step movement
(horizontal progress, height lift, alternating anchors, A/D heading turn) all confirmed working together
by the owner in Play mode, on flat ground. Uneven-ground step landing is explicitly deferred (see
Decisions log) and does not block M1. Moving on to M1.5 next.
