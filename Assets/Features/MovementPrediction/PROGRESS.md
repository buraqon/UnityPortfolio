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

## What's NOT done yet

- **No demo scene / player rig exists yet.** There's no scene, `NetworkManager`, or assembled
  player prefab (`CharacterController` + `Input_Handler_Player` + `Character_Movement` +
  `PredictedTransform`) to actually press Play and exercise this. `Demo_MovementDebug.prefab` is
  just the debug line-renderer/silhouette visualizer and expects to be parented under an already-
  working player.
- The specific prediction issues to investigate haven't been discussed/identified yet.

## Decisions / tradeoffs

- Chose to trim unrelated gameplay code out of the input handlers rather than add local stub
  types for `SettingsManager`/`Match`/etc., to keep this feature self-contained like the rest of
  `Assets/Features/`. Tradeoff: reimporting fixes back to the source project is a manual merge, not
  a file copy.
