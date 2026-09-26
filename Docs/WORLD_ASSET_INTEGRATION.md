# Versatile Studio + Tenkoku World Integration

The production world now uses the Versatile Studio Demo City asset family as the visual environment when the imported prefabs have been prepared. The existing RoadNetwork/RoadBuilder remains authoritative for drivable roads, gameplay triggers and collision.

## One-time Unity setup

1. Import Demo City By Versatile Studio from the Unity Asset Store.
2. Import TENKOKU Dynamic Sky from the Unity Asset Store.
3. Open the project in Unity 6000.0.65f1.
4. Run: Ultimate Truck Empire > World > Prepare Versatile Studio + Tenkoku Assets
5. Open Assets/Scenes/World_Gujarat.unity and press Play.

The editor setup searches the imported Asset Store folders by prefab filename, so package folder locations do not need to be hard-coded.

## Runtime architecture

- VersatileStudioWorldBuilder loads the prepared Demo City, buildings, houses, trees and street lamps.
- Imported asset colliders are stripped from runtime instances so the art pack cannot replace gameplay collision.
- When the prepared Versatile Studio assets are missing, the existing procedural world remains the fallback.
- TenkokuWorldBridge detects Tenkoku DynamicSky without taking a compile-time dependency on the Asset Store package.
- Tenkoku receives Ahmedabad latitude/longitude, game time and the project's weather state.
- Tenkoku owns sky/sun/weather on desktop and console.
- Mobile builds intentionally keep the existing lightweight atmosphere because the Tenkoku publisher documents the asset as desktop/console and not mobile.
- The project's existing TimeWeatherManager remains the single gameplay-facing time/weather source.

## Important

The Asset Store packages themselves are not redistributed in this repository. They must be imported through the user's Unity Asset Store entitlement before running the preparation menu.

Tenkoku's official documentation recommends using its own built-in lighting and avoiding additional directional lights; the integration therefore skips creation of the project's fallback directional sun whenever Tenkoku is active.