using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    /// <summary>
    /// Bookkeeping for one generated vehicle appearance. Lets the pass be run
    /// again on the same GameObject (when the player repaints or swaps models)
    /// without leaving orphaned geometry behind, and gives gameplay code a
    /// single place to find the body, wheels, lamps and coupling point.
    /// </summary>
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

        [Tooltip("Objects created by the visual pass. Destroyed before a rebuild.")]
        public List<GameObject> generatedObjects = new List<GameObject>();

        [Tooltip("Pre-existing renderers switched off by the pass. Restored if the pass is removed.")]
        public List<Renderer> hiddenRenderers = new List<Renderer>();

        public void Track(GameObject go)
        {
            if (go != null) generatedObjects.Add(go);
        }

        /// <summary>Destroys generated geometry. Existing gameplay objects are untouched.</summary>
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

        /// <summary>Re-enables any placeholder renderers this pass switched off.</summary>
        public void RestoreHiddenRenderers()
        {
            for (int i = 0; i < hiddenRenderers.Count; i++)
                if (hiddenRenderers[i] != null) hiddenRenderers[i].enabled = true;
            hiddenRenderers.Clear();
        }
    }
}
