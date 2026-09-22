# Trailer System

Authoring and runtime architecture for Ultimate Truck Empire trailers.

## Exact Unity structure

Assets/
- TrailerSystem/
  - Definitions/
    - TrailerDefinition.cs
    - CargoDefinition.cs
    - TrailerSkinDefinition.cs
  - Catalog/
    - TrailerCatalogAsset.cs
  - Runtime/
    - TrailerCompatibility.cs
    - TrailerSkinApplier.cs
    - LoadedTrailer.cs
  - Prefabs/                 # trailer prefabs only
  - Materials/               # shared mobile trailer materials
  - Textures/
    - Trailers/              # base trailer textures
    - Skins/                 # fictional commercial-safe skins
  - Cargo/
    - Prefabs/               # reusable cargo meshes
    - Textures/              # cargo textures
  - Data/
    - Trailers/              # TrailerDefinition .asset files
    - Cargo/                 # CargoDefinition .asset files
    - Skins/                 # TrailerSkinDefinition .asset files
    - TrailerCatalog.asset

## Design

TrailerDefinition is the authoritative data asset for one trailer base. CargoDefinition is the authoritative data asset for one cargo type. A LoadedTrailer combines one of each at runtime.

The model is intentionally modular: one trailer mesh can support many cargo types and many skins. Skin changes are applied at runtime without duplicating the trailer mesh.

Target mobile budgets are stored on TrailerDefinition and should be enforced during asset import/review: shared materials, small material count, LODs, and texture atlases.

The existing Gameplay.CargoCatalog remains as a legacy contract generator. New systems should consume TrailerSystem.CargoDefinition and TrailerSystem.TrailerDefinition assets.