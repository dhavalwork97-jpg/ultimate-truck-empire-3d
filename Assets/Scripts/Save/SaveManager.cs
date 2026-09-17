using System.IO;
using UnityEngine;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.Save
{
    public sealed class SaveManager : MonoBehaviour
    {
        private const string FileName = "ute_save.json";

        [System.Serializable]
        private sealed class SaveData { public float money; public int xp; }

        public void Save()
        {
            var game = GameManager.Instance;
            if (game == null) return;
            var data = new SaveData { money = game.Money, xp = game.PlayerXp };
            File.WriteAllText(Path.Combine(Application.persistentDataPath, FileName), JsonUtility.ToJson(data, true));
        }

        public void Load()
        {
            var game = GameManager.Instance;
            string path = Path.Combine(Application.persistentDataPath, FileName);
            if (game == null || !File.Exists(path)) return;
            var data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
            if (data != null) game.Restore(data.money, data.xp);
        }

        private void OnApplicationPause(bool pause) { if (pause) Save(); }
        private void OnApplicationQuit() => Save();
    }
}
