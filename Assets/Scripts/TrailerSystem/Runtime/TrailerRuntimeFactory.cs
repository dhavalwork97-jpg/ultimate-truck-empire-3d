using UnityEngine;

namespace UltimateTruckEmpire.TrailerSystem
{
    public static class TrailerRuntimeFactory
    {
        public static LoadedTrailer Spawn(
            TrailerDefinition definition,
            Transform parent = null,
            CargoDefinition cargo = null,
            float cargoWeightTons = 0f,
            TrailerSkinDefinition skin = null)
        {
            if (definition == null || definition.prefab == null)
                return null;

            GameObject instance = Object.Instantiate(definition.prefab, parent);
            var calibrator = instance.GetComponent<TrailerRuntimeCalibrator>();
            if (calibrator != null && !calibrator.ApplyCalibration())
            {
                Object.Destroy(instance);
                return null;
            }
            LoadedTrailer loaded = instance.GetComponent<LoadedTrailer>();
            if (loaded == null)
                loaded = instance.AddComponent<LoadedTrailer>();

            if (cargo != null)
            {
                if (!loaded.Configure(definition, cargo, cargoWeightTons, skin))
                {
                    Object.Destroy(instance);
                    return null;
                }
            }
            else
            {
                TrailerSkinApplier applier = instance.GetComponent<TrailerSkinApplier>();
                if (applier == null)
                    applier = instance.AddComponent<TrailerSkinApplier>();
                applier.Initialize(definition, skin);
            }

            return loaded;
        }
    }
}
