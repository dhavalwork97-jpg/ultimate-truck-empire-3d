using System;
using UnityEngine;

namespace UltimateTruckEmpire
{
    [Serializable]
    public sealed class FreightContract
    {
        public string id;
        public string title;
        public string origin;
        public string destination;
        public int payout;
        public float cargoMass;

        public FreightContract(string id, string title, string origin, string destination, int payout, float cargoMass)
        {
            this.id = id;
            this.title = title;
            this.origin = origin;
            this.destination = destination;
            this.payout = payout;
            this.cargoMass = cargoMass;
        }
    }

    public sealed class ContractSystem : MonoBehaviour
    {
        public FreightContract ActiveContract { get; private set; }
        public bool HasActiveContract => ActiveContract != null;

        public event Action ContractChanged;
        public event Action<int> ContractCompleted;

        private void Awake()
        {
            CreateStarterContract();
        }

        public void Accept(FreightContract contract)
        {
            if (contract == null || HasActiveContract) return;
            ActiveContract = contract;
            ContractChanged?.Invoke();
        }

        public void Complete()
        {
            if (!HasActiveContract) return;

            int payout = ActiveContract.payout;
            ActiveContract = null;
            ContractCompleted?.Invoke(payout);
            ContractChanged?.Invoke();
        }

        public FreightContract[] GetAvailableContracts()
        {
            return new[]
            {
                new FreightContract("retail_restock", "Retail Restock", "Metro", "North", 1450, 9000f),
                new FreightContract("steel_run", "Steel Run", "Port", "Iron District", 2200, 14500f),
                new FreightContract("cold_chain", "Cold Chain", "Fresh Market", "Airport", 3150, 7000f),
                new FreightContract("express_freight", "Express Freight", "Metro", "Airport", 4100, 11000f)
            };
        }

        private void CreateStarterContract()
        {
            var contracts = GetAvailableContracts();
            if (contracts.Length > 0)
                Accept(contracts[0]);
        }
    }
}
