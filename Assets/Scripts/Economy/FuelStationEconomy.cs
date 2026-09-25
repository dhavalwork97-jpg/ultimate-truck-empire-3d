using UnityEngine;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Economy
{
    [RequireComponent(typeof(Collider))]
    public sealed class FuelStationEconomy : MonoBehaviour
    {
        [SerializeField] private string region = "Ahmedabad";
        [SerializeField] private bool fullTankOnTrigger;
        [SerializeField] private float interactionRadius = 8f;

        public string Region => region;

        public void Configure(string stationRegion, bool fillFullTank = false)
        {
            region = string.IsNullOrWhiteSpace(stationRegion) ? "Ahmedabad" : stationRegion;
            fullTankOnTrigger = fillFullTank;
        }

        public float GetPricePerLitre()
            => FuelPriceManager.Instance != null ? FuelPriceManager.Instance.GetPricePerLitre(region) : EconomyConfig.FuelPricePerLitre;

        public bool TryPurchaseLitres(float litres)
        {
            var fleet = FleetManager.Instance;
            var truck = fleet?.EnsureActiveTruck();
            if (truck == null || litres <= 0f) return false;
            return fleet.RefuelAtRegion(truck.id, region, litres);
        }

        public bool TryFillTank()
        {
            var fleet = FleetManager.Instance;
            var truck = fleet?.EnsureActiveTruck();
            if (truck == null) return false;
            float litres = Mathf.Max(0f, truck.fuelCapacity - truck.fuel);
            return litres > 0f && fleet.RefuelAtRegion(truck.id, region, litres);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!fullTankOnTrigger) return;
            var truck = other.GetComponentInParent<UltimateTruckEmpire.Truck.TruckController>();
            if (truck == null) return;
            TryFillTank();
        }

        public float EstimateCost(float litres) => FuelEconomyService.CalculateCost(litres, GetPricePerLitre());
        public float InteractionRadius => Mathf.Max(1f, interactionRadius);
    }
}
