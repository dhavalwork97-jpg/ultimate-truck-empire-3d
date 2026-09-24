using System;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    /// <summary>
    /// Explicit production calibration data for a trailer. Values are authored in the
    /// trailer prefab's local space and are deliberately separate from generated import
    /// heuristics so physical calibration can be reviewed and changed without rewriting
    /// the source Meshy asset.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrailerProductionCalibration : MonoBehaviour
    {
        [Header("Scale")]
        [Min(0.01f)] public float modelScale = 1f;
        public Vector3 localScaleMultiplier = Vector3.one;

        [Header("Coupling")]
        public Vector3 kingpinLocalPosition;
        [Min(0f)] public float kingpinHeight;

        [Header("Axles / Wheels")]
        public Vector3 wheelFL;
        public Vector3 wheelFR;
        public Vector3 wheelRL;
        public Vector3 wheelRR;

        [Header("Physics")]
        [Min(0.01f)] public float massTons = 7f;
        public Vector3 centerOfMassLocalPosition;
        public Vector3 colliderCenterLocalPosition;
        public Vector3 colliderSize = Vector3.one;

        [Header("Suspension / Braking")]
        [Min(0f)] public float suspensionStiffness = 1f;
        [Min(0f)] public float suspensionDamping = 1f;
        [Range(0f, 2f)] public float brakingMultiplier = 1f;

        [Header("Calibration")]
        public bool authored = false;
        [TextArea(2, 5)] public string calibrationNotes;
        public string source = "Meshy Batch 01";

        public bool IsValid()
        {
            return modelScale > 0f &&
                   localScaleMultiplier.x > 0f &&
                   localScaleMultiplier.y > 0f &&
                   localScaleMultiplier.z > 0f &&
                   massTons > 0f &&
                   colliderSize.x > 0f &&
                   colliderSize.y > 0f &&
                   colliderSize.z > 0f;
        }
    }
}
