# Ultimate Truck Empire — Unity Android Direction

The project direction is now officially a real Unity Android game rather than a browser-only Three.js game.

## Target
- Android APK
- 3D truck driving + tycoon gameplay
- Touch controls
- High-quality authored 3D assets
- PBR materials and mobile-optimized lighting
- Garage, fleet, contracts, economy, upgrades and progression
- Save/load system

## Existing browser prototype
The existing Vite/Three.js implementation is retained as a gameplay/reference prototype. It is not the target production renderer.

## Unity production plan
1. Create Unity project with mobile-first rendering.
2. Establish truck controller and camera.
3. Add authored truck/trailer assets.
4. Build optimized city/highway/industrial environments.
5. Add contracts, economy, garage and progression.
6. Add touch controls and Android UI.
7. Add save system and settings.
8. Profile on Android hardware and optimize draw calls, textures, shadows and memory.
9. Produce development APK builds.

## Visual target
Aim for polished mobile 3D visuals using real 3D assets, PBR materials, physically based lighting where practical, baked lighting for static environments, optimized shadows, LODs, occlusion/culling and mobile-friendly post-processing. Do not treat the current procedural low-poly Three.js scene as the final art direction.
