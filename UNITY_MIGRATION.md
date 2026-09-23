# Ultimate Truck Empire — Unity Production Structure

## Canonical project

The **repository root** is the production Unity project.

It contains the required Unity project structure:

- Assets/
- Packages/
- ProjectSettings/

Unity version:

- **6000.0.65f1**

GitHub Actions validates this root project with Unity 6000.0.65f1.

## Removed duplicate

The former UnityProject/ directory was a second, incomplete Unity implementation. It did not contain its own Packages/ or ProjectSettings/ and duplicated older truck, garage, mobile-input, and world-builder code.

It has been removed from the production branch so developers cannot accidentally open or modify the wrong implementation.

Useful concepts from that prototype should be reimplemented against the canonical root architecture rather than copied back wholesale.

## Browser prototype

The Vite/Three.js files under src/, public/, and the root package.json remain as a browser/reference prototype. They are intentionally separate from the Unity production renderer.

## Production rule

All future Unity work must be made under the repository root:

    Assets/
    Packages/
    ProjectSettings/

Do not create another nested Unity project.
