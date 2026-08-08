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

- **Spawner added (M1.5 start)** — `MicrobotSpawner`/`MicrobotSpawnerAuthoring`/`MicrobotSpawnSystem`:
  a one-shot system that instantiates `SpawnCount` copies of a microbot prefab at random positions
  within `SpawnAreaSize` around the spawner's position (plus a fixed `spawnHeight` vertical offset), then destroys the spawner entity so it doesn't
  re-fire. Spawned bots have no dock command by default — they're idle (and thus `Dockable`, once
  `MicrobotClimbPointsSystem` picks them up) until something assigns them one.
  **Required prefab-setup change**: `MicrobotAuthoring`'s `targetA`/`targetB` now bake with
  `TransformUsageFlags.WorldSpace` instead of `.Dynamic`, so they can be nested as children of the
  microbot prefab root (needed for the whole bot — root, segments, targets — to instantiate as one
  linked group) while still baking to world-space `LocalTransform.Position`, which every system already
  assumes. Should be a no-op for the existing scene setup (unparented targets bake the same either way);
  worth double-checking if anything looks off after this change. **Bug found and fixed**: the first
  attempt used `TransformUsageFlags.WorldSpace` *alone* (replacing `.Dynamic` instead of combining with
  it) — that dropped the runtime-movable `LocalTransform` request entirely, so target entities baked
  without a usable `LocalTransform`, causing a Burst-abort `ArgumentException`
  (`AppendRemovedComponentRecordError`) the moment `MicrobotNavigationSystem` indexed into it. Fixed by
  using `TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace` (they're meant to be combined,
  not substituted). Also added an explicit `HasComponent` guard in `MicrobotNavigationSystem` before
  indexing target entities' transforms, so a stale/invalid entity reference (e.g. a leftover authoring
  reference from reorganizing the prefab) skips gracefully instead of crashing the whole system.
  **Manual Editor work still needed** (can't be done from here): turn the existing microbot setup into
  an actual Prefab asset (root + segmentA/segmentB as children), assign it to a
  `MicrobotSpawnerAuthoring` GameObject in the SubScene. Not yet tested in Play mode.
- **Removed target Transforms (and even the idea of an authored offset) entirely — `targetA`/`targetB`
  no longer exist as fields at all.** This fixes a real bug the prefab/spawner setup exposed: target
  entities baked with `TransformUsageFlags.WorldSpace` are deliberately *unparented* (needed so their
  `LocalTransform.Position` reads as world position, which every system assumes) — but that also means
  they don't follow the root at runtime. When `MicrobotSpawnSystem` repositioned only the root to a
  random spawn point, every spawned bot's targets would've stayed wherever the *original prefab* was
  authored in the Editor, not moved to the new spawn position. First fix attempt added authored
  `targetAOffset`/`targetBOffset` `Vector3` fields — unnecessary, since a target's starting position is
  just "wherever that segment's tip already is," fully derivable from data `MicrobotAuthoring` already
  has (segment rotation + length), the same forward-kinematics math the old (now-removed)
  `ComputeTipWorldPosition` used. So the Baker computes it directly:
  `rootRotation * segment.localRotation * forward * length`, no field, no manual entry, no way for it to
  be wrong. `MicrobotAuthoring`'s Baker creates the target entities via `CreateAdditionalEntity` (no
  GameObject needed at all); `MicrobotIkTargets` still carries the resulting offsets
  (`TargetAOffset`/`TargetBOffset`) so `MicrobotSpawnSystem` — which switched from a deferred
  `EntityCommandBuffer` to direct `EntityManager` calls specifically so it can immediately read back each
  clone's `MicrobotIkTargets` (entity references already remapped to the clone's own targets) — can
  reposition targets to `spawnPosition + offset`, not just the root. No manual per-bot setup needed at
  all now; segment length/rotation is the only input, same as it always was.
  **Bug found and fixed**: the target entities' `LocalTransform` was set via `SetComponent`, which
  assumes the component already exists — but `CreateAdditionalEntity` doesn't auto-add `LocalTransform`
  the way GameObject-derived entities do (there's no source Transform for the baking pipeline to default
  from), so it didn't exist yet at that point and baking threw
  (`AssertEntityHasComponent`/`AppendRemovedComponentRecordError`, spamming even outside Play mode since
  SubScene live-conversion re-bakes continuously). Fixed by using `AddComponent` instead.
- **Fixed: `MicrobotSpawnSystem` crashed on run** (`InvalidOperationException: Structural changes are
  not allowed while iterating over entities`) — `state.EntityManager.DestroyEntity(entity)` was called
  on the spawner entity *while still inside* the `foreach` over the same `MicrobotSpawner` query that
  produced it; destroying the entity you're currently iterating invalidates the chunk mid-iteration.
  `Instantiate`/`SetComponentData` on the newly-spawned (different-archetype) entities didn't trigger
  this. Fixed by collecting spawner entities into a `NativeList` during the loop and destroying them in
  a separate pass after it finishes.
