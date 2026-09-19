using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.Truck
{
    public enum TrailerType { DryVan, Refrigerated, Flatbed, Tanker, Container, HeavyHaul }

    public sealed class TrailerController : MonoBehaviour
    {
        public TrailerType Type { get; private set; } = TrailerType.DryVan;
        public UltimateTruckEmpire.Gameplay.TrailerType ContractTrailerType { get; private set; } = CargoCatalogTrailerType.Curtainsider;
        public float CargoWeightTons { get; private set; }
        public bool CargoLoaded { get; private set; }

        // Rear axle/docking reference for the current procedural trailer.
        public Transform DockingPoint { get; private set; }

        public void Configure(TrailerType type, float weightTons = 0f)
        {
            Type = type;
            ContractTrailerType = FromPhysicalType(type);
            CargoWeightTons = Mathf.Max(0f, weightTons);
            EnsureDockingPoint();
        }

        public void ConfigureGameplay(CargoCatalogTrailerType type, float weightTons = 0f)
        {
            ContractTrailerType = type;
            Type = ToPhysicalType(type);
            CargoWeightTons = Mathf.Max(0f, weightTons);
            EnsureDockingPoint();
        }

        public void Load(float weightTons) { CargoWeightTons = Mathf.Max(0f, weightTons); CargoLoaded = true; }
        public void Unload() { CargoWeightTons = 0f; CargoLoaded = false; }

        private void EnsureDockingPoint()
        {
            if (DockingPoint != null) return;
            var point = new GameObject("Trailer Docking Point");
            point.transform.SetParent(transform, false);
            point.transform.localPosition = new Vector3(0f, 1.35f, -4.55f);
            DockingPoint = point.transform;
        }

        public static TrailerType ToPhysicalType(CargoCatalogTrailerType type)
        {
            switch (type)
            {
                case CargoCatalogTrailerType.Refrigerated: return TrailerType.Refrigerated;
                case CargoCatalogTrailerType.Flatbed: return TrailerType.Flatbed;
                case CargoCatalogTrailerType.Tanker: return TrailerType.Tanker;
                case CargoCatalogTrailerType.Lowboy: return TrailerType.HeavyHaul;
                case CargoCatalogTrailerType.Box:
                case CargoCatalogTrailerType.Curtainsider:
                default: return TrailerType.DryVan;
            }
        }

        public static CargoCatalogTrailerType FromPhysicalType(TrailerType type)
        {
            switch (type)
            {
                case TrailerType.Refrigerated: return CargoCatalogTrailerType.Refrigerated;
                case TrailerType.Flatbed: return CargoCatalogTrailerType.Flatbed;
                case TrailerType.Tanker: return CargoCatalogTrailerType.Tanker;
                case TrailerType.HeavyHaul: return CargoCatalogTrailerType.Lowboy;
                case TrailerType.Container:
                case TrailerType.DryVan:
                default: return CargoCatalogTrailerType.Box;
            }
        }
    }

    // Alias keeps the public TrailerController API readable while explicitly
    // distinguishing the gameplay contract enum from the physical enum.
    public enum CargoCatalogTrailerType
    {
        Curtainsider = 0,
        Box = 1,
        Refrigerated = 2,
        Flatbed = 3,
        Tanker = 4,
        Lowboy = 5
    }
}
