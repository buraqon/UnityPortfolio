# Blueprint — how to generate one and how to read it

Read this before touching anything in this folder. It is the **contract**: what a blueprint is, what
guarantees hold, and how to consume one without making assumptions that aren't true.

For *why* the math is the way it is — bot count derivations, the remainder policy, why spans never use
full reach, the corner-valence problem — see [BLUEPRINT.md](BLUEPRINT.md). This file does not repeat any
of that.

## What this is

A **blueprint** is a graph describing where bots should be in order to form a target shape. It is pure
data produced by a pure function. No ECS, no entities, no per-frame work, no `MonoBehaviour` dependency
in the core types.

| File | Role |
|---|---|
| `BotSpanSpec.cs` | A bot as a structural element: segment lengths → reach, usable span, fold limit, elbow angle |
| `StructureBlueprint.cs` | The graph itself: nodes, links, traversal, validation |
| `CubeWireframeBuilder.cs` | Cube settings → blueprint. The only cube-aware code |
| `CubeBlueprintSource.cs` | `MonoBehaviour` holding one blueprint in the scene, with gizmos |
| `BLUEPRINT.md` | Design rationale and derivations |

## Core model

- **Node** — an attachment point in world space. Has a `Capacity`: how many bot extremities may converge
  there. Mid-edge nodes are `Chain`/capacity 2; cube corners are `Hub`/capacity 3.
- **Link** — **one bot**. `BotCount == Links.Count`. Carries the two exact world points that bot's two
  extremities must occupy.

The graph is general. Do not assume valence 2 anywhere, and do not assume a link's endpoints are the same
as its attach points.

## Data contract

### `StructureNode`

| Field | Meaning |
|---|---|
| `Id` | Index into `Nodes`. Stable for the lifetime of the blueprint object |
| `Position` | World-space position of the node itself |
| `Kind` | `Chain` (pass-through) or `Hub` (junction, 3+ bots meet) |
| `Capacity` | Max extremities that may attach. **Not** the current count — use `Valence(id)` for that |

### `StructureLink`

| Field | Meaning |
|---|---|
| `Id` | Index into `Links`. Stable |
| `NodeA` / `NodeB` | The two nodes this bot spans |
| `AttachA` / `AttachB` | The exact world points the extremities must occupy. **Not always the node positions** |
| `Span` | `distance(AttachA, AttachB)`. Computed at construction, never stale |
| `SourceFeatureId` | Which feature of the target shape this came from. For the cube: edge index 0–11 |

**`AttachA`/`AttachB` vs `Node.Position`** is the field pair most likely to be misused. They are equal for
`Chain` nodes always, and equal for `Hub` nodes only when `CornerHubRadius == 0`. With a nonzero hub
radius each bot at a corner attaches at its own slightly offset slot so the three segment tips don't
interpenetrate, while the node's `Position` stays the true corner.

**Use `AttachA`/`AttachB` when positioning bots. Use `Position` when reasoning about the shape.**

## Guarantees

These hold for any blueprint from `CubeWireframeBuilder`, and the first four are the general contract any
future builder must also satisfy:

1. `Σ Valence(node) == 2 * Links.Count` — every bot has exactly two extremities.
2. `Valence(node) <= node.Capacity`.
3. `link.Span == distance(link.AttachA, link.AttachB)`.
4. Every node is referenced by at least one link (no orphans).
5. **Node ids 0–7 are the cube's 8 corners**, in canonical order. Corner `v` is a 3-bit number: bit 0 = X,
   bit 1 = Y, bit 2 = Z; a set bit means `+size/2`, clear means `−size/2`. Mid-edge nodes start at id 8.
6. **Links are grouped by edge and contiguous**, in edge-table order, each group running from the lower
   corner toward the higher. Within a group, `NodeA`/`AttachA` is always the end nearer `EdgeCorners(e).x`.
7. **Every link on a cube has the same `Span`** — uniform by construction, not by coincidence.
8. Corner sharing is structural: the three edges meeting at a corner reference *the same node id*. There
   is no position-based deduplication anywhere, and none should be added.

Edge-table order, if you need to map link groups back to geometry:

```
0:(0,1) 1:(0,2) 2:(0,4) 3:(1,3)  4:(1,5)  5:(2,3)
6:(2,6) 7:(3,7) 8:(4,5) 9:(4,6) 10:(5,7) 11:(6,7)
```

**Invalid settings produce an empty blueprint, not an exception.** Zero or negative size, zero segment
lengths, or a hub radius that eats the whole edge all yield 0 nodes and 0 links. Readers must tolerate an
empty blueprint — do not assume `Links.Count > 0`.

## Generating one

Three entry points, in increasing order of directness.

