using System.Collections.Generic;
using UnityEngine;

namespace DV_Boats
{
    internal static class BoatStructuralProfiles
    {
        internal static readonly Dictionary<string, BoatStructureProfile> Profiles =
            new Dictionary<string, BoatStructureProfile>
            {
                ["FishingBoat_01"] = CreateFishingBoat01(),
                ["FishingBoat_02"] = CreateFishingBoat02(),
                ["FishingBoat_03"] = CreateFishingBoat03(),
            };


        private static BoatStructureProfile CreateFishingBoat01()
        {
            return new BoatStructureProfile
            {
                Probes = new ProbeLayout
                {
                    Bow = new Vector3(0f, 0f, 17f),
                    Stern = new Vector3(0f, 0f, -16.75f),
                    Port = new Vector3(-4f, 0f, 0f),
                    Starboard = new Vector3(4f, 0f, 0f),

                    PortFront = new Vector3(-4f, 0f, 9.5f),
                    PortRear = new Vector3(-4f, 0f, -9.5f),
                    StarboardFront = new Vector3(4f, 0f, 9.5f),
                    StarboardRear = new Vector3(4f, 0f, -9.5f),
                },

                DeckLight = new DeckLightLayout
                {
                    Position = new Vector3(0f, 20f, -3f),
                    Direction = Vector3.down
                },

                NavLights = new NavLightLayout
                {
                    Port = new Vector3(-1.62f, 9.85f, 10.35f),
                    Starboard = new Vector3(1.62f, 9.85f, 10.35f),
                    Mast = new Vector3(0f, 23f, -3f),
                },

                SpotLight = new SpotLightLayout
                {
                    Position = new Vector3(0f, 5.65f, 16.386f)
                }
            };
        }

        private static BoatStructureProfile CreateFishingBoat02()
        {
            return new BoatStructureProfile
            {
                Probes = new ProbeLayout
                {
                    Bow = new Vector3(0f, 0f, 14f),
                    Stern = new Vector3(0f, 0f, -14f),
                    Port = new Vector3(-4f, 0f, 0f),
                    Starboard = new Vector3(4f, 0f, 0f),

                    PortFront = new Vector3(-4f, 0f, 10.5f),
                    PortRear = new Vector3(-4f, 0f, -10.5f),
                    StarboardFront = new Vector3(4f, 0f, 10.5f),
                    StarboardRear = new Vector3(4f, 0f, -10.4f), //was 10.5
                },

                DeckLight = new DeckLightLayout
                {
                    Position = new Vector3(0f, 20f, 0f),
                    Direction = Vector3.down
                },

                NavLights = new NavLightLayout
                {
                    Port = new Vector3(-2f, 7.35f, 7f),
                    Starboard = new Vector3(2f, 7.35f, 7.0f),
                    Mast = new Vector3(-0.04f, 15.9f, 0.05f),
                },
                SpotLight = new SpotLightLayout
                {
                    Position = new Vector3(0f, 4.845f, 13.19f) 
                }
            };
              
        }

        private static BoatStructureProfile CreateFishingBoat03()
        {
            return new BoatStructureProfile
            {
                Probes = new ProbeLayout
                {
                    Bow = new Vector3(0f, 0f, 17f),
                    Stern = new Vector3(0f, 0f, -16.75f),
                    Port = new Vector3(-4f, 0f, 0f),
                    Starboard = new Vector3(4f, 0f, 0f),

                    PortFront = new Vector3(-4f, 0f, 9.5f),
                    PortRear = new Vector3(-4f, 0f, -9.5f),
                    StarboardFront = new Vector3(4f, 0f, 9.5f),
                    StarboardRear = new Vector3(4f, 0f, -9.5f),
                },

                DeckLight = new DeckLightLayout
                {
                    Position = new Vector3(0f, 20f, -3.0f),
                    Direction = Vector3.down
                },

                NavLights = new NavLightLayout
                {
                    Port = new Vector3(-2.55f, 9.385f, 8.2f),
                    Starboard = new Vector3(2.55f, 9.385f, 8.2f),
                    Mast = new Vector3(0f, 21.5f, -2.85f),
                },

                SpotLight = new SpotLightLayout
                {
                    Position = new Vector3(0f, 05.35f, 16.45f) //back by .15
                }
 
            };
        }
    }

    internal sealed class BoatStructureProfile
    {
        public ProbeLayout Probes;
        public NavLightLayout NavLights;
        public DeckLightLayout DeckLight;
        public SpotLightLayout SpotLight;
    }

    internal sealed class ProbeLayout
    {
        public Vector3 Bow;
        public Vector3 Stern;
        public Vector3 Port;
        public Vector3 Starboard;

        public Vector3 PortFront;
        public Vector3 PortRear;
        public Vector3 StarboardFront;
        public Vector3 StarboardRear;
    }

    internal sealed class NavLightLayout
    {
        public Vector3 Port;
        public Vector3 Starboard;
        public Vector3 Mast;
    }

    internal sealed class DeckLightLayout
    {
        public Vector3 Position;
        public Vector3 Direction; 
    }

    internal sealed class SpotLightLayout
    {
        public Vector3 Position;
    }
}

