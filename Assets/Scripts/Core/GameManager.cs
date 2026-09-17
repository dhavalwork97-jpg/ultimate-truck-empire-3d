using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateTruckEmpire.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        [SerializeField] private string worldScene = "World_Gujarat";
        public float Money { get; private set; } = 350000f;
        public int PlayerXp { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddMoney(float amount) => Money = Mathf.Max(0f, Money + amount);
        public bool TrySpendMoney(float amount)
        {
            if (amount < 0f || Money < amount) return false;
            Money -= amount;
            return true;
        }
        public void AddXp(int amount) => PlayerXp = Mathf.Max(0, PlayerXp + amount);
        public void Restore(float money, int xp) { Money = Mathf.Max(0f, money); PlayerXp = Mathf.Max(0, xp); }
        public void LoadWorld() { if (!string.IsNullOrWhiteSpace(worldScene)) SceneManager.LoadScene(worldScene); }
    }
}
