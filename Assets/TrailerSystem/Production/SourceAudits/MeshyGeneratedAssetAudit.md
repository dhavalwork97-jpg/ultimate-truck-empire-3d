# Meshy-generated trailer/cargo integration audit

The supplied bundle contains six Meshy-generated FBX assets with 2048x2048 PBR texture sets.

## License

Meshy's current documentation states that Free-plan generated models are available under **CC BY 4.0** and may be used commercially with Meshy attribution. Meshy 6 Lite is listed as a generation model.

Required attribution:

> Model created with Meshy – CC BY 4.0 License

## Provenance condition

The Meshy-generated output can be used under the applicable license, but the generation source/reference material still matters. Do not treat a model as fully release-verified until we confirm that any uploaded reference images or other inputs were owned by the project or commercially usable.

## Asset mapping

| Meshy asset | Role | Target |
|---|---|---|
| Covered Cargo Trailer | Trailer | Dry Van |
| Gas Combustion Tanker 2427 | Trailer | Tanker candidate |
| Gas Combustion Tanker 3940 | Trailer | Tanker candidate |
| Tarp Covered Flatbed | Trailer | Flatbed |
| Timber Hauler | Trailer | Heavy Flatbed |
| Corrugated Steel Ware | Cargo | Steel / Construction cargo |

## Mobile production work

Each trailer still needs:
- LOD0 <= 15,000 triangles
- <= 3 material slots
- max texture 1024
- 3 LOD levels
- required sockets: Kingpin, CargoSocket, Wheel_FL, Wheel_FR, Wheel_RL, Wheel_RR
- existing TrailerPhysicsAttachment only
- LoadedTrailer + TrailerCargoModule + TrailerSkinApplier + LODGroup
- catalog/launch/physics/prefab validation

The supplied 2048 textures should be resized/baked to the project's 1024 mobile target.

## Remaining generation targets

Still needed: Refrigerated, Lowboy/RGN, Container Chassis, Grain Hopper.

A tanker should only be registered as CementTanker if its geometry is suitable for cement/bulk cargo; otherwise create/use an appropriate tanker category.
