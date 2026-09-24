# Master World Map

This document is the authoritative layout blueprint for the planned Ultimate Truck Empire 3D world.

## Scope

- 18 km x 18 km target footprint
- 8 x 8 streaming grid
- 64 chunks, 2.25 km per chunk
- 6 cities
- 12 villages
- 4 national highway corridors
- Approximately 370 km target drivable network

## Asset policy

City art is intentionally kept minimal. The project should use small reusable city modules and freely available premade assets only after checking their licenses. Do not make a large monolithic city base-map asset a dependency.

Priority environment art is rural and road infrastructure: farmland, villages, country roads, highways, bridges and truck stops.

## Implementation rule

This is a world design specification, not a claim that the complete visual world or 370 km road network already exists. Runtime integration must reuse the project's existing road, GPS, traffic, facility and streaming architecture rather than introducing duplicate systems.

The JSON definition at `Assets/WorldSystem/Data/MasterWorldDefinition.json` is the machine-readable companion specification.
