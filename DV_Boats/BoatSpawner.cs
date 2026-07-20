using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace DV_Boats
{
    internal static class BoatSpawner
    {
        private static GameObject currentClone;

        public static readonly Dictionary<int, GameObject> cachedBoatSources
          = new Dictionary<int, GameObject>();

        public const float WATER_Y = 108.8f;

        private static bool pendingBoatUnlockTravel = false;

        static Vector3 pendingReturnLocalPos;
        static Vector3 pendingReturnWorldMove;
        static Quaternion pendingReturnRot;
        static bool pendingReturnValid;
      
        public static bool pendingReturnDialogScheduled = false;
        public static event Action RadioReturnOfferReady;

        private static MonoBehaviour _runner;
        internal static class BoatDebug
        {
            internal const bool ShowNavLightEmitters = false; 
        }

        private static void ___________SPAWN___________()
        {
        }

        private static void SpawnBoatInternal(
            GameObject sourceBoat,
            Vector3 spawnPos,
            Quaternion spawnRot,
            string reason
        )
        {

            if (sourceBoat != null && !sourceBoat.activeSelf)
                sourceBoat.SetActive(true);

            currentClone = UnityEngine.Object.Instantiate(sourceBoat, spawnPos, spawnRot);
            currentClone.name = sourceBoat.name + "_Clone";
            currentClone.SetActive(true);
            
            Main.Log(
                $"BoatSpawner: ✅ Spawned ({reason}) '{currentClone.name}' @ " +
                $"({spawnPos.x:F1}, {spawnPos.y:F1}, {spawnPos.z:F1})"
            );

            SetupSpawnedBoat(sourceBoat);
        }

        private static void SetupSpawnedBoat(GameObject sourceBoat)
        {
            if (currentClone == null)
            {
                Main.Log("[BoatSpawner] ❌ SetupSpawnedBoat called with null currentClone");
                return;
            }

            BoatPhysics.SetupRigidbody(currentClone);

            if (currentClone.GetComponent<BoatProbes>() == null)
                currentClone.AddComponent<BoatProbes>();

            BoatController controller = currentClone.GetComponent<BoatController>();
            if (controller == null)
                controller = currentClone.AddComponent<BoatController>();

            string boatTypeId = NormalizeBoatName(sourceBoat.name);
            controller.SetBoatTypeId(boatTypeId);
            controller.SetPassive(); 

            BoatProbes probes = currentClone.GetComponent<BoatProbes>();
            if (probes != null)
            {
                probes.CreateProbesFromProfile();
            }

            controller.engineSource = currentClone.AddComponent<AudioSource>();
            controller.engineSource.loop = true;
            controller.engineSource.playOnAwake = false;
            controller.engineSource.spatialBlend = 1f;
            controller.engineSource.dopplerLevel = 0f;
            controller.engineSource.rolloffMode = AudioRolloffMode.Linear;
            controller.engineSource.maxDistance = 240f;

            string modDir = Path.GetDirectoryName(
                Assembly.GetExecutingAssembly().Location
            );

            string enginePath = AudioLoader.AudioPath("engine_loop.mp3");
            currentClone.GetComponent<MonoBehaviour>().StartCoroutine(
                AudioLoader.LoadMp3(enginePath, clip =>
                {
                    if (clip == null)
                    {
                        Main.Log("BoatSpawner: ❌ Engine clip failed to load");
                        return;
                    }

                    controller.engineSource.clip = clip;
                    controller.engineSource.pitch = controller.engineMinPitch;

                    if (controller.Mode == BoatMode.Driving)
                    {
                        controller.engineSource.Play();
                        Main.Log("[BoatSpawner] Engine started (DRIVING)");
                    }
                    else
                    {
                        controller.engineSource.volume = 0f;
                    }
                })
            );

            probes.scrapeSource = currentClone.AddComponent<AudioSource>();
            probes.scrapeSource.playOnAwake = false;
            probes.scrapeSource.loop = true;

            probes.scrapeSource.rolloffMode = AudioRolloffMode.Linear;
            probes.scrapeSource.minDistance = 5f;     
            probes.scrapeSource.maxDistance = 80f;    
            probes.scrapeSource.dopplerLevel = 0f;
            probes.scrapeSource.volume = 0f;
            probes.scrapeSource.pitch = 1f;
            probes.scrapeSource.spatialBlend = 1f;

            string scrapePath = AudioLoader.AudioPath("scrape_loop.mp3");
            currentClone.GetComponent<MonoBehaviour>().StartCoroutine(
                AudioLoader.LoadMp3(scrapePath, clip =>
                {
                    if (clip == null)
                    {
                        Main.Log("[SCRAPE] ❌ Scrape clip FAILED to load");
                        return;
                    }

                    probes.scrapeSource.clip = clip;
                    probes.scrapeSource.Play();
                })
            );

            probes.crashSource = currentClone.AddComponent<AudioSource>();
            probes.crashSource.playOnAwake = false;
            probes.crashSource.loop = false;

            probes.crashSource.spatialBlend = 1f;
            probes.crashSource.rolloffMode = AudioRolloffMode.Linear;
            probes.crashSource.minDistance = 8f;
            probes.crashSource.maxDistance = 220f;
            probes.crashSource.dopplerLevel = 0f;

            string crashPath = AudioLoader.AudioPath("crash.mp3");

            currentClone.GetComponent<MonoBehaviour>().StartCoroutine(
                AudioLoader.LoadMp3(crashPath, clip =>
                {
                    if (clip == null)
                    {
                        Main.Log("[CRASH] ❌ Crash clip FAILED to load");
                        return;
                    }

                    probes.crashSource.clip = clip;
                })
            );

            controller.hornSource = currentClone.AddComponent<AudioSource>();
            controller.hornSource.playOnAwake = false;
            controller.hornSource.loop = false;
            controller.hornSource.spatialBlend = 1f;
            controller.hornSource.rolloffMode = AudioRolloffMode.Linear;
            controller.hornSource.minDistance = 15f;
            controller.hornSource.maxDistance = 400f;
            controller.hornSource.dopplerLevel = 0f;

            controller.foghornSource = currentClone.AddComponent<AudioSource>();
            controller.foghornSource.playOnAwake = false;
            controller.foghornSource.loop = false;
            controller.foghornSource.spatialBlend = 1f;
            controller.foghornSource.rolloffMode = AudioRolloffMode.Linear;
            controller.foghornSource.minDistance = 25f;
            controller.foghornSource.maxDistance = 600f;
            controller.foghornSource.dopplerLevel = 0f;

            string hornPath = AudioLoader.AudioPath("horn.mp3");
            string foghornPath = AudioLoader.AudioPath("foghorn.mp3");

            currentClone.GetComponent<MonoBehaviour>().StartCoroutine(
                AudioLoader.LoadMp3(hornPath, clip =>
                {
                    if (clip == null)
                    {
                        Main.Log("[HORN] ❌ horn.mp3 failed to load");
                        return;
                    }

                    controller.hornSource.clip = clip;
                 })
            );

            currentClone.GetComponent<MonoBehaviour>().StartCoroutine(
                AudioLoader.LoadMp3(foghornPath, clip =>
                {
                    if (clip == null)
                    {
                        Main.Log("[FOGHORN] ❌ foghorn.mp3 failed to load");
                        return;
                    }

                    controller.foghornSource.clip = clip;
                 })
            );

            if (!BoatStructuralProfiles.Profiles.TryGetValue(
                    controller.BoatTypeId, out var dlstructure) ||
                dlstructure.DeckLight == null)
            {
                Main.Log($"[BoatSpawner] ⚠ No DeckLight profile for boatId={controller.BoatTypeId}");
            }
            else
            {
                DeckLightLayout layout = dlstructure.DeckLight;

                GameObject deckLightObj = new GameObject("DeckLight");
                deckLightObj.transform.SetParent(currentClone.transform, false);
                deckLightObj.transform.localPosition = layout.Position;

                Vector3 dir = layout.Direction.sqrMagnitude > 0.001f
                    ? layout.Direction.normalized
                    : Vector3.down;

                deckLightObj.transform.localRotation =
                    Quaternion.LookRotation(dir, Vector3.forward);

                Light deckLight = deckLightObj.AddComponent<Light>();
                deckLight.type = LightType.Spot;
                deckLight.range = 30f;
                deckLight.spotAngle = 80f;
                deckLight.intensity = 3.0f;
                deckLight.color = new Color(1f, 0.95f, 0.85f); // warm
                deckLight.enabled = false;
                deckLight.shadows = LightShadows.None;

                controller.deckLight = deckLight;
            }

            if (!BoatStructuralProfiles.Profiles.TryGetValue(controller.BoatTypeId, out var structure))
            {
                Main.Log($"[BoatSpawner] ❌ No structural profile for {controller.BoatTypeId}, nav lights skipped");
            }
            else
            {
                NavLightLayout nav = structure.NavLights;

                GameObject portLightObj = new GameObject("NavLight_Port");
                portLightObj.transform.SetParent(currentClone.transform, false);
                portLightObj.transform.localPosition = nav.Port;

                Light portLight = portLightObj.AddComponent<Light>();
                portLight.type = LightType.Point;
                portLight.color = Color.red;
                portLight.range = 3.5f;
                portLight.intensity = 1.2f;
                portLight.enabled = false;
                portLight.shadows = LightShadows.None;

                controller.navBulbPort = CreateNavLightBulb(portLight, Color.red);
                controller.navLightPort = portLight;

                if (BoatDebug.ShowNavLightEmitters)
                    portLightObj.AddComponent<MeshRenderer>().material.color = Color.red;

                GameObject starLightObj = new GameObject("NavLight_Starboard");
                starLightObj.transform.SetParent(currentClone.transform, false);
                starLightObj.transform.localPosition = nav.Starboard;

                Light starLight = starLightObj.AddComponent<Light>();
                starLight.type = LightType.Point;
                starLight.color = Color.green;
                starLight.range = 3.5f;
                starLight.intensity = 1.2f;
                starLight.enabled = false;
                starLight.shadows = LightShadows.None;

                controller.navBulbStarboard = CreateNavLightBulb(starLight, Color.green);
                controller.navLightStarboard = starLight;

                GameObject mastLightObj = new GameObject("NavLight_Mast");
                mastLightObj.transform.SetParent(currentClone.transform, false);
                mastLightObj.transform.localPosition = nav.Mast;

                Light mastLight = mastLightObj.AddComponent<Light>();
                mastLight.type = LightType.Point;
                mastLight.color = Color.white;
                mastLight.range = 8f;
                mastLight.intensity = 2.0f;
                mastLight.enabled = false;
                mastLight.shadows = LightShadows.None;

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                visual.name = "NavLight_Mast_Visual";
                visual.transform.SetParent(mastLightObj.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localScale = Vector3.one * 0.15f;
                UnityEngine.Object.Destroy(visual.GetComponent<Collider>());

                var r = visual.GetComponent<Renderer>();
                var mat = new Material(Shader.Find("Standard"));
                mat.color = Color.white;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white * 3.0f);
                r.material = mat;

                mastLightObj.SetActive(false);

                controller.navLightMast = mastLight;
                controller.navLightMastObj = mastLightObj;
            }

            // --------------------------------------------------
            if (controller == null)
                controller = currentClone.AddComponent<BoatController>();

            if (structure.SpotLight == null)
            {
                Main.Log($"[BoatSpawner] ⚠ No SpotLight profile for boatId={controller.BoatTypeId}");
            }
            else
            {
                GameObject spotRoot;
                Transform swivelPivot;
                GameObject lensOn;
                GameObject lensOff;
                GameObject housing;

                Light spot = CreateBoatSpotLight(
                    currentClone,
                    structure.SpotLight,
                    out spotRoot,
                    out swivelPivot,
                    out lensOn,
                    out lensOff,
                    out housing
                );

                controller.SetSpotLight(spot);
                controller.spotLightRoot = spotRoot;
                controller.spotLightSwivelPivot = swivelPivot;
                controller.spotLightHousing = housing;
                controller.spotLightLensOn = lensOn;
                controller.spotLightLensOff = lensOff;

            }

            currentClone.gameObject.AddComponent<DVBoatCloneMarker>();
            controller.SetPassive();
        }

        private static bool suppressSpawnFromScan = false;


        public static void SpawnBoatFromScan(GameObject staticBoat, Vector3 worldPos, Quaternion worldRot)
        {
            if (staticBoat == null)
            {
                Main.Log("BoatSpawner: ❌ Scan spawn failed — staticBoat null");
                return;
            }

            if (!TryGetBoatType(staticBoat, out int boatType))
            {
                Main.Log($"BoatSpawner: ❌ Scan spawn failed — unknown boat type '{staticBoat.name}'");
                return;
            }

            GameObject sourceBoat = null;

            if (!cachedBoatSources.TryGetValue(boatType, out sourceBoat) || sourceBoat == null)
            {

                if (IsLodVariant(staticBoat))
                {
                    Main.Log($"BoatSpawner: ❌ Scan spawn failed — static LOD '{staticBoat.name}'");
                    return;
                }

                GameObject archetype = CreateArchetypeFrom(staticBoat);
                if (archetype == null)
                {
                    Main.Log($"BoatSpawner: ❌ Scan spawn failed — failed to create archetype from '{staticBoat.name}'");
                    return;
                }

                cachedBoatSources[boatType] = archetype;
                sourceBoat = archetype;

                Main.Log($"BoatSpawner: 📌 Cached FishingBoat_{boatType:00} archetype from scan '{staticBoat.name}'");
            }
  
            if (!pendingReturnDialogScheduled)
            {
                SpawnBoatFromRadio_ArmReturnOffer();
            }

            if (!suppressSpawnFromScan)
            {
                    SpawnBoatInternal(
                    sourceBoat: sourceBoat,
                    spawnPos: worldPos,
                    spawnRot: worldRot,
                    reason: "Scan"
                );
            }
        }

        public static void SpawnBoatFromRadio_TeleportToHarbour(bool spawnClones)
        {
            Transform player = PlayerManager.PlayerTransform;

            if (player == null)
            {
                Main.Log("[BoatRadio] ❌ PlayerTransform null");
                return;
            }

            pendingReturnLocalPos = player.position - WorldMover.currentMove;
            pendingReturnWorldMove = WorldMover.currentMove;
            pendingReturnRot = player.rotation;
            pendingReturnValid = true;

            pendingBoatUnlockTravel = true;
            pendingReturnDialogScheduled = false;

            TeleportPlayerToHarbour(spawnClones);
        }

        
        public static void SpawnBoatFromRadio_ArmReturnOffer()
        {
            if (!pendingReturnValid)
            {
                Main.Log("[BoatRadio] ❌ No valid return stored");
                return;
            }

            pendingReturnDialogScheduled = true;
            

            Main.Log("[BoatRadio] 🔁 Return offer armed");

            RadioReturnOfferReady?.Invoke();
        }
  
        public static void SpawnBoatFromRadio_ReturnTeleport()
        {
            if (!pendingReturnValid)
            {
                Main.Log("[BoatRadio] ❌ Return teleport requested but invalid");
                return;
            }

            Vector3 worldPos = pendingReturnLocalPos + WorldMover.currentMove;

            PlayerManager.TeleportPlayer(
                worldPos,
                pendingReturnRot,
                null,
                true,
                false
            );

            pendingReturnValid = false;
            pendingBoatUnlockTravel = false;
            pendingReturnDialogScheduled = false;
        }


        public static void SpawnBoatFromRadio_BeamHit(
            Vector3 hitPoint,
            Vector3 beamForward,
            int selectedBoatType)
        {
            if (!cachedBoatSources.TryGetValue(selectedBoatType, out GameObject sourceBoat))
            {
                Main.Log($"[BoatRadio] ❌ No cached boat for type {selectedBoatType}");
                return;
            }

            Vector3 forwardFlat = beamForward;
            forwardFlat.y = 0f;
            forwardFlat.Normalize();

            Vector3 spawnPos = hitPoint;
            spawnPos.y = WATER_Y;

            Quaternion spawnRot = Quaternion.LookRotation(forwardFlat, Vector3.up);

            if (!IsSpawnPosOverWater(spawnPos))
            {
                Main.Log("[BoatRadio] ❌ Beam spawn blocked (not over water)");
                return;
            }

            SpawnBoatInternal(
                sourceBoat: sourceBoat,
                spawnPos: spawnPos,
                spawnRot: spawnRot,
                reason: "Radio"
            );
        }


        public static void DestroyClone()
        {
            if (currentClone != null)
            {
                UnityEngine.Object.Destroy(currentClone);
                currentClone = null;
                Main.Log("BoatSpawner: 🧹 Clone destroyed");
            }
        }

        private static void ___________LODs___________()
        {
        }

        public static bool IsLodVariant(GameObject go)
        {
            if (go == null)
                return false;

            string name = NormalizeBoatName(go.name);
            return name.Contains("_LOD");
        }

        private static void ___________LIGHTS___________()
        {
        }
        private static MeshRenderer CreateNavLightBulb(Light light, Color color)
        {
            GameObject bulb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bulb.name = "NavLightBulb";
            bulb.transform.SetParent(light.transform, false);
            bulb.transform.localPosition = Vector3.zero;
            bulb.transform.localScale = Vector3.one * 0.15f;

            UnityEngine.Object.Destroy(bulb.GetComponent<Collider>());

            var mr = bulb.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Standard"));

            mat.color = color;
            mat.DisableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
            mr.material = mat;
            mr.enabled = false;
            bulb.SetActive(false);

            return mr;
        }
        private static Light CreateBoatSpotLight(
            GameObject boat,
            SpotLightLayout layout,
            out GameObject spotRoot,
            out Transform swivelPivot,
            out GameObject lensOn,
            out GameObject lensOff,
            out GameObject housing
        )
        {           
            spotRoot = new GameObject("BoatSpotLight");
            spotRoot.transform.SetParent(boat.transform, false);

            Vector3 pos = layout.Position;
            pos.y += 2.5f;
            pos.z += 1.5f;
            spotRoot.transform.localPosition = pos;
            spotRoot.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

            GameObject pivotGO = new GameObject("SpotLight_SwivelPivot");
            pivotGO.transform.SetParent(spotRoot.transform, false);
            pivotGO.transform.localPosition = Vector3.zero;
            pivotGO.transform.localRotation = Quaternion.identity;
            swivelPivot = pivotGO.transform;

            GameObject visualOffset = new GameObject("SpotLight_VisualOffset");
            visualOffset.transform.SetParent(swivelPivot, false);
            visualOffset.transform.localPosition = new Vector3(0f, 0f, -0.18f);
            visualOffset.transform.localRotation = Quaternion.identity;

 
            housing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            housing.name = "SpotLight_Housing";
            housing.transform.SetParent(visualOffset.transform, false);
            housing.transform.localPosition = Vector3.zero;
            housing.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            housing.transform.localScale = new Vector3(0.35f, 0.12f, 0.35f);
            UnityEngine.Object.Destroy(housing.GetComponent<Collider>());

            {
                var r = housing.GetComponent<MeshRenderer>();
                var m = new Material(Shader.Find("Standard"));
                m.color = new Color(0.18f, 0.18f, 0.18f);
                m.SetFloat("_Glossiness", 0.25f);
                r.material = m;
            }

            Light l = visualOffset.AddComponent<Light>();
            l.type = LightType.Spot;
            l.range = 120f;
            l.spotAngle = 65f;
            l.intensity = 8.0f;
            l.color = new Color(1.0f, 0.95f, 0.85f);
            l.shadows = LightShadows.None;
            l.renderMode = LightRenderMode.ForcePixel;
            l.enabled = false;

            lensOff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lensOff.name = "SpotLight_Lens_OFF";
            lensOff.transform.SetParent(visualOffset.transform, false);
            lensOff.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            lensOff.transform.localScale = new Vector3(0.32f, 0.32f, 0.04f);
            UnityEngine.Object.Destroy(lensOff.GetComponent<Collider>());

            {
                var r = lensOff.GetComponent<MeshRenderer>();
                var m = new Material(Shader.Find("Standard"));
                m.color = new Color(0.15f, 0.15f, 0.15f);
                m.SetFloat("_Glossiness", 0.85f);
                m.SetFloat("_Metallic", 0.0f);
                r.material = m;
            }

            lensOn = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            lensOn.name = "SpotLight_Lens_ON";
            lensOn.transform.SetParent(visualOffset.transform, false);
            lensOn.transform.localPosition = new Vector3(0f, 0f, -0.06f);
            lensOn.transform.localScale = new Vector3(0.32f, 0.32f, 0.04f);

            UnityEngine.Object.Destroy(lensOn.GetComponent<Collider>());
            {
                var r = lensOn.GetComponent<MeshRenderer>();
                var m = new Material(Shader.Find("Unlit/Color"));
                m.color = new Color(1.0f, 0.95f, 0.8f, 1.0f);
                r.material = m;
            }

            lensOn.SetActive(false);
            lensOff.SetActive(true);
  
            GameObject pivot = new GameObject("SpotLight_SwivelPivot");

            pivot.transform.SetParent(spotRoot.transform, false);

            var hr = housing.GetComponent<Renderer>();
            Vector3 worldCenter = hr.bounds.center;

           
            pivot.transform.localPosition =
                spotRoot.transform.InverseTransformPoint(worldCenter);

            pivot.transform.localRotation = Quaternion.identity;
           
            housing.transform.SetParent(pivot.transform, true);
            lensOn.transform.SetParent(pivot.transform, true);
            lensOff.transform.SetParent(pivot.transform, true);
            l.transform.SetParent(pivot.transform, true);

            // OUTPUT
            swivelPivot = pivot.transform;
 
            const float HOUSING_HALF_LENGTH = 0.12f;  
            const float LENS_FORWARD_OFFSET = 0.002f;  

            float lensZ = HOUSING_HALF_LENGTH + LENS_FORWARD_OFFSET;

            lensOn.transform.localPosition = new Vector3(0f, 0f, lensZ);
            lensOff.transform.localPosition = new Vector3(0f, 0f, lensZ);

            lensOn.transform.localRotation = Quaternion.identity;
            lensOff.transform.localRotation = Quaternion.identity;

            return l;
        }

        private static void ___________ARCHETYPES____________________()
        {
        }

        private static GameObject CreateArchetypeFrom(GameObject staticBoat)
        {
            if (staticBoat == null)
                return null;

            GameObject archetype = UnityEngine.Object.Instantiate(staticBoat);

            string canonicalName = NormalizeBoatName(staticBoat.name);
            archetype.name = canonicalName;

            archetype.SetActive(false);
            return archetype;
        }

        private static void ___________TELEPORT___________()
        {
        }

        private static void TeleportPlayerToHarbour(bool spawnClones)
        {
            foreach (var dest in DV.Teleporters.FastTravelDestination.ActiveDestinations)
            {
                if (dest == null)
                    continue;

                if (dest.MarkerName != null && dest.MarkerName.Contains("Harbor"))
                {
                    dest.TeleportPlayer();
  
                    if (!spawnClones)
                    {
                        DVBoatsRunner.Instance.StartCoroutine(
                            RunCacheOnlyScanAfterArrival()
                        );

                    }

                    return;
                }
            }

            Main.Log("[BoatSpawner] ❌ No harbour fast travel destination found");
        }

        private static void EnsureRunner()
        {
            if (_runner != null)
                return;

            GameObject go = GameObject.Find("DV_Boats_CoroutineRunner");
            if (go == null)
            {
                go = new GameObject("DV_Boats_CoroutineRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            _runner = go.GetComponent<MonoBehaviour>();
            if (_runner == null)
                _runner = go.AddComponent<DVBoatsCoroutineHost>();
        }

        private class DVBoatsCoroutineHost : MonoBehaviour { }

        private static IEnumerator ShowReturnDialogAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            UIHelpers.ShowDialog(
                title: "Boats Unlocked",
                message:
                    "You may now spawn boats anywhere (on water) in this session.\n\n" +
                    "Return to your previous location?",
                onYes: () =>
                {
                    if (!pendingReturnValid)
                    {
                        Main.Log("[BoatUnlock] ❌ Return requested but no valid stored position");
                        return;
                    }

                    Vector3 returnWorldPos =
                        pendingReturnLocalPos + WorldMover.currentMove;

                    PlayerManager.TeleportPlayer(
                        returnWorldPos,
                        pendingReturnRot,
                        null,
                        true,
                        false
                    );

                    pendingReturnValid = false;
                },
                onNo: () =>
                {
                    pendingReturnValid = false;
                }
            );
        }

        private static IEnumerator RunCacheOnlyScanAfterArrival()
        {
            yield return new WaitForSecondsRealtime(1.5f);

            Main.Log($"[BoatSpawner] Waited 15 seconds before running - spawnOnly active — running cache-only archetype scan");

            BoatFinder.ScanAndCacheArchetypes();

            if (BoatSpawner.cachedBoatSources != null &&
                BoatSpawner.cachedBoatSources.Count > 0)
            {
                Main.Log("[BoatSpawner] Cache-only scan successful — arming return offer");
                BoatSpawner.SpawnBoatFromRadio_ArmReturnOffer();
            }
            else
            {
                Main.Log("[BoatSpawner] ⚠️ Cache-only scan completed but no boats cached");
            }
        }

        private static void ___________HELPERS__________()
        {
        }

        public static void CacheBoatFromScanOnly(GameObject staticBoat)
        {
            if (staticBoat == null)
            {
                Main.Log("BoatSpawner: ❌ Cache-only scan failed — staticBoat null");
                return;
            }

            if (!TryGetBoatType(staticBoat, out int boatType))
            {
                Main.Log($"BoatSpawner: ❌ Cache-only scan failed — unknown boat type '{staticBoat.name}'");
                return;
            }

            GameObject sourceBoat = null;

            if (!cachedBoatSources.TryGetValue(boatType, out sourceBoat) || sourceBoat == null)
            {
                if (IsLodVariant(staticBoat))
                {
                    Main.Log($"BoatSpawner: ❌ Cache-only scan failed — static LOD '{staticBoat.name}'");
                    return;
                }

                GameObject archetype = CreateArchetypeFrom(staticBoat);
                if (archetype == null)
                {
                    Main.Log(
                        $"BoatSpawner: ❌ Cache-only scan failed — failed to create archetype from '{staticBoat.name}'"
                    );
                    return;
                }

                cachedBoatSources[boatType] = archetype;
                sourceBoat = archetype;

                Main.Log(
                    $"BoatSpawner: 📌 Cached FishingBoat_{boatType:00} archetype (cache-only) from '{staticBoat.name}'"
                );
            }

            if (!pendingReturnDialogScheduled)
            {
                SpawnBoatFromRadio_ArmReturnOffer();
            }
        }

        public static string NormalizeBoatName(string name)
        {
            int cloneIndex = name.IndexOf("(Clone)");
            if (cloneIndex >= 0)
                name = name.Substring(0, cloneIndex).Trim();

            int suffixIndex = name.LastIndexOf(" (");
            if (suffixIndex >= 0 && name.EndsWith(")"))
            {
                name = name.Substring(0, suffixIndex).Trim();
            }

            return name;
        }

        public static bool TryGetBoatType(GameObject go, out int boatType)
        {
            boatType = -1;

            if (go == null)
                return false;

            string name = NormalizeBoatName(go.name);

            if (name == "FishingBoat_01") { boatType = 1; return true; }
            if (name == "FishingBoat_02") { boatType = 2; return true; }
            if (name == "FishingBoat_03") { boatType = 3; return true; }

            return false;
        }

        public static bool IsOverWaterForBoat(GameObject boat)
        {
            if (boat == null)
                return false;

            Vector3 spawnPos = boat.transform.position;

            Vector3 origin = spawnPos + Vector3.up * 15f;
            Ray ray = new Ray(origin, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool sawWaterTrigger = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider is CharacterController)
                    continue;

                if (hit.collider.transform.IsChildOf(boat.transform))
                    continue;

                if (hit.collider.GetComponentInParent<ItemBuoyancyEnabler>() != null)
                {
                    sawWaterTrigger = true;
                    continue;
                }

                if (hit.collider.isTrigger)
                    continue;

                if (!sawWaterTrigger)
                {

                    if (hit.collider.GetType().FullName == "UnityEngine.TerrainCollider")
                        return false;

                    if (!BoatUIController.GhostBoatOn)
                        return false;
                }
            }

            return sawWaterTrigger;
        }

        private static bool IsSpawnPosOverWater(Vector3 spawnPos)
        {
            Vector3 origin = spawnPos + Vector3.up * 20f;
            Ray ray = new Ray(origin, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray, 200f);

            if (hits == null || hits.Length == 0)
                return false;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            bool sawWater = false;

            foreach (var hit in hits)
            {
                if (hit.collider == null)
                    continue;

                if (hit.collider.GetComponentInParent<ItemBuoyancyEnabler>() != null)
                {
                    sawWater = true;
                    continue;
                }

                if (hit.collider.isTrigger)
                    continue;

                if (!sawWater)
                    return false;

            }

            return sawWater;
        }

        public static void OnGameLoaded()
        {
            if (!pendingBoatUnlockTravel)
                return;

            if (pendingReturnDialogScheduled)
                return;

            pendingReturnDialogScheduled = true;
  
            EnsureRunner();

            float delaySeconds = 3.5f; 
            _runner.StartCoroutine(ShowReturnDialogAfterDelay(delaySeconds));
        }
    }
}
namespace DV_Boats
{
    internal sealed class DVBoatCloneMarker : MonoBehaviour
    {
    }
}
