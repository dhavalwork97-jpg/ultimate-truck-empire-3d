using UnityEngine;

namespace UltimateTruckEmpire.Truck
{
    /// Marks an imported production truck prefab as already resolved by the
    /// runtime fleet binding. This prevents recursive prefab replacement.
    public sealed class TruckProductionRuntimeInstance : MonoBehaviour
    {
        [SerializeField] private string productionProfileId = "";

        public string ProductionProfileId => productionProfileId;

        public void Initialize(string profileId)
        {
            productionProfileId = profileId ?? "";
        }
    }
}
