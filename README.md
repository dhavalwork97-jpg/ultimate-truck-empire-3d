# Ultimate Truck Empire

Unity 6 production game for **ULTIMATE TRUCK EMPIRE** — DRIVE THE TRUCK. BUILD THE COMPANY. CREATE THE EMPIRE.

## Canonical Unity project

**The repository root is the only production Unity project.** Open the repository root in Unity **6000.0.65f1**. Do not open a nested UnityProject folder; the old duplicate prototype has been removed to prevent two competing Unity implementations.

1. Open the repository root in Unity **6000.0.65f1**.
2. Open Assets/Scenes/World_Gujarat.unity.
3. Press **Play**. The runtime bootstrap creates the playable world and gameplay services automatically.
4. If the scene needs to be regenerated, use **Ultimate Truck Empire > Build Phase 1 World** from the Unity Editor menu.

The root project contains Assets/, Packages/, and ProjectSettings/ and is the project used by GitHub Actions Unity validation.

## Production direction

- Android-first 3D trucking + tycoon game
- Authored production 3D assets
- Mobile-optimized rendering and physics
- Trailer/cargo production pipeline
- Garage, fleet, contracts, economy and progression
- JSON save/load
- Gujarat logistics world foundation

The Vite/Three.js implementation in src/ is retained only as a browser/reference prototype. It is not the production renderer.

## Controls

- **WASD / Arrow keys:** drive
- **Space:** brake
- **I:** engine
- **L:** headlights
- **Q / R:** indicators
- **F1–F6:** management views
- **F7:** cycle weather
- **D:** truck dealership

Mobile touch controls are a production task on the canonical root project; do not copy the removed legacy UnityProject implementation back into the repository.

The project uses only fictional manufacturers/assets in the base systems; no Truck Simulator: Ultimate proprietary assets are included.
