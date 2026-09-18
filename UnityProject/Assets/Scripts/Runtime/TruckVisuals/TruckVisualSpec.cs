using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimateTruckEmpire.Visuals
{
    public enum CabLayout { CabOverEngine, Conventional }
    public enum GrilleStyle { HorizontalBars, VerticalBars, MeshInsert, SplitChrome }

    [Serializable]
    public class TruckVisualSpec
    {
        public string id = "generic";
        public string displayName = "Generic Hauler";
        public Color paint = new Color(0.16f, 0.35f, 0.62f);
        public Color accent = new Color(0.08f, 0.09f, 0.11f);
        public CabLayout layout = CabLayout.CabOverEngine;
        public float cabWidth = 2.48f, cabHeight = 2.05f, cabLength = 2.30f;
        public float windscreenRake = 0.30f, roofChamfer = 0.22f;
        public bool sleeper = true;
        public float sleeperLength = 1.45f;
        public bool roofFairing = true;
        public float hoodLength = 1.75f, hoodHeight = 1.05f;
        public float frameTopY = 1.02f, frameHeight = 0.26f, frameRailSpacing = 0.86f;
        public float frameFrontZ = 3.30f, frameRearZ = -3.05f;
        public GrilleStyle grille = GrilleStyle.HorizontalBars;
        public int grilleBars = 5;
        public float bumperDepth = 0.30f, bumperHeight = 0.46f;
        public bool chromeBumper = false, sunVisor = true;
        public float wheelRadius = 0.53f, wheelWidth = 0.32f, trackWidth = 2.28f;
        public float frontAxleZ = 2.35f, rearAxleZ = -1.55f;
        public int rearAxleCount = 2;
        public float rearAxleSpacing = 1.34f;
        public bool dualRearWheels = true;
        public bool stackExhaust = false, sideSkirts = false;
        public float fuelTankRadius = 0.32f, fuelTankLength = 1.35f;
        public bool twinFuelTanks = true, fifthWheel = true;
        public float fifthWheelZ = -1.15f;
        public bool sideSteps = true, mirrors = true, canTowTrailer = true;
        public float CabFrontZ => layout == CabLayout.Conventional
            ? frameFrontZ - bumperDepth - hoodLength
            : frameFrontZ - bumperDepth * 0.5f;
        public TruckVisualSpec Clone() => (TruckVisualSpec)MemberwiseClone();
    }

    [Serializable]
    public class TrailerVisualSpec
    {
        public string id = "dryvan";
        public Color skin = new Color(0.86f, 0.87f, 0.88f);
        public float boxLength = 13.2f, boxWidth = 2.55f, boxHeight = 2.85f, floorY = 1.22f, kingpinZ = 5.35f;
        public int axleCount = 3;
        public float rearAxleZ = -4.6f, axleSpacing = 1.32f, wheelRadius = 0.51f, wheelWidth = 0.30f, trackWidth = 2.12f;
        public bool dualWheels = true, landingGear = true, ribbedSides = true;
        public int ribCount = 10;
        public TrailerVisualSpec Clone() => (TrailerVisualSpec)MemberwiseClone();
    }

    public static class TruckVisualPresets
    {
        private static List<TruckVisualSpec> _all;
        public static List<TruckVisualSpec> All => _all ?? (_all = BuildAll());

        private static List<TruckVisualSpec> BuildAll()
        {
            var list = new List<TruckVisualSpec>();

            var kestrel = new TruckVisualSpec { id="kestrel-lt", displayName="Kestrel LT", paint=new Color(.86f,.86f,.88f), accent=new Color(.20f,.42f,.66f), cabWidth=2.22f, cabHeight=1.82f, cabLength=1.95f, sleeper=false, roofFairing=false, frameTopY=.86f, frameFrontZ=2.75f, frameRearZ=-2.55f, frameRailSpacing=.78f, grille=GrilleStyle.MeshInsert, grilleBars=3, bumperDepth=.24f, bumperHeight=.38f, wheelRadius=.44f, wheelWidth=.26f, trackWidth=2.02f, frontAxleZ=1.95f, rearAxleZ=-1.55f, rearAxleCount=1, twinFuelTanks=false, fuelTankLength=.95f, fuelTankRadius=.26f, fifthWheel=false, canTowTrailer=false, sunVisor=false };
            list.Add(kestrel);

            var drover = new TruckVisualSpec { id="drover-m", displayName="Drover M-Series", paint=new Color(.72f,.24f,.16f), accent=new Color(.12f,.12f,.14f), cabWidth=2.40f, cabHeight=1.98f, cabLength=2.15f, sleeper=false, roofFairing=true, frameTopY=.96f, frameFrontZ=3.05f, frameRearZ=-2.80f, grille=GrilleStyle.HorizontalBars, grilleBars=4, wheelRadius=.50f, trackWidth=2.20f, frontAxleZ=2.15f, rearAxleZ=-1.50f, rearAxleCount=1, fifthWheelZ=-1.25f };
            list.Add(drover);

            var nomad = new TruckVisualSpec { id="nomad-aero", displayName="Nomad Aero 500", paint=new Color(.13f,.30f,.55f), accent=new Color(.80f,.82f,.84f), cabWidth=2.50f, cabHeight=2.18f, cabLength=2.35f, windscreenRake=.42f, roofChamfer=.30f, sleeper=true, sleeperLength=1.60f, roofFairing=true, grille=GrilleStyle.MeshInsert, grilleBars=6, sideSkirts=true, wheelRadius=.53f, trackWidth=2.30f, frontAxleZ=2.40f, rearAxleZ=-1.60f, rearAxleCount=2 };
            list.Add(nomad);

            var brontes = new TruckVisualSpec { id="brontes-900", displayName="Brontes 900", paint=new Color(.10f,.28f,.18f), accent=new Color(.85f,.70f,.28f), layout=CabLayout.Conventional, cabWidth=2.46f, cabHeight=1.90f, cabLength=1.85f, hoodLength=1.95f, hoodHeight=1.12f, windscreenRake=.18f, sleeper=true, sleeperLength=1.55f, roofFairing=false, frameTopY=1.08f, frameFrontZ=3.75f, frameRearZ=-3.10f, grille=GrilleStyle.VerticalBars, grilleBars=7, chromeBumper=true, bumperDepth=.34f, bumperHeight=.52f, stackExhaust=true, wheelRadius=.55f, wheelWidth=.34f, trackWidth=2.34f, frontAxleZ=2.95f, rearAxleZ=-1.45f, rearAxleCount=2 };
            list.Add(brontes);

            var atlas = new TruckVisualSpec { id="atlas-hd", displayName="Atlas HD Heavy", paint=new Color(.82f,.52f,.06f), accent=new Color(.14f,.14f,.16f), cabWidth=2.55f, cabHeight=2.30f, cabLength=2.45f, sleeper=true, sleeperLength=1.50f, roofFairing=false, frameTopY=1.20f, frameHeight=.32f, frameFrontZ=3.55f, frameRearZ=-3.55f, frameRailSpacing=.95f, grille=GrilleStyle.HorizontalBars, grilleBars=6, bumperDepth=.38f, bumperHeight=.58f, stackExhaust=true, wheelRadius=.60f, wheelWidth=.38f, trackWidth=2.42f, frontAxleZ=2.60f, rearAxleZ=-1.40f, rearAxleCount=3, rearAxleSpacing=1.42f, fuelTankRadius=.36f, fuelTankLength=1.55f };
            list.Add(atlas);

            var sable = new TruckVisualSpec { id="sable-classic", displayName="Sable Classic", paint=new Color(.10f,.10f,.12f), accent=new Color(.78f,.80f,.83f), layout=CabLayout.Conventional, cabWidth=2.44f, cabHeight=1.95f, cabLength=1.90f, hoodLength=1.70f, hoodHeight=1.00f, sleeper=true, sleeperLength=1.35f, grille=GrilleStyle.SplitChrome, grilleBars=8, chromeBumper=true, stackExhaust=true, frameTopY=1.05f, frameFrontZ=3.60f, frameRearZ=-3.00f, wheelRadius=.54f, trackWidth=2.30f, frontAxleZ=2.80f, rearAxleZ=-1.50f, rearAxleCount=2 };
            list.Add(sable);

            return list;
        }

        public static TruckVisualSpec Resolve(string idOrName)
        {
            var all = All;
            if (string.IsNullOrEmpty(idOrName)) return all[2].Clone();
            string key = idOrName.ToLowerInvariant().Trim();
            for (int i=0;i<all.Count;i++)
                if (all[i].id.ToLowerInvariant()==key || all[i].displayName.ToLowerInvariant()==key) return all[i].Clone();
            if (Contains(key,"light")||Contains(key,"small")||Contains(key,"van")||Contains(key,"starter")||Contains(key,"rigid")) return all[0].Clone();
            if (Contains(key,"medium")||Contains(key,"mid")||Contains(key,"day")) return all[1].Clone();
            if (Contains(key,"aero")||Contains(key,"sleeper")||Contains(key,"long")||Contains(key,"euro")) return all[2].Clone();
            if (Contains(key,"classic")||Contains(key,"vintage")||Contains(key,"retro")||Contains(key,"chrome")) return all[5].Clone();
            if (Contains(key,"heavy")||Contains(key,"hd")||Contains(key,"max")||Contains(key,"titan")||Contains(key,"ultimate")) return all[4].Clone();
            if (Contains(key,"nose")||Contains(key,"hood")||Contains(key,"american")) return all[3].Clone();
            int hash=17; for(int i=0;i<key.Length;i++) hash=unchecked(hash*31+key[i]);
            return all[Mathf.Abs(hash)%all.Count].Clone();
        }

        private static bool Contains(string haystack,string needle) => haystack.IndexOf(needle,StringComparison.Ordinal)>=0;
    }
}