- **Spawner can now automatically dock every bot it spawns onto the same dock** —
  `MicrobotSpawner`/`MicrobotSpawnerAuthoring` gained an optional `dock` reference (plus
  `tolerance`/`restTime`); if set, `MicrobotSpawnSystem` creates a `MicrobotDockCommand` linker entity
  (with a one-entry `MicrobotDockListElement` buffer pointing at that dock) for each bot right after
  spawning it. **Expected behavior worth knowing going in**: since each bot's claim-tracking
  (`PointAClaimed`/`PointBClaimed`) is independent per `MicrobotDockCommand`, nothing coordinates between
  different bots targeting the *same* dock — they'll all genuinely converge on the identical two points
  and overlap, not spread out or queue. No reservation/uniqueness system exists yet (that's the harder
  N-way-junction problem flagged earlier, not solved here).
  **Bug found (unconfirmed, being retested)**: no `MicrobotDockCommand` linker entities were showing up
  at all for spawned bots, despite the spawner's `dock` field being correctly assigned. Suspect cause:
  `state.EntityManager.CreateEntity(typeof(MicrobotDockCommand))` used `typeof()`, which goes through
  managed .NET reflection — inside `[BurstCompile]` code that's the kind of thing that can misbehave
  silently rather than throw a loud, visible error (unlike the Burst aborts seen elsewhere in this log).
  Changed to `ComponentType.ReadWrite<MicrobotDockCommand>()`, the canonical Burst-safe way to construct
  a `ComponentType` (resolved via generics at compile time, not runtime type lookup). Not yet confirmed
  this was the actual root cause.
  **Real root cause found**: the `typeof()` theory was a red herring — `AddBuffer` (also a structural
  change) was being called while still inside the `foreach` over the `MicrobotSpawner` query, the exact
  same class of bug as the earlier `DestroyEntity` crash, just not fully fixed there (only the destroy
  call had been pulled out of the loop, not the rest of the structural changes). Fixed properly this
  time by fully separating concerns: one pass over the query just *reads* spawner data into a
  `NativeList<MicrobotSpawner>` (plus the entities to remove), then a second pass, entirely outside the
  query iteration, performs every structural change (`Instantiate`, `SetComponentData`, `CreateEntity`,
  `AddBuffer`, `DestroyEntity`).
- **Moved auto-docking out of the spawner entirely, into its own system.** `MicrobotSpawner` is back to
  pure spawning (no `DockEntity`/`DockTolerance`/`DockRestTime`) — cleaner separation of concerns, and
  sidesteps a real lifecycle problem: the spawner entity is destroyed once it's done spawning, so it
  couldn't have stayed around as a persistent source of "which dock to use" anyway. New pieces:
  `MicrobotAutoDockConfig` (a singleton — `DockEntity`/`Tolerance`/`RestTime`, baked once from a
  standalone `MicrobotAutoDockAuthoring` GameObject, persists for the whole session), `MicrobotDockAssigned`
  (an empty tag marking a bot as already handled), and `MicrobotAutoDockSystem`
  (`[UpdateAfter(MicrobotSpawnSystem)]`) — each frame, finds every microbot with `MicrobotTag` but
  *without* `MicrobotDockAssigned`, creates a `MicrobotDockCommand` linker for it targeting the
  configured dock, and tags it so it isn't reprocessed. Runs continuously rather than as a one-shot, so
  it naturally picks up newly-spawned bots whenever they appear, with no explicit dependency on spawn
  timing. Same two-pass structural-change pattern as the spawner (collect entities during read-only
  iteration, do all `CreateEntity`/`AddBuffer`/`AddComponent` calls after).
- **Reverted the `targetA`/`targetB` → `Vector3` offset change (and `CreateAdditionalEntity`) back to
  plain child Transforms.** In practice it caused legs to snap and rotate incorrectly — never fully
  root-caused (leading suspicion: the geometrically-computed offset, derived from `segment.localRotation`
  + length, didn't actually match the real authored segment geometry closely enough, versus manually-
  placed Transforms which are exact by construction) — owner asked to revert to the known-working state
  rather than keep debugging mid-flow. `MicrobotAuthoring` has `targetA`/`targetB` (`Transform`) fields
  again, baked via `GetEntity(..., TransformUsageFlags.Dynamic | TransformUsageFlags.WorldSpace)`;
  `MicrobotIkTargets` is back to just the two entity references, no offsets. **This reintroduces the
  spawn-repositioning limitation** the offset approach was originally meant to fix: `MicrobotSpawnSystem`
  now only repositions the spawned root, not its targets, so a spawned bot's targets stay wherever the
  *prefab* was authored in the Editor rather than following the new spawn position — a known,
  understood gap now, not a mystery, and a deliberate tradeoff for a stable pause point. **Also**: any
  microbot prefab/instance reconfigured during the Vector3 detour needs its `targetA`/`targetB` Transform
  references reassigned again in the Inspector.
