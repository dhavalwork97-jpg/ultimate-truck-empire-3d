#if UNITY_EDITOR
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UltimateTruckEmpire.Truck;
using UltimateTruckEmpire.TrailerSystem;

namespace UltimateTruckEmpire.Tests.Editor
{
    public sealed class ProductionVehicleIntegrationTests
    {
        private static readonly string[] TruckIds =
        {
            "meshy-ai-volvo-fh-globetrotter-0923130558-texture",
            "meshy-ai-golden-hauler-0923132145-texture"
        };

        private static readonly string[] TrailerIds =
        {
            "TRAILER_DRY_VAN_001",
            "TRAILER_FUEL_TANKER_001"
        };

        [TestCaseSource(nameof(TruckIds))]
        public void ProductionTruckHasRuntimeMarkerAndPhysicalContract(string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/TruckSystem/Prefabs/Imported/" + id + ".prefab");

            Assert.IsNotNull(prefab, "Missing production truck prefab: " + id);
            Assert.IsNotNull(prefab.GetComponent<TruckProductionRuntimeInstance>());
            Assert.IsNotNull(prefab.GetComponent<TruckController>());
            Assert.IsNotNull(prefab.GetComponent<TruckPhysics>());
            Assert.IsNotNull(prefab.GetComponent<TrailerController>());
            Assert.IsNotNull(prefab.GetComponent<Rigidbody>());
            Assert.GreaterOrEqual(prefab.GetComponentsInChildren<WheelCollider>(true).Length, 4);
            Assert.IsNotNull(prefab.transform.Find("TrailerCoupling"));
        }

        [TestCaseSource(nameof(TrailerIds))]
        public void ProductionTrailerHasRuntimeContractAndCollider(string id)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TrailerDefinition>(
                "Assets/TrailerSystem/Data/Trailers/" + id + ".asset");

            Assert.IsNotNull(definition, "Missing production trailer definition: " + id);
            Assert.IsNotNull(definition.prefab, "Missing production trailer prefab reference: " + id);

            var prefab = definition.prefab;
            Assert.IsNotNull(prefab.GetComponent<LoadedTrailer>());
            Assert.IsNotNull(prefab.GetComponent<TrailerCargoModule>());
            Assert.IsNotNull(prefab.GetComponent<TrailerSkinApplier>());
            Assert.IsNotNull(prefab.GetComponent<TrailerRuntimeCalibrator>());
            Assert.IsNotNull(prefab.GetComponentInChildren<LODGroup>(true));

            bool hasSolidCollider = false;
            foreach (var collider in prefab.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null && !collider.isTrigger)
                {
                    hasSolidCollider = true;
                    break;
                }
            }

            Assert.IsTrue(hasSolidCollider, "Production trailer has no solid collider: " + id);
            Assert.IsNotNull(definition.kingpinSocket, "Missing Kingpin socket reference: " + id);
            Assert.IsNotNull(definition.cargoSocket, "Missing CargoSocket reference: " + id);
            Assert.IsNotNull(FindChild(prefab.transform, "Kingpin"), "Missing Kingpin transform: " + id);
            Assert.IsNotNull(FindChild(prefab.transform, "CargoSocket"), "Missing CargoSocket transform: " + id);
        }

        [Test]
        public void ProductionVehicleSourcesRemainAuditable()
        {
            var trailerAudit = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/TrailerSystem/Production/SourceAudits/MeshyGeneratedAssetAudit.json");
            var trailerAttribution = AssetDatabase.LoadAssetAtPath<TextAsset>(
                "Assets/TrailerSystem/Production/MeshyAssetAttribution.md");

            Assert.IsNotNull(trailerAudit);
            Assert.IsNotNull(trailerAttribution);
            StringAssert.Contains("CC BY 4.0", trailerAudit.text);
            StringAssert.Contains("CC BY 4.0", trailerAttribution.text);
        }

        private static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, System.StringComparison.Ordinal))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                var result = FindChild(root.GetChild(i), name);
                if (result != null) return result;
            }

            return null;
        }
    }
}
#endif
