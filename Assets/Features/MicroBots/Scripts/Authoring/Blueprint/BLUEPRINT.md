# Structure Blueprint — target shape → bot count → connectivity graph

Standalone planning/math layer: given a target shape, produce a **blueprint** — a graph of attachment
nodes and the bot-sized links between them. Pure data, no ECS, no runtime coupling. Nothing here reads or
writes entities; the runtime's job later is to consume a blueprint, not to produce one.

Files: `BotSpanSpec.cs` (the bot as a structural element), `StructureBlueprint.cs` (the graph),
`CubeWireframeBuilder.cs` (cube → blueprint).

---

## 1. What to build: the wireframe, not the solid

**Recommendation: the cube's 12 edges only.** Confirmed — but the reason matters, because it decides what
comes after.

A microbot is a **1-D structural element**: two extremities, a span between them, nothing in between. It
is a strut. Struts naturally tile *curves*, not areas or volumes. So the shapes a bot swarm represents
cheaply are exactly the shapes made of curves — and a wireframe cube is 12 straight runs plus 8 junctions.

The alternatives, and why they're not first:

| Target | Bots (2m cube, 1m reach) | Why not yet |
|---|---|---|
| **12 edges (wireframe)** | 36 | Every link is a straight run; only 8 junctions are non-trivial |
| 6 faces (surface lattice) | ≈110 bare, ≈160 braced | Needs a 2-D tiling decision (triangular? grid?); a 3×3 grid of struts per face with 4-valence nodes is a floppy mechanism until you add ~54 diagonals |
| Solid interior | thousands | Needs a 3-D lattice (octet truss or similar) and packing math; nothing about it is learned by doing the wireframe badly first |

The wireframe also exercises the one thing that is genuinely new — **junctions where more than two bots
meet** — while keeping everything else a straight chain. That's the interesting part, and it's the part
that generalizes: a face lattice and a solid lattice are the same graph model with different node
valences (4 and 6+ instead of 3). Getting the graph representation right on the cube means the later
targets are a different *builder*, not a different *system*.

Deferred by design: faces, interior, curved/arbitrary meshes, structural rigidity analysis.

---

## 2. The bot as a structural element

Fixed properties of the bot (from `MicrobotSegments.LengthA` / `LengthB`):

```
Reach     R = La + Lb                 both segments in a straight line
MaxSpan   S = R · k,  k ≈ 0.9         the usable span
MinSpan   m = |La − Lb|               fully folded back on itself
```

**Why `k < 1`.** A bot spanning exactly `R` is a fully extended 2-bone chain — an IK singularity. The bend
plane is undefined, and any float error past the boundary makes the target unreachable, so the solver
clamps and jitters. `PROGRESS.md` already records this failure mode from the far-away-goal deadlock: past
max reach, `distance(newPosition, GoalPoint)` stops improving. Building a structure that *asks* every
single bot to sit on that boundary is asking for 36 simultaneous instances of that bug. `k = 0.9` keeps a
real elbow bend, which also just looks better — a lattice of perfectly straight bots doesn't read as
"made of robots".

**Elbow angle for a given span** — the pose the blueprint implies, useful for previews and for sanity
checks:

```
θ = acos( (La² + Lb² − d²) / (2·La·Lb) )
```

Equal segments, `d = S = 0.9R` → θ ≈ 128°. `d = R/2` → θ = 60°.

---

## 3. Bot count, and the remainder

Per straight run of length `L`:

```
n    = ceil(L / S)          bots on this run
span = L / n                span each of them holds
```

### Remainder policy: uniform span, fixed segments

The question was whether to shrink segment lengths to fit evenly, or leave a short final bot. **Neither —
shrink the *span*, uniformly, and leave the segments alone.**

