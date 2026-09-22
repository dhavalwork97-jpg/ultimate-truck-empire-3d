# Phase B.8 Production Merge Review

## Safety-first integration decision

This branch is an integration/staging branch. Claude-generated duplicate runtime systems are **not** being blindly copied over the current Truck Empire architecture.

### Compared systems

| Candidate | Decision |
|---|---|
| FleetSystem.cs | Keep current FleetManager as the production base. Port only demonstrably useful missing features. |
| Runtime/TruckController.cs | Keep current Truck/TruckController.cs as the production base. Do not replace WheelCollider/mobile/trailer behavior with the simpler Rigidbody implementation. |
| GarageService.cs | Keep the existing garage/fleet architecture. Claude's thin wrapper is not a replacement. |
| RuntimeBootstrap.cs | Do not introduce a second initialization owner. Keep WorldBootstrap as the authoritative runtime bootstrap. |
| InputRuntime.cs | No replacement without a verified superior implementation and references. |
| ServiceLocator.cs | Do not introduce a parallel service-locator architecture without a concrete integration need. |

## Rules for the remaining B.8 work

1. Compare implementations before replacing any existing system.
2. Preserve current gameplay behavior and save compatibility.
3. Port features, not duplicate architectures.
4. Remove obsolete files only after all references are migrated.
5. Unity compilation must pass before merging to main.
6. Toll gameplay should integrate with existing finance, contracts, delivery, fleet, and save systems rather than creating parallel managers.
7. Art assets remain separate from gameplay-code integration and must be production-quality before inclusion.

## Current status

- Branch created from `main`.
- Existing core systems preserved.
- No destructive duplicate deletion performed.
- This branch is the staging point for verified B.8 integration.
