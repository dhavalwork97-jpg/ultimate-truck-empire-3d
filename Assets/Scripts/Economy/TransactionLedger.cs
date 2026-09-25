using System;
using System.Collections.Generic;
using UnityEngine;
using UltimateTruckEmpire.Core;
using UltimateTruckEmpire.Company;

namespace UltimateTruckEmpire.Economy
{
    public enum TransactionType { FreightRevenue, FuelPurchase, TollFee, Repair, Upgrade, OtherIncome, OtherExpense }

    [Serializable]
    public sealed class TransactionRecord
    {
        public string id;
        public TransactionType type;
        public float amount;
        public string description;
        public string referenceId;
        public long unixTime;
    }

    [Serializable]
    public sealed class LedgerSaveData
    {
        public TransactionRecord[] transactions;
        public int nextTransactionId = 1;
    }

    public sealed class TransactionLedger : MonoBehaviour
    {
        public static TransactionLedger Instance { get; private set; }
        [SerializeField] private int maxRecords = 100;
        private readonly List<TransactionRecord> records = new List<TransactionRecord>();
        private int nextId = 1;

        public IReadOnlyList<TransactionRecord> Records => records;
        public float TotalIncome { get; private set; }
        public float TotalExpenses { get; private set; }
        public float NetChange => TotalIncome - TotalExpenses;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool TryRecordIncome(float amount, TransactionType type, string description, string referenceId = "")
        {
            if (amount <= 0f || GameManager.Instance == null) return false;
            GameManager.Instance.AddMoney(amount);
            RecordInternal(amount, type, description, referenceId, true);
            return true;
        }

        public bool TryRecordExpense(float amount, TransactionType type, string description, string referenceId = "")
        {
            if (amount <= 0f || GameManager.Instance == null || !GameManager.Instance.TrySpendMoney(amount))
                return false;
            RecordInternal(amount, type, description, referenceId, false);
            return true;
        }

        public void RecordIncomeWithoutWallet(float amount, TransactionType type, string description, string referenceId = "")
        {
            if (amount > 0f) RecordInternal(amount, type, description, referenceId, true);
        }

        public void RecordExpenseWithoutWallet(float amount, TransactionType type, string description, string referenceId = "")
        {
            if (amount > 0f) RecordInternal(amount, type, description, referenceId, false);
        }

        private void RecordInternal(float amount, TransactionType type, string description, string referenceId, bool income)
        {
            var record = new TransactionRecord
            {
                id = "TX-" + nextId.ToString("000000"),
                type = type,
                amount = Mathf.Max(0f, amount),
                description = description ?? string.Empty,
                referenceId = referenceId ?? string.Empty,
                unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            nextId++;
            records.Add(record);
            if (income) TotalIncome += record.amount; else TotalExpenses += record.amount;
            while (records.Count > Mathf.Max(1, maxRecords)) records.RemoveAt(0);

            if (FinanceManager.Instance != null)
            {
                switch (type)
                {
                    case TransactionType.FreightRevenue:
                        FinanceManager.Instance.Data.revenue += record.amount;
                        break;
                    case TransactionType.FuelPurchase:
                        FinanceManager.Instance.RecordFuelExpense(record.amount);
                        break;
                    case TransactionType.TollFee:
                        FinanceManager.Instance.RecordTollExpense(record.amount);
                        break;
                    case TransactionType.Repair:
                        FinanceManager.Instance.RecordMaintenance(record.amount);
                        break;
                    case TransactionType.Upgrade:
                        FinanceManager.Instance.RecordCapitalExpense(record.amount);
                        break;
                    case TransactionType.OtherExpense:
                        FinanceManager.Instance.RecordExpense(record.amount);
                        break;
                }
            }
        }

        public LedgerSaveData CaptureState()
        {
            return new LedgerSaveData { transactions = records.ToArray(), nextTransactionId = nextId };
        }

        public void RestoreState(LedgerSaveData state)
        {
            if (state == null) return;
            records.Clear();
            TotalIncome = 0f;
            TotalExpenses = 0f;
            nextId = Mathf.Max(1, state.nextTransactionId);
            if (state.transactions == null) return;
            foreach (var record in state.transactions)
            {
                if (record == null || record.amount < 0f) continue;
                records.Add(record);
                if (record.type == TransactionType.FreightRevenue || record.type == TransactionType.OtherIncome)
                    TotalIncome += record.amount;
                else
                    TotalExpenses += record.amount;
            }
        }
    }
}
