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

Required anchors are:

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

20 reusable locations cover all 10 existing freight cities.

The same warehouse/factory source models are intentionally reused through different location definitions.


## Company data
Example company definitions are stored under `Assets/FreightLocations/Data/Companies/` and reference the reusable production prefabs.