**In the scene** — `CubeBlueprintSource` on a GameObject. Cube center is `transform.position`; everything
else is an inspector field. Access via the `Blueprint` property, which rebuilds lazily whenever the
settings it was built from stop matching the current ones (covers both inspector edits and moving the
transform). Must live in the **main scene, not a SubScene** — plain MonoBehaviours in a baked SubScene do
not exist at runtime.

**From code:**

```csharp
var settings = new CubeWireframeSettings
{
    Center = float3.zero,
    Size = 2f,
    Bot = new BotSpanSpec { LengthA = 0.5f, LengthB = 0.5f, SpanSafety = 0.9f },
    CornerHubRadius = 0f
};

var blueprint = CubeWireframeBuilder.Build(settings);
```

**Counting without building** — all cheap, no allocation, for UI and budgeting:

```csharp
CubeWireframeBuilder.BotsPerEdge(settings);   // bots along one edge
CubeWireframeBuilder.SpanPerBot(settings);    // span each one holds
CubeWireframeBuilder.TotalBots(settings);     // 12 * BotsPerEdge
CubeWireframeBuilder.TotalNodes(settings);    // 8 + 12 * (BotsPerEdge - 1)
```

## Reading one

**Every bot placement:**

```csharp
foreach (var link in blueprint.Links)
{
    // link.AttachA and link.AttachB are the two points one bot must occupy.
}
```

**Placement order** — `BuildOrderFrom(seedNodeId)` returns every link id exactly once in BFS order, so
each link, when its turn comes, has at least one endpoint already reached:

```csharp
foreach (var linkId in blueprint.BuildOrderFrom(0))   // 0 = a cube corner
{
    var link = blueprint.GetLink(linkId);
}
```

**Walking the graph** from a node:

```csharp
foreach (var linkId in blueprint.IncidentLinks(nodeId))
{
    var neighbour = blueprint.OtherNode(linkId, nodeId);
    var attach = blueprint.AttachPointOf(linkId, nodeId);   // this link's slot at this node
}
```

`OtherNode` and `AttachPointOf` exist so callers never branch on `NodeA == nodeId` themselves — that
branch is easy to get backwards and is the reason both helpers are on the type.

**Finding junctions:**

```csharp
foreach (var node in blueprint.Nodes)
{
    if (node.Kind == StructureNodeKind.Hub) { /* 3 extremities converge here */ }
}
```

## Validation

```csharp
var problems = new List<string>();
if (!blueprint.Validate(settings.Bot, problems))
{
    // problems holds one human-readable line per failure
}
```

Checks capacity overflow, orphaned nodes, the valence-sum identity, and every span against the bot's
`MaxSpan`/`MinSpan`. The list is cleared on entry, so it is safe to reuse. `CubeBlueprintSource.Validate`
wraps this with the component's own settings.

Validation is a **debug/authoring aid**, not a runtime gate — nothing calls it automatically.

## Gotchas

- **Managed collections.** `StructureBlueprint` holds `List<T>`, so it cannot be touched inside a Burst
  job. Build it on the main thread, then copy what jobs need into native containers.
- **Not serialized.** Blueprints are rebuilt, never saved. Generation is deterministic and takes
  microseconds; caching one in a scene file would only create a staleness risk. Don't add
  `[SerializeField]` to a blueprint.
- **Rebuilds replace the object.** `CubeBlueprintSource.Blueprint` may return a *different instance* than
  the last call if settings changed. Don't hold a long-lived reference across an inspector edit — re-read
  the property, or cache node/link ids rather than the blueprint itself.
- **Ids are per-instance.** Stable within one blueprint object, meaningless across a rebuild.
- **`Validate` doesn't check reachability**, only local structure. A geometrically absurd but
  well-connected graph passes.

## Adding a shape builder

`StructureBlueprint` is shape-agnostic; only `CubeWireframeBuilder` knows about cubes. A new builder
(mesh wireframe, face lattice, solid lattice) needs to:

1. Create all junction/shared nodes **first**, so their ids are predictable and sharing is structural.
2. Derive node identity from the source shape's own indexing — mesh vertex indices, corner bit patterns —
   **never by comparing float positions**.
3. Set each node's `Capacity` from its true valence in the target shape. A face lattice has capacity-4
   nodes; a solid lattice 6+. Nothing in `StructureBlueprint` assumes 2 or 3.
4. Divide each run into `n = Bot.BotsToSpan(length)` equal spans rather than packing full-span bots and
   leaving a short remainder. See BLUEPRINT.md for why.
5. Return an empty blueprint for invalid settings instead of throwing or looping.

Satisfy those and `Validate`, `BuildOrderFrom`, and every consumer keep working unchanged.