Segment lengths are hardware. Every bot in the swarm is the same prefab; `LengthA`/`LengthB` are baked
properties, not a per-instance fitting parameter. But a bot doesn't have to span its full reach — it spans
whatever you ask, anywhere in `[m, S]`, by **bending its elbow more**. That's not a workaround; it's
exactly what the existing planar 2-bone IK does every frame already. So the remainder costs nothing: the
run is divided into `n` equal spans of `L/n`, every bot on that edge is identical, and every node sits at a
clean multiple of `span`.

Why not "full-span bots plus one short remainder bot": the remainder can be arbitrarily small. At
`L = 2.01·S` you get two bots at `S` and one at `0.01·S` — a hairpin-folded bot, possibly below `m` and
therefore *unbuildable*. The failure is silent and depends on the cube size, which is the worst kind.

**The uniform policy has a floor.** With `n = ceil(L/S)`, the resulting span is always in `(S/2, S]` for
`n ≥ 2`. Proof: `n − 1 < L/S`, so `L/n > S·(n−1)/n ≥ S/2`. Verified numerically across `L/S ∈ [0.01, 40]`
— worst observed ratio 0.505 at `L/S = 1.01`. So no bot is ever asked to fold below half-extended, for any
cube size, ever. No clamping, no special cases.

The one uncovered case is `n = 1` (`L ≤ S`, an edge shorter than a single bot's span): then `span = L`,
which can be below `m` for a very small cube. `StructureBlueprint.Validate` flags it rather than silently
emitting an impossible link.

### Totals for a cube

```
bots  = 12 · n
nodes = 8 + 12 · (n − 1)          8 corners + (n−1) mid-edge nodes per edge
```

For the current bot (`La = Lb = 0.5`, `R = 1.0`, `k = 0.9`, so `S = 0.9`):

| Cube size | n/edge | span | elbow | **bots** | nodes | corner nodes | mid-edge nodes |
|---|---|---|---|---|---|---|---|
| 1.0 | 2 | 0.500 | 60.0° | **24** | 20 | 8 | 12 |
| 2.0 | 3 | 0.667 | 83.6° | **36** | 32 | 8 | 24 |
| 2.5 | 3 | 0.833 | 112.9° | **36** | 32 | 8 | 24 |
| 5.0 | 6 | 0.833 | 112.9° | **72** | 68 | 8 | 60 |
| 10.0 | 12 | 0.833 | 112.9° | **144** | 140 | 8 | 132 |

Cube size scales bot count linearly (`≈ 12L/S`), not cubically — the whole point of building the
wireframe. A 10-unit cube is 144 bots, which is a plausible swarm; the same cube as a solid lattice is
five figures.

---

## 4. The connectivity graph

### Model

```
Node  = an attachment point in space     { Position, Kind, Capacity }
Link  = one bot                          { NodeA, NodeB, AttachA, AttachB, Span }
```

Deliberately a **general graph**, not a chain-of-pairs:

- A node has a **capacity** (how many extremities may converge there), not an implicit 2. Mid-edge nodes
  are `Chain`/capacity 2; the 8 cube corners are `Hub`/capacity 3.
- A link stores its **own two attachment points** (`AttachA`/`AttachB`) alongside its node ids. With a
  corner hub radius of 0 these equal the node positions; with a nonzero radius each incident bot attaches
  at its own slightly-offset slot, so the three segment tips at a corner don't occupy identical space.
  This also means a link is directly consumable as a dock target — it *is* a pair of world points, the
  same shape as the existing `Dockable { PointA, PointB }`.
- Links carry `SourceFeatureId` (which cube edge they came from) so a blueprint can be sliced, coloured,
  or built one edge at a time.

Invariants checked by `Validate` (all confirmed passing on the generated cube):

```
Σ valence(node) == 2 · linkCount          every bot has exactly 2 extremities
valence(node)   <= capacity(node)
valence(corner) == 3,  valence(mid) == 2
m <= link.Span <= S                       every link is physically buildable
no orphan nodes
```

For the cube: `Σ valence = 8·3 + 12(n−1)·2 = 24n = 2 · 12n` ✓.

### Corner identity: construct it, don't dedup it

The one real correctness trap. The corner where three edges meet must be **one** node with valence 3, not
three coincident nodes with valence 1 each — and float-hashing positions to discover that is fragile.

So the builder never dedups. It assigns **canonical corner indices** up front: corner `v ∈ [0,8)` is a
3-bit number, bit `i` selecting `−size/2` or `+size/2` on axis `i`. The 12 edges are then enumerated
exactly once, with no duplicates and no comparisons, by the rule *"for each corner, for each axis whose bit
is 0, connect to the corner with that bit set"*. Each edge's chain is generated fresh, referencing the two
pre-existing corner node ids as its endpoints. Sharing is structural, by construction.

For arbitrary meshes later, the same principle transfers: dedup on the *mesh's* vertex indices (an edge is
a sorted vertex-index pair), never on positions.

