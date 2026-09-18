using System;
using UnityEngine;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.Gameplay
{
    public sealed class DeliveryManager : MonoBehaviour
    {
        public static DeliveryManager Instance { get; private set; }
        public bool ContractAccepted { get; private set; }
        public bool CargoLoaded { get; private set; }
        public string CargoName { get; private set; } = "Industrial Machinery";
        public float Reward { get; private set; } = 145000f;
        public int RewardXp { get; private set; } = 350;
        public string Pickup => "Ahmedabad Logistics Depot";
        public string Destination => "Vadodara Factory Warehouse";

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AcceptStarterContract()
        {
            if (ContractAccepted) return;
            ContractAccepted = true;
            CargoLoaded = false;
        }

        public void Restore(bool contractAccepted, bool cargoLoaded)
        {
            ContractAccepted = contractAccepted;
            CargoLoaded = contractAccepted && cargoLoaded;
        }

        public void LoadCargo()
        {
            if (ContractAccepted) CargoLoaded = true;
        }

        public void CompleteDelivery()
        {
            if (!ContractAccepted || !CargoLoaded) return;
            GameManager.Instance?.AddMoney(Reward);
            GameManager.Instance?.AddXp(RewardXp);
            ContractAccepted = false;
            CargoLoaded = false;
        }
    }
}
