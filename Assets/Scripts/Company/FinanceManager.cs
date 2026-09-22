using System;
using UnityEngine;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FinanceData
    {
        public float revenue, fuelExpense, payrollExpense, maintenanceExpense, otherExpense, capitalExpense, tollExpense, debt;
        public float loanInterestRate = .08f;
        public float accruedInterest;
        public float lastInterestDay;
    }

    public sealed class FinanceManager : MonoBehaviour
    {
        public static FinanceManager Instance { get; private set; }
        public FinanceData Data { get; private set; } = new();

        public float OperatingProfit => Data.revenue - Data.fuelExpense - Data.payrollExpense - Data.maintenanceExpense - Data.otherExpense - Data.accruedInterest;
        public float NetProfit => OperatingProfit;
        public float TotalOperatingExpenses => Data.fuelExpense + Data.payrollExpense + Data.maintenanceExpense + Data.otherExpense + Data.accruedInterest;
        public float TotalExpenses => TotalOperatingExpenses + Data.capitalExpense;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Data.lastInterestDay = Time.realtimeSinceStartup / 86400f;
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
            float r = Mathf.Max(0f, revenue);
            float fuel = Mathf.Max(0f, fuelExpense);
            float payroll = Mathf.Max(0f, payrollExpense);
            Data.revenue += r;
            Data.fuelExpense += fuel;
            Data.payrollExpense += payroll;
            GameManager.Instance?.AddMoney(Mathf.Max(0f, r - fuel - payroll));
        }

        public void RecordFuelExpense(float amount) => Data.fuelExpense += Mathf.Max(0f, amount);
        public void RecordMaintenance(float amount) => Data.maintenanceExpense += Mathf.Max(0f, amount);
        public void RecordTollExpense(float amount) => Data.tollExpense += Mathf.Max(0f, amount);
        public void RecordExpense(float amount) => Data.otherExpense += Mathf.Max(0f, amount);
        public void RecordCapitalExpense(float amount) => Data.capitalExpense += Mathf.Max(0f, amount);

        public bool TakeLoan(float amount, float annualInterestRate = .08f)
        {
            if (amount <= 0f || GameManager.Instance == null) return false;
            Data.debt += amount;
            Data.loanInterestRate = Mathf.Clamp01(annualInterestRate);
            Data.lastInterestDay = Time.realtimeSinceStartup / 86400f;
            GameManager.Instance.AddMoney(amount);
            return true;
        }

        public bool RepayLoan(float amount)
        {
            if (amount <= 0f || Data.debt <= 0f || GameManager.Instance == null || GameManager.Instance.Money < amount) return false;
            float principal = Mathf.Min(amount, Data.debt);
            if (!GameManager.Instance.TrySpendMoney(principal)) return false;
            Data.debt -= principal;
            return true;
        }

        public void Restore(FinanceData saved)
        {
            if (saved == null) return;
            Data = saved;
            Data.loanInterestRate = Mathf.Clamp01(Data.loanInterestRate);
            Data.debt = Mathf.Max(0f, Data.debt);
            Data.accruedInterest = Mathf.Max(0f, Data.accruedInterest);
            Data.capitalExpense = Mathf.Max(0f, Data.capitalExpense);
            Data.lastInterestDay = Time.realtimeSinceStartup / 86400f;
        }
    }
}
