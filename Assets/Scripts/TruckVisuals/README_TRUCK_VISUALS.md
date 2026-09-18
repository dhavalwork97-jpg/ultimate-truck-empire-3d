# Truck & Trailer Visual Pass

Procedural 3D presentation upgrade for the truck, trailer, wheels and lighting.
Everything lives in the `UltimateTruckEmpire.Visuals` namespace under
`Assets/Scripts/TruckVisuals/`.

**This pass is purely additive.** It does not reference `WorldBootstrap`,
`TruckController`, `TruckPhysics`, `TruckWheelRig`, `TrailerController`,
`TruckLights` or `TruckDealer` by type, so it compiles on its own and cannot
break existing gameplay, company or economy code. Wiring it in is one line.

> These scripts were written without access to the repository (it is private and
> no credentials were available), so nothing here has been reconciled against the
> existing code. The integration notes below tell you exactly what to check.

---

## 1. Minimum integration

Either drop the `TruckVisualApplier` component on the truck prefab in the
Inspector and set `Model Id`, **or** call this once where the truck is spawned
(e.g. in `WorldBootstrap` after the truck GameObject and its WheelColliders
exist):

```csharp
using UltimateTruckEmpire.Visuals;

// modelId can be the TruckDealer catalogue id or the display name.
TruckVisualAssembler.Apply(truckGameObject, modelId);
```

Trailer:

```csharp
TruckVisuals trailerVisuals = TruckVisualAssembler.ApplyTrailer(trailerGameObject, null);
TruckVisualAssembler.LinkTrailerLights(truckVisuals, trailerVisuals);
```

Order matters: call `Apply` **after** the WheelColliders exist. The builder reads
their real positions and radii and builds the bodywork, mudguards and axle beams
around them. With no colliders present it falls back to the spec numbers so the
model still looks right in an unconfigured scene.

## 2. What it will and will not touch

| Does | Does not |
| --- | --- |
| Creates `TruckVisuals`, `WheelVisuals` child objects | Add, move or edit any WheelCollider, Rigidbody or joint |
| Disables pre-existing placeholder renderers (reversible via `RestoreHiddenRenderers()`) | Destroy existing GameObjects or components |
| Adds `TruckWheelVisualSync` and `TruckLightRig` if absent | Replace an existing wheel rig or lights script |
| Strips colliders from **generated** cosmetic meshes only | Touch colliders that were already in the scene |

Running `Apply` twice replaces its own output rather than duplicating it, so it
is safe to call on repaint or model swap.

## 3. If `TruckWheelRig` already syncs wheels

`TruckWheelVisualSync` disables itself on `Awake` when a component named
`TruckWheelRig` is on the same GameObject (the check is by type *name*, to avoid
a compile-time dependency). That is the default and it means **no duplicate wheel
system**. Two options:

- Keep your rig: hand it the generated transforms from
  `truckVisuals.wheelVisuals` (ordered front-to-back, left-to-right per axle) and
  leave `deferToExistingRig = true`.
- Use this one: set `deferToExistingRig = false`, or clear
  `existingRigTypeNames`.

Either way the visuals follow steering, suspension travel and wheel spin, because
they are driven from `WheelCollider.GetWorldPose` in a single `LateUpdate`.

## 4. If `TruckLights` already exists

Keep it. `TruckLightRig` is presentation only — let your existing script call it:

```csharp
rig.SetHeadlights(HeadlightMode.Low);   // Off / Low / High
rig.SetBrakes(isBraking);
rig.SetReverse(inReverse);
rig.SetIndicator(IndicatorState.Left);  // None / Left / Right / Hazard
rig.SetInteriorLight(true);             // optional, off by default
```

Lamps are emissive **shared** material swaps, not lights. The only realtime
lights are two headlamp spots (shadows off, created on first use) plus an
optional cab point light. That is the whole lighting budget.

## 5. Dealership differentiation

`TruckVisualPresets.Resolve(id)` maps a catalogue entry onto one of six fictional
models: `kestrel-lt`, `drover-m`, `nomad-aero`, `brontes-900`, `atlas-hd`,
`sable-classic`. They differ in cab layout (cab-over vs long bonnet), cab size,
grille style and bar count, bumper, chassis length and height, wheel size, axle
count and trailer capability.

Resolution order: exact id/name match → keyword match (`light`, `heavy`,
`classic`, `aero`, …) → stable hash, so an unknown id always yields the same
look. To map your catalogue explicitly, either rename catalogue ids to the preset
ids or add a `case` in `Resolve`.

No real manufacturer names, badges, trade dress or model geometry are used.

## 6. Performance notes

- All static bodywork is welded into **one mesh with one submesh per material**,
  so a truck is roughly: 1 body renderer + 1 renderer per wheel + small lamp
  renderers + 1 mirrors renderer.
- Wheel meshes are cached by shape, so a whole fleet shares a couple of `Mesh`
  assets. Materials are cached by description and shared between vehicles.
- No `Update` anywhere. One `LateUpdate` for wheels; lights are event-driven with
  a coroutine for indicator blink.
- Generated cosmetic geometry never gets a collider.
- Tyres use 18 radial segments; most detail parts are boxes.

`TruckVisuals.generatedVertexCount` reports the welded body mesh only (wheels and
lamps are separate).

## 7. Files

| File | Role |
| --- | --- |
| `ProcMesh.cs` | Mesh primitives (box, frustum, cylinder, profile extrusion with ear-clipping) + `MeshBuilder` |
| `TruckMaterialLibrary.cs` | Pipeline-aware (Built-in / URP / HDRP) cached materials and the slot palette |
| `TruckVisualSpec.cs` | Data-only truck & trailer specs + the six fictional presets |
| `TruckBodyBuilder.cs` | Cab, glass, bonnet, grille, bumper, tanks, steps, exhaust, fenders, mirrors, lamps |
| `TrailerVisualBuilder.cs` | Dry-van box, rear doors, bogie, landing gear, kingpin, lamps |
| `TruckVisualUtility.cs` | Shared wheel mesh cache, collider stripping, shadow helpers |
| `TruckWheelVisualSync.cs` | Wheel meshes ← WheelColliders (defers to an existing rig) |
| `TruckLightRig.cs` | Lamp material states + at most two realtime headlight spots |
| `TruckVisuals.cs` | Bookkeeping component for generated objects |
| `TruckVisualAssembler.cs` | Entry point; derives wheel layout, builds, wires everything |
| `TruckVisualApplier.cs` | Optional Inspector-driven wrapper |

## 8. Verification still required in the Unity Editor

None of this has been run in Unity. Before merging:

1. Open the scene, enter Play mode, confirm no compile errors and no
   `NullReferenceException` from `Apply`.
2. Check the truck reads correctly from outside: cab proportions, wheels sitting
   inside the arches, nothing intersecting.
3. Drive it — steering, suspension travel and wheel spin should all be visible.
4. Toggle headlights, brakes, reverse and indicators.
5. Confirm the existing placeholder mesh is hidden and not double-drawn.
6. If using URP/HDRP, confirm nothing renders magenta (the library picks the
   shader at runtime, but verify).
7. Check the trailer couples and aligns with the fifth wheel.
8. Profile once on the target device; drop tyre segments from 18 to 12 in
   `TruckVisualUtility` if needed.
