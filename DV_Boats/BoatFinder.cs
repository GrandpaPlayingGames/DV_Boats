using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using static DV_Boats.DuplicateBoatProbe;


namespace DV_Boats
{
    internal static class BoatFinder
    {

        private static readonly HashSet<StaticBoatRegistrationKey> KnownBoatKeys
            = new HashSet<StaticBoatRegistrationKey>();

        private static readonly HashSet<StaticBoatRegistrationKey> _staticBoatRegistry
            = new HashSet<StaticBoatRegistrationKey>();

        private const float STATIC_BOAT_SCAN_RADIUS = 5000f;
 
        public static bool RegisterStaticBoatIfNeeded(
            Transform root,
            string boatType
        )
        {
            StaticBoatRegistrationKey key =
                StaticBoatKeyHelper.CreateKey(root, boatType);

            if (_staticBoatRegistry.Contains(key))
            {
                Main.Log(
                    $"[StaticBoatRegistry] SEEN {boatType} @ {key}"
                );
                return false;
            }

            _staticBoatRegistry.Add(key);

            Main.Log(
                $"[StaticBoatRegistry] NEW {boatType} @ {key}"
            );

            return true;
        }

        public static void ScanAndCacheArchetypes()
        {
            Main.Log("[BoatFinder] spawnOnly active — scanning for boat archetypes only");

            Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();

            foreach (var t in transforms)
            {
                if (t == null || string.IsNullOrEmpty(t.name))
                    continue;

                string type = GetFishingBoatType(t.name);
                if (type == null)
                    continue;

                Transform root = GetBoatRoot(t);
                if (root == null)
                    continue;

                BoatSpawner.CacheBoatFromScanOnly(root.gameObject);
            }

            Main.Log("[BoatFinder] ✅ Archetype scan complete");
        }

        public static Transform GetStaticFishingBoatRootFromHit(Transform hitTf)
        {
            Transform cur = hitTf;
            while (cur != null)
            {
                string n = cur.name;
                if (!string.IsNullOrEmpty(n))
                {
                    if (n.Contains("FishingBoat_01") || n.Contains("FishingBoat_02") || n.Contains("FishingBoat_03"))
                        return cur;
                }

                cur = cur.parent;
            }

            return null;
        }

        public static string GetFishingBoatType(string name)
        {
            if (name.Contains("FishingBoat_01"))
                return "FishingBoat_01";

            if (name.Contains("FishingBoat_02"))
                return "FishingBoat_02";

            if (name.Contains("FishingBoat_03"))
                return "FishingBoat_03";

            return null;
        }

        public static bool IsBoatActive(GameObject boatRoot)
        {
            var renderers = boatRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null && renderers[i].enabled)
                    return true;
            }

            var colliders = boatRoot.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null && colliders[i].enabled)
                    return true;
            }

            return false;
        }

        public static void DisableStaticBoatVisualsAndColliders(Transform t)
        {

            Main.Log("[BOATSPAWNERPROBE] Reached ");
            if (t.GetComponent<DVBoatCloneMarker>() != null)
            {
                Main.Log("❌ ERROR: Attempted to disable clone visuals");
                return;
            }


            if (t == null)
                return;

            LODGroup lodGroup = t.GetComponentInParent<LODGroup>();

            Transform root = lodGroup != null
                ? lodGroup.transform
                : t.root;

            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r != null && r.enabled)
                    r.enabled = false;
            }

            var colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                if (c != null && c.enabled)
                    c.enabled = false;
            }
        }

       
        public static Transform GetBoatRoot(Transform t)
        {
            Transform cur = t;

            while (cur.parent != null && cur.name.Contains("_LOD"))
                cur = cur.parent;

            return cur;
        }

        public static void clearCaches()
        {
            _staticBoatRegistry.Clear();
            KnownBoatKeys.Clear();
        }        

        public readonly struct StaticBoatRegistrationKey : IEquatable<StaticBoatRegistrationKey>
        {
            // millimetre precision
            public readonly long x;
            public readonly long y;
            public readonly long z;

            public readonly int yaw;

            public readonly string boatType;

            public StaticBoatRegistrationKey(
                long x,
                long y,
                long z,
                int yaw,
                string boatType)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.yaw = yaw;
                this.boatType = boatType;
            }

            public bool Equals(StaticBoatRegistrationKey other)
            {
                return x == other.x
                    && y == other.y
                    && z == other.z
                    && yaw == other.yaw
                    && boatType == other.boatType;
            }

            public override bool Equals(object obj)
                => obj is StaticBoatRegistrationKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + x.GetHashCode();
                    hash = hash * 31 + y.GetHashCode();
                    hash = hash * 31 + z.GetHashCode();
                    hash = hash * 31 + yaw;
                    hash = hash * 31 + (boatType?.GetHashCode() ?? 0);
                    return hash;
                }
            }

            public override string ToString()
            {
                return $"{boatType} @ ({x},{y},{z}) yaw={yaw}";
            }
        }

        public static class StaticBoatKeyHelper
        {
            public static StaticBoatRegistrationKey CreateKey(Transform root, string boatType)
            {
                Vector3 canonicalPos = root.position - WorldMover.currentMove;

                long qx = (long)Mathf.Round(canonicalPos.x * 1000f);
                long qy = (long)Mathf.Round(canonicalPos.y * 1000f);
                long qz = (long)Mathf.Round(canonicalPos.z * 1000f);

                int qYaw = Mathf.RoundToInt(root.rotation.eulerAngles.y * 100f);

                return new StaticBoatRegistrationKey(
                    qx,
                    qy,
                    qz,
                    qYaw,
                    boatType
                );
            }
        }
    }
}
