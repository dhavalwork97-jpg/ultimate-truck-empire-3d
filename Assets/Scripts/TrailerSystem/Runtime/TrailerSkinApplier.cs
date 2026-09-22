using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public sealed class TrailerSkinApplier : MonoBehaviour
    {
        [SerializeField] private TrailerDefinition definition;
        [SerializeField] private TrailerSkinDefinition skin;

        public TrailerDefinition Definition => definition;
        public TrailerSkinDefinition Skin => skin;

        public void Initialize(TrailerDefinition trailer, TrailerSkinDefinition selectedSkin = null)
        {
            definition = trailer;
            skin = selectedSkin != null ? selectedSkin : trailer != null ? trailer.defaultSkin : null;
            Apply();
        }

        public void ApplySkin(TrailerSkinDefinition selectedSkin)
        {
            skin = selectedSkin;
            Apply();
        }

        public void Apply()
        {
            if (skin == null) return;

            var renderers = GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);

                if (skin.materialOverride != null)
                {
                    var materials = renderer.sharedMaterials;
                    var replaced = new Material[materials.Length];
                    for (int i = 0; i < replaced.Length; i++) replaced[i] = skin.materialOverride;
                    renderer.sharedMaterials = replaced;
                }

                for (int slot = 0; slot < renderer.sharedMaterials.Length; slot++)
                {
                    Texture2D texture = skin.GetTexture(slot);
                    if (texture != null)
                    {
                        block.SetTexture("_BaseMap", texture);
                        block.SetTexture("_MainTex", texture);
                    }
                    block.SetColor("_BaseColor", skin.GetTint(slot));
                    block.SetColor("_Color", skin.GetTint(slot));
                }

                renderer.SetPropertyBlock(block);
            }
        }
    }
}