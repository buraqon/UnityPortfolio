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
  Transforms baked to two world-space `float3`s) — no knowledge of any microbot.
- **The command is a third, standalone link** (`MicrobotDockCommandAuthoring`, its own GameObject,
  references a microbot + a dock by `GameObject`) — not baked into either `MicrobotAuthoring` or
  `DockAuthoring`. Its `MicrobotDockCommand` component lives on the *linker's* own entity (a Baker can
  only `AddComponent` on an entity it owns), holding `MicrobotEntity`/`DockEntity` references that
  `MicrobotNavigationSystem` resolves via `ComponentLookup`.

**Current architecture — unified goal-seeking primitive** (after several iterations, see below):
`MicrobotStepState` carries an optional active **goal** (`HasGoal`/`GoalPoint`/`GoalTolerance`) for
whichever extremity is currently free. `MicrobotStepMovementSystem` is the single authority for all
stepping — turning, dynamic step-sizing, height-targeting, and landing/arrival detection — regardless of
whether a goal is driving it or not:
- **No goal** (plain WASD): heading comes from `MoveInput.z`, step trigger from `MoveInput.y`, step size
  is the nominal `StepSize`, height always inherits the anchor's current height (flat) — i.e. exactly the
  original manual-walking behavior.
- **Active goal** (docking): heading turns toward `GoalPoint` (gated by `HeadingEpsilon`/`TurnGate`,
  same as before), step size is `min(StepSize, horizontal distance from anchor to GoalPoint)` (dynamic,
  avoids overshoot — measured from the anchor, matching how the step itself is actually applied, not
  from the free extremity's current position), and step height targets `GoalPoint.y` directly (no
  ground/raycast needed, since it's a known point).
- **Arrival is checked before *and* after each step**: if the free extremity is already within
  `GoalTolerance` before moving, it hands off immediately with no wasted step; if a step lands within
  tolerance, the goal clears and control hands off; if a step lands *short*, the goal stays active and
  **the normal per-landing toggle is suppressed** — the same extremity keeps taking steps toward the
  same goal (re-aimed/re-sized fresh each time) instead of alternating away mid-approach. Only when a
  goal is fully satisfied does control pass to the other extremity.
- **`MicrobotNavigationSystem` shrank to one job**: whenever the currently-free extremity has no active
  goal (`!stepState.HasGoal`) and the command isn't fully docked, give it one — its paired dock point
  (pairing decided once via `AssignmentDecided`/`SwapAssignment`, whichever pairing minimizes total
  distance). Since only the free extremity is ever assigned a goal, **sequencing is automatic**: the
  second point is never touched until the first extremity's goal clears and control has passed to the
  other one — matching the docking geometry assumption (the two points are close enough together that
  the second extremity can reach its point without the first ever needing to move again).
- **Bonus**: goal-driven bots no longer route through the shared `MicrobotInputState` singleton at all
  (they carry `HasGoal`/`GoalPoint` on their own `MicrobotStepState`) — this incidentally fixes the
  "one shared `MoveInput` for all bots" limitation *for docking*, though manual WASD control still
  shares it (fine for now, single-bot testing).

**How it got here** (compressed — see git history for full blow-by-blow): started as `MicrobotNavigationSystem`
overwriting the shared `MoveInput` singleton every frame plus a set of override flags on `MicrobotStepState`
(`HasStepSizeOverride`, `HasStepHeightOverride`, `ForceEnd`) bolted onto the pre-existing manual-walking
gait. That worked but kept accumulating special cases (dynamic sizing, height, skip-a-wasted-step,
height-gated-on-counterpart-anchored, unify-toggle-triggering) each as a separate mechanism layered on
top of the manual-gait code. Recognized these were all facets of one concept — "an extremity seeking a
goal point, turn as needed, dynamically sized, hands off on arrival" — and refactored into the single
`HasGoal` primitive above, which subsumed *all* of the override flags at once. Also fixed two real bugs
found along the way, now folded into the current design rather than listed separately: (1) dynamic
step-size distance must be measured from the anchor, not the free extremity's current position, since
that's how the step itself gets applied; (2) heading changes were only being applied once per step
landing instead of continuously mid-step (`StepTargetPosition`, a fixed point computed once at step-start,
became `StepSignedDistance`, a fixed scalar re-applied against the *live* heading every frame during the
step, so a step visibly curves toward wherever the bot turns mid-flight).

**Also split the single shared/snapped IK target into two persistent per-extremity targets**
(`MicrobotIkTarget.TargetEntity` → `MicrobotIkTargets.TargetAEntity`/`TargetBEntity`) — this exact split
was tried once before, early on, and reverted back to a single snapped target; revisited because the old
single-target design meant the anchor's position was *recomputed via forward-kinematics*
(`ComputeTipWorldPosition`, from segment rotation) rather than read directly — a genuinely different
calculation than the free extremity's raw target-entity position, so the two could disagree by a hair
right at a tolerance boundary. With two persistent targets, an extremity's position **is** its target
entity's position, full stop, for both anchor and free roles. This also let `MicrobotIkSystem` drop its
whole "needsAnchorRefresh / snap-target-on-toggle / continue-without-solving" branch — the anchor's
target simply stays put, so there's no pop to guard against, and the IK solve runs every frame
unconditionally. `MicrobotIkState` shrank to just `BaseIsSegmentB`. **Required a scene change**:
`MicrobotAuthoring.target` (one Transform) is now `targetA`/`targetB` (two Transforms).

- **Unified "dockable" concept (`Dockable`) so bots can dock onto other bots, not just static Dock
  prefabs** — the ECS answer to "an `IDockable` interface": `Dockable : IComponentData { float3 PointA;
  float3 PointB; }` replaces `MicrobotDockPoints`. `DockAuthoring` bakes it once, statically, same as
  before (Dock prefabs are *always* dockable). A new `MicrobotDockableStateSystem` (runs after
  `MicrobotStepMovementSystem`) adds/removes `Dockable` on microbots dynamically, based on whether
  they're idle (`!HasGoal && !stepping`) — live-updating its `PointA`/`PointB` to the bot's current
  `MicrobotIkTargets` positions while idle, removing the component entirely the moment the bot starts
  moving (via `EndSimulationEntityCommandBufferSystem`, since add/remove are structural changes).
  `MicrobotNavigationSystem` now reads `Dockable` uniformly regardless of source — no branching on what
  kind of entity a dock-list entry actually is. This makes "must be stationary to dock onto" an enforced
  invariant (a moving bot simply isn't found by the query) rather than a caveat, and it's meant as the
  first real building block of M2's "spatial-hash based extremity proximity queries" — a queryable set of
  currently-available attachment points is exactly what that needs, whether or not a spatial hash sits on
  top later. If a dock-list entry currently isn't `Dockable` (e.g. targeting a bot that's still walking
  elsewhere), `MicrobotNavigationSystem` just waits — doesn't assign a goal until it becomes available.
  **Not yet tested**: bot-to-bot docking (only tested against static `Dock` prefabs so far).
- **Quick test: a list of docks with a rest pause between them**, to exercise the system against
  multiple targets in sequence rather than just one. `MicrobotDockCommand.DockEntity` (single) became
  `CurrentDockIndex` + a `DynamicBuffer<MicrobotDockListElement>` (added via
  `MicrobotDockCommandAuthoring.docks`, a `List<GameObject>`); `RestTime`/`RestTimer`/`Resting`... — well,
  `Resting` ended up folded into the existing `Docked` flag rather than a separate state (once `Docked`,
  `MicrobotNavigationSystem` counts `RestTimer` down instead of doing anything else; at zero it advances
  `CurrentDockIndex` — wrapping back to `0` after the last dock, looping indefinitely — and resets
  `Docked`/`PointAClaimed`/`PointBClaimed` for the next one). Purely a test/demo addition, not part of
  the M1.5/M2 plan.
- **Dropped the fixed A/B↔point pairing decision entirely — it doesn't matter which extremity lands on
  which dock point**, only that each point ends up occupied by *someone*. Removed
  `AssignmentDecided`/`SwapAssignment`/the upfront `costDirect`/`costSwap` comparison; `MicrobotDockCommand`
  now just tracks `PointAClaimed`/`PointBClaimed`, updated live each frame by checking whether *either*
  extremity's current position is within tolerance of that point. When a new goal needs assigning, the
  free extremity gets whichever unclaimed point is nearer to it (or, if both are still open, whichever
  it's closer to). This also cleaned up `MicrobotNavigationSystem`'s role-awareness: reading `posA`/`posB`
  no longer needs `anchorIsB` at all (`TargetAEntity`/`TargetBEntity`'s positions directly), and
  `anchorIsB` is now only computed for the one narrow purpose of finding the free extremity's position
  for the nearer-point tie-break.
- **Fixed: ordinary long-distance strides toward a distant/elevated dock would jump into the air.**
  `targetHeight` was set to `GoalPoint.y` unconditionally for every goal-driven step, including the
  far-away, full-stride ones — so the very first step toward an elevated dock (or just a dock at a
  different height) tried to reach the *final* height in one hop instead of staying level while still
  many steps away. Fix: `targetHeight` is now `GoalPoint.y` only when `isFinalApproach` is true;
  otherwise it's hardcoded ground level (`0f`), not `anchorPos.y` — since the anchor itself can be
  elevated (left over from a previous docking approach that climbed), "match the anchor's current
  height" would carry a stale elevation forward into the next walk instead of returning to the ground.
  Applied the same fix to plain manual/ad-hoc stepping (`!hasGoal`) for consistency — ordinary ground
  walking should always be at `0`, not wherever the anchor happens to currently sit. Height only ever
  leaves `0` during a final-approach step toward a genuinely elevated goal.
- **Fixed: far-away goals would deadlock.** Suppressing the toggle whenever a goal was merely
  unsatisfied assumed the goal was always within one extremity's reach from a *stationary* anchor
  (bounded by `segmentALength + segmentBLength`) — fine for final docking alignment, but if `GoalPoint`
  is farther than that, every step gets physically clamped by the IK solve to the same max-reach
  boundary, so `distance(newPosition, GoalPoint)` never improves and the goal never clears — the anchor
  never gets a turn to advance either, so the whole body gets stuck short of a distant goal. Fixed with
  an **implicit path**, no waypoint list needed: `MicrobotStepState.IsFinalApproach` records, at
  step-start, whether this step's distance was clamped by `StepSize` (still far — ordinary full-stride
  step) or by the goal's actual remaining distance (`remaining <= StepSize` — close enough to possibly
  finish this step). On landing, only suppress the toggle when `IsFinalApproach` was true; otherwise
  toggle normally, same as ordinary walking. Repeating full-stride steps with normal alternation
  naturally produces a path of steps toward a distant goal — generated one hop at a time instead of
  precomputed — and only the last leg, once within `StepSize`, switches to the no-toggle precise
  convergence behavior. Needed no new capability in `MicrobotNavigationSystem` at all.
- **Open bug, not yet retested against the current architecture**: a rapid multi-toggle glitch (segments
  briefly overlapping) right as the first point was reached, then self-correcting. Seen before the
  target-splitting and goal-unification refactors — both plausibly related (the old FK-vs-target mismatch,
  and the old per-landing-toggle-always-fires behavior that the new suppress-while-goal-active logic
  replaces). Status unknown until re-verified in Play mode.
- **Turn-gate/heading-epsilon angles are authored fields, plain degrees end-to-end** —
  `MicrobotAuthoring.turnGate`/`headingEpsilon`, no `Degrees`/`Radians` suffixes anywhere, converted to
  radians only at the point of use (`math.radians(...)`), matching how `TurnSpeed` already worked.
- **Known limitation, deliberately not solved yet**: manual WASD control still shares one
  `MicrobotInputState` singleton across all bots — fine for single-bot testing, needs per-bot input once
  M1.5's spawner makes multi-bot manual control relevant (goal-driven/docking bots are unaffected, see
  above).
- **Next up**: unification just landed, not yet tested in Play mode end-to-end.

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
