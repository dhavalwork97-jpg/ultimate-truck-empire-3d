# World Visual Quality, Lighting and Atmosphere

Third presentation pass on `phase-1-playable-world`. Rebuilds the procedural
environment: roads, city, trucking sites, vegetation, street furniture, sky,
day/night, weather and traffic bodywork.

Gameplay is untouched. Company, driver, fleet, finance, contract, dispatch,
delivery and save systems are all unchanged, and no manager was added or
duplicated.

## Road network

`RoadBuilder` builds each road as one welded mesh with a submesh per material:
asphalt slab, gravel shoulders, curbs, double yellow centre line, white lane
dashes, edge lines, arrows, hatching and parking bays. One box collider per
segment; markings and curbs carry none.

Heights are fixed and deliberate, because coplanar surfaces are what cause
z-fighting:

| Layer | Y |
| --- | --- |
| Ground plane | -0.060 |
| Gravel shoulder | -0.015 |
| Asphalt | 0.000 |
| Paint | 0.012 |
| Arrows / hatching | 0.014 |
| Curb top | 0.140 |

Crossing roads never overlap. The east–west highways stop 8 m short of the
junction with the link road, and a separate intersection pad fills the gap with
stop bars and zebra crossings.

Previously every road drew its top face at exactly y = 0, the same plane as the
ground, and the lane markings were rotated 90° from the road they sat on (dashes
ran across the carriageway, and one set was painted where there is no road at
all). Both are gone.

## Sites

`SiteBuilder` builds four locations with published footprints, so the city
generator can keep its blocks out of them:

- **Ahmedabad logistics depot** (spawn area) — concrete apron, 48 m warehouse with
  roller doors and dock bumpers, office block, gate with barrier posts, perimeter
  fence, painted bays, hatched no-parking, container stacks, pallets, bollards,
  yard lighting and `ULTIMATE TRUCK EMPIRE` signage.
- **Vadodara factory** (destination) — sawtooth-roof factory with chimneys, storage
  tanks and pipework, receiving warehouse, gated yard.
- **Truck stop** — fuel canopy over pump islands, shop, marked truck bays.
- **Service centre** — workshop with roller doors, tyre stacks, jack stands.

The load and unload triggers moved off the carriageway and into the yards, so
the player pulls into the site instead of driving past a box on the road.

## City and skyline

Six reusable building styles — warehouse, factory, office, shop, residential and
a detail-free distant silhouette — each varied by a deterministic per-building
seed: dimensions, facade colour, floor counts, window grids, roof shapes, water
tanks, parapets, chimneys, glazing bands.

Placement rejects anything within clearance of a road corridor or inside a site
footprint. A ring of 34 silhouettes at 215–275 m gives the horizon depth through
the fog, so looking down a road gives foreground markings, midground buildings
and traffic, and a background skyline.

## Lighting, day/night and weather

`EnvironmentAtmosphere` owns the sun, ambient, fog and skybox. `TimeWeatherManager`
keeps ticking time of day and calls into it — one system drives lighting, not two.

- Sun angle is correct now: 06:00 on the horizon, 12:00 overhead, 18:00 opposite
  horizon. The previous rotation had a spare −90° in it, which put the sun below
  the horizon at midday.
- Sun colour warms toward the horizon; shadows switch off in heavy weather and
  after dark.
- At night a low, cool key light stands in for moonlight and ambient bottoms out
  at a blue-grey, never black, so the road stays readable.
- `StreetLightManager` enables street lamps only at night and only the closest
  ten to the player, re-evaluated a few times a second. Lamp heads glow via a
  cached emissive material.
- Weather drives light level, fog colour and density, sky tint and exposure.
  Rain, heavy rain and storms also darken and gloss the shared asphalt materials,
  which wets the entire road network in one call with no allocation.

There are no rain particles — see limitations.

## Materials

`WorldPalette` is one shared, pipeline-aware material set (~40 instances total,
regardless of object count): asphalt wet and dry, worn asphalt, concrete, curb,
gravel, dirt, two grasses, six facade colours, glass, dark glass, metal, painted
metal, roofing, rust, four sign colours, markings, warning stripe, timber, tyre,
three container colours.

The mesh builder now takes an `IMaterialSlots` interface, so world geometry can
use the full palette while vehicles keep their own, and both still weld into
single meshes with one submesh per material.

## Traffic

`TrafficVisualFactory` builds five silhouettes — hatchback, sedan, van, bus, box
truck — with eight body colours, glass, tyres, trim and lamps. Meshes are cached
per shape, no colliders, no AI changes. `TrafficManager` used to drop untextured
boxes at random coordinates, which put them inside buildings and across roads; it
now stands down when `TrafficSpawner` is present.

## Performance notes

- Small repeated props are batched: every fence, bollard, pallet, container and
  dumpster on a site is one renderer. All roadside trees are two renderers.
- Realtime lights, worst case: 1 sun + 2 truck headlight spots + 10 street lamps
  + 1 optional cab light. Everything else is emissive material.
- Roughly 150 renderers for the whole environment.
- Colliders exist only on roads, the ground, buildings and the player truck.
- No per-frame allocations in the atmosphere or street light systems; the skybox
  material only updates when time or weather actually moves.

## Known limitations

- **No rain particles.** Wet weather is conveyed by lighting, fog and wet road
  materials only. A particle system was deliberately left out of this pass.
- **`Shader.Find("Skybox/Procedural")` can return null in a player build** if the
  shader is not in Always Included Shaders. The code degrades gracefully to fog
  plus ambient, but add it in Project Settings → Graphics for builds.
- Buildings are box-based; there are no curved or angled facades.
- Traffic still drives a fixed waypoint loop and passes through everything — the
  AI was out of scope for this pass.
- No lightmaps or reflection probes; everything is realtime.
- The city generator places buildings on a jittered grid, so street layout reads
  as blocks rather than organic streets.

## Still needs actual Unity Editor testing

Nothing here has been run in Unity, built, or looked at. Compilation was verified
with Roslyn against Unity API stubs, which catches references, namespaces and
signatures — not appearance or runtime behaviour.

1. Press Play and confirm no compile errors and no exceptions during bootstrap.
2. Look down the highway from the chase camera and check the composition reads.
3. Check for z-fighting on roads, markings, aprons and the ground plane.
4. Drive through both junctions and into both yards; check nothing intersects the
   carriageway and the truck cannot get stuck on scenery.
5. Let time run to dusk and night (`timeScale` 0.08, so roughly five minutes per
   day) and confirm street lights come on, headlights matter and the road is
   still readable.
6. Cycle weather with F7 through all six states and check sky, fog and wet roads.
7. Confirm pickup and delivery still fire now that the triggers sit in the yards.
8. Profile: check frame time, draw calls, and total vertex count in the Stats
   window on your target machine.
9. Add `Skybox/Procedural` to Always Included Shaders before making a build.