- **Went back to the `float3`-based `MicrobotIkTargets` after all** (owner-driven, iterated directly in
  the systems this time rather than via the Baker) — `MicrobotIkTargets` is `{ float3 TargetAPos; float3
  TargetBPos; }` again, no entity references, no separate target entities at all. `MicrobotStepMovementSystem`,
  `MicrobotNavigationSystem`, and `MicrobotClimbPointsSystem` all read/write these fields directly on
  the bot's own component instead of going through a `ComponentLookup<LocalTransform>` indirection to a
  separate entity. **Initial values are set lazily, not at bake time**: `MicrobotIkState` gained
  `Initialized`; on a bot's first `MicrobotIkSystem` update, if `!Initialized`, it computes both tip
  positions from current geometry (`rootPos + rotate(rootRotation * segment.localRotation, forward) *
  length` — same forward-kinematics formula used throughout this feature's history) and writes them into
  `TargetAPos`/`TargetBPos` before doing anything else that frame. This was chosen over doing the init in
  `OnCreate` specifically because `OnCreate` only fires once, when the *system* is created — not once per
  entity, and not guaranteed to run after a bot actually exists (spawned bots created later via
  `MicrobotSpawnSystem` would never get initialized if this lived in `OnCreate`). Lazy per-entity init on
  first `OnUpdate` correctly handles both scene-baked and runtime-spawned bots uniformly. **This also
  incidentally fixes the spawn-repositioning gap** noted above — since target values are computed fresh
  from wherever the entity actually is (not baked once and carried forward), a spawned bot's targets
  naturally end up correct without `MicrobotSpawnSystem` needing to know about them at all. Not yet
  verified in Play mode.
- **Fixed: dock commands never started at all.** `MicrobotNavigationSystem`'s query tried to pull
  `RefRO<MicrobotIkTargets>` directly alongside `RefRW<MicrobotDockCommand>`/
  `DynamicBuffer<MicrobotDockListElement>` from the same entity — but `MicrobotIkTargets` lives on the
  *microbot's* entity, while the dock command components live on the separate *linker* entity (by
  design, per the standalone-link architecture). No single entity ever has all three components at once,
  so the query matched zero entities and the whole system body never ran, for any dock command, ever.
  Fixed by going back to a `ComponentLookup<MicrobotIkTargets>` indexed by `dockCommand.MicrobotEntity` —
  the same pattern already used for `ikStateLookup`/`stepStateLookup` in the same method.
