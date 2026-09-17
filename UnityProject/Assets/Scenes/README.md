# First Playable Scene

The runtime bootstrap is intentionally scene-light at this stage.

## Run locally in Unity

1. Open the `UnityProject` folder in Unity 6 LTS.
2. Install Android Build Support, Android SDK & NDK Tools, and OpenJDK through Unity Hub.
3. Create or open any scene.
4. Press Play.
5. `RuntimeGameStarter` automatically creates the prototype world, truck, camera, lighting, roads, depot and tycoon save service.
6. On desktop, drive with W/S and A/D (or arrow keys).

The current world is a functional greybox used to validate gameplay architecture. It is not the final art pass. The production pass will replace procedural geometry with authored truck, road, depot, city, traffic and environment assets, then add PBR materials, baked/optimized lighting, LODs and mobile performance tuning.

## Android direction

The target is a real Android APK, not the browser prototype. Mobile touch controls are implemented as reusable `MobileDriveButton` bindings for throttle, brake and steering and will be wired into the final HUD.
