# Centipede — Progress

## What it is

Standalone demo: a procedural, multi-segment centipede that can drive on any surface (floor, walls,
ceilings) and animates its legs via a shared gait clock + per-leg IK targets (Animation Rigging
Two-Bone IK). Includes a custom editor window to generate a centipede from prefabs.

## Current state (as of 2026-08-03)

Scripts present and appear complete:

- `Game/CentipedeController.cs` — resolves the body-segment chain each frame from the head's position,
  respecting a max joint bend angle (`jointAngleLimit`) and per-segment front/back joint offsets.
  `[ExecuteAlways]`, has a `ResetJointsToStraightLine` context menu for editor setup.
- `Game/CentipedeHeadController.cs` — car-style input (W/A/D via new Input System) drives the head;
  raycasts beneath it each frame to hover/align to whatever surface it's over (walls/ceilings included),
  with a turn-rate-per-distance-moved so alignment transitions gradually instead of snapping.
- `Game/CentipedeJoint.cs` — simple front/back joint marker component, falls back to own transform if
  either is unset.
- `Visual/CentipedeGaitClock.cs` — tracks distance travelled by a reference transform (the head) and
  turns it into a repeating 0-1 gait phase; freezes when not moving.
- `Visual/CentipedeLeg.cs` — drives a leg's IK target: alternates stance/swing per a phase window
  (`sidePhaseOffset` + `wavePhaseOffset` off the shared clock), grounds via raycast along the hip's own
  up vector (so it still works tilted onto a wall), arcs the foot during swing.
- `Editor/CentipedeGeneratorWindow.cs` — `Game/Centipede/Generator` menu window; instantiates a head +
  N body segments from assigned prefabs, wires up `CentipedeController`/`CentipedeGaitClock`, and
  distributes `wavePhaseOffset` across segments so the leg wave travels head-to-tail.

Prefabs/scene present: `Game/Centipide_Head.prefab`, `Game/Centipede_Section.prefab`, `Game/RightRig.prefab`,
and a demo scene `Centipede.unity`.

## Open items / not yet verified this session

- Haven't opened `Centipede.unity` in the Editor this session to confirm it still plays correctly
  end-to-end (chain resolution, surface-driving, leg IK) after any recent engine/package updates.
- No automated tests (consistent with the rest of the repo).

## Decisions / notes worth remembering

- Segment chain resolution and surface alignment both key off the *previous* frame's rotation/up vector
  rather than world down, specifically so the whole rig keeps working when the head drives onto a wall
  or ceiling — don't "simplify" these to `Vector3.up`/`Vector3.down` without re-checking that.
- `CentipedeGeneratorWindow.Generate()` wires child components via `SerializedObject.FindProperty` by
  **string field name** — if you rename a private serialized field on `CentipedeController`,
  `CentipedeGaitClock`, or `CentipedeLeg`, the generator will fail silently (property not found) rather
  than throwing a compile error. Grep the generator when renaming those fields.