- **Unified "dockable" concept (`Dockable`) so bots can dock onto other bots, not just static Dock
  prefabs** — the ECS answer to "an `IDockable` interface": `Dockable : IComponentData { float3 PointA;
  float3 PointB; }` replaces `MicrobotDockPoints`. `DockAuthoring` bakes it once, statically, same as
  before (Dock prefabs are *always* dockable). A new `MicrobotClimbPointsSystem` (runs after
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

## Structure blueprint — target shape → bot count → connectivity graph

A standalone planning/math layer under `Scripts/Authoring/Blueprint/`, deliberately decoupled from
the ECS/runtime code (pure C#, no entities, nothing consumes it yet). Full write-up and derivations in
[Scripts/Authoring/Blueprint/BLUEPRINT.md](Scripts/Authoring/Blueprint/BLUEPRINT.md). **Anyone picking this
up should read [Scripts/Authoring/Blueprint/README.md](Scripts/Authoring/Blueprint/README.md) first** — it's
the contract (data model, guarantees, how to generate and read one, gotchas); BLUEPRINT.md is the *why*
behind the math. The short version:

- **Target = the cube's 12 edges (wireframe), not faces or interior.** A bot is a 1-D strut, so struts tile
  curves cheaply and areas/volumes expensively (2m cube: 36 bots as a wireframe, ~110 as a face lattice
  (~160 braced), thousands as a solid). The wireframe still exercises the genuinely new thing — junctions where 3 bots
  meet — while every other link stays a straight chain.
- **`BotSpanSpec`** models a bot as a structural element: `Reach = LengthA + LengthB`, usable
  `MaxSpan = Reach · 0.9` (never span the full reach — that's the 2-bone IK singularity, the same boundary
  the far-away-goal deadlock ran into), `MinSpan = |LengthA − LengthB|`, plus the elbow angle a given span
  implies.
- **Remainder policy: uniform span, fixed segments.** Segment lengths are hardware (one prefab for the
  whole swarm) — don't shrink them to fit. Divide each edge into `n = ceil(L / MaxSpan)` equal spans of
  `L/n` and let the elbow bend more, which is what the IK does anyway. Proven bound: the resulting span is
  always in `(MaxSpan/2, MaxSpan]` for `n ≥ 2`, so no bot is ever asked to fold below half-extended, at any
  cube size. The rejected alternative (full-span bots + one short remainder) can silently produce a
  remainder bot below `MinSpan`, i.e. unbuildable, depending on cube size.
- **Totals**: `bots = 12n`, `nodes = 8 + 12(n−1)`. With the current bot (0.5/0.5, reach 1.0): a 2-unit cube
  is 36 bots / 32 nodes, a 10-unit cube is 144 bots / 140 nodes — linear in cube size, not cubic.
- **`StructureBlueprint` is a general graph, not a chain of pairs**: nodes carry a **capacity** (mid-edge
  nodes 2, the 8 corners 3), links carry their own two attachment points (so a link is directly consumable
  as a `Dockable`-shaped pair of world points) plus which cube edge they came from. Corner sharing is
  **structural, by construction** — canonical 3-bit corner indices and a bit-rule edge enumeration, never
  float-position dedup. `Validate` checks valence-sum, capacity, span-in-range and orphans;
  `BuildOrderFrom` gives BFS placement order so every bot has an already-anchored endpoint when its turn
  comes.
- **`CubeBlueprintSource` (MonoBehaviour) holds one blueprint in the scene.** Cube center is
  `transform.position`, so dragging the GameObject moves the blueprint; the rest (cube size, segment
  lengths, span safety, corner hub radius) are inspector fields. Rebuilds lazily whenever the settings it
  was built from stop matching the current ones — one mechanism covering both inspector edits and
  transform moves, so it can't go stale. The blueprint is **not serialized**: regenerating is deterministic
  and takes microseconds, so caching it in the scene file would only create a staleness risk. Draws the
  node/link graph as gizmos (alternating link colours so individual bots are countable, hubs bigger and
  yellow). **Must live in the main scene, not the SubScene** — plain MonoBehaviours in a baked SubScene
  don't exist at runtime.
- **What it will ask of the runtime later** (flagged, not worked around): `Dockable` exposes exactly two
  points and joints mate exactly two extremities, but a cube corner needs **three** coincident — an
  attachment site with capacity + occupancy is unavoidable for any closed shape (every polyhedron vertex
  has valence ≥ 3). Also needs an assignment layer (which bot takes which link). No change to IK, stepping
  or navigation.
- **Deferred, documented in the write-up**: corner hub radius (implemented, defaults to 0), Euler-trail
  routing (4 trails instead of 12 runs — saves up to 33% on small cubes, chamfers the corners), general
  meshes, and the fact that a ball-jointed wireframe cube is a *mechanism, not a structure* (it shears
  without diagonals or stiff joints).

## Paused scope (not deleted — resume after single-bot movement is satisfying)

- [x] Multiple bots, spawner/spawn system.
- [ ] Separation/anti-overlap between roaming bots.
- [ ] Climbing over stationary/docked bots (ant-bridge style) and the standable-surface spatial-hash query
      that would detect it.
- [x] The "dumb-follower shared-command" controller as a real system — `MicrobotFollowCommand` singleton
      + a pass inside `MicrobotNavigationSystem` (see Decisions log).

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
- [~] **Blueprint (planning layer, off to the side — not a milestone gate):** target shape → bot count →
      connectivity graph, pure C# under `Scripts/Authoring/Blueprint/`. Cube wireframe done and validated,
      `CubeBlueprintSource` MonoBehaviour holds one in the scene; consuming a
      blueprint at runtime is unscheduled and belongs to M3.
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
- **Removed the auto-dock system entirely.** Deleted `MicrobotAutoDockConfig`, `MicrobotAutoDockAuthoring`,
  `MicrobotAutoDockSystem`, and `MicrobotDockAssigned` (components/authoring/systems). The
  `MicrobotDockCommand`/`MicrobotDockListElement` linker + `MicrobotNavigationSystem` path (the one just
  fixed above) is unaffected — it's a separate, standalone way of driving a bot to dock via an explicitly
  authored `MicrobotDockCommandAuthoring` linker, not dependent on the auto-assigning system. No other
  files referenced the removed types (checked scenes/prefabs too), so this was a clean removal.
- **Spawner no longer self-destructs, and now tracks what it spawned.** `MicrobotSpawner` gained a
  `HasSpawned` flag so `MicrobotSpawnSystem` only runs the spawn loop once per spawner instead of
  destroying the entity afterward; the spawner entity (and its `MicrobotSpawner` data) persists for the
  rest of the session. It also carries a baked `DynamicBuffer<MicrobotSpawnedElement>` (`Entity Value`)
  that the system fills with every instantiated bot's `Entity`, giving other systems/tools a way to look
  up "which bots came from this spawner" later. Kept the existing two-pass structural-change discipline:
  instantiated entities are collected into a local `NativeList<Entity>` first, and the buffer is only
  fetched fresh *after* all `Instantiate` calls for that spawner finish (a `DynamicBuffer` handle held
  across a structural change is stale/unsafe) — then appended in one pass.
- **Added the "dumb-follower shared-command" controller (M1.5 item).** `MicrobotFollowCommand`
  (`float3 Destination`, `float Tolerance`) is a session-wide singleton baked once from a standalone
  `MicrobotFollowCommandAuthoring` GameObject (a `Transform` field or, if left empty, the authoring
  GameObject's own position, at bake time — not live-tracked at runtime yet). New
  `MicrobotFollowCommandSystem` (`[UpdateBefore(MicrobotStepMovementSystem)]`) runs every frame: if the
  singleton exists, every `MicrobotTag` entity without an active goal (`!HasGoal`) and not already within
  `Tolerance` of `Destination` (checked against the bot's root `LocalTransform.Position`, a coarse
  approximation — the actual precise arrival check is the existing per-extremity one already inside
  `MicrobotStepMovementSystem`) gets `HasGoal`/`GoalPoint`/`GoalTolerance` set to the shared destination.
  Reuses the exact same goal-seeking primitive `MicrobotNavigationSystem` drives docking through — no new
  movement logic, just a different source assigning the goal. Deliberately "dumb": no per-bot opt-in tag,
  no path planning, every bot with the singleton present just walks straight at the same shared point.
  **Not yet tested in Play mode.**
- **Fixed: multi-bot snapping/teleporting once several bots stepped simultaneously (surfaced by
  follow-command testing with a spawned group).** `MicrobotStepMovementSystem` used to accumulate
  landings into a single shared local `stepLanded` bool across its *entire* per-frame loop over all
  bots, then write `ToggleBase = inputState.ToggleBase || stepLanded` to the one global
  `MicrobotInputState` singleton; `MicrobotIkSystem` then applied that single flag to flip
  `BaseIsSegmentB` on **every** bot with `MicrobotTag`. With one manually-driven test bot this was
  invisible (the only bot that could land *was* the only bot). With many bots stepping on independent
  frames, any single bot landing anywhere forced every other bot — including ones mid-lerp through
  their own step — to swap anchor/free roles out from under themselves, instantly relocating whichever
  extremity was newly "free" to the other's stale position. Fixed by toggling `BaseIsSegmentB` directly,
  per-entity, at each of the four landing points already inside `MicrobotStepMovementSystem`'s loop
  (query changed from `RefRO<MicrobotIkState>` to `RefRW<MicrobotIkState>`), instead of funneling through
  the shared singleton. The manual `T`-key toggle in `MicrobotIkSystem` is untouched and still reads
  `MicrobotInputState.ToggleBase` — that one's meant to stay a global broadcast (manual single/shared-bot
  testing) since it comes straight from the keyboard, not from any particular bot's landing.
- **Centralized goal-arming in `MicrobotNavigationSystem`; extracted the goal fields into their own
  component.** New `MicrobotGoal` (`bool HasGoal; float3 GoalPoint; float GoalTolerance;`) replaces the
  three fields that used to live inline on `MicrobotStepState`; baked onto every microbot root alongside
  `MicrobotIkState`/`MicrobotStepState` in `MicrobotAuthoring`'s `Baker`. The write contract is now
  explicit and asymmetric by design: **only `MicrobotNavigationSystem` ever sets `HasGoal = true`** (plus
  `GoalPoint`/`GoalTolerance`) — `MicrobotStepMovementSystem` only *reads* `GoalPoint`/`GoalTolerance` and
  is the sole system allowed to *clear* `HasGoal` back to `false`, on arrival. `MicrobotFollowCommandSystem`
  is gone — its logic (read the `MicrobotFollowCommand` singleton, arm a goal for any `!HasGoal` bot not
  yet within tolerance) is now a second pass inside `MicrobotNavigationSystem.OnUpdate`, after the
  existing dock-command pass. Because both passes only act on `!HasGoal` bots, and the dock-command pass
  runs first, a bot with an active dock-command linker naturally wins over the shared follow-command
  destination for that frame — no explicit priority flag needed, just pass ordering. The intent: **any
  future system that wants to move a bot expresses that as data (a new command component +
  `MicrobotNavigationSystem` reading it), never by writing `MicrobotGoal` directly** — `MicrobotGoal` is
  private to the Navigation ↔ StepMovement handshake. `MicrobotClimbPointsSystem`'s idle check
  (`!HasGoal && !stepping`) updated to read the new component. **Not yet tested in Play mode.**
  **Deliberate scope note**: manual WASD/`T` keyboard control is *not* routed through
  `MicrobotNavigationSystem` — `MicrobotStepMovementSystem` still reads `MicrobotInputState.MoveInput`
  directly and steps without ever touching `MicrobotGoal` when there's no active goal. Owner explicitly
  asked to leave this alone for now; the "everything goes through Navigation" rule currently covers the
  dock-command and follow-command sources only, not manual debug control.
- **Fixed: follow-command was driving both extremities all the way to the destination.** The
  arrival check used to test the bot's *root* `LocalTransform.Position` against
  `MicrobotFollowCommand.Tolerance` — since the root sits between the two extremities, that check
  didn't pass until whichever extremity was trailing behind also finished walking all the way to the
  destination, so both segments ended up converging on the same point (a degenerate stacked pose).
  Changed to check each extremity's own `MicrobotIkTargets.TargetAPos`/`TargetBPos` directly: once
  *either* one is within `Tolerance` of the destination, the follow command counts as satisfied for
  that bot and no further goal gets armed — the other extremity is left wherever it currently is,
  matching the docking-adjacent idea that a bot doesn't need every part of itself to physically arrive,
  just enough of it to be "there." Also let this loop drop its `LocalTransform` dependency entirely
  (reads `MicrobotIkTargets` instead), which was only ever a coarse approximation anyway.
- **Started the shared spatial-hash grid (climbing + dynamic docking-detection, step 1 of 3).** Climbing
  (M1.5) and dynamic docking-detection (M2) both need a way to find nearby stationary bots, so they'll
  share one grid instead of being built twice. New `MicrobotSpatialGrid` (runtime-only singleton, **not**
  baked — `NativeParallelMultiHashMap<int2, Entity>` can't go through the baking pipeline, so it's created
  directly by `MicrobotSpatialGridSystem.OnCreate`) is rebuilt every frame: cleared, then every
  `MicrobotTag` bot that's currently idle (`!HasGoal && !stepping` — same signal `MicrobotClimbPointsSystem`
  already computes) gets inserted keyed by an `int2` cell from its root `LocalTransform.Position`
  (`[UpdateAfter(MicrobotIkSystem)]` so the position read is this frame's final one, not stale). Cell size
  is a tunable singleton (`MicrobotSpatialGridSettings`, baked from a standalone
  `MicrobotSpatialGridAuthoring` GameObject), falling back to `1f` if no such GameObject exists in the
  scene. **Deliberately no occupancy/claim filtering at all** — a bot stays in the grid (climbable) and
  every one of its `Dockable` points stays a valid target (dockable) no matter how many other bots are
  already touching it, since junctions can legitimately be N-way (e.g. 3 bots meeting at a cube corner);
  there's no cap to enforce, so there's nothing to track. This is infrastructure only — no consumer reads
  from the grid yet. Planned order: (1) this grid, (2) climbing consumer (step-height lookup in
  `MicrobotStepMovementSystem`), (3) dynamic docking-detection consumer (extremity searches the grid
  instead of walking `MicrobotDockCommandAuthoring`'s hand-authored list). **Not yet tested in Play mode.**
- **Climbing consumer (step 2 of 3).** `MicrobotStepMovementSystem` now resolves a non-final-approach
  step's landing height by querying `MicrobotSpatialGrid` instead of hardcoding flat ground (`0f`):
  computes the intended landing X/Z (using the post-turn `HeadingAngle`, same forward-vector math as the
  live per-frame lerp target), scans the landing cell + its 8 neighbors for any other idle bot within
  `CellSize`, and if found, uses the flat `StandableHeight` from `MicrobotSpatialGridSettings` instead of
  `0f` — that's the whole climbing effect (walk a straight line horizontally, but land higher when
  there's a bot underfoot). Final-approach steps (landing exactly on a `GoalPoint`) are unaffected — they
  keep using the goal's own authored height, no grid lookup needed. Height is still only decided once at
  step-start and held fixed for the step's duration (`StepTargetHeight`), matching the existing
  X/Z-drifts-but-height-doesn't approximation already in place for turning mid-step.
  **Self-exclusion bug caught before testing**: the grid can contain the querying bot itself (built last
  frame, one frame of staleness by design - see the grid entry above), which would make a bot detect its
  own body as a climbable neighbor of its own landing spot. Query now takes the stepping bot's own
  `Entity` and explicitly skips it as a candidate. **Not yet tested in Play mode.**
- **Added an opt-out for follow-command, as its own tag component rather than a field on `MicrobotTag`.**
  `MicrobotTag` stays a pure zero-size marker (every system already assumes `.WithAll<MicrobotTag>()`
  queries carry no data - bolting state onto it would be an easy-to-miss footgun for future systems). New
  `MicrobotIgnoresFollowCommand` (empty tag), added via an `ignoreFollowCommand` checkbox on
  `MicrobotAuthoring`'s `Baker`; `MicrobotNavigationSystem`'s follow-command pass now has
  `.WithNone<MicrobotIgnoresFollowCommand>()`. There's still no way to pin a bot down after the fact at
  runtime (only at bake time) - that's fine for now since "stationary" itself isn't a runtime-toggleable
  flag either, it's just the natural `!HasGoal && !stepping` state.
- **Fixed: bot teleported straight to an elevated destination instead of climbing up to it.** Owner's
  first real climbing test (a follow-command destination placed above a stationary bot) revealed that
  `isFinalApproach` only ever checked *horizontal* distance to the goal
  (`remaining <= StepSize`) — the moment that was true, the step aimed exactly at the goal's literal 3D
  position (height included), completely bypassing the climbing grid by design (final-approach steps
  trust the goal's authored height outright, see the climbing-consumer entry above). If the bot got
  horizontally close to the destination while still down at ground level, it would flip into
  final-approach and, in one step, jump straight up to the destination's exact height — regardless of
  whatever obstacle sat physically between them. Fixed by also requiring the **anchor's current height**
  to already be within `GoalTolerance` of `GoalPoint.y` before `isFinalApproach` can be true
  (`MicrobotStepMovementSystem.cs`, step-start block). Until that's satisfied, steps stay in ordinary
  climbing mode (grid-height lookup, normal toggle-and-alternate), so a bot has to actually climb its way
  up to roughly the right elevation via real intermediate steps before the system ever trusts a direct
  line to the goal - it now only "snaps precisely" once it's already there in height, not just in X/Z.
  **Not yet retested in Play mode.**
- **Replaced the flat `StandableHeight` constant with real per-candidate geometry.** Debug logging
  (temporarily added to `MicrobotStepMovementSystem`, `[BurstCompile]` stripped so `Debug.Log` could run)
  confirmed the spatial-hash climbing lookup itself was working correctly - it found the stationary bot
  and returned a climb height every time. The actual problem was the height value itself: `StandableHeight`
  was an arbitrary flat guess (`0.2`) completely disconnected from a bot's real geometry, when a fully
  elongated 2-segment bot (owner: "the bots are .8m each when they are fully elongated") can reach up to
  segment-length-sum tall depending on pose. Removed `StandableHeight` from `MicrobotSpatialGridSettings`/
  `MicrobotSpatialGridAuthoring` entirely; `SampleStandableHeight` now looks up the candidate's own
  `MicrobotIkTargets` and uses `max(TargetAPos.y, TargetBPos.y)` — the candidate's actual current highest
  extremity — so climbable height reflects that specific bot's real current stance instead of a guessed
  constant. **Debug logging is still active** (`[BurstCompile]` still stripped from
  `MicrobotStepMovementSystem.OnUpdate`) pending a retest with a destination height that's actually
  reachable via a single climb (the earlier 1.8-high test destination was never reachable regardless of
  this fix, since it's more than double even a fully-elongated single bot's height - that's a multi-bot-
  stacking problem, out of scope for now). **Not yet retested in Play mode.**
- **Split climbing from docking: new `MicrobotClimbPoints` (3 points) alongside `Dockable` (2 points),
  deliberately separate.** Owner: any stationary bot should be climbable at any of 3 points - its two
  extremities *or* its elbow - "ant-bridge" style, not just at the feet; but shape-forming connections
  must stay extremity-only, so `Dockable` is untouched and still only ever has `PointA`/`PointB`. New
  `MicrobotClimbPoints` (`PointA`, `PointB`, `Elbow`) is maintained by `MicrobotClimbPointsSystem`
  alongside `Dockable`, added/removed under the exact same idle gate (now also reads the bot's own root
  `LocalTransform.Position` for `Elbow`). `MicrobotStepMovementSystem`'s `SampleStandableHeight` now reads
  `MicrobotClimbPoints` instead of raw `MicrobotIkTargets`, taking the max of all 3 points instead of just
  the 2 extremities - a bot bent into an arch (elbow higher than both feet) now correctly offers its elbow
  as the tallest standable point, which the old extremities-only lookup could never represent. **Not yet
  retested in Play mode** (debug logging from the earlier climbing investigation is still active pending
  this retest - see the `[BurstCompile]`-stripped note above).
- **Fixed: climbing could jump straight to a far-above candidate point in one step (e.g. leaping to the
  top of a 2-bot stack instead of climbing bot 1, then bot 2).** `SampleStandableHeight` used to return
  the single tallest point found among ANY nearby candidate, with no regard for how far above the
  anchor's current height that point actually was - so if a stacked bot's elbow happened to sit at
  `2.28` while the walker was still at `0`, the very next step targeted `2.28` directly (smoothly lerped
  over one step's duration, but still an unrealistic single-step leap, not a two-stage climb). Owner's
  framing: **a point can only gain height by climbing something that's actually within reach right now -
  it should stay at `0` if nothing reachable is nearby**, not partially/artificially capped toward an
  unreachable target. Added `MicrobotSpatialGridSettings.MaxClimbHeight` (authored via
  `MicrobotSpatialGridAuthoring`, default `0.3f`); `SampleStandableHeight` now takes the anchor's current
  height and only counts a candidate's individual point (`PointA`/`PointB`/`Elbow`, checked separately,
  not just the candidate's overall max) if `point.y <= anchorHeight + MaxClimbHeight` - a point beyond
  reach is ignored entirely for that step, exactly as if it weren't there. This forces genuinely
  incremental climbing: a 2-bot stack now requires climbing bot 1 first (bringing the anchor's height up
  within reach of bot 2's points) before bot 2's higher points become reachable at all, rather than being
  visible as a single giant leap from the start. **Not yet retested in Play mode** (debug logging from
  the earlier investigations is still active).
- **Cleaned up after the climbing investigation.** Removed all temporary `Debug.Log` calls and restored
  `[BurstCompile]` on `MicrobotStepMovementSystem`/`OnUpdate`; trimmed several over-long comments added
  during debugging (`MicrobotClimbPoints`, `MicrobotSpatialGridSettings`, `MicrobotSpatialGrid`,
  `MicrobotSpatialGridSystem`) back down to one line each. `SampleStandableHeight` dropped its
  `nearestDistance`/`candidateCount` diagnostic `out` parameters since nothing but the removed logging
  used them. **Climbing is left in a not-fully-working state** - the reach-limited (`MaxClimbHeight`)
  version is in place and should prevent single-step leaps to a far-above point, but it was not
  successfully verified end-to-end (still landing on/near the destination height in ways that didn't look
  like genuine incremental climbing before the owner asked to stop and clean up). Revisit this fresh
  rather than assuming the current state is correct.
- **Removed the reactive per-step climbing lookup - it violated the Navigation-is-sole-decision-maker
  rule and that's why it kept misbehaving.** Root design realization: reaching an elevated destination
  needs *intermediate* goals (real climbable points chained together as sequential `GoalPoint`s), decided
  by `MicrobotNavigationSystem`, not guessed reactively by `MicrobotStepMovementSystem` one step at a
  time with no sense of an overall route. `SampleStandableHeight`, `MaxClimbHeight`
  (`MicrobotSpatialGridSettings`/`Authoring`), and the height half of `isFinalApproach`
  (`closeInHeight`) are all removed - `MicrobotStepMovementSystem` is back to its pre-climbing shape:
  `targetHeight = isFinalApproach ? GoalPoint.y : 0f`, `isFinalApproach` gated on horizontal distance
  only (`remaining <= StepSize`), no grid consultation at all. Once Navigation is extended to chain real
  climbable points as intermediate goals, every step toward any goal (intermediate or final) can trust
  `GoalPoint.y` directly the same way dock points already do - no separate climbing-aware step logic
  needed.
  **Kept** (Navigation will need these as a query source for picking intermediate points):
  `MicrobotSpatialGrid`/`MicrobotSpatialGridSystem` (the spatial index) and
  `MicrobotClimbPoints`/`MicrobotClimbPointsSystem` (the 3-point-per-idle-bot data). Only the reactive
  step-level *consumer* of that data is gone.
  **Next planned step (not yet started)**: extend `MicrobotNavigationSystem` to, when a goal's height is
  out of single-step reach, pick the nearest currently-reachable climbable point (queried from the grid)
  in the destination's general direction as an intermediate `GoalPoint` instead of the final destination,
  re-evaluating once each intermediate point is reached (greedy one-hop-at-a-time, not full pathfinding -
  a real graph-search version was discussed as a fallback if greedy gets stuck in a local dead end).
- **Microbots can no longer be dock targets.** `MicrobotClimbPointsSystem` no longer adds/removes/updates
  `Dockable` on microbots - it now only maintains `MicrobotClimbPoints`. `Dockable` still exists exactly as
  before for static `Dock` prefabs (baked once, statically, via `DockAuthoring` - untouched). A dock
  command whose list happens to reference a microbot entity will now simply never find a `Dockable` there
  and stay waiting, since only real `Dock` prefabs carry it anymore. Bot-to-bot climbing (via
  `MicrobotClimbPoints`) is unaffected - this only removes bot-to-bot *docking*.
- **Standalone feature** — per project rule, MicroBots does not reference or depend on any other
  `Assets/Features/` folder (e.g. Pooling, Conjure, Dependency) unless a dependency is explicitly
  requested later.

## Status

M1 is done: IK foundation, anchor toggle (manual `T` + automatic on step landing), and step movement
(horizontal progress, height lift, alternating anchors, A/D heading turn) all confirmed working together
by the owner in Play mode, on flat ground. Uneven-ground step landing is explicitly deferred (see
Decisions log) and does not block M1. Moving on to M1.5 next.
