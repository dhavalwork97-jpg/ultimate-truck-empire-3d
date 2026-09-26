# Freight Location Pack 01

This pack turns the warehouse and factory source models into reusable logistics locations.

## Source assets

The supplied ZIPs are **raw 3D assets**, not Unity prefabs. They must be imported through the Unity Editor and converted into production prefabs under:

`Assets/Resources/FreightLocations/Prefabs/`

Expected production prefab resource names:

- `Warehouse_Small.prefab`
- `Warehouse_Large.prefab`
- `Warehouse_Port.prefab`
- `Factory_Base.prefab`
- `Factory_Textile.prefab`
- `Factory_Chemical.prefab`
- `Factory_Automotive.prefab`
- `Factory_Food.prefab`

The runtime catalog in `FreightLocationRegistry` deliberately references these prefab paths without embedding model-specific logic into the freight economy.

## Gameplay contract

Location prefabs are visual/logistics infrastructure only. They must not replace the authoritative road network, city delivery triggers, trailer system, economy, or save system.

When production prefabs are created, use these optional child anchors where appropriate:

- `LoadingDock_A`
- `LoadingDock_B`
- `TrailerSpawn`
- `CargoSpawn`
- `ParkingSlot_01`
- `CompanySign`

Existing `DeliveryTrigger` instances remain authoritative for freight completion.

## Current catalog

20 reusable locations cover all 10 existing freight cities:

Ahmedabad, Vadodara, Surat, Rajkot, Kandla, Mumbai, Pune, Jaipur, Delhi, and Indore.

The same warehouse/factory source models are intentionally reused through different prefab resource paths and location definitions.

## Important validation rule

Do not claim the raw ZIPs are Unity-ready. A Unity Editor import step is still required to produce the actual `.prefab`, `.mat`, and `.meta` files. The code in this branch is safe without those assets: missing production prefabs produce a warning rather than breaking compilation.
