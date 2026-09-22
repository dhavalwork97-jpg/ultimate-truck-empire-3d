# Trailer System

The trailer system is data-driven: one trailer base can support many cargo definitions and many skins without duplicating gameplay logic.

## Runtime contract

Every production trailer prefab should contain:

- `LoadedTrailer`
- `TrailerCargoModule`
- `TrailerSkinApplier`
- `LODGroup`
- `CargoSocket`
- `Kingpin`

Recommended hierarchy:

```
TrailerRoot
├── Visuals
├── Sockets
│   ├── CargoSocket
│   ├── Kingpin
│   ├── Wheel_FL
│   ├── Wheel_FR
│   ├── Wheel_RL
│   └── Wheel_RR
├── Colliders
└── LODGroup
```

The exact wheel count can vary by trailer. Wheel socket transforms should be authored at the real wheel centers.

## Mobile budgets

Each `TrailerDefinition` owns the target budgets:

- triangles: default 15,000
- unique material slots: default 3
- maximum texture dimension: default 1024
- LOD levels: default 3

These are targets, not a substitute for profiling. Final performance validation should be done on representative mobile hardware.

## Editor validation

Use:

**Ultimate Truck Empire > Trailer System > Validate Catalog**

for data validation.

Use:

**Ultimate Truck Empire > Trailer System > Validate Trailer Prefabs**

to validate every prefab referenced by the catalog. The prefab validator checks the runtime components, sockets, LODGroup, renderer presence, triangle count, unique material count, and main texture resolution.

The validator does not modify artist assets.

## Asset drop-in workflow

1. Import an original or commercially licensed model into `Assets/TrailerSystem/Prefabs`.
2. Create or update its `TrailerDefinition`.
3. Add the required runtime components.
4. Author sockets at the correct physical positions.
5. Build LOD0/LOD1/LOD2 and assign the LODGroup.
6. Keep materials and textures within the definition budgets.
7. Run both validators.
8. Only then register the prefab for gameplay/dealer use.

Do not ship third-party meshes or textures merely because their source package is described as "free"; verify the license permits commercial redistribution first.
