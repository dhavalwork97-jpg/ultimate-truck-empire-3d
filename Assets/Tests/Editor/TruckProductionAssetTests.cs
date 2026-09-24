#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UltimateTruckEmpire.Truck;

namespace UltimateTruckEmpire.Tests.Editor
{
    public sealed class TruckProductionAssetTests
    {
        private static readonly string[] ProductionIds =
        {
            "meshy-ai-volvo-fh-globetrotter-0923130558-texture",
            "meshy-ai-golden-hauler-0923132145-texture"
        };

        [TestCaseSource(nameof(ProductionIds))]
        public void ProductionProfileExists(string id)
        {
            var profile = AssetDatabase.LoadAssetAtPath<TruckProductionProfile>(
                "Assets/TruckSystem/Data/Production/" + id + ".asset");

            Assert.IsNotNull(profile, "Missing production profile: " + id);
            Assert.AreEqual(id, profile.id);
            Assert.IsNotNull(profile.prefab, "Profile has no prefab: " + id);
            Assert.IsNotNull(profile.couplingSocket, "Profile has no coupling socket: " + id);
            Assert.IsNotNull(profile.wheelSockets);
            Assert.AreEqual(4, profile.wheelSockets.Length);
            Assert.IsTrue(profile.wheelSockets[0] != null && profile.wheelSockets[1] != null &&
                          profile.wheelSockets[2] != null && profile.wheelSockets[3] != null);
            Assert.LessOrEqual(profile.materialSlotBudget, 6);
            Assert.LessOrEqual(profile.maxTextureResolution, 1024);
        }

        [TestCaseSource(nameof(ProductionIds))]
        public void ProductionPrefabExists(string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/TruckSystem/Prefabs/Imported/" + id + ".prefab");

            Assert.IsNotNull(prefab, "Missing production prefab: " + id);
            Assert.IsNotNull(prefab.GetComponent<TruckController>());
            Assert.IsNotNull(prefab.GetComponent<TruckPhysics>());
            Assert.IsNotNull(prefab.GetComponent<TruckInput>());
            Assert.IsNotNull(prefab.GetComponent<PlayerTruckFleetBinding>());
            Assert.IsNotNull(prefab.GetComponent<TrailerController>());
            Assert.IsNotNull(prefab.GetComponent<Rigidbody>());
            Assert.GreaterOrEqual(prefab.GetComponentsInChildren<WheelCollider>(true).Length, 4);
            Assert.IsNotNull(prefab.transform.Find("TrailerCoupling"));
        }

        [TestCaseSource(nameof(ProductionIds))]
        public void ImportedSourceModelExists(string id)
        {
            string[] guids = AssetDatabase.FindAssets("t:Model", new[]
            {
                "Assets/TruckSystem/Imports/Trucks/Batch02"
            });

            bool found = false;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.IndexOf(id, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "Missing imported source model for production ID: " + id);
        }
    }
}
#endif
