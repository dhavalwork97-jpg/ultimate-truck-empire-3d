using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    [CreateAssetMenu(fileName = "TruckProductionProfile", menuName = "Ultimate Truck Empire/Truck/Production Profile")]
    public sealed class TruckProductionProfile : ScriptableObject
    {
        public string id = "truck-id";
        public string displayName = "New Truck";
        public string manufacturer = "UTE Motors";
        public GameObject prefab;
        public float emptyMassTons = 8f;
        public float fuelCapacityLitres = 500f;
        public float enginePowerHp = 320f;
        public float maxSpeedKph = 110f;
        public float fuelEfficiency = 7.2f;
        public string drivetrain = "6x4";
        public Transform couplingSocket;
        public Transform[] wheelSockets;
        public int lodCount = 1;
        public int maxTextureResolution = 1024;
        public int materialSlotBudget = 6;
    }
}
