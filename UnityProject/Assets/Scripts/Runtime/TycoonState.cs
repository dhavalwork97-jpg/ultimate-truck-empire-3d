using System;
using UnityEngine;

namespace UltimateTruckEmpire
{
    [Serializable]
    public sealed class TycoonState
    {
        public int cash = 25000;
        public int level = 1;
        public int deliveries;
        public float fuel = 100f;
        public string selectedTruckId = "starter_truck";
    }

    public sealed class TycoonStateService : MonoBehaviour
    {
        public TycoonState State { get; private set; } = new TycoonState();

        private const string SaveKey = "ultimate_truck_empire_save_v1";

        private void Awake() => Load();

        public void AddCash(int amount)
        {
            State.cash = Mathf.Max(0, State.cash + amount);
            State.deliveries++;
            State.level = 1 + State.deliveries / 5;
            Save();
        }

        public void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(State));
            PlayerPrefs.Save();
        }

        public void Load()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            State = JsonUtility.FromJson<TycoonState>(PlayerPrefs.GetString(SaveKey)) ?? new TycoonState();
        }
    }
}
