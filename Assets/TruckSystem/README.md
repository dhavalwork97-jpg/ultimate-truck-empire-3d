# Truck Production Intake

Batch 02 truck intake is prepared for the two future user-supplied assets.

Import models under Assets/TruckSystem/Imports/Trucks/Batch02/.

The existing gameplay architecture remains authoritative:
- TruckController: driving and wheel control
- FleetManager: ownership, persistence and active truck
- TruckDealer: dealership/economy
- PlayerTruckFleetBinding: runtime fleet binding
- TrailerController: trailer and cargo contract

The importer adds only the asset-side contract: Rigidbody, four WheelColliders, TrailerCoupling, TruckProductionProfile, mobile texture import settings and headless validation.

Generated wheel/coupling positions are integration defaults. Before shipping each truck, they must be replaced or calibrated against the actual model. Profile power, mass, fuel and drivetrain values are also game calibration values, not manufacturer specifications.

Headless entry point:
UltimateTruckEmpire.Truck.Editor.TruckAssetImportPipeline.PrepareAllImportedHeadless
