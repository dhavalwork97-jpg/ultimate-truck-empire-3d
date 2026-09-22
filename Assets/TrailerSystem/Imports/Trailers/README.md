# Trailer Asset Import Staging

Place **legally licensed** trailer model files in this folder before importing them into the game.

Supported model formats:
- FBX
- OBJ
- glTF
- GLB

## Pipeline

1. Put the source model and its licensed textures/material inputs under this staging area.
2. In Unity, select the imported model asset.
3. Run **Ultimate Truck Empire > Trailer System > Prepare Selected Imported Trailer**.
4. The tool creates a prefab under `Assets/TrailerSystem/Prefabs/Imported/`.
5. It creates/updates a `TrailerDefinition` and registers it in the Resources trailer catalog.
6. Replace the generated Kingpin/CargoSocket/wheel socket positions with authored production positions.
7. Replace the generated physics proxy with production collision geometry.
8. Author LOD0/LOD1/LOD2 and keep the trailer within its mobile triangle/material/texture budgets.
9. Run the Trailer System catalog, prefab, and physics validators before merging.

The generated collider and sockets are integration placeholders only. They are not shipping-quality authored physics.

Do not place assets here unless their license explicitly permits commercial use and redistribution in this game. Keep attribution/license files with the source asset when required.
