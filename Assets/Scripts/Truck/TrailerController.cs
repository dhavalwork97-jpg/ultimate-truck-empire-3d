using UnityEngine;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.TrailerSystem;

namespace UltimateTruckEmpire.Truck
{
    public enum TrailerType { DryVan, Refrigerated, Flatbed, HeavyFlatbed, Tanker, Container, HeavyHaul, GrainHopper, CementTanker, Dump, AgriculturalBulk }

    public sealed class TrailerController : MonoBehaviour
    {
        public TrailerType Type { get; private set; } = TrailerType.DryVan;
        public UltimateTruckEmpire.Gameplay.TrailerType ContractTrailerType { get; private set; } = UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider;
        public float CargoWeightTons { get; private set; }
        public bool CargoLoaded { get; private set; }
        public TrailerDefinition Definition { get; private set; }
        public CargoDefinition LoadedCargo { get; private set; }
        public Transform DockingPoint { get; private set; }

        public void SetAuthoredDockingPoint(Transform trailerRoot)
        {
            if (trailerRoot == null) { EnsureDockingPoint(); return; }
            Transform socket = trailerRoot.Find("Sockets/Kingpin") ?? trailerRoot.Find("Kingpin");
            if (socket == null)
            {
                foreach (var child in trailerRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child != trailerRoot && string.Equals(child.name, "Kingpin", System.StringComparison.OrdinalIgnoreCase))
                    {
                        socket = child;
                        break;
                    }
                }
            }
            DockingPoint = socket != null ? socket : trailerRoot;
        }

        public void Configure(TrailerType type, float weightTons = 0f)
        {
            Definition = null;
            Type = type;
            ContractTrailerType = FromPhysicalType(type);
            CargoWeightTons = Mathf.Max(0f, weightTons);
            CargoLoaded = CargoWeightTons > 0f;
            LoadedCargo = null;
            EnsureDockingPoint();
            ApplyPresentation();
        }

        public void ConfigureGameplay(UltimateTruckEmpire.Gameplay.TrailerType type, float weightTons = 0f)
        {
            Definition = null;
            ContractTrailerType = type;
            Type = ToPhysicalType(type);
            CargoWeightTons = Mathf.Max(0f, weightTons);
            CargoLoaded = CargoWeightTons > 0f;
            LoadedCargo = null;
            EnsureDockingPoint();
            ApplyPresentation();
        }

        public void Load(float weightTons) { CargoWeightTons = Mathf.Max(0f, weightTons); CargoLoaded = true; }
        public void Unload()
        {
            var loadedTrailer = GetComponentInChildren<LoadedTrailer>(true);
            if (loadedTrailer != null) loadedTrailer.Unload();
            CargoWeightTons = 0f;
            CargoLoaded = false;
            LoadedCargo = null;
        }

        /// <summary>Configures this physical trailer from the ScriptableObject data model.</summary>
        public bool ConfigureDefinition(TrailerDefinition definition, CargoDefinition cargo = null, float weightTons = 0f, TrailerSkinDefinition skin = null)
        {
            if (definition == null) return false;

            float resolvedWeight = 0f;
            if (cargo != null)
            {
                if (!TrailerCompatibility.CanLoad(definition, cargo, weightTons)) return false;
                resolvedWeight = TrailerCompatibility.GetAllowedWeight(definition, cargo, weightTons);
            }

            var loadedTrailer = GetComponentInChildren<LoadedTrailer>(true);
            if (loadedTrailer == null)
            {
                loadedTrailer = gameObject.AddComponent<LoadedTrailer>();
            }

            // Validate and apply the data-driven visual/runtime state before mutating
            // the controller's public state. A rejected cargo load therefore cannot
            // leave Definition/CargoLoaded pointing at a configuration that failed.
            bool configured = cargo != null
                ? loadedTrailer.Configure(definition, cargo, resolvedWeight, skin)
                : loadedTrailer.ConfigureEmpty(definition, skin);
            if (!configured) return false;

            Definition = definition;
            LoadedCargo = cargo;
            CargoWeightTons = resolvedWeight;
            CargoLoaded = cargo != null;
            var skinApplier = GetComponent<TrailerSkinApplier>() ?? gameObject.AddComponent<TrailerSkinApplier>();
            skinApplier.Initialize(definition, skin);
            ConfigureGameplay(FromDefinitionType(definition.category), CargoWeightTons);
            Definition = definition; // ConfigureGameplay clears legacy definition state.
            return true;
        }

        private static UltimateTruckEmpire.Gameplay.TrailerType FromDefinitionType(TrailerCategory category)
        {
            switch (category)
            {
                case TrailerCategory.Refrigerated: return TrailerTypeForGameplay.Refrigerated;
                case TrailerCategory.Flatbed: return TrailerTypeForGameplay.Flatbed;
                case TrailerCategory.HeavyFlatbed: return TrailerTypeForGameplay.HeavyFlatbed;
                case TrailerCategory.Lowboy: return TrailerTypeForGameplay.Lowboy;
                case TrailerCategory.ContainerChassis: return TrailerTypeForGameplay.Container;
                case TrailerCategory.GrainHopper: return TrailerTypeForGameplay.GrainHopper;
                case TrailerCategory.CementTanker: return TrailerTypeForGameplay.CementTanker;
                case TrailerCategory.DumpTrailer: return TrailerTypeForGameplay.Dump;
                case TrailerCategory.AgriculturalBulk: return TrailerTypeForGameplay.AgriculturalBulk;
                default: return TrailerTypeForGameplay.Box;
            }
        }

