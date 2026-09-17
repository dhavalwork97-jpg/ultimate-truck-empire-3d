using System;
using UnityEngine;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FinanceData
    {
        public float revenue, fuelExpense, payrollExpense, maintenanceExpense, otherExpense, debt;
        public float loanInterestRate = .08f;
        public float accruedInterest;
        public float lastInterestDay;
    }

    public sealed class FinanceManager : MonoBehaviour
    {
        public static FinanceManager Instance { get; private set; }
        public FinanceData Data { get; private set; } = new();
        public float NetProfit => Data.revenue - Data.fuelExpense - Data.payrollExpense - Data.maintenanceExpense - Data.otherExpense - Data.accruedInterest;
        public float TotalExpenses => Data.fuelExpense + Data.payrollExpense + Data.maintenanceExpense + Data.otherExpense + Data.accruedInterest;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject); Data.lastInterestDay = Time.realtimeSinceStartup / 86400f;
        }

        private void Update()
        {
            if (Data.debt <= 0f) return;
            float now = Time.realtimeSinceStartup / 86400f;
            float days = now - Data.lastInterestDay;
            if (days < 1f) return;
            Data.accruedInterest += Data.debt * Data.loanInterestRate * days / 365f;
            Data.lastInterestDay = now;
        }

        public void RecordDelivery(float revenue, float fuelExpense, float payrollExpense)
        {
            float r = Mathf.Max(0, revenue), fuel = Mathf.Max(0, fuelExpense), payroll = Mathf.Max(0, payrollExpense);
            Data.revenue += r; Data.fuelExpense += fuel; Data.payrollExpense += payroll;
            GameManager.Instance?.AddMoney(Mathf.Max(0, r - fuel - payroll));
        }

        public void RecordMaintenance(float amount) => Data.maintenanceExpense += Mathf.Max(0, amount);
        public void RecordExpense(float amount) => Data.otherExpense += Mathf.Max(0, amount);

        public bool TakeLoan(float amount, float annualInterestRate = .08f)
        {
            if (amount <= 0 || GameManager.Instance == null) return false;
            Data.debt += amount; Data.loanInterestRate = Mathf.Clamp01(annualInterestRate); Data.lastInterestDay = Time.realtimeSinceStartup / 86400f; GameManager.Instance.AddMoney(amount); return true;
        }

        public bool RepayLoan(float amount)
        {
            if (amount <= 0 || Data.debt <= 0 || GameManager.Instance == null || GameManager.Instance.Money < amount) return false;
            float p = Mathf.Min(amount, Data.debt); if (!GameManager.Instance.TrySpendMoney(p)) return false; Data.debt -= p; return true;
        }

        public void Restore(FinanceData saved)
        {
            if (saved == null) return;
            Data = saved; Data.loanInterestRate = Mathf.Clamp01(Data.loanInterestRate); Data.debt = Mathf.Max(0, Data.debt); Data.accruedInterest = Mathf.Max(0, Data.accruedInterest); Data.lastInterestDay = Time.realtimeSinceStartup / 86400f;
        }
    }
}
