# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

This is a Unity gameplay-programming portfolio: a collection of largely independent, self-contained systems
("Features") rather than a single game. Each feature under `Assets/Features/` demonstrates one gameplay
system (multiplayer prediction, a spell-casting framework, a status-effect system, a visual FSM editor tool,
dependency injection, object pooling, etc.) and typically ships with its own demo scene so it can be
exercised in isolation.

- Engine: Unity **6000.4.0f1** (Unity 6) — open with this exact version via Unity Hub.
- Render pipeline: URP (Universal Render Pipeline).
- Multiplayer: `com.unity.netcode.gameobjects` (Netcode for GameObjects), with a custom client-side
  prediction/reconciliation layer built on top of it.
- Other key packages: Input System, AI Navigation, Timeline, Visual Scripting.

## Project rules

See [PROJECT_RULES.md](PROJECT_RULES.md) for the full list. Most importantly, at the start of **every**
session: if the conversation involves starting or continuing work on a feature, check whether
`Assets/Features/<Name>/PROGRESS.md` exists — if it doesn't, remind the user to create one before starting
work, per rule 1.

## Build, test, and running

There is no CLI build/lint/test pipeline in this repo — it's a Unity project, developed and verified from
the Unity Editor:

- Open the project in Unity Hub using editor version `6000.4.0f1` (see `ProjectSettings/ProjectVersion.txt`).
- "Build" = check the Console window for compile errors after the Editor finishes importing/recompiling.
- There is no `Tests/` assembly or automated test suite yet, despite `com.unity.test-framework` being a
  declared package dependency — don't assume Test Runner coverage exists for a feature.
- To manually verify a feature, open its demo scene and press Play, e.g.:
  - `Assets/Features/Conjure/Demo/DemoScene_Conjure.unity`
  - `Assets/Features/Movement/Demo/DemoScene_Movement.unity`
  - `Assets/Features/Network/Demo/DemoScene_Input.unity`
  - `Assets/Features/Network/Demo/NetworkListTest/DemoScene_NetworkListTest.unity`
  - `Assets/Features/FSM/Scene/FSM_Viewer.unity` (visual FSM editor tool)
  - `Assets/Features/Centipede/Centipede.unity`
- Multiplayer features generally need Play Mode with Netcode's host/client setup (see
  `Assets/Features/Network/NetworkManager.prefab` and `NetworkManagerUI.cs`) to actually exercise
  server/client code paths — a single-instance Play session will mostly hit the `IsServer`/owner branches.

### `.csproj` / `.sln` files are stale — don't hand-edit or trust them for file layout

The generated `*.csproj` files (`HippoLib.csproj`, `Effects.csproj`, etc.) still reference an old
`Assets/Runtime/...` path layout, while the actual source now lives under `Assets/Features/...`. These
project files are Unity-generated (Editor > regenerate project files) and out of sync with the current
folder structure — do not use them as a source of truth for what files exist or belong to which assembly;
use `Assets/Features/` on disk instead.

## Architecture

### Assembly layout

Only two features define their own assembly (`.asmdef`): `StateMachine` and `Rotary Heart` (a third-party
serializable-dictionary library, treat as vendored/do-not-modify). Everything else compiles into the
default `Assembly-CSharp`. Keep this in mind when adding `using` references or worrying about circular
dependencies — most feature code shares one assembly.

### Namespace conventions (inconsistent — read before assuming)

Most feature scripts live under `HippoLib.<Feature>` (e.g. `HippoLib.Effects`, `HippoLib.Dependency`,
`HippoLib.Conjures`, `HippoLib.Pooling`, `HippoLib.Movement`, `HippoLib.StateMachine`). However, several
core/networking types are deliberately or historically in the **global namespace** — e.g. `PredictedSpawn`,
`PredictedTransform`, `IInteractable`, and the visual-FSM's `FSM` class (which instead uses a bare
`FiniteStateMachine` namespace for its state/condition/transition types). Don't assume a type is namespaced
just because its neighbors are; check the file.

### Two unrelated "state machine" systems — don't conflate them

- `Assets/Features/StateMachine/` (`HippoLib.StateMachine`) — a plain, generic, code-only base class
  `StateMachine<T> where T : StateMachine_State`. No editor tooling; you subclass it and call `SetState`.
- `Assets/Features/FSM/` (bare `FiniteStateMachine` namespace) — a designer-facing system: `FSM` is a
  `ScriptableObject` asset holding a list of `FSM_State` sub-assets connected by `FSM_Transition`s with
  swappable `FSM_Condition`s, edited via a custom Editor window (`Assets/Features/FSM/Editor/FSM_Window.cs`,
  `.uxml`/`.uss` UI Toolkit views). `FSM.InstantiateFSM()` deep-clones the asset graph (states, then rewires
  transitions by GUID, then calls `AfterInstantiate`) so runtime instances don't mutate the shared asset.

These are independent — a feature using one is not related to a feature using the other.

### Dependency injection (`Assets/Features/Dependency/`)

A small custom DI container, not a general-purpose framework:

- `DependencyContext` is an abstract `MonoBehaviour` (`DontDestroyOnLoad`, `DefaultExecutionOrder(-1)`) —
  subclass it, implement `Setup()` to populate `dependenciesCollection` and `Configure()` for post-injection
  setup. On `Awake()` it builds a `DependencyProvider` and injects into **every** `MonoBehaviour` in its
  children (`GetComponentsInChildren<MonoBehaviour>(true)`) before `Configure()` runs.
- Fields are marked with `[InjectField]` and populated via reflection across the whole base-type chain
  (`DependencyProvider.Inject`), regardless of access modifier.
