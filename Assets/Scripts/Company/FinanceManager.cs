using System;
using UnityEngine;
using UltimateTruckEmpire.Core;
namespace UltimateTruckEmpire.Company
{
 [Serializable] public sealed class FinanceData { public float revenue; public float fuelExpense; public float payrollExpense; public float maintenanceExpense; public float otherExpense; public float debt; public float loanInterestRate=.08f; }
 public sealed class FinanceManager:MonoBehaviour
 {
  public static FinanceManager Instance{get;private set;} public FinanceData Data{get;private set;}=new();
  public float NetProfit=>Data.revenue-Data.fuelExpense-Data.payrollExpense-Data.maintenanceExpense-Data.otherExpense-Data.loanInterestRate*Data.debt;
  public float TotalExpenses=>Data.fuelExpense+Data.payrollExpense+Data.maintenanceExpense+Data.otherExpense;
  private void Awake(){if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);}
  public void RecordDelivery(float revenue,float fuelExpense,float payrollExpense){Data.revenue+=Mathf.Max(0,revenue);Data.fuelExpense+=Mathf.Max(0,fuelExpense);Data.payrollExpense+=Mathf.Max(0,payrollExpense);GameManager.Instance?.AddMoney(Mathf.Max(0,revenue-fuelExpense-payrollExpense));}
  public void RecordMaintenance(float amount)=>Data.maintenanceExpense+=Mathf.Max(0,amount); public void RecordExpense(float amount)=>Data.otherExpense+=Mathf.Max(0,amount);
  public bool TakeLoan(float amount,float annualInterestRate=.08f){if(amount<=0||GameManager.Instance==null)return false;Data.debt+=amount;Data.loanInterestRate=Mathf.Clamp01(annualInterestRate);GameManager.Instance.AddMoney(amount);return true;}
  public bool RepayLoan(float amount){if(amount<=0||Data.debt<=0||GameManager.Instance==null||GameManager.Instance.Money<amount)return false;float p=Mathf.Min(amount,Data.debt);if(!GameManager.Instance.TrySpendMoney(p))return false;Data.debt-=p;return true;}
  public void Restore(FinanceData saved){if(saved!=null)Data=saved;}
 }
}
