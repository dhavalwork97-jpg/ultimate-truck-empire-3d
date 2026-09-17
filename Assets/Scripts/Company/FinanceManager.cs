using System;
using UnityEngine;
using UltimateTruckEmpire.Core;

namespace UltimateTruckEmpire.Company
{
    [Serializable]
    public sealed class FinanceData
    {
        public float revenue;
        public float fuelExpense;
        public float payrollExpense;
        public float maintenanceExpense;
        public float otherExpense;
        public float debt;
        public float loanInterestRate = 0.08f;
    }

    public sealed class FinanceManager : MonoBehaviour
    {
        public static FinanceManager Instance { get; private set; }
        public FinanceData Data { get; private set; } = new();
        public float NetProfit => Data.revenue - Data.fuelExpense - Data.payrollExpense - Data.maintenanceExpense - Data.otherExpense - Data.loanInterestRate * Data.debt;
        public float TotalExpenses => Data.fuelExpense + Data.payrollExpense + Data.maintenanceExpense + Data.otherExpense;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void RecordDelivery(float revenue, float fuelExpense, float payrollExpense)
        {
            Data.revenue += Mathf.Max(0f, revenue);
            Data.fuelExpense += Mathf.Max(0f, fuelExpense);
            Data.payrollExpense += Mathf.Max(0f, payrollExpense);
            GameManager.Instance?.AddMoney(Mathf.Max(0f, revenue - fuelExpense - payrollExpense));
        }

        public void RecordMaintenance(float amount) => Data.maintenanceExpense += Mathf.Max(0f, amount);
        public void RecordExpense(float amount) => Data.otherExpense += Mathf.Max(0f, amount);

        public bool TakeLoan(float amount, float annualInterestRate = 0.08f)
        {
            if (amount <= 0f || GameManager.Instance == null) return false;
            Data.debt += amount;
            Data.loanInterestRate = Mathf.Clamp(annualInterestRate, 0f, 1f);
            GameManager.Instance.AddMoney(amount);
            return true;
        }

        public bool RepayLoan(float amount)
        {
            if (amount <= 0f || Data.debt <= 0f || GameManager.Instance == null || GameManager.Instance.Money < amount) return false;
            float payment = Mathf.Min(amount, Data.debt);
            if (!GameManager.Instance.TrySpendMoney(payment)) return false;
            Data.debt -= payment;
            return true;
        }
    }
}
