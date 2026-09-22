using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;
using UltimateTruckEmpire.Gameplay;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.Save;

namespace UltimateTruckEmpire.Gameplay.Toll
{
    public enum TollPaymentMethod { FastTag, Cash }
    public enum TollTruckClass { Light, Medium, Heavy, ExtraHeavy }
    public enum TollCrossingState { Detected, PaymentPending, Paid, Failed }
    
    [Serializable] public sealed class TollPlazaDefinition
    {
        public string id, displayName, roadId, region = "Ahmedabad";
        public float distanceKm = 14f, baseToll = 220f;
        public bool active = true, fastTag = true, cash = true;
    }
    
    [Serializable] public sealed class TollPaymentRecord
    {
        public string transactionId, plazaId, vehicleKey, method;
        public float amount, time;
    }
    
    [Serializable] public sealed class TollSaveState
    {
        public float fastTagBalance = 1500f;
        public int nextTransaction = 1;
        public TollPaymentRecord[] payments;
    }

    public sealed class FastTagWallet : MonoBehaviour
    {
        public static FastTagWallet Instance { get; private set; }
        [SerializeField] private float balance = 1500f;
        public float Balance => balance;
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
        }
        public bool TryPay(float amount) { if (amount <= 0f) return true; if (balance < amount) return false; balance -= amount; return true; }
        public void Add(float amount) { balance += Mathf.Max(0f, amount); }
        public void Restore(float value) { balance = Mathf.Max(0f, value); }
    }

    public static class TollPricingEngine
    {
        public static float Calculate(float baseToll, TruckController truck, TrailerType trailer, TollPaymentMethod method)
        {
            float truckFactor = 1f;
            if (truck != null)
            {
                var fleet = FleetManager.Instance?.ActiveTruck;
                float tons = fleet != null ? fleet.capacityTons : 18f;
                truckFactor = tons >= 30f ? 1.45f : tons >= 20f ? 1.15f : tons <= 10f ? .65f : 1f;
            }
            float trailerFactor = trailer == TrailerType.Tanker ? 1.25f :
                                  trailer == TrailerType.Flatbed ? 1.15f :
                                  trailer == TrailerType.Refrigerated ? 1.20f : 1.10f;
            float paymentFactor = method == TollPaymentMethod.FastTag ? .90f : 1f;
            return Mathf.Max(10f, Mathf.Round(baseToll * truckFactor * trailerFactor * paymentFactor));
        }
    }

    public sealed class TollPlazaManager : MonoBehaviour
    {
        public static TollPlazaManager Instance { get; private set; }
        public event Action<TollPlazaDefinition> ApproachChanged;
        public event Action<TollPaymentRecord> PaymentSucceeded;
        public event Action<string> PaymentFailed;
        public IReadOnlyList<TollPaymentRecord> RecentPayments => payments;
        public float TotalTollSpend => FinanceManager.Instance?.Data.tollExpense ?? 0f;
        private readonly List<TollPaymentRecord> payments = new List<TollPaymentRecord>();
        private readonly Dictionary<string, float> paidKeys = new Dictionary<string, float>();
        private int nextTransaction = 1;

        public TollPlazaDefinition[] Plazas { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            Plazas = new[]
            {
                new TollPlazaDefinition { id="TOLL_AHM_VAD_01", displayName="Ahmedabad Expressway", roadId="ROAD_AHM_VAD", region="Ahmedabad", distanceKm=14f, baseToll=220f }
            };
            if (FastTagWallet.Instance == null) new GameObject("FASTag Wallet").AddComponent<FastTagWallet>();
        }

        public TollPlazaDefinition FindPlaza(string id)
        {
            if (Plazas == null) return null;
            foreach (var p in Plazas) if (string.Equals(p.id, id, StringComparison.OrdinalIgnoreCase)) return p;
            return null;
        }

        public void AnnounceApproach(string plazaId)
        {
            var p = FindPlaza(plazaId);
            if (p != null) ApproachChanged?.Invoke(p);
        }

        public bool TryPay(string plazaId, TollPaymentMethod method, TruckController truck, string vehicleKey)
        {
            var p = FindPlaza(plazaId);
            if (p == null || !p.active || truck == null) return false;
            string key = plazaId + "|" + (string.IsNullOrEmpty(vehicleKey) ? truck.GetInstanceID().ToString() : vehicleKey);
            if (paidKeys.ContainsKey(key)) return true;
            var trailer = truck.GetComponent<TrailerController>();
            float amount = TollPricingEngine.Calculate(p.baseToll, truck, trailer != null ? trailer.Type : TrailerType.DryVan, method);
            bool charged = method == TollPaymentMethod.FastTag ? FastTagWallet.Instance != null && FastTagWallet.Instance.TryPay(amount)
                                                               : GameManager.Instance != null && GameManager.Instance.TrySpendMoney(amount);
            if (!charged) { PaymentFailed?.Invoke(method == TollPaymentMethod.FastTag ? "FASTag balance too low" : "Insufficient cash"); return false; }
            paidKeys[key] = Time.time;
            string tx = "TOLL-" + nextTransaction.ToString("00000"); nextTransaction++;
            var record = new TollPaymentRecord { transactionId=tx, plazaId=p.id, vehicleKey=key, method=method.ToString(), amount=amount, time=Time.time };
            payments.Add(record);
            if (payments.Count > 50) payments.RemoveAt(0);
            FinanceManager.Instance?.RecordTollExpense(amount);
            PaymentSucceeded?.Invoke(record);
            SaveManager.Instance?.Save();
            return true;
        }

        public TollSaveState CaptureState()
        {
            return new TollSaveState { fastTagBalance = FastTagWallet.Instance?.Balance ?? 0f, nextTransaction = nextTransaction, payments = payments.ToArray() };
        }
        public void RestoreState(TollSaveState state)
        {
            if (state == null) return;
            nextTransaction = Mathf.Max(1, state.nextTransaction);
            payments.Clear(); paidKeys.Clear();
            if (state.payments != null) foreach (var p in state.payments) if (p != null) { payments.Add(p); if (!string.IsNullOrEmpty(p.vehicleKey)) paidKeys[p.vehicleKey] = p.time; }
            if (FastTagWallet.Instance != null) FastTagWallet.Instance.Restore(state.fastTagBalance);
        }
    }

    [RequireComponent(typeof(Collider))]
    public sealed class TollGateTrigger : MonoBehaviour
    {
        [SerializeField] private string plazaId = "TOLL_AHM_VAD_01";
        [SerializeField] private TollPaymentMethod method = TollPaymentMethod.FastTag;
        private readonly HashSet<int> inside = new HashSet<int>();
        public void Configure(string id, TollPaymentMethod paymentMethod) { plazaId=id; method=paymentMethod; }
        private void OnTriggerEnter(Collider other)
        {
            var truck = other.GetComponentInParent<TruckController>();
            if (truck == null || !inside.Add(truck.GetInstanceID())) return;
            TollPlazaManager.Instance?.AnnounceApproach(plazaId);
            TollPlazaManager.Instance?.TryPay(plazaId, method, truck, truck.GetInstanceID().ToString());
        }
        private void OnTriggerExit(Collider other)
        {
            var truck = other.GetComponentInParent<TruckController>();
            if (truck != null) inside.Remove(truck.GetInstanceID());
        }
    }

    public static class TollPlazaBuilder
    {
        public static GameObject BuildDrivenMapPlaza(Transform parent = null)
        {
            var root = new GameObject("Toll Plaza Ahmedabad Expressway");
            if (parent != null) root.transform.SetParent(parent, false);
            root.transform.position = Vector3.zero;
            CreateRoadDeck(root.transform);
            
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "TOLL - FASTag";
            sign.transform.SetParent(root.transform, false);
            sign.transform.localPosition = new Vector3(0f, 4.2f, 8f);
            sign.transform.localScale = new Vector3(10f, 2.2f, .25f);
            UnityEngine.Object.Destroy(sign.GetComponent<Collider>());
            CreateGate(root.transform, "FastTag Gate", new Vector3(0f,0f,0f), TollPaymentMethod.FastTag);
            return root;
        }
        private static void CreateRoadDeck(Transform root)
        {
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Toll Deck"; deck.transform.SetParent(root,false);
            deck.transform.localScale = new Vector3(30f,.25f,16f);
            deck.transform.localPosition = new Vector3(0f,-.1f,0f);
        }
        private static void CreateLane(Transform root, int lane)
        {
            var island = GameObject.CreatePrimitive(PrimitiveType.Cube);
            island.name = "Lane Divider"; island.transform.SetParent(root,false);
            island.transform.localPosition = new Vector3(lane*9f,.15f,0f);
            island.transform.localScale = new Vector3(.22f,.3f,16f);
        }
        private static void CreateGate(Transform root, string name, Vector3 pos, TollPaymentMethod method)
        {
            var go = new GameObject(name); go.transform.SetParent(root,false); go.transform.localPosition=pos;
            var post=GameObject.CreatePrimitive(PrimitiveType.Cube); post.transform.SetParent(go.transform,false); post.transform.localScale=new Vector3(.3f,3.2f,.3f); post.transform.localPosition=new Vector3(-3f,1.6f,0f); UnityEngine.Object.Destroy(post.GetComponent<Collider>());
            var trigger=go.AddComponent<BoxCollider>(); trigger.isTrigger=true; trigger.size=new Vector3(4f,4f,16f);
            go.AddComponent<TollGateTrigger>().Configure("TOLL_AHM_VAD_01",method);
        }
    }
}
