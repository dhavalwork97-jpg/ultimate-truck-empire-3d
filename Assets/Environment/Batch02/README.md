# Environment Asset Batch 02

Batch 02 contains two photogrammetry environment captures supplied for the world/map layer.

## Integration contract

- Source models live under `Assets/Environment/Batch02/Source/008/` and `Source/009/`.
- The editor import bridge configures models for mobile-oriented rendering: no animation/camera/light import, mesh optimization, medium mesh compression, and non-readable meshes.
- Albedo textures are capped at 2048 with mipmaps and compressed import settings.
- `Prepare All Environment Tiles` creates reusable static prefabs under `Assets/Environment/Batch02/Prefabs/` and shared materials under `Assets/Environment/Batch02/Materials/`.
- Environment tiles do **not** own gameplay collision. Existing road, warehouse, fuel, garage, toll, delivery, and job systems remain authoritative.
- No freight/economy/map/save architecture is changed by this asset batch.

## Source asset handling

The supplied raw photogrammetry meshes are larger than GitHub's normal single-file limit. Before repository import, each capture should be represented as spatial tiles rather than a monolithic OBJ. The intended production split is four X/Z tiles per capture, preserving UVs and sharing one 2048² albedo texture per capture.

## World usage

The assets are reusable regional/background environment tiles, not replacements for the procedural freight world. They should be placed only in visually appropriate open/background areas and kept clear of existing freight triggers and drivable road ownership.
