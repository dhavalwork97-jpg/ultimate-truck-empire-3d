# Environment Asset Batch 02

Batch 02 contains two photogrammetry environment captures supplied for the world/map layer.

## Integration contract

- Source models live under `Assets/Environment/Batch02/Source/008/` and `Source/009/`.
- The committed editor bridge currently provides the Batch 02 preparation menu entry; Unity-side import/prefab generation still requires validation in the target Unity project.
- Albedo textures are prepared at 2048² with mipmaps as the intended mobile-friendly source representation.
- Environment tiles do **not** own gameplay collision. Existing road, warehouse, fuel, garage, toll, delivery, and job systems remain authoritative.
- No freight/economy/map/save architecture is changed by this asset batch.

## Source asset handling

The supplied raw photogrammetry meshes are large monolithic OBJs. For repository-friendly integration, the supplied captures are represented as spatial tiles rather than monolithic OBJs while preserving the source UV layout.

- Capture 008: four X/Z tiles.
- Capture 009: six X/Z tiles. The extra split keeps every OBJ comfortably below the GitHub browser-upload size threshold.
- Each capture uses one shared 2048² albedo texture.
- Each tile keeps its `material.mtl` beside the OBJ and texture.

The tile files are derived from the two supplied ZIPs; they were not present in the original ZIPs.

## World usage

The assets are reusable regional/background environment tiles, not replacements for the procedural freight world. They should be placed only in visually appropriate open/background areas and kept clear of existing freight triggers and drivable road ownership.

## Validation status

Binary source tiles have been prepared locally from the supplied ZIPs. They are not yet present in the GitHub repository because the available GitHub integration does not provide a binary/LFS upload operation. Unity import, prefab generation, compilation, and CI therefore remain pending until the binary assets are uploaded through a suitable Git/LFS path.
