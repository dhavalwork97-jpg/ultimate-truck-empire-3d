using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    [DisallowMultipleComponent]
    public class TruckVisuals : MonoBehaviour
    {
        public string specId;
        public bool isTrailer;
        public int generatedVertexCount;
        public GameObject bodyRoot;
        public Transform fifthWheelAnchor;
        public Transform[] wheelVisuals;
        public TruckLightRig lightRig;
        public TruckWheelVisualSync wheelSync;
        public List<GameObject> generatedObjects = new List<GameObject>();
        public List<Renderer> hiddenRenderers = new List<Renderer>();

        public void Track(GameObject go) { if (go != null) generatedObjects.Add(go); }

        public void ClearGenerated()
        {
            for (int i = 0; i < generatedObjects.Count; i++)
                TruckVisualUtility.SafeDestroy(generatedObjects[i]);
            generatedObjects.Clear();
            bodyRoot = null;
            fifthWheelAnchor = null;
            wheelVisuals = null;
            generatedVertexCount = 0;
        }

        public void RestoreHiddenRenderers()
        {
            for (int i = 0; i < hiddenRenderers.Count; i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = true;
            hiddenRenderers.Clear();
        }
    }
}
