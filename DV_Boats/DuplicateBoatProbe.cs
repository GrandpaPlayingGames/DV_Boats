using System.Collections.Generic;
using UnityEngine;
using static DV_Boats.BoatFinder;

namespace DV_Boats
{
    internal class DuplicateBoatProbe : MonoBehaviour
    {
        public StaticBoatRegistrationKey Key;

        public float rayLength = 50f;
        public LayerMask mask = ~0; // everything

        private readonly HashSet<Transform> hitsThisFrame =
            new HashSet<Transform>();

        private static float _lastPopupTime;
        private const float POPUP_COOLDOWN = 3f;

        private bool duplicateAlertActive = false;

        public Vector3 ProbeWorldPos;

        private void Awake()
        {
            BoatWorldShiftManager.WOSDeltaAdjustment += ApplyWorldShift;
        }

        private void OnDestroy()
        {
            BoatWorldShiftManager.WOSDeltaAdjustment -= ApplyWorldShift;
        }

        void Update()
        {
            hitsThisFrame.Clear();

            Vector3 origin = transform.position;
            Ray ray = new Ray(origin, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(
                ray,
                rayLength,
                mask,
                QueryTriggerInteraction.Ignore
            );

            for (int i = 0; i < hits.Length; i++)
            {
                Transform t = hits[i].transform;
                if (t == null)
                    continue;

                Transform root = FindBoatRoot(t);
                if (root == null)
                    continue;

                hitsThisFrame.Add(root);
            }

            if (hitsThisFrame.Count > 1)
            {
                if (!duplicateAlertActive)
                {
                    duplicateAlertActive = true;
                    ReportDuplicate();
                }
            }
            else
            {
                  duplicateAlertActive = false;
            }

        }
       
        private Transform FindBoatRoot(Transform t)
        {
            if (t == null)
                return null;

            BoatController bc = t.GetComponentInParent<BoatController>();
            if (bc != null)
                return bc.transform;

            return null;
        }

        private void ReportDuplicate()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendLine("🚨 DUPLICATE BOAT DETECTED 🚨");
            sb.AppendLine($"Key: {Key}");
            sb.AppendLine($"Probe pos: {transform.position}");
            sb.AppendLine($"Hit count: {hitsThisFrame.Count}");
            sb.AppendLine();

            foreach (var r in hitsThisFrame)
            {
                if (r == null) continue;
                sb.AppendLine($" - {r.name} @ {r.position}");
            }

            string msg = sb.ToString();
            Main.Log(msg);

            // 🔔 POPUP ALERT
            UIHelpers.ShowDialog(
                title: "Duplicate Boat Detected",
                message: msg,
                onYes: null,
                onNo: null,
                scale: 1.1f
            );
        }  

        private static GameObject CreateProbeVisual()
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                bool visible = Main.Settings != null && Main.Settings.debugLogging;

                r.enabled = visible;

                if (visible)
                {
                    r.material = new Material(Shader.Find("Standard"));
                    r.material.color = Color.yellow;
                    r.material.EnableKeyword("_EMISSION");
                    r.material.SetColor("_EmissionColor", Color.yellow * 0.5f);
                }
            }

            Object.Destroy(go.GetComponent<Collider>()); // visual only
            return go;
        }

        public static class BoatDuplicateProbeManager
        {
            private static readonly Dictionary<
                StaticBoatRegistrationKey,
                DuplicateBoatProbe
            > probes = new Dictionary<StaticBoatRegistrationKey, DuplicateBoatProbe>();

            public static void clearProbes()
            {
                probes.Clear();
            }
       
            public static void EnsureProbe(
                    StaticBoatRegistrationKey key,
                    Vector3 spawnPos
                )
            {
                if (probes.ContainsKey(key))
                    return;

                GameObject go = CreateProbeVisual();
                go.name = $"BoatDuplicateProbe_{key}";

                var probe = go.AddComponent<DuplicateBoatProbe>();
                probe.Key = key;                
                probe.ProbeWorldPos = spawnPos;

                Vector3 pos = spawnPos;
                pos.y = BoatSpawner.WATER_Y + 20f;
                go.transform.position = pos;


                Vector3 p = go.transform.position;
                Vector3 canonical = p - BoatWorldOriginWatcher.CurrentMove;

                Main.Log(
                    $"[ProbeProfile] {key} = new Vector3({canonical.x:F3}f, {canonical.y:F3}f, {canonical.z:F3}f);"
                );

                probes.Add(key, probe);

                Main.Log($"[Probe] Created presence probe for {key}");
            }
        }

        public void ApplyWorldShift(Vector3 delta)
        {
            ProbeWorldPos += delta;
            transform.position += delta;
        }
    }
}
