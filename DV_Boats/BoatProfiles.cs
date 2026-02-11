using UnityEngine;

namespace DV_Boats
{
 
    internal class BoatHullProfile
    {
 
        public float Length;
        public float Beam;
        public float Draft;

  
        public float BowProbeZ;
        public float SternProbeZ;
    }
}


namespace DV_Boats
{
    public class BoatCameraProfile
    {
        public string id;
        public string label;
        public Vector3 offset;
        public string mode;
    }
}
