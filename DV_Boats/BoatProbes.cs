using System.Collections.Generic;
using UnityEngine;

namespace DV_Boats
{
    internal class BoatProbes : MonoBehaviour
    {
        private bool DebugEnabled => Main.Settings != null && Main.Settings.debugLogging;
 
        public Transform bow;
        public Transform stern;
        public Transform port;
        public Transform starboard;
        private Transform portFront;
        private Transform portRear;
        private Transform starboardFront;
        private Transform starboardRear;
        private const float ProbeHeight = 12f;

        private bool probeDebugged = false;
        public float probeRayStartHeight = 7f;
        public float probeRayDistance = 100f;
        public bool probeDebugDraw = false;

        // Debug visuals
        public float debugHitLingerTime = 0.6f;

        private readonly Dictionary<Transform, float> probeHitTimes =
            new Dictionary<Transform, float>();
        public float probeVisualLingerTime = 0.6f;


        private Rigidbody rb;

        public bool BowBlocked;
        public bool SternBlocked;
        public bool PortBlocked;
        public bool StarboardBlocked;

        private bool wasBlocked;

        private int blockedFrameCount = 0;
        private const int blockedFramesRequired = 2;

        [Header("Side Bounce (No Yaw)")]
        public float sideBounceStrength = 3.0f;
        public float maxBounceSpeed = 12f;
        public float forwardDampingOnBounce = 0.98f;
        public float frontYawMultiplier = 1.4f;

        private float lastSideBounceTime;
        private const float sideBounceCooldown = 0.08f;

        [Header("Scrape Audio")]
        public AudioSource scrapeSource;
        public float scrapeMaxVolume = 5f;
        public float scrapeSpeedForMax = 4f;
        public float scrapeLingerTime = 0.4f;   
        private float scrapeEndTime = 0f;

        [Header("Crash Audio")]
        public AudioSource crashSource;
        public float crashMaxVolume = 6f;
        public float crashSpeedForMax = 14f;

        private BoatController _controller;

     
        private bool _enabled;

        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;

            if (!enabled)
            {
                ClearProbeBlockState();
                blockedFrameCount = 0;
                scrapeEndTime = 0f;

                if (scrapeSource != null)
                    scrapeSource.volume = 0f;
            }
        }


        private void Awake()
        {
            rb = GetComponent<Rigidbody>();        
        }     

