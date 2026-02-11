using System;
using UnityEngine;

namespace DV_Boats
{
    [Serializable]
    public class CameraViewDefinition
    {
        public string name;
        public Vector3 offset;
        public string mode;
        public Vector3 lookAtOffset;
        public bool useCabPivot = false;
    }
}