- Registrations distinguish singleton vs. transient (`Dependency.IsSingleton`); singletons are lazily
  constructed and cached in `DependencyProvider._singletons`.
- `DependencyFactory.FromClass<T>()` constructs plain (non-`MonoBehaviour`) objects via
  `FormatterServices.GetUninitializedObject` + injection + parameterless-constructor invocation, so
  dependencies are already injected by the time the constructor body runs.

### Networking / prediction (`Assets/Features/Network/`)

Built on Netcode for GameObjects, with a custom tick-buffered client prediction layer rather than relying
solely on stock `NetworkTransform`:

- `PredictedTransform` keeps a ring buffer (`clientMovementDatas`, size 1024) of the owning client's
  transform per network tick, compares it against the server's `NetworkVariable<SyncedTransformData>` when
  it changes, and either passively nudges the client toward the server value or — if the error persists for
  `errorTickThreshold` ticks — rewrites the buffered history from that tick forward and snaps
  (`CorrectPositionError`). Non-owner clients just `Lerp` toward server state.
- `PredictedSpawn` / `PredictedSpawner` handle spawning: `PredictedSpawn.Owned` is true if
  `IsOwner || IsServer || !IsSpawned`, and `OnNetworkSpawn` dispatches to `OnPredictedSpawn()` (owner/server
  path) vs. `OnLocalSpawn()` (not-yet-networked/local-only path) — subclasses (e.g. `Conjure`) override these
  instead of `OnNetworkSpawn` directly.
- `Conjure` (see below) is the main consumer of this prediction layer and also strips
  `AnticipatedNetworkTransform` on purely local (non-networked) spawns via `OnLocalSpawn`.

### Effects system (`Assets/Features/Effects/`)

A generic status-effect framework: `Effect_Handler<TSender, TReciever>` (a `NetworkBehaviour`, generic over
`IEffectSender`/`IEffectReciever` interfaces) owns a list of `Effect_Effector<TSender, TReciever>` instances
and drives their lifecycle once per server tick via `OnServerUpdate()` → `RemoveOldEffectors` /
`StartNewEffectors` / `UpdateEffectors`. Concrete effector shapes (`Effector_Instant`, `Effector_Overtime`,
`Effector_Stack`, `Effector_Toggle`) pair with matching `Effect_Data` subclasses
(`Effect_Instant`/`Effect_Overtime`/`Effect_Stack`/`Effect_Toggle`) that describe/instantiate them. New
effect types are added by creating a new `Effect_Data` + `Effect_Effector` pair, not by modifying the
handler.

### Conjure — spell/ability framework (`Assets/Features/Conjure/`)

`Conjure` (extends `PredictedSpawn`, requires `Pool_Item`) is the base class for anything cast/spawned by a
caster (`IConjureSender`) at a target (`IConjureReciever`): projectiles (`Conjure_Projectile`,
`_Homing`, `_Throwing`), AOEs (`Conjure_AOE`), and multi-instance formations (`Conjure_Multiple` +
`Formation_Circle`/`Formation_Rectangle`/`Formation_Scatter`). Config is data-driven via `Conjure_Data`
ScriptableObjects (damage multiplier, linger time, and an optional `ChainedConjure` spawned on
`EndLife()` for chaining spells). `TargetType` is a `[Flags]` enum (`Enemy`/`Ally`/`Self`) that
`IsTarget()` checks against the sender/receiver relationship. Spawning goes through the pooling system
(`Assets/Features/Pooling/`), not raw `Instantiate`.

### Pooling (`Assets/Features/Pooling/`)

`Pool` is a plain static-singleton `MonoBehaviour` (`Pool.instance`) keyed by prefab reference
(`Dictionary<GameObject, List<GameObject>>`). Any pooled prefab must carry a `Pool_Item` component (it
records which prefab it came from so `Pool.PoolItem(obj)` knows which bucket to return it to). There's no
pre-warming — pools grow lazily via `Instantiate` on first miss.

### Feature directory map

| Directory | What it is |
|---|---|
| `AssetDatabase/` | Editor tooling for generating/managing asset databases |
| `Centipede/` | Standalone demo: procedural multi-segment IK-style creature (custom editor window to generate segments) |
| `Conjure/` | Spell/ability-casting framework (see above) |
| `Dependency/` | Custom DI container (see above) |
| `Effects/` | Generic status-effect system (see above) |
| `Extensions/` | Small `Vector2`/`Vector3`/`Quaternion` extension methods |
| `FSM/` | Designer-facing visual finite-state-machine editor (`FiniteStateMachine` namespace) |
| `Interactable/` | `IInteractable` + `Interactor` — minimal interaction-detection contract |
| `Movement/` | `MovementHandler` base + force-based movement variant |
| `Network/` | Netcode-based multiplayer + custom prediction layer (see above) |
| `Pooling/` | Static object pool (see above) |
| `Rotary Heart/` | Vendored third-party serializable-dictionary library — treat as read-only |
| `StateMachine/` | Plain generic code-driven state machine (its own asmdef) |
| `Template/` | Minimal networked-scene starting point for new demos |
| `UI/` | `UGUIMenu` helper |
| `Util/` | Grab-bag: collider/hitbox helpers, `FieldCopier`, `TypeUtil` |

When adding a new feature, follow the existing convention: a folder under `Assets/Features/<Name>/`
containing `Scripts/` (or flat `.cs` files for small features), an optional `Demo/` scene + user scripts, and
an optional `Editor/` subfolder for editor-only tooling.
