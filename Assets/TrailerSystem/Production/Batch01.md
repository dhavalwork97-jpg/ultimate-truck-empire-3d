# Meshy production trailer batch 01

Source assets are staged under Assets/TrailerSystem/Production/Meshy/<TrailerId>/ and must retain the FBX and PBR textures.

Batch 01:
- TRAILER_DRY_VAN_001 — Covered Cargo Trailer
- TRAILER_FUEL_TANKER_001 — Gas Combustion Tanker

The existing intake pipeline prepares runtime prefabs and TrailerDefinition assets. Production authors must verify kingpin, cargo and wheel sockets, collision geometry and authored LODs before shipping.

Mobile validation budgets:
- LOD0 <= 25,000 triangles
- LOD1 <= 12,000 triangles
- LOD2 <= 5,000 triangles
- LOD3 <= 1,500 triangles
- Material slots <= 3
- Meshy staging texture cap: 1024

The optimizer deliberately does not fabricate LOD geometry; LOD meshes must be authored or generated deterministically and validated.