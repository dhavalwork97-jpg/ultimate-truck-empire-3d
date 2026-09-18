using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    public enum CabLayout
    {
        CabOverEngine,   // flat fronted European style
        Conventional     // long nose / bonneted
    }

    public enum GrilleStyle
    {
        HorizontalBars,
        VerticalBars,
        MeshInsert,
        SplitChrome
    }

    /// <summary>
    /// Pure data. Describes how one fictional truck model should look.
    /// All measurements are in metres, with the truck root at ground level,
    /// +Z pointing forward and +X pointing right.
    ///
    /// Nothing in here touches physics - the builder prefers real WheelCollider
    /// positions when they exist and only falls back to these numbers.
    /// </summary>
    [Serializable]
    public class TruckVisualSpec
    {
        public string id = "generic";
        public string displayName = "Generic Hauler";

        [Header("Paint")]
        public Color paint = new Color(0.16f, 0.35f, 0.62f);
        public Color accent = new Color(0.08f, 0.09f, 0.11f);

        [Header("Cab")]
        public CabLayout layout = CabLayout.CabOverEngine;
        public float cabWidth = 2.48f;
        public float cabHeight = 2.05f;
        public float cabLength = 2.30f;
        public float windscreenRake = 0.30f;
        public float roofChamfer = 0.22f;
        public bool sleeper = true;
        public float sleeperLength = 1.45f;
        public bool roofFairing = true;
        public float hoodLength = 1.75f;     // Conventional layout only
        public float hoodHeight = 1.05f;

        [Header("Chassis")]
        public float frameTopY = 1.02f;      // top surface of the chassis rails
        public float frameHeight = 0.26f;
        public float frameRailSpacing = 0.86f;
        public float frameFrontZ = 3.30f;
        public float frameRearZ = -3.05f;

        [Header("Front end")]
        public GrilleStyle grille = GrilleStyle.HorizontalBars;
        public int grilleBars = 5;
        public float bumperDepth = 0.30f;
        public float bumperHeight = 0.46f;
        public bool chromeBumper = false;
        public bool sunVisor = true;

        [Header("Wheels")]
        public float wheelRadius = 0.53f;
        public float wheelWidth = 0.32f;
        public float trackWidth = 2.28f;
        public float frontAxleZ = 2.35f;
        public float rearAxleZ = -1.55f;
        public int rearAxleCount = 2;
        public float rearAxleSpacing = 1.34f;
        public bool dualRearWheels = true;

        [Header("Details")]
        public bool stackExhaust = false;    // vertical stacks behind the cab
        public bool sideSkirts = false;
        public float fuelTankRadius = 0.32f;
        public float fuelTankLength = 1.35f;
        public bool twinFuelTanks = true;
        public bool fifthWheel = true;
        public float fifthWheelZ = -1.15f;
        public bool sideSteps = true;
        public bool mirrors = true;

        [Header("Trailer capability")]
        public bool canTowTrailer = true;

        public float CabFrontZ
        {
            get
            {
                return layout == CabLayout.Conventional
                    ? frameFrontZ - bumperDepth - hoodLength
                    : frameFrontZ - bumperDepth * 0.5f;
            }
        }

        public TruckVisualSpec Clone()
        {
            return (TruckVisualSpec)MemberwiseClone();
        }
    }

    [Serializable]
    public class TrailerVisualSpec
    {
        public string id = "dryvan";
        public Color skin = new Color(0.86f, 0.87f, 0.88f);
        public float boxLength = 13.2f;
        public float boxWidth = 2.55f;
        public float boxHeight = 2.85f;
        public float floorY = 1.22f;         // underside of the box from the ground
        public float kingpinZ = 5.35f;       // forward of the trailer origin
        public int axleCount = 3;
        public float rearAxleZ = -4.6f;
        public float axleSpacing = 1.32f;
        public float wheelRadius = 0.51f;
        public float wheelWidth = 0.30f;
        public float trackWidth = 2.12f;
        public bool dualWheels = true;
        public bool landingGear = true;
        public bool ribbedSides = true;
        public int ribCount = 10;

        public TrailerVisualSpec Clone()
        {
            return (TrailerVisualSpec)MemberwiseClone();
        }
    }

    /// <summary>
    /// Fictional dealership line-up. These exist purely so that different
    /// catalogue entries look meaningfully different - proportions, cab layout,
    /// grille, bumper, axle count and wheel size all change.
    ///
    /// No real manufacturer names, badges or trade dress are used anywhere.
    /// </summary>
    public static class TruckVisualPresets
    {
        private static List<TruckVisualSpec> _all;

        public static List<TruckVisualSpec> All
        {
            get
            {
                if (_all == null) _all = BuildAll();
                return _all;
            }
        }

        private static List<TruckVisualSpec> BuildAll()
        {
            List<TruckVisualSpec> list = new List<TruckVisualSpec>();

            // 1. Light rigid - short, single rear axle, small wheels.
            TruckVisualSpec kestrel = new TruckVisualSpec();
            kestrel.id = "kestrel-lt";
            kestrel.displayName = "Kestrel LT";
            kestrel.paint = new Color(0.86f, 0.86f, 0.88f);
            kestrel.accent = new Color(0.20f, 0.42f, 0.66f);
            kestrel.layout = CabLayout.CabOverEngine;
            kestrel.cabWidth = 2.22f; kestrel.cabHeight = 1.82f; kestrel.cabLength = 1.95f;
            kestrel.sleeper = false; kestrel.roofFairing = false;
            kestrel.frameTopY = 0.86f; kestrel.frameFrontZ = 2.75f; kestrel.frameRearZ = -2.55f;
            kestrel.frameRailSpacing = 0.78f;
            kestrel.grille = GrilleStyle.MeshInsert; kestrel.grilleBars = 3;
            kestrel.bumperDepth = 0.24f; kestrel.bumperHeight = 0.38f;
            kestrel.wheelRadius = 0.44f; kestrel.wheelWidth = 0.26f; kestrel.trackWidth = 2.02f;
            kestrel.frontAxleZ = 1.95f; kestrel.rearAxleZ = -1.55f;
            kestrel.rearAxleCount = 1; kestrel.dualRearWheels = true;
            kestrel.twinFuelTanks = false; kestrel.fuelTankLength = 0.95f; kestrel.fuelTankRadius = 0.26f;
            kestrel.fifthWheel = false; kestrel.canTowTrailer = false;
            kestrel.sunVisor = false;
            list.Add(kestrel);

            // 2. Mid weight day cab.
            TruckVisualSpec drover = new TruckVisualSpec();
            drover.id = "drover-m";
            drover.displayName = "Drover M-Series";
            drover.paint = new Color(0.72f, 0.24f, 0.16f);
            drover.accent = new Color(0.12f, 0.12f, 0.14f);
            drover.layout = CabLayout.CabOverEngine;
            drover.cabWidth = 2.40f; drover.cabHeight = 1.98f; drover.cabLength = 2.15f;
            drover.sleeper = false; drover.roofFairing = true;
            drover.frameTopY = 0.96f; drover.frameFrontZ = 3.05f; drover.frameRearZ = -2.80f;
            drover.grille = GrilleStyle.HorizontalBars; drover.grilleBars = 4;
            drover.wheelRadius = 0.50f; drover.trackWidth = 2.20f;
            drover.frontAxleZ = 2.15f; drover.rearAxleZ = -1.50f;
            drover.rearAxleCount = 1;
            drover.fifthWheelZ = -1.25f;
            list.Add(drover);

            // 3. Aerodynamic long haul sleeper.
            TruckVisualSpec nomad = new TruckVisualSpec();
            nomad.id = "nomad-aero";
            nomad.displayName = "Nomad Aero 500";
            nomad.paint = new Color(0.13f, 0.30f, 0.55f);
            nomad.accent = new Color(0.80f, 0.82f, 0.84f);
            nomad.layout = CabLayout.CabOverEngine;
            nomad.cabWidth = 2.50f; nomad.cabHeight = 2.18f; nomad.cabLength = 2.35f;
            nomad.windscreenRake = 0.42f; nomad.roofChamfer = 0.30f;
            nomad.sleeper = true; nomad.sleeperLength = 1.60f; nomad.roofFairing = true;
            nomad.grille = GrilleStyle.MeshInsert; nomad.grilleBars = 6;
            nomad.sideSkirts = true;
            nomad.wheelRadius = 0.53f; nomad.trackWidth = 2.30f;
            nomad.frontAxleZ = 2.40f; nomad.rearAxleZ = -1.60f;
            nomad.rearAxleCount = 2;
            list.Add(nomad);

            // 4. Classic long nose with stacks.
            TruckVisualSpec brontes = new TruckVisualSpec();
            brontes.id = "brontes-900";
            brontes.displayName = "Brontes 900";
            brontes.paint = new Color(0.10f, 0.28f, 0.18f);
            brontes.accent = new Color(0.85f, 0.70f, 0.28f);
            brontes.layout = CabLayout.Conventional;
            brontes.cabWidth = 2.46f; brontes.cabHeight = 1.90f; brontes.cabLength = 1.85f;
            brontes.hoodLength = 1.95f; brontes.hoodHeight = 1.12f;
            brontes.windscreenRake = 0.18f;
            brontes.sleeper = true; brontes.sleeperLength = 1.55f; brontes.roofFairing = false;
            brontes.frameTopY = 1.08f; brontes.frameFrontZ = 3.75f; brontes.frameRearZ = -3.10f;
            brontes.grille = GrilleStyle.VerticalBars; brontes.grilleBars = 7;
            brontes.chromeBumper = true; brontes.bumperDepth = 0.34f; brontes.bumperHeight = 0.52f;
            brontes.stackExhaust = true;
            brontes.wheelRadius = 0.55f; brontes.wheelWidth = 0.34f; brontes.trackWidth = 2.34f;
            brontes.frontAxleZ = 2.95f; brontes.rearAxleZ = -1.45f;
            brontes.rearAxleCount = 2;
            list.Add(brontes);

            // 5. Heavy haulage tractor - wide, tall, three rear axles.
            TruckVisualSpec atlas = new TruckVisualSpec();
            atlas.id = "atlas-hd";
            atlas.displayName = "Atlas HD Heavy";
            atlas.paint = new Color(0.82f, 0.52f, 0.06f);
            atlas.accent = new Color(0.14f, 0.14f, 0.16f);
            atlas.layout = CabLayout.CabOverEngine;
            atlas.cabWidth = 2.55f; atlas.cabHeight = 2.30f; atlas.cabLength = 2.45f;
            atlas.sleeper = true; atlas.sleeperLength = 1.50f; atlas.roofFairing = false;
            atlas.frameTopY = 1.20f; atlas.frameHeight = 0.32f;
            atlas.frameFrontZ = 3.55f; atlas.frameRearZ = -3.55f;
            atlas.frameRailSpacing = 0.95f;
            atlas.grille = GrilleStyle.HorizontalBars; atlas.grilleBars = 6;
            atlas.bumperDepth = 0.38f; atlas.bumperHeight = 0.58f;
            atlas.stackExhaust = true;
            atlas.wheelRadius = 0.60f; atlas.wheelWidth = 0.38f; atlas.trackWidth = 2.42f;
            atlas.frontAxleZ = 2.60f; atlas.rearAxleZ = -1.40f;
            atlas.rearAxleCount = 3; atlas.rearAxleSpacing = 1.42f;
            atlas.fuelTankRadius = 0.36f; atlas.fuelTankLength = 1.55f;
            list.Add(atlas);

            // 6. Chrome heavy classic.
            TruckVisualSpec sable = new TruckVisualSpec();
            sable.id = "sable-classic";
            sable.displayName = "Sable Classic";
            sable.paint = new Color(0.10f, 0.10f, 0.12f);
            sable.accent = new Color(0.78f, 0.80f, 0.83f);
            sable.layout = CabLayout.Conventional;
            sable.cabWidth = 2.44f; sable.cabHeight = 1.95f; sable.cabLength = 1.90f;
            sable.hoodLength = 1.70f; sable.hoodHeight = 1.00f;
            sable.sleeper = true; sable.sleeperLength = 1.35f;
            sable.grille = GrilleStyle.SplitChrome; sable.grilleBars = 8;
            sable.chromeBumper = true;
            sable.stackExhaust = true;
            sable.frameTopY = 1.05f; sable.frameFrontZ = 3.60f; sable.frameRearZ = -3.00f;
            sable.wheelRadius = 0.54f; sable.trackWidth = 2.30f;
            sable.frontAxleZ = 2.80f; sable.rearAxleZ = -1.50f;
            sable.rearAxleCount = 2;
            list.Add(sable);

            return list;
        }

        /// <summary>
        /// Maps whatever id / display name the existing TruckDealer catalogue uses
        /// onto one of the presets. Exact id match wins; then keyword matching;
        /// then a stable hash so an unknown model still looks consistent every
        /// time it is spawned rather than changing between sessions.
        /// </summary>
        public static TruckVisualSpec Resolve(string idOrName)
        {
            List<TruckVisualSpec> all = All;
            if (string.IsNullOrEmpty(idOrName)) return all[2].Clone();

            string key = idOrName.ToLowerInvariant().Trim();

            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].id.ToLowerInvariant() == key) return all[i].Clone();
                if (all[i].displayName.ToLowerInvariant() == key) return all[i].Clone();
            }

            if (Contains(key, "light") || Contains(key, "small") || Contains(key, "van") || Contains(key, "starter") || Contains(key, "rigid"))
                return all[0].Clone();
            if (Contains(key, "medium") || Contains(key, "mid") || Contains(key, "day"))
                return all[1].Clone();
            if (Contains(key, "aero") || Contains(key, "sleeper") || Contains(key, "long") || Contains(key, "euro"))
                return all[2].Clone();
            if (Contains(key, "classic") || Contains(key, "vintage") || Contains(key, "retro") || Contains(key, "chrome"))
                return all[5].Clone();
            if (Contains(key, "heavy") || Contains(key, "hd") || Contains(key, "max") || Contains(key, "titan") || Contains(key, "ultimate"))
                return all[4].Clone();
            if (Contains(key, "nose") || Contains(key, "hood") || Contains(key, "american"))
                return all[3].Clone();

            int hash = 17;
            for (int i = 0; i < key.Length; i++) hash = unchecked(hash * 31 + key[i]);
            int index = Mathf.Abs(hash) % all.Count;
            return all[index].Clone();
        }

        private static bool Contains(string haystack, string needle)
        {
            return haystack.IndexOf(needle, StringComparison.Ordinal) >= 0;
        }
    }
}
