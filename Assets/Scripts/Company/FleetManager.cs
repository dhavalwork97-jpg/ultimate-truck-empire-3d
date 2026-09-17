using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Company
{
    [Serializable] public sealed class FleetTruckData { public string id; public string model; public float purchasePrice; public float fuelCapacity=500f; public float fuel=500f; public float capacityTons=30f; public float condition=100f; public bool available=true; public string assignedDriverId=""; public string assignedContractId=""; }
    public sealed class FleetManager : MonoBehaviour
    {
        public static FleetManager Instance { get; private set; }
        public IReadOnlyList<FleetTruckData> Trucks => trucks;
        private readonly List<FleetTruckData> trucks = new(); private int nextId=1;
        private void Awake(){if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);}
        public FleetTruckData BuyTruck(string model,float price,float capacityTons=30f){var c=CompanyManager.Instance;var g=Core.GameManager.Instance;if(c==null||!c.IsCompanyCreated||trucks.Count>=c.Data.truckCapacity||g==null||!g.TrySpendMoney(price))return null;var t=new FleetTruckData{id="TRK-"+nextId++,model=string.IsNullOrWhiteSpace(model)?"UTE Hauler":model.Trim(),purchasePrice=price,capacityTons=Mathf.Max(1f,capacityTons)};trucks.Add(t);return t;}
        public FleetTruckData Find(string id)=>trucks.Find(t=>t.id==id);
        public bool Assign(string truckId,string driverId,string contractId){var t=Find(truckId);var d=DriverManager.Instance?.Find(driverId);if(t==null||d==null||!t.available||!d.available)return false;t.assignedDriverId=driverId;t.assignedContractId=contractId??"";t.available=false;d.assignedTruckId=truckId;d.assignedContractId=contractId??"";d.available=false;return true;}
        public void Release(string truckId){var t=Find(truckId);if(t==null)return;var d=DriverManager.Instance?.Find(t.assignedDriverId);if(d!=null){d.assignedTruckId="";d.assignedContractId="";d.available=true;}t.assignedDriverId="";t.assignedContractId="";t.available=true;}
        public void Restore(FleetTruckData[] saved){trucks.Clear();if(saved==null)return;trucks.AddRange(saved);nextId=1;foreach(var t in trucks)if(t!=null&&int.TryParse(t.id?.Replace("TRK-",""),out int n))nextId=Mathf.Max(nextId,n+1);}
    }
}
