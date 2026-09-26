using System;
using System.Collections.Generic;

namespace UltimateTruckEmpire.FreightLocations
{
    /// <summary>
    /// Data-only catalog for reusable factory/warehouse locations.
    /// Prefabs are referenced by Resources path so adding a new visual asset
    /// never requires changing the freight economy or route systems.
    /// </summary>
    public static class FreightLocationRegistry
    {
        private static readonly FreightLocationDefinition[] definitions =
        {
            new FreightLocationDefinition("AMD-TEXTILE-01", "Ahmedabad", "Gujarat Textiles Factory", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Textile", 3, "FABRIC_ROLLS", "COTTON", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("AMD-WH-01", "Ahmedabad", "Western Logistics Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Large", 4, "FABRIC_ROLLS", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("VAD-CHEM-01", "Vadodara", "IndChem Manufacturing Plant", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Chemical", 3, "CHEMICALS", "STEEL", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("VAD-WH-01", "Vadodara", "Chemical Distribution Depot", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Large", 4, "CHEMICALS", "PALLETS", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("SUR-FAB-01", "Surat", "Surat Fabric Works", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Textile", 3, "FABRIC_ROLLS", "COTTON", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("SUR-WH-01", "Surat", "Surat Export Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Large", 4, "FABRIC_ROLLS", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("RJK-ENG-01", "Rajkot", "Rajkot Engineering Works", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Base", 2, "STEEL", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("RJK-WH-01", "Rajkot", "Rajkot Machinery Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Small", 2, "STEEL", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("KDL-PORT-01", "Kandla", "Kandla Port Logistics", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Port", 6, "CONTAINERS", "STEEL", "MACHINERY"),
            new FreightLocationDefinition("KDL-PORT-02", "Kandla", "Kandla Container Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Port", 6, "CONTAINERS", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("MUM-IMP-01", "Mumbai", "Mumbai Import Cargo Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Port", 6, "CONTAINERS", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("MUM-IND-01", "Mumbai", "Mumbai Industrial Works", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Base", 3, "STEEL", "MACHINERY", "CHEMICALS"),
            new FreightLocationDefinition("PUN-AUTO-01", "Pune", "Pune Automotive Manufacturing", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Automotive", 4, "AUTO_PARTS", "STEEL", "MACHINERY"),
            new FreightLocationDefinition("PUN-WH-01", "Pune", "Pune Auto Parts Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Large", 4, "AUTO_PARTS", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("JAI-WH-01", "Jaipur", "Jaipur Stone Distribution Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Small", 2, "STONE", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("JAI-FAC-01", "Jaipur", "Jaipur Stone Works", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Base", 2, "STONE", "MACHINERY"),
            new FreightLocationDefinition("DEL-WH-01", "Delhi", "Delhi Retail Fulfillment Center", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Large", 4, "PALLETS", "GENERAL_FREIGHT", "AUTO_PARTS"),
            new FreightLocationDefinition("DEL-FAC-01", "Delhi", "Delhi Consumer Goods Factory", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Base", 3, "PALLETS", "MACHINERY", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("IND-FOOD-01", "Indore", "Indore Food Processing Factory", FreightLocationKind.Factory, "FreightLocations/Prefabs/Factory_Food", 3, "FOOD", "PALLETS", "GENERAL_FREIGHT"),
            new FreightLocationDefinition("IND-WH-01", "Indore", "Indore Grain Warehouse", FreightLocationKind.Warehouse, "FreightLocations/Prefabs/Warehouse_Large", 4, "FOOD", "PALLETS", "GENERAL_FREIGHT")
        };

        public static IReadOnlyList<FreightLocationDefinition> All => definitions;

        public static FreightLocationDefinition Find(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            for (int i = 0; i < definitions.Length; i++)
                if (string.Equals(definitions[i].id, id, StringComparison.OrdinalIgnoreCase))
                    return definitions[i];
            return null;
        }

        public static IReadOnlyList<FreightLocationDefinition> ForCity(string city)
        {
            var result = new List<FreightLocationDefinition>();
            if (string.IsNullOrWhiteSpace(city)) return result;
            for (int i = 0; i < definitions.Length; i++)
                if (string.Equals(definitions[i].city, city, StringComparison.OrdinalIgnoreCase))
                    result.Add(definitions[i]);
            return result;
        }
    }
}