        private void FixedUpdate()
        {
            if (!_enabled)
                return;

            if (rb == null)
                return;

            if (bow == null || stern == null || port == null || starboard == null)
                return;
            
           float forwardSpeed = Vector3.Dot(rb.velocity, transform.forward);

            Vector3 down = Vector3.down;
            Vector3 up = Vector3.up;

            RefreshProbeVisibility();

            BowBlocked = CheckProbe(bow,down);
            SternBlocked = CheckProbe(stern,down);

            bool isAllowedToPassThroughBridges = true;


            if (isAllowedToPassThroughBridges)
            {
                bool portCenterBlockedAbove = CheckProbe(port, up);
                bool portFrontBlockedAbove = CheckProbe(portFront, up);
                bool portRearBlockedAbove = CheckProbe(portRear, up);

                bool starboardCenterBlockedAbove = CheckProbe(starboard, up);
                bool starboardFrontBlockedAbove = CheckProbe(starboardFront, up);
                bool starboardRearBlockedAbove = CheckProbe(starboardRear, up);

                bool bowBlockedAbove = CheckProbe(bow, up);
                bool sternBlockedAbove = CheckProbe(stern, up);

                UpdateProbeVisual(bow);
                UpdateProbeVisual(stern);

                UpdateProbeVisual(port);
                UpdateProbeVisual(portFront);
                UpdateProbeVisual(portRear);

                UpdateProbeVisual(starboard);
                UpdateProbeVisual(starboardFront);
                UpdateProbeVisual(starboardRear);

                bool aboveBoatBlocked = portCenterBlockedAbove || portFrontBlockedAbove || portRearBlockedAbove || starboardCenterBlockedAbove
                    || starboardFrontBlockedAbove || starboardRearBlockedAbove || bowBlockedAbove || sternBlockedAbove;

                if (aboveBoatBlocked)
                {
                    Main.Log($"[SPECIAL] DETECTED something above - returning");
                    return;
                }
            }
       
            bool portCenterBlocked = CheckProbe(port,down);
            bool portFrontBlocked = CheckProbe(portFront,down);
            bool portRearBlocked = CheckProbe(portRear,down);

            bool starboardCenterBlocked = CheckProbe(starboard,down);
            bool starboardFrontBlocked = CheckProbe(starboardFront,down);
            bool starboardRearBlocked = CheckProbe(starboardRear,down);

            UpdateProbeVisual(bow);
            UpdateProbeVisual(stern);

            UpdateProbeVisual(port);
            UpdateProbeVisual(portFront);
            UpdateProbeVisual(portRear);

            UpdateProbeVisual(starboard);
            UpdateProbeVisual(starboardFront);
            UpdateProbeVisual(starboardRear);

            PortBlocked = portCenterBlocked || portFrontBlocked || portRearBlocked;
            StarboardBlocked = starboardCenterBlocked || starboardFrontBlocked || starboardRearBlocked;

            bool sideScrape = PortBlocked || StarboardBlocked;
            bool bowScrape = BowBlocked && forwardSpeed < -0.1f;
            bool sternScrape = SternBlocked && forwardSpeed > 0.1f;

            bool scrapeContact = sideScrape || bowScrape || sternScrape;

            if (scrapeContact)
                scrapeEndTime = Time.time + scrapeLingerTime;

            bool scraping = Time.time < scrapeEndTime;          
            
            if (scrapeSource != null && scrapeSource.clip != null)
            {
                if (scraping)
                {
                    float speed = rb.velocity.magnitude;
                    
                    scrapeSource.volume =
                        Mathf.Clamp01(speed / scrapeSpeedForMax)
                        * scrapeMaxVolume
                        * Main.Settings.masterVolume
                        * Main.Settings.effectsVolume;
                }
                else
                {
                    scrapeSource.volume = 0f;
                }
            }

            if (Time.time - lastSideBounceTime > sideBounceCooldown)
            {
                if (PortBlocked && !StarboardBlocked)
                {
                    if (portFrontBlocked)
                        ApplySideBounce(Vector3.right, portFront);
                    else if (portCenterBlocked)
                        ApplySideBounce(Vector3.right, port);
                    else if (portRearBlocked)
                        ApplySideBounce(Vector3.right, portRear);

                    lastSideBounceTime = Time.time;
                }
                else if (StarboardBlocked && !PortBlocked)
                {
                    if (starboardFrontBlocked)
                        ApplySideBounce(Vector3.left, starboardFront);
                    else if (starboardCenterBlocked)
                        ApplySideBounce(Vector3.left, starboard);
                    else if (starboardRearBlocked)
                        ApplySideBounce(Vector3.left, starboardRear);

                    lastSideBounceTime = Time.time;
                }
            }

            bool bowStop = BowBlocked && forwardSpeed > 0.1f;
            bool sternStop = SternBlocked && forwardSpeed < -0.1f;

            //==================== V1.1 MAKE GLITCH RESILIENT ====================
            bool rawBlocked = bowStop || sternStop;

            if (rawBlocked)
            {
                blockedFrameCount++;
            }
            else
            {
                blockedFrameCount = 0;
            }

            bool nowBlocked = blockedFrameCount >= blockedFramesRequired;
            //====================================================================

            if (nowBlocked && !wasBlocked)
            {
                float impactSpeed = Mathf.Abs(forwardSpeed);
                var activeBoat = BoatDriveManager.ActiveBoat;
                int speed = Mathf.RoundToInt(activeBoat.CurrentSpeedKmh);

                float vol =
                    Mathf.Clamp01(impactSpeed / crashSpeedForMax)
                    * crashMaxVolume
                    * Main.Settings.masterVolume
                    * Main.Settings.effectsVolume;

                if (crashSource != null && crashSource.clip != null)
                    crashSource.PlayOneShot(crashSource.clip, vol);


                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.Sleep();

                var controller = GetComponent<BoatController>();
                if (controller != null)
                    controller.ForceIdle();

                if (controller != null && speed >= 22f)
                {
                    Main.Log("[CRASH] Terminal Speed - now sinking");
                    controller.TryEnterSinking(speed);
                }
                else
                {
                    Main.Log($"[CRASH] impact sppeed = {speed} km/h");
                }
            }

            wasBlocked = nowBlocked;
        }

        private void UpdateProbeVisual(Transform probe)
        {
            if (probe == null)
                return;

            if (!DebugEnabled)
                return;

            var renderer = probe.GetComponent<Renderer>();
            if (renderer == null)
                return;

            bool showRed =
                probeHitTimes.TryGetValue(probe, out float until) &&
                Time.time < until;

            renderer.enabled = true;
            renderer.material.color = showRed ? Color.red : Color.green;
        }

