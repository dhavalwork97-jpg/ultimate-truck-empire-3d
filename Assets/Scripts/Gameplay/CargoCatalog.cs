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
        Lowboy = 5
    }

    [Serializable]
    public sealed class CargoDefinition
    {
        public string id;
        public string displayName;
        public TrailerType trailer;
        public float minWeightTons;
        public float maxWeightTons;
        public bool fragile;
        public bool temperatureSensitive;
        public bool highValue;

        public CargoDefinition(string id, string displayName, TrailerType trailer, float minWeightTons, float maxWeightTons,
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

    /// <summary>
    /// Small, deterministic cargo/trailer catalogue used by the freight market.
    /// It adds delivery choices without requiring a second progression or save system.
    /// </summary>
    public static class CargoCatalog
    {
        private static readonly CargoDefinition[] Definitions =
        {
            new CargoDefinition("electronics", "Electronics", TrailerType.Box, 6f, 18f, highValue: true),
            new CargoDefinition("refrigerated-food", "Refrigerated Food", TrailerType.Refrigerated, 8f, 24f, temperatureSensitive: true),
            new CargoDefinition("steel-coils", "Steel Coils", TrailerType.Flatbed, 12f, 30f),
            new CargoDefinition("furniture", "Furniture", TrailerType.Curtainsider, 6f, 20f, fragile: true),
            new CargoDefinition("machinery", "Industrial Machinery", TrailerType.Flatbed, 10f, 34f, highValue: true),
            new CargoDefinition("agricultural-goods", "Agricultural Goods", TrailerType.Curtainsider, 5f, 22f),
            new CargoDefinition("fuel-tank", "Fuel & Liquid Freight", TrailerType.Tanker, 14f, 32f, highValue: true),
            new CargoDefinition("heavy-equipment", "Heavy Equipment", TrailerType.Lowboy, 18f, 42f, highValue: true)
        };

        public static int Count => Definitions.Length;

        public static CargoDefinition Find(string id)
        {
            foreach (var definition in Definitions)
                if (string.Equals(definition.id, id, StringComparison.OrdinalIgnoreCase))
                    return definition;
            return null;
        }

        public static CargoDefinition Get(int index)
        {
            if (Definitions.Length == 0) return null;
            return Definitions[Mathf.Clamp(index, 0, Definitions.Length - 1)];
        }

        public static CargoDefinition PickFor(int index, int difficulty)
        {
            int offset = Mathf.Max(0, difficulty - 1) * 2;
            return Get((index + offset) % Definitions.Length);
        }
    }

    public static class ContractModifierRules
    {
        public enum Modifier
        {
            Standard = 0,
            Fragile = 1,
            Express = 2,
            HighValue = 3,
            TemperatureControlled = 4
        }

        public static Modifier GetModifier(CargoDefinition cargo, int difficulty, int seed)
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
