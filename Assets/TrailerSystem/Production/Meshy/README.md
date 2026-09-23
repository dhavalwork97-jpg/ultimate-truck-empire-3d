# Meshy production intake

Place the six user-supplied Meshy FBX/PNG generation packages here after importing them into Unity:

- Covered Cargo Trailer → DryVan
- Tarp Covered Flatbed → Flatbed
- Timber Hauler → HeavyFlatbed
- Gas Combustion Tanker variants → tanker candidates
- Corrugated Steel Ware → Cargo

Run:

**Ultimate Truck Empire → Trailer Assets → Optimize Meshy Mobile Imports**

Then author the production prefabs and three actual LOD meshes. The optimizer configures Unity import settings and caps texture imports at 1024; it deliberately does not create fake LODs or silently decimate gameplay-critical geometry.

Production requirements remain:
- LOD0 <= 15,000 triangles
- <= 3 material slots
- max texture 1024
- 3 authored LOD levels
- optimized collision
- Kingpin/Cargo/Wheel sockets
- existing trailer physics and cargo architecture

Do not commit the original source ZIP. Only the production FBX/prefab/texture outputs that have passed the asset provenance and production validators belong in this folder.
