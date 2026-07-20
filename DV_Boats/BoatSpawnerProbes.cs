using System.Collections.Generic;
using UnityEngine;

namespace DV_Boats
{
    internal static class BoatSpawnerProbes
    {
        private static readonly List<ProbeMarker> _markers =
            new List<ProbeMarker>();

        private static GameObject _root;
        public static Vector3 CanonicalPos;

        internal static void CreateDebugProbeMarkers()
        {
            ClearExisting();

            _root = new GameObject("BoatSpawnerProbes_ROOT");

            Vector3 currentMove = BoatWorldOriginWatcher.CurrentMove;

            foreach (var profile in BoatSpawnerProbeProfiles.Profiles)
            {
                CreateMarker(profile, currentMove);
            }
        }

        private static void ClearExisting()
        {
            for (int i = 0; i < _markers.Count; i++)
            {
                if (_markers[i] != null)
                    UnityEngine.Object.Destroy(_markers[i].gameObject);
            }

            _markers.Clear();

            if (_root != null)
            {
                UnityEngine.Object.Destroy(_root);
                _root = null;
            }
        }

        private static void CreateMarker(
            BoatSpawnerProbeProfiles.ProbeProfile profile,
            Vector3 currentMove
        )
        {
            GameObject go = new GameObject($"BoatSpawnerProbe_{profile.Id}");
            go.transform.SetParent(_root.transform, true);

            Vector3 sessionPos = profile.CanonicalPosition + currentMove;
            go.transform.position = sessionPos;

            if (Main.Settings.debugLogging)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.SetParent(go.transform, false);
                sphere.transform.localScale = Vector3.one * 1.25f;

                UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());
                ApplyCyanMaterial(sphere);
            }

            var marker = go.AddComponent<ProbeMarker>();
            marker.CanonicalPos = profile.CanonicalPosition;

            _markers.Add(marker);

            Main.Log($"[ProbeMarker] {profile.Id} spawned @ {sessionPos}");
        }

        private static void ApplyCyanMaterial(GameObject go)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null)
                return;

            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.renderQueue = 3000;
            mat.color = new Color(0f, 1f, 1f, 0.45f);
            r.material = mat;
        }

        public sealed class ProbeMarker : MonoBehaviour
        {
            public Vector3 CanonicalPos;
 
            private void Start()
            {
                Vector3 currentMove = BoatWorldOriginWatcher.CurrentMove;
                BoatWorldShiftManager.WOSDeltaAdjustment += OnWorldShift;
            }

            private void OnDestroy()
            {
                BoatWorldShiftManager.WOSDeltaAdjustment -= OnWorldShift;
            }

            private const float RAY_LENGTH = 250f; 

            private static readonly RaycastHit[] _hits = new RaycastHit[32];

            private void Update()
            {
                Ray ray = new Ray(transform.position, Vector3.down);

                int hitCount = Physics.RaycastNonAlloc(
                    ray,
                    _hits,
                    RAY_LENGTH,
                    ~0,
                    QueryTriggerInteraction.Collide
                );

                if (hitCount <= 0)
                    return;

                for (int i = 0; i < hitCount; i++)
                {
                    Transform hitTf = _hits[i].transform;
                    if (hitTf == null)
                        continue;

                    Transform root = BoatFinder.GetStaticFishingBoatRootFromHit(hitTf);

                    if (root == null)
                        continue;
                  
                    if (root.GetComponent<DuplicateBoatProbe>() != null)
                        continue;

                    if (root.GetComponent<ProbeMarker>() != null)
                        continue;
                  
                    if (root.GetComponent<DVBoatCloneMarker>() != null)
                        continue;
                   
                    if (!BoatFinder.IsBoatActive(root.gameObject))
                        continue;
  
                    string type = BoatFinder.GetFishingBoatType(root.name);
                    if (type == null)
                        continue;

                    bool isNew = BoatFinder.RegisterStaticBoatIfNeeded(root, type);

                    if (isNew)
                    {
                        BoatSpawner.SpawnBoatFromScan(
                            root.gameObject,
                            root.position,
                            root.rotation
                        );
                    }

                    BoatFinder.DisableStaticBoatVisualsAndColliders(root);
                    return; 
                }
            }



            private void OnWorldShift(Vector3 delta)
            {
                Vector3 before = transform.position;
                Vector3 currentMove = BoatWorldOriginWatcher.CurrentMove;
                Vector3 recomputed = CanonicalPos + currentMove;
                transform.position = recomputed;
            }
        }
    }

    internal static class BoatSpawnerProbeProfiles
    {
        internal readonly struct ProbeProfile
        {
            public readonly string Id;
            public readonly Vector3 CanonicalPosition;

            public ProbeProfile(string id, Vector3 canonicalPosition)
            {
                Id = id;
                CanonicalPosition = canonicalPosition;
            }
        }

        internal static readonly List<ProbeProfile> Profiles =
            new List<ProbeProfile>
            {
                // =========================
                // HB — Harbor Bay
                // =========================
                new ProbeProfile("HB_FishingBoat_02_01", new Vector3(13099.210f, 128.800f, 3474.755f)),
                new ProbeProfile("HB_FishingBoat_03_01", new Vector3(12956.460f, 128.800f, 3525.831f)),
                new ProbeProfile("HB_FishingBoat_02_02", new Vector3(12947.480f, 128.800f, 3534.502f)),
                new ProbeProfile("HB_FishingBoat_01_01", new Vector3(12927.650f, 128.800f, 3540.715f)),
                new ProbeProfile("HB_FishingBoat_03_02", new Vector3(13068.170f, 128.800f, 3489.493f)),
                new ProbeProfile("HB_FishingBoat_01_02", new Vector3(13363.680f, 128.800f, 3180.781f)),
                new ProbeProfile("HB_FishingBoat_02_03", new Vector3(12330.220f, 128.800f, 3301.533f)),
                new ProbeProfile("HB_FishingBoat_01_03", new Vector3(12351.920f, 128.800f, 3287.498f)),
                new ProbeProfile("HB_FishingBoat_03_03", new Vector3(13198.450f, 128.800f, 3017.250f)),
                new ProbeProfile("HB_FishingBoat_03_04", new Vector3(12364.590f, 128.800f, 3281.556f)),

                // =========================
                // CS — City South
                // =========================
                new ProbeProfile("CS_FishingBoat_01_01", new Vector3(10026.900f, 128.800f, 1192.780f)),
                new ProbeProfile("CS_FishingBoat_02_01", new Vector3(10065.980f, 128.800f, 1178.370f)),
                new ProbeProfile("CS_FishingBoat_01_02", new Vector3(9648.381f, 128.800f, 1176.460f)),
                new ProbeProfile("CS_FishingBoat_03_01", new Vector3(9622.381f, 128.800f, 1175.570f)),
                new ProbeProfile("CS_FishingBoat_02_02", new Vector3(9987.820f, 128.800f, 1192.510f)),
                new ProbeProfile("CS_FishingBoat_01_03", new Vector3(9799.280f, 128.800f, 1193.280f)),
                new ProbeProfile("CS_FishingBoat_02_03", new Vector3(9607.980f, 128.800f, 1179.370f)),
                new ProbeProfile("CS_FishingBoat_03_02", new Vector3(10086.380f, 128.800f, 1173.570f)),
            };
    }
}
