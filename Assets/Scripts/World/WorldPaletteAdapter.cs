using UnityEngine;
using UltimateTruckEmpire.Visuals;

namespace UltimateTruckEmpire.World
{
    /// <summary>
    /// Bridges <see cref="WorldPalette"/> to the mesh builder's slot system: the
    /// material slot index IS the <see cref="WorldSurface"/> value, so world
    /// geometry can use the full palette while still welding everything into
    /// single meshes with one submesh per material.
    /// </summary>
    public static class WorldPaletteAdapter
    {
        private sealed class Slots : IMaterialSlots
        {
            public Material Get(int slot) => WorldPalette.Get((WorldSurface)slot);
        }

        public static readonly IMaterialSlots Palette = new Slots();

        public static int Slot(WorldSurface surface) => (int)surface;
    }
}
