using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    public enum TrailerType { DryVan, Refrigerated, Flatbed, Tanker, Container, HeavyHaul }

    public sealed class TrailerController : MonoBehaviour
    {
        public TrailerType Type { get; private set; } = TrailerType.DryVan;
        public float CargoWeightTons { get; private set; }
        public bool CargoLoaded { get; private set; }

        public void Configure(TrailerType type, float weightTons = 0f) { Type = type; CargoWeightTons = Mathf.Max(0f, weightTons); }
        public void Load(float weightTons) { CargoWeightTons = Mathf.Max(0f, weightTons); CargoLoaded = true; }
        public void Unload() { CargoWeightTons = 0f; CargoLoaded = false; }
    }
}
