using System;
using UnityEngine;

namespace UltimateTruckEmpire.Gameplay
{
    public enum TrailerType
    {
        Curtainsider = 0,
        Box = 1,
        Refrigerated = 2,
        Flatbed = 3,
        Tanker = 4,
        Lowboy = 5,
        Container = 6,
        GrainHopper = 7,
        CementTanker = 8,
        Dump = 9,
        AgriculturalBulk = 10,
        HeavyFlatbed = 11
    }

    // Legacy runtime contract record. TrailerSystem.CargoDefinition is the new
    // authoring asset used for reusable cargo data and trailer compatibility.
    [Serializable]
    public sealed class ContractCargoDefinition
    {
        public string id;
        public string displayName;
        public TrailerType trailer;
        public float minWeightTons;
        public float maxWeightTons;
        public bool fragile;
        public bool temperatureSensitive;
        public bool highValue;

        public ContractCargoDefinition(string id, string displayName, TrailerType trailer, float minWeightTons, float maxWeightTons,
            bool fragile = false, bool temperatureSensitive = false, bool highValue = false)
        {
            this.id = id;
            this.displayName = displayName;
            this.trailer = trailer;
            this.minWeightTons = minWeightTons;
            this.maxWeightTons = maxWeightTons;
            this.fragile = fragile;
            this.temperatureSensitive = temperatureSensitive;
            this.highValue = highValue;
        }
    }

    public static class CargoCatalog
    {
        private static readonly ContractCargoDefinition[] Definitions =
        {
            new ContractCargoDefinition("electronics", "Electronics", TrailerType.Box, 6f, 18f, highValue: true),
            new ContractCargoDefinition("refrigerated-food", "Refrigerated Food", TrailerType.Refrigerated, 8f, 24f, temperatureSensitive: true),
            new ContractCargoDefinition("steel-coils", "Steel Coils", TrailerType.Flatbed, 12f, 30f),
            new ContractCargoDefinition("furniture", "Furniture", TrailerType.Curtainsider, 6f, 20f, fragile: true),
            new ContractCargoDefinition("machinery", "Industrial Machinery", TrailerType.Flatbed, 10f, 34f, highValue: true),
            new ContractCargoDefinition("agricultural-goods", "Agricultural Goods", TrailerType.Curtainsider, 5f, 22f),
            new ContractCargoDefinition("fuel-tank", "Fuel & Liquid Freight", TrailerType.Tanker, 14f, 32f, highValue: true),
            new ContractCargoDefinition("heavy-equipment", "Heavy Equipment", TrailerType.Lowboy, 18f, 42f, highValue: true)
        };

        public static int Count => Definitions.Length;

        public static ContractCargoDefinition Find(string id)
        {
            foreach (var definition in Definitions)
                if (string.Equals(definition.id, id, StringComparison.OrdinalIgnoreCase)) return definition;
            return null;
        }

        public static ContractCargoDefinition Get(int index)
        {
            if (Definitions.Length == 0) return null;
            return Definitions[Mathf.Clamp(index, 0, Definitions.Length - 1)];
        }

        public static ContractCargoDefinition PickFor(int index, int difficulty)
        {
            int offset = Mathf.Max(0, difficulty - 1) * 2;
            return Get((index + offset) % Definitions.Length);
        }
    }

    public static class ContractModifierRules
    {
        public enum Modifier { Standard = 0, Fragile = 1, Express = 2, HighValue = 3, TemperatureControlled = 4 }

        public static Modifier GetModifier(ContractCargoDefinition cargo, int difficulty, int seed)
        {
            if (cargo == null) return Modifier.Standard;
            if (cargo.temperatureSensitive) return Modifier.TemperatureControlled;
            if (cargo.fragile) return Modifier.Fragile;
            if (cargo.highValue) return Modifier.HighValue;
            return difficulty >= 3 && seed % 3 == 0 ? Modifier.Express : Modifier.Standard;
        }

        public static string GetLabel(Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.Fragile: return "FRAGILE";
                case Modifier.Express: return "EXPRESS";
                case Modifier.HighValue: return "HIGH VALUE";
                case Modifier.TemperatureControlled: return "TEMP CONTROLLED";
                default: return "STANDARD";
            }
        }

        public static float GetBonusMultiplier(Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.Fragile: return 0.08f;
                case Modifier.Express: return 0.12f;
                case Modifier.HighValue: return 0.10f;
                case Modifier.TemperatureControlled: return 0.11f;
                default: return 0f;
            }
        }

        public static float GetPenaltyMultiplier(Modifier modifier)
        {
            switch (modifier)
            {
                case Modifier.Fragile: return 0.10f;
                case Modifier.Express: return 0.14f;
                case Modifier.HighValue: return 0.12f;
                case Modifier.TemperatureControlled: return 0.15f;
                default: return 0f;
            }
        }
    }
}
