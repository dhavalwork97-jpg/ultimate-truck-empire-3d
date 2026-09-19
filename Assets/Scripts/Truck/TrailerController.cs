using UnityEngine;
using UltimateTruckEmpire.Gameplay;

namespace UltimateTruckEmpire.Truck
{
    public enum TrailerType { DryVan, Refrigerated, Flatbed, Tanker, Container, HeavyHaul }

    public sealed class TrailerController : MonoBehaviour
    {
        public TrailerType Type { get; private set; } = TrailerType.DryVan;
        public UltimateTruckEmpire.Gameplay.TrailerType ContractTrailerType { get; private set; } = UltimateTruckEmpire.Gameplay.TrailerType.Curtainsider;
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
            ApplyPresentation();
        }

        public void ConfigureGameplay(UltimateTruckEmpire.Gameplay.TrailerType type, float weightTons = 0f)
        {
            ContractTrailerType = type;
            Type = ToPhysicalType(type);
            CargoWeightTons = Mathf.Max(0f, weightTons);
            EnsureDockingPoint();
            ApplyPresentation();
        }

        public void Load(float weightTons) { CargoWeightTons = Mathf.Max(0f, weightTons); CargoLoaded = true; }
        public void Unload() { CargoWeightTons = 0f; CargoLoaded = false; }

        private void ApplyPresentation()
        {
            Transform visual = transform.Find("Dry Van Trailer");
            if (visual == null) visual = transform.Find("Trailer Visual");
            if (visual == null)
            {
                foreach (var child in GetComponentsInChildren<Transform>())
                {
                    if (child != transform && child.name.EndsWith(" Trailer"))
                    {
                        visual = child;
                        break;
                    }
                }
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
                case TrailerType.Container:
                case TrailerType.DryVan:
                default: return UltimateTruckEmpire.Gameplay.TrailerType.Box;
            }
        }
    }

}
