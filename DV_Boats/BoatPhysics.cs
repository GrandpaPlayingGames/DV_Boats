using UnityEngine;

namespace DV_Boats
{
    internal static class BoatPhysics
    {

        public static void SetupRigidbody(GameObject boat)
        {
            if (boat == null)
                return;

 
            Rigidbody rb = boat.GetComponent<Rigidbody>();
            if (rb == null)
                rb = boat.AddComponent<Rigidbody>();

            rb.mass = 5000f;
            rb.useGravity = false;
            rb.drag = 2f;
            rb.angularDrag = 2.5f;

            rb.constraints =
                RigidbodyConstraints.FreezePositionY |
                RigidbodyConstraints.FreezeRotationX |
                RigidbodyConstraints.FreezeRotationZ;

            rb.centerOfMass = new Vector3(0f, 0f, 1.5f);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            boat.layer = LayerMask.NameToLayer("Default");

 

            foreach (var c in boat.GetComponents<BoxCollider>())
                Object.Destroy(c);

    
            AddHull(boat, "Hull_Mid",
                new Vector3(0f, -0.5f, 0f),
                new Vector3(3.6f, 1.3f, 5.5f));

            AddHull(boat, "Hull_Bow",
                new Vector3(0f, -0.45f, 3.8f),
                new Vector3(3.2f, 1.1f, 3.2f));

            AddHull(boat, "Hull_Stern",
                new Vector3(0f, -0.45f, -3.6f),
                new Vector3(3.0f, 1.1f, 3.0f));

            
            var buoyancy = boat.GetComponentInChildren<ItemBuoyancyEnabler>();
            if (buoyancy != null)
            {
                Collider waterCol = buoyancy.GetComponent<Collider>();
                if (waterCol != null)
                {
                    foreach (var col in boat.GetComponents<Collider>())
                    {
                        if (!col.isTrigger)
                            Physics.IgnoreCollision(col, waterCol);
                    }
                }
            }
        }

        private static void AddHull(GameObject boat, string name, Vector3 center, Vector3 size)
        {
            BoxCollider c = boat.AddComponent<BoxCollider>();
            c.name = name;
            c.center = center;
            c.size = size;
            c.isTrigger = false;
        }       
    }
}





