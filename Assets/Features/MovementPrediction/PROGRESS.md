# MovementPrediction — Progress

## Goal

Port over a client-side prediction / reconciliation system (`PredictedTransform` + tick-buffered
movement) from another project, use this portfolio repo as an isolated sandbox to find and fix
prediction bugs, then port the fixes back.

## Status: dependencies resolved, compiles standalone

The imported scripts referenced several types that don't exist in this project. Resolved as
follows:

- **`Character_Movement`** (new, [Character_Movement.cs](Character_Movement.cs)) — this type was
  referenced by `PredictedTransform` but never included in the drop. It's the missing glue: reads
  `Input_Handler` each tick, runs one simulation step via `SimulateTick`, and feeds the result to
  `PredictedTransform.RecordTickState`. `SimulateTick` is also the exact method
  `PredictedTransform`'s reconciliation replay calls per-tick when correcting a mispredicted
  position — live ticks and replay ticks now run through the identical code path, which is a hard
  requirement for reconciliation to actually converge.
- **`IEntity`** (new, [IEntity.cs](IEntity.cs)) — minimal `gameObject`/`transform` contract that
  `IMovable` extends. Needed for `Force_Movement` subclasses to compile.
- **Weapon/ability/aim-assist/settings dependencies removed** — `Input_Handler` and
  `Input_Handler_Player` carried over unrelated weapon/ability/aim-assist/mobile-input code from
  the source game (`Match`/`Team`, `SettingsManager`, `Testing_Manager`, `AimAssist_Handler`,
  `Character_LineOfSightHandler`, `SwipeDetection`, `ISender`). None of it is relevant to testing
  movement prediction, so it was trimmed rather than stubbed out — this feature is now
  self-contained with only move/look/jump/sprint input and the tick-delay/reconciliation
  machinery. **This means these two files are no longer drop-in replacements for the source
  project's versions** — fixes made here will need to be merged back manually rather than copy-
  pasted wholesale.
- `Force_Movement_Charge`'s AI-targeting branch (`ISender`) was removed for the same reason; the
  player-controlled path is untouched.
- Fixed a broken script reference on `Demo_MovementDebug.prefab` — its one component pointed at a
  GUID with no matching `.meta` in this project. Realigned
  `PredictedTransform_Debug.cs.meta`'s GUID to match (the prefab was otherwise untouched).

## Status: test rig built, ready to start on the actual prediction bugs

The scene/rig is wired up and playable:

- **`PredictionPlayer.prefab`** — `NetworkObject` + `CharacterController` + `Input_Handler_Player` +
  `Character_Movement` + `PredictedTransform`, plus a `CameraTarget` child (eye height, since the
  root transform sits at the capsule's feet) and a `PlayerCamera` child carrying a
  `CinemachineCamera` (`Target.TrackingTarget` → `CameraTarget`) with `CinemachineThirdPersonFollow`
  as Body and no Aim component, so it doesn't fight the character's own mouse-look rotation.
- **`Predicttion.unity`** — the demo scene: ground, `NetworkManager` (`PredictionPlayer` registered
  in `DefaultNetworkPrefabs.asset`), `Main Camera` with a `CinemachineBrain` (was also found
  disabled in the scene — re-enabled, since nothing would've rendered otherwise), and a
  `CursorToggle` object (locks the cursor on start, Esc toggles lock/visibility so mouse-look works
  without fighting the OS cursor).
- **`Prediction_Actions.inputactions`** — Move/Look/Jump/Sprint action map feeding
  `Input_Handler_Player`.
- Found and fixed a real ordering bug while wiring `Character_Movement` up: it and `Input_Handler`
  were both independently subscribing to `NetworkTickSystem.Tick`, so whether input was sampled
  before movement read it depended on component order on the GameObject. Fixed by having
  `Input_Handler.SampleTick()` be called explicitly by `Character_Movement.OnTick` instead of
  self-subscribing.
- Re-added a `CanJump` gate — `Jump()` previously only checked "not mid a forced movement", with no
  grounded check at all, so holding jump while airborne reset vertical velocity to jump height
  every tick (infinite jump/hover).
- Ground-detection bug (not a camera issue, despite first appearances): the ground `Cube` in
  `Predicttion.unity` was on layer `Default` while `groundLayer` on the player only checks
  `Ground` — the raycast never hit, so grounded state fell back entirely to Unity's flickery
  built-in `controller.isGrounded`, causing the capsule to visibly vibrate. Fixed by putting the
  ground (and the new low wall / ramp below) on the `Ground` layer.
- Added a `TestMap` (perimeter walls, an interior corner, a steppable low wall, and a ~20° ramp)
  to `Predicttion.unity` for exercising collision/slope/step cases.
- Merged `Movement_Handler.cs` + `Movement_Controller.cs` + `Character_Movement.cs` into a single
  `Character_Movement.cs` — was a 3-level inheritance chain that existed only because it was
  imported that way from the source project; nothing else in this feature extends any of these
  three, so the extra levels weren't buying anything here. All fields/methods carried over
  unchanged (including now-legal overloads like the two `CalculateVelocity` signatures, previously
  split one-per-class). `PredictionPlayer.prefab`'s serialized data (`Speed`, `moveParams`,
  `controller`, `groundLayer`, `rayDistance`, `startSpeed`) needed no changes - Unity serializes by
  field name regardless of which class in a hierarchy declared it, and only `Character_Movement`
  was ever added to the GameObject as its own component.

## What's NOT done yet

- **The actual prediction issues haven't been investigated yet.** Everything above was getting the
  rig to a testable state - the specific bugs to chase (the "main objective") start next.
- Camera ownership isn't gated: every `PredictionPlayer` instance's `CinemachineCamera` is
  unconditionally live. Fine for one player in the scene; will need an `IsOwner` gate before testing
  with more than one.

## Decisions / tradeoffs

- Chose to trim unrelated gameplay code out of the input handlers rather than add local stub
  types for `SettingsManager`/`Match`/etc., to keep this feature self-contained like the rest of
  `Assets/Features/`. Tradeoff: reimporting fixes back to the source project is a manual merge, not
  a file copy.
- Several scene/prefab edits (Cinemachine components, `CanJump`, `CursorToggle`) were hand-authored
  YAML rather than done through the Editor, since Claude can't drive the Unity Editor UI directly.
  Verified against Unity's own `Editor.log` where possible; otherwise flagged for the user to check
  in-Editor before relying on them.