        private static class TrailerTypeForGameplay
        {
            public const UltimateTruckEmpire.Gameplay.TrailerType Refrigerated = UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated;
            public const UltimateTruckEmpire.Gameplay.TrailerType Flatbed = UltimateTruckEmpire.Gameplay.TrailerType.Flatbed;
            public const UltimateTruckEmpire.Gameplay.TrailerType HeavyFlatbed = UltimateTruckEmpire.Gameplay.TrailerType.HeavyFlatbed;
            public const UltimateTruckEmpire.Gameplay.TrailerType Lowboy = UltimateTruckEmpire.Gameplay.TrailerType.Lowboy;
            public const UltimateTruckEmpire.Gameplay.TrailerType Container = UltimateTruckEmpire.Gameplay.TrailerType.Container;
            public const UltimateTruckEmpire.Gameplay.TrailerType GrainHopper = UltimateTruckEmpire.Gameplay.TrailerType.GrainHopper;
            public const UltimateTruckEmpire.Gameplay.TrailerType CementTanker = UltimateTruckEmpire.Gameplay.TrailerType.CementTanker;
            public const UltimateTruckEmpire.Gameplay.TrailerType Dump = UltimateTruckEmpire.Gameplay.TrailerType.Dump;
            public const UltimateTruckEmpire.Gameplay.TrailerType AgriculturalBulk = UltimateTruckEmpire.Gameplay.TrailerType.AgriculturalBulk;
            public const UltimateTruckEmpire.Gameplay.TrailerType Box = UltimateTruckEmpire.Gameplay.TrailerType.Box;
        }

        private void ApplyPresentation()
        {
            if (Definition != null) return;
            Transform visual = transform.Find("Dry Van Trailer") ?? transform.Find("Trailer Visual");
            if (visual == null)
            {
                foreach (var child in GetComponentsInChildren<Transform>())
                    if (child != transform && child.name.EndsWith(" Trailer")) { visual = child; break; }
            }
            if (visual == null) return;
            visual.name = Type.ToString() + " Trailer";
            switch (Type)
            {
                case TrailerType.Refrigerated: visual.localScale = new Vector3(2.75f, 2.9f, 4.4f); break;
                case TrailerType.Flatbed: visual.localScale = new Vector3(2.9f, 0.85f, 4.8f); break;
                case TrailerType.Tanker: visual.localScale = new Vector3(2.65f, 2.3f, 4.5f); break;
                case TrailerType.HeavyHaul: visual.localScale = new Vector3(3.0f, 1.15f, 5.2f); break;
                default: visual.localScale = new Vector3(2.75f, 2.8f, 4.2f); break;
            }
        }

        private void EnsureDockingPoint()
        {
            if (DockingPoint != null) return;
            var point = new GameObject("Trailer Docking Point");
            point.transform.SetParent(transform, false);
            point.transform.localPosition = new Vector3(0f, 1.35f, -4.55f);
            DockingPoint = point.transform;
        }

        public static TrailerType ToPhysicalType(UltimateTruckEmpire.Gameplay.TrailerType type)
        {
            switch (type)
            {
                case UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated: return TrailerType.Refrigerated;
                case UltimateTruckEmpire.Gameplay.TrailerType.Flatbed: return TrailerType.Flatbed;
                case UltimateTruckEmpire.Gameplay.TrailerType.Tanker: return TrailerType.Tanker;
                case UltimateTruckEmpire.Gameplay.TrailerType.Lowboy: return TrailerType.HeavyHaul;
                case UltimateTruckEmpire.Gameplay.TrailerType.HeavyFlatbed: return TrailerType.HeavyFlatbed;
                case UltimateTruckEmpire.Gameplay.TrailerType.Container: return TrailerType.Container;
                case UltimateTruckEmpire.Gameplay.TrailerType.GrainHopper: return TrailerType.GrainHopper;
                case UltimateTruckEmpire.Gameplay.TrailerType.CementTanker: return TrailerType.CementTanker;
                case UltimateTruckEmpire.Gameplay.TrailerType.Dump: return TrailerType.Dump;
                case UltimateTruckEmpire.Gameplay.TrailerType.AgriculturalBulk: return TrailerType.AgriculturalBulk;
                case UltimateTruckEmpire.Gameplay.TrailerType.Box:
                case UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider:
                default: return TrailerType.DryVan;
            }
        }

        public static UltimateTruckEmpire.Gameplay.TrailerType FromPhysicalType(TrailerType type)
        {
            switch (type)
            {
                case TrailerType.Refrigerated: return UltimateTruckEmpire.Gameplay.TrailerType.Refrigerated;
                case TrailerType.Flatbed: return UltimateTruckEmpire.Gameplay.TrailerType.Flatbed;
                case TrailerType.Tanker: return UltimateTruckEmpire.Gameplay.TrailerType.Tanker;
                case TrailerType.HeavyHaul: return UltimateTruckEmpire.Gameplay.TrailerType.Lowboy;
                case TrailerType.HeavyFlatbed: return UltimateTruckEmpire.Gameplay.TrailerType.HeavyFlatbed;
                case TrailerType.Container: return UltimateTruckEmpire.Gameplay.TrailerType.Container;
                case TrailerType.GrainHopper: return UltimateTruckEmpire.Gameplay.TrailerType.GrainHopper;
                case TrailerType.CementTanker: return UltimateTruckEmpire.Gameplay.TrailerType.CementTanker;
                case TrailerType.Dump: return UltimateTruckEmpire.Gameplay.TrailerType.Dump;
                case TrailerType.AgriculturalBulk: return UltimateTruckEmpire.Gameplay.TrailerType.AgriculturalBulk;
                case TrailerType.DryVan:
                default: return UltimateTruckEmpire.Gameplay.TrailerType.Box;
            }
        }
    }
}
