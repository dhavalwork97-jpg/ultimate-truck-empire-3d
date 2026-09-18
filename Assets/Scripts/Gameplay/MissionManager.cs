using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Gameplay
{
    public sealed class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }
        public int CompletedMissions { get; private set; }
        public string CurrentMission { get; private set; } = "Complete your first delivery";
        public bool FirstDeliveryComplete { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }

        public void NotifyDeliveryComplete()
        {
            if (FirstDeliveryComplete) return;
            FirstDeliveryComplete = true; CompletedMissions++;
            CurrentMission = "Build your fleet: own 2 trucks";
            GameManager.Instance?.AddXp(250);
            CompanyManager.Instance?.AddRevenue(0f);
        }
    }
}
