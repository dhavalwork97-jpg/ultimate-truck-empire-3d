using System;
using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    [Serializable]
    public struct TrailerTextureOverride
    {
        [Min(0)] public int materialSlot;
        public Texture2D albedo;
        public Color tint;
    }

    [CreateAssetMenu(fileName = "TrailerSkin", menuName = "Ultimate Truck Empire/Trailer/Trailer Skin")]
    public sealed class TrailerSkinDefinition : ScriptableObject
    {
        public string id = "skin-id";
        public string displayName = "New Skin";
        public Material materialOverride;
        public Color fallbackTint = Color.white;
        public TrailerTextureOverride[] textureOverrides;

        public bool HasTextureOverride(int materialSlot)
        {
            if (textureOverrides == null) return false;
            for (int i = 0; i < textureOverrides.Length; i++)
                if (textureOverrides[i].materialSlot == materialSlot && textureOverrides[i].albedo != null)
                    return true;
            return false;
        }

        public Texture2D GetTexture(int materialSlot)
        {
            if (textureOverrides == null) return null;
            for (int i = 0; i < textureOverrides.Length; i++)
                if (textureOverrides[i].materialSlot == materialSlot)
                    return textureOverrides[i].albedo;
            return null;
        }

        public Color GetTint(int materialSlot)
        {
            if (textureOverrides == null) return fallbackTint;
            for (int i = 0; i < textureOverrides.Length; i++)
                if (textureOverrides[i].materialSlot == materialSlot)
                    return textureOverrides[i].tint;
            return fallbackTint;
        }
    }
}