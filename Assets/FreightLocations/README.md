# Freight Location Pack 01

This pack turns the warehouse and factory source models into reusable logistics locations.

## Source assets

The supplied models are imported into Unity under:

- `Assets/FreightLocations/Source/Factory/basic_factory_modeling_.fbx`
- `Assets/FreightLocations/Source/Warehouse/warehouseupload2.fbx`

Their imported materials/textures are retained under the FreightLocations source/material folders.

## Production prefabs

Production prefabs live under:

`Assets/FreightLocations/Prefabs/`

Current variants:

- Warehouse_Base.prefab
- Warehouse_Large.prefab
- Warehouse_Port.prefab
- Factory_Base.prefab
- Factory_Textile.prefab
- Factory_Chemical.prefab
- Factory_Automotive.prefab

The runtime `FreightLocationPrefabCatalog` in `Assets/Resources/FreightLocations/` references these prefabs directly, so no duplicate prefab copies are required.

## Gameplay contract

Location prefabs are visual/logistics infrastructure only. They do not replace the authoritative road network, city delivery triggers, trailer system, economy, or save system.

Each production prefab contains a `MeshRoot`, a 3-level `LODGroup`, and the following logistics anchors:

- `LoadingDock_A`
- `LoadingDock_B`
- `TrailerSpawn`
- `CargoSpawn`
- `DeliveryTrigger`
- `ParkingSlot_01`
- `ParkingSlot_02`
- `CompanySign`
- `EnvironmentCollision`

Existing `DeliveryTrigger` remains authoritative for freight completion.

## Current catalog

30 reusable locations cover all 10 existing freight cities.

The same warehouse/factory source models are intentionally reused through different location definitions.


## Company data
Example company definitions are stored under `Assets/FreightLocations/Data/Companies/` and reference the reusable production prefabs.

## Editor prefab generation

The source FBX models can be converted into the seven production prefabs automatically with:

**Unity menu:** `Ultimate Truck Empire > Freight Locations > Build Production Prefabs`

The generator reads:

- `Assets/FreightLocations/Source/Warehouse/warehouseupload2.fbx`
- `Assets/FreightLocations/Source/Factory/basic_factory_modeling_.fbx`

and writes the seven contract prefabs into the existing `Warehouse` and `Factory` prefab folders. It also refreshes `Assets/Resources/FreightLocations/FreightLocationPrefabCatalog.asset` so all 30 registered locations point at a valid production prefab.

A second menu item, **Validate Production Prefabs**, checks the seven prefab paths, required anchors, root `LODGroup` and its three LOD levels, and the non-static `DeliveryTrigger` anchor.

Because the supplied source pack contains one high-detail mesh per building type rather than authored LOD meshes, the generator initially uses the imported renderer set at all three LOD thresholds. This keeps the hierarchy contract and avoids duplicating mesh memory. Authored/decimated LOD1 and LOD2 meshes can be substituted later without changing the prefab contract.
