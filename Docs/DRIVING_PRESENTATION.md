# Camera, Cockpit and Driving Presentation

Second presentation pass on `phase-1-playable-world`. Adds the driving camera,
a procedural cab interior, a driving HUD and a gear/cruise layer on top of the
existing `TruckController`.

## The branch did not compile before this pass

`WorldBootstrap.cs` line 63 called `camGo.localPosition` and `camGo.LookAt(...)`
on a `GameObject`. Neither member exists on `GameObject` (they live on
`Transform`), so the assembly failed to build and nothing in the project ran.
That line was the camera creation code, so it is replaced by the new rig.

Verified with Roslyn against Unity API stubs: those were the only two errors in
the branch, and the tree now compiles clean.

## Controls

| Key | Action |
| --- | --- |
| WASD / arrows | Steer and throttle |
| SPACE | Service brake |
| C | Cycle camera: chase → bumper → hood → cockpit |
| Right mouse (hold) | Look around; recentres when released |
| G | Cycle gear P → R → N → D |
| V | Cruise control (Drive, above 25 km/h) |
| I | Engine on/off |
| L | Headlights |
| Q / R | Left / right indicator |
| Z | Hazards |
| H | Horn |
| TAB | Show/hide management panel (ESC closes) |
| F1–F6 | Management tabs |
| F7 | Cycle weather |
| F8 | Management actions panel |
| F10 | Dispatch panel |
| F11 | Truck dealership |

`D` used to open the dealership, which also fired every time the player steered
right. It moved to `F11`.

## Camera

`Assets/Scripts/Camera/TruckCameraRig.cs`. One `Camera` component, four
viewpoints — there is no second camera in the scene, so "only the active view
renders" holds by construction rather than by enabling and disabling cameras.

- Chase: position smoothed with `SmoothDamp`, rotation with an exponential
  slerp, look-ahead ahead of the nose so the road reads. Runs in `LateUpdate`
  against the truck's interpolated transform, so no jitter against FixedUpdate.
- Collision: `SphereCast` from the pivot toward the desired position, pulled in
  to the first blocker with a margin and a hard minimum distance. Hits on the
  truck's own colliders are ignored.
- FOV widens slightly with speed; the cockpit view uses a fixed FOV.
- Chase distance (11.5 m) and height (4.7 m) are set so the tractor and trailer
  both stay in frame.

## Cockpit

`Assets/Scripts/Truck/TruckCockpit.cs`. Right-hand drive, matching the Gujarat
setting. Dashboard with vents and a radio stack, instrument binnacle, two backlit
dials with live needles, steering wheel that turns with steering input, seats,
door cards with handles, A-pillars, headliner, sun visors, centre console, gear
lever and pedals.

Geometry is welded into a single mesh and the whole interior is disabled unless
the cockpit view is active, so exterior views pay nothing for it.

The interior is aligned numerically to the cab the visual pass builds: floor
1.18, roof 3.16, windscreen base ~2.29, driver eye at (0.62, 2.45, 1.82). The
eye sits above the screen base with the dash top just below the sight line, so
the dashboard occupies the lower part of the frame and the road is framed by the
screen rather than blocked by bodywork.

## HUD

`Assets/Scripts/UI/DrivingHUD.cs`. Two small dark panels in the bottom corners
plus indicator arrows at the top; nothing in the middle of the screen. Shows
speed in km/h, gear, fuel bar, engine/brake/headlight/cruise/limiter/hazard
state, cargo, route, load status, next stop, distance and reward.

It draws at `sortingOrder -10`, underneath the management UI, and does not
replace it.

The management panels used to be full-screen, opaque and always on, which hid
the entire driving view. They now start hidden and toggle on their keys.

## Gearbox and driving feel

`TruckController` keeps its existing wheel wiring and `ConfigureWheels`
signature. Added: P/R/N/D state, steering smoothed with `MoveTowards` plus the
existing speed-based angle reduction, back-input-as-brake, a speed limiter flag,
and cruise control with a simple proportional term.

Holding reverse from a standstill still reverses the truck — it auto-shifts into
R after 0.35 s — so the old WASD behaviour is unchanged. Key presses moved from
`FixedUpdate` to `Update`, where `GetKeyDown` is reliable.

## Other fixes found while working in the branch

- No `EventSystem` existed anywhere, so every management `Button` and
  `InputField` was inert. One is created in `EnsureSystems`.
- `TruckWheelRig` parented wheel meshes to the `WheelCollider` transform, which
  does not steer, spin or move with suspension. Meshes are now parented to the
  truck and driven from `GetWorldPose`.
- `truck.tag = "PlayerTruck"` throws when the tag is not in the Tag Manager, and
  this project ships no `ProjectSettings/TagManager.asset`. The assignment is now
  guarded and `DeliveryTrigger` identifies the truck by component first.
- The world was built entirely from default white materials. `WorldPalette` adds
  a small set of shared, pipeline-aware materials for road, ground, markings,
  buildings, trees and poles, plus emissive depot markers.
- The pass-1 truck/trailer visual package was never present on this branch. It
  is included here and wired into `WorldBootstrap`.

## Verification still required in the Unity Editor

None of this has been run in Unity — there is no editor in the environment where
it was written. Before merging:

1. Open `Assets/Scenes/World_Gujarat.unity`, press Play, confirm no compile
   errors and no `NullReferenceException` from bootstrap.
2. Press C through all four views. Check the cockpit view specifically: eye
   height, dash in the lower frame, no clipping through the cab.
3. Drive: steering response, suspension travel on the wheels, brake lights,
   indicators blinking, gear changes on G, cruise on V.
4. Reverse from a standstill with S to confirm the auto-shift still feels right.
5. Confirm the chase camera does not clip through buildings or the depot.
6. Check the HUD scales sensibly at your target resolution and on 16:10.
7. Confirm management panels open and close and that their buttons now respond
   (they need the new EventSystem).
8. Add a `PlayerTruck` tag in Project Settings if you want tag-based checks.
9. Profile once: the scene has one directional light, two headlight spots and
   nine street lamps.