        private Transform CreateProbe(string name, Vector3 localOffset)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;

            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffset + Vector3.up * ProbeHeight;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);

            var renderer = go.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.magenta;
            renderer.enabled = DebugEnabled;
            Main.Log($"[ProbeCreate] {name} renderer.enabled = {renderer.enabled}",false);

            Destroy(go.GetComponent<Collider>());

            return go.transform;
        }

        public void CreateProbesFromProfile()
        {
            BoatController controller = GetComponentInParent<BoatController>();
            if (controller == null)
            {
                Main.Log("[BoatProbes] ❌ No BoatController found");
                return;
            }

            string boatId = controller.BoatTypeId;
            if (string.IsNullOrEmpty(boatId))
            {
                Main.Log("[BoatProbes] ❌ BoatTypeId not set");
                return;
            }

            if (!BoatStructuralProfiles.Profiles.TryGetValue(boatId, out var profile))
            {
                Main.Log($"[BoatProbes] ❌ No structure profile for boatId={boatId}");
                return;
            }

            ProbeLayout p = profile.Probes;

            bow = CreateProbe("Probe_Bow", p.Bow);
            stern = CreateProbe("Probe_Stern", p.Stern);

            port = CreateProbe("Probe_Port", p.Port);
            starboard = CreateProbe("Probe_Starboard", p.Starboard);

            portFront = CreateProbe("Probe_Port_Front", p.PortFront);
            portRear = CreateProbe("Probe_Port_Rear", p.PortRear);

            starboardFront = CreateProbe("Probe_Starboard_Front", p.StarboardFront);
            starboardRear = CreateProbe("Probe_Starboard_Rear", p.StarboardRear);
        }


        private bool IsProbeBlocked(Transform probe, Vector3 dir)
        {
            if(Main.Settings.disableBoatProbes)
                return false;

            Vector3 origin = probe.position + Vector3.up * probeRayStartHeight;
            if (!probeDebugged)
            {
                Main.Log($"[PROBE.POSITION] = {probe.position}");
                Main.Log($"[PROBE_ORIGIN] = {origin}");
                probeDebugged = true;
            }

            if (probeDebugDraw)
                Debug.DrawRay(origin, dir * probeRayDistance, Color.magenta);

            RaycastHit[] hits = Physics.RaycastAll(origin, dir, probeRayDistance);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            if (dir == Vector3.down)
            {
                bool sawWater = false;

                foreach (var hit in hits)
                {
                    if (hit.collider == null)
                        continue;

                    if (hit.collider.gameObject.name.StartsWith("DV_BEACHBALL_"))
                        continue;

                    if (hit.collider.transform.IsChildOf(transform))
                        continue;

                     if (hit.collider.GetComponentInParent<ItemBuoyancyEnabler>() != null)
                    {
                        sawWater = true;
                        continue;
                    }

                    if (hit.collider.isTrigger)
                        continue;

                    if (!sawWater)
                        return true;

                }

                return false;
            }
            else
            {
                int hitCount = 0;
                bool needToDuck = false;

                foreach (var hit in hits)
                {
                    if (hit.collider == null)
                        continue;

                    if (hit.collider.gameObject.name.StartsWith("DV_BEACHBALL_"))
                        continue;

                    hitCount++;           
                }
                if (hitCount > 0)
                {
                    Main.Log("[SPECIAL] Something above us");
                    return true;
                }
                else
                {
                    return false;
                }
                   
            }
        }
        
        private void ApplySideBounce(Vector3 localSideDir, Transform probe)
        {
            if (rb == null || probe == null)
                return;

            float now = Time.time;
            if (now - lastSideBounceTime < sideBounceCooldown)
                return;

            float speed = rb.velocity.magnitude;
            if (speed < 0.2f)
                return;

            lastSideBounceTime = now;

            float speedFactor = Mathf.Clamp01(speed / maxBounceSpeed);

            Vector3 worldDir = transform.TransformDirection(localSideDir);

            rb.AddForce(
                worldDir * sideBounceStrength * speedFactor,
                ForceMode.VelocityChange
            );

            float zOffset = transform.InverseTransformPoint(probe.position).z;
        
            if (Mathf.Abs(zOffset) < 0.1f)
                return;

 
            float sideSign = Mathf.Sign(localSideDir.x);

            float frontRearSign = Mathf.Sign(zOffset);

            float yawSign = sideSign * frontRearSign;

            float yawStrength =
                sideBounceStrength *
                speedFactor *
                (frontRearSign > 0f ? frontYawMultiplier : 1f);

            rb.AddRelativeTorque(
                Vector3.up * yawSign * yawStrength,
                ForceMode.VelocityChange
            );

            rb.velocity *= forwardDampingOnBounce;
        }

        public void ClearProbeBlockState()
        {
            BowBlocked = false;
            SternBlocked = false;
            PortBlocked = false;
            StarboardBlocked = false;
            wasBlocked = false;
        }

        private void RefreshProbeVisibility()
        {
            bool visible = Main.Settings != null && Main.Settings.debugLogging;

            //DEBUG
            visible = false;

            SetProbeRenderer(bow, visible);
            SetProbeRenderer(stern, visible);
            SetProbeRenderer(port, visible);
            SetProbeRenderer(starboard, visible);

            SetProbeRenderer(portFront, visible);
            SetProbeRenderer(portRear, visible);
            SetProbeRenderer(starboardFront, visible);
            SetProbeRenderer(starboardRear, visible);
        }

        private void SetProbeRenderer(Transform probe, bool visible)
        {
            if (probe == null)
                return;

            var r = probe.GetComponent<Renderer>();
            if (r != null)
                r.enabled = visible;
        }


        private bool CheckProbe(Transform probe, Vector3 dir)
        {
            if (probe == null)
                return false;

            bool blocked = IsProbeBlocked(probe,dir);
            if (dir == Vector3.up && blocked)
                Main.Log("[SPECIAL] Returning true for up blocked");

            if (blocked)
                probeHitTimes[probe] = Time.time + probeVisualLingerTime; 

            return blocked;
        }

    }
}