### Build order

`BuildOrderFrom(seedNode)` returns links in BFS order from a seed. A bot needs one extremity docked to
something already anchored before it can place the other, so the structure must grow outward from a seed
rather than materialize in arbitrary order. BFS from a corner guarantees every link, when its turn comes,
has at least one endpoint node already reached.

---

## 5. What this asks of the runtime (later, not now)

Two things the blueprint needs that the current model doesn't have. Both are flagged here rather than
worked around, because working around them would corrupt the graph representation.

1. **N-way hubs.** `Dockable` exposes exactly two points, and a docking joint currently mates exactly two
   extremities. A cube corner needs **three** extremities coincident at one point. The blueprint models
   this honestly (`Capacity = 3`); the runtime will need an attachment site with a capacity and an
   occupancy count, rather than a `PointA`/`PointB` pair. This is unavoidable for *any* closed shape —
   every vertex of every polyhedron has valence ≥ 3.

2. **Placement targets that aren't a dock prefab.** Each link is a pair of world points that some bot must
   be assigned to occupy. The existing goal-seeking primitive (`HasGoal`/`GoalPoint`) already does the
   hard part per-extremity; what's missing is the assignment layer (which bot takes which link) and
   sequencing against `BuildOrderFrom`.

Nothing here requires changing IK, stepping, or navigation.

---

## 6. Refinements, deliberately not in v1

- **Corner hub radius** (implemented, defaults to 0). Three ball joints at one mathematical point means
  three segment tips interpenetrating. Setting `CornerHubRadius = r` pulls each edge's chain in by `r`, so
  the three tips sit on a small sphere around the corner. Usable edge length becomes `L − 2r` and the span
  math runs on that unchanged. At `size = 2, r = 0.05`: still 3 bots/edge, span 0.633, elbow 78.6°.

- **Euler-trail routing.** All 8 cube corners have odd degree, so no single chain covers the wireframe;
  the minimum is `8/2 = 4` open trails, each covering 3 edges. Routing as 4 trails of length `3L` instead
  of 12 runs of length `L` pays the rounding penalty 4 times instead of 12: for `size = 1` that's 16 bots
  instead of 24 (−33%), for `size = 10` it's 136 instead of 144 (−6%), and for `size = 2.5` it's a wash.
  The cost is that a bot straddling a corner chords across it — the corner gets visibly chamfered, and
  sharp corners are most of what makes a cube read as a cube. Worth revisiting only if bot budget becomes
  the binding constraint, and mainly for small cubes.

- **General meshes.** `StructureBlueprint` is already shape-agnostic; only `CubeWireframeBuilder` knows
  about cubes. A `MeshWireframeBuilder` taking unique mesh edges + per-vertex valence would drop straight
  in, with hub capacity read from the vertex's degree.

- **Rigidity.** A wireframe cube of ball joints is a **mechanism, not a structure** — it shears. Physically
  it needs face diagonals or stiff joints to hold shape. Out of scope for the blueprint math, but it's the
  first thing that will look wrong on screen.
