using DV;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System.Reflection;

namespace DV_Boats
{
    public class BoatComms : MonoBehaviour, ICommsRadioMode
    {
        private enum CommsState
        {
            Root_NoCachedBoats,
            PostArrival_WaitForScan,
            OfferReturn,
            Root_CachedBoats,
            SelectBoatType,
            SpawnAction
        }

        private CommsState currentState;

        private readonly string[] boatTypes =
        {
            "BOAT TYPE 1",
            "BOAT TYPE 2",
            "BOAT TYPE 3"
        };

        private readonly int[] boatTypeOrder = { 1, 2, 3 };

        private int selectedBoatIndex;

        public CommsRadioController Controller;

        private CommsRadioDisplay display;
        private Transform signalOrigin;

        private Coroutine scanCoroutine;
        private bool pendingReturnValid;

        private GameObject previewQuad;
        private bool isSpawnValid;
        private RaycastHit spawnHit;
        private bool hasHit;

        private AudioClip clickClip;  
        private AudioClip scrollClip; 


        public ButtonBehaviourType ButtonBehaviour =>
            currentState == CommsState.SelectBoatType
                ? ButtonBehaviourType.Override
                : ButtonBehaviourType.Regular;

        public void Enable()
        {
            BoatSpawner.RadioReturnOfferReady += OnReturnOfferReady;
            ResolveRefs();
            UpdateDisplay();
        }

        public void Disable()
        {
            BoatSpawner.RadioReturnOfferReady -= OnReturnOfferReady;

            if (currentState == CommsState.PostArrival_WaitForScan ||
        currentState == CommsState.OfferReturn)
            {
                pendingReturnValid = false;
                DecideInitialState();
            }

            StopScanCoroutine();
            DestroyPreview();

        }

        public void SetStartingDisplay()
        {
            ResolveRefs();
            UpdateDisplay();
        }

        public void OverrideSignalOrigin(Transform origin)
        {
            signalOrigin = origin;
        }

        public void OnUpdate()
        {
            if (currentState != CommsState.SpawnAction)
                return;

            ResolveSignalOriginIfNeeded();
            UpdateSpawnRaycast();
            UpdatePreview();
            UpdateSpawnText();

            if (Controller != null && Controller.laserBeam != null)
            {
                Controller.laserBeam.SetBeamColor(GetLaserBeamColor());
            }
        }

        public void OnUse()
        {
            switch (currentState)
            {
                case CommsState.Root_NoCachedBoats:
                    PlayClick();

                    bool spawnClonesOnArrival = !Main.Settings.spawnOnly;

                    BoatSpawner.SpawnBoatFromRadio_TeleportToHarbour(spawnClonesOnArrival);

                    currentState = CommsState.PostArrival_WaitForScan;
                    UpdateDisplay();
                    break;


                case CommsState.OfferReturn:
                    PlayClick();
                    pendingReturnValid = false;

                    BoatSpawner.SpawnBoatFromRadio_ReturnTeleport();

                    EnterCachedBoatsRoot();
                    break;


                case CommsState.Root_CachedBoats:
                    PlayClick();
                    currentState = CommsState.SelectBoatType;
                    UpdateDisplay();
                    break;

                case CommsState.SelectBoatType:
                    PlayClick();
                    currentState = CommsState.SpawnAction;
                    UpdateDisplay();
                    break;

                case CommsState.SpawnAction:
                    if (!isSpawnValid || !hasHit)
                    {
                        PlayClick();
                        DestroyPreview();
                        ClearBeam();
                        currentState = CommsState.Root_CachedBoats;
                        UpdateDisplay();
                        break;
                    }

                    PlayConfirm();

                    int selectedBoatType = boatTypeOrder[selectedBoatIndex];

                    if (!BoatSpawner.cachedBoatSources.ContainsKey(selectedBoatType))
                    {
                        Main.Log($"[BoatRadio] ❌ No cached boat for type {selectedBoatType}");
                        PlayClick();
                        return;
                    }

                    BoatSpawner.SpawnBoatFromRadio_BeamHit(
                         spawnHit.point,
                         signalOrigin != null ? signalOrigin.forward : Vector3.forward,
                         selectedBoatType
                     );

                    DestroyPreview();
                    ClearBeam();

                    isSpawnValid = false;
                    hasHit = false;

                    currentState = CommsState.Root_CachedBoats;
                    UpdateDisplay();

                    break;
                    ;

                case CommsState.PostArrival_WaitForScan:
                    // ignore
                    break;
            }
        }

        public bool ButtonACustomAction()
        {
            if (currentState != CommsState.SelectBoatType)
                return false;

            selectedBoatIndex = (selectedBoatIndex - 1 + boatTypes.Length) % boatTypes.Length;
            PlayScroll();
            UpdateDisplay();
            return true;
        }

        public bool ButtonBCustomAction()
        {
            if (currentState != CommsState.SelectBoatType)
                return false;

            selectedBoatIndex = (selectedBoatIndex + 1) % boatTypes.Length;
            PlayScroll();
            UpdateDisplay();
            return true;
        }

        public Color GetLaserBeamColor()
        {
            if (currentState != CommsState.SpawnAction)
                return Color.clear;

            return isSpawnValid ? Color.green : Color.red;
        }

        private void ClearBeam()
        {
            if (Controller != null && Controller.laserBeam != null)
            {
                Controller.laserBeam.SetBeamColor(Color.clear);
            }
        }

        private void DecideInitialState()
        {
            currentState =
                BoatSpawner.cachedBoatSources != null &&
                BoatSpawner.cachedBoatSources.Count > 0
                    ? CommsState.Root_CachedBoats
                    : CommsState.Root_NoCachedBoats;
        }

        private void EnterCachedBoatsRoot()
        {
            currentState = CommsState.Root_CachedBoats;
            UpdateDisplay();
        }

        private void StopScanCoroutine()
        {
            if (scanCoroutine != null)
            {
                StopCoroutine(scanCoroutine);
                scanCoroutine = null;
            }
        }

        private void ResolveSignalOriginIfNeeded()
        {
            if (signalOrigin != null)
                return;

            if (Controller != null && Controller.laserBeam != null)
                signalOrigin = Controller.laserBeam.transform;
        }

        private void UpdateSpawnRaycast()
        {
            hasHit = false;
            isSpawnValid = false;

            if (signalOrigin == null)
                return;

            Ray ray = new Ray(signalOrigin.position, signalOrigin.forward);

            if (Physics.Raycast(ray, out spawnHit, 250f))
            {
                hasHit = true;
                isSpawnValid = IsSpawnPosOverWater(spawnHit.point);
            }
        }

        private void UpdatePreview()
        {
            if (!hasHit)
            {
                if (previewQuad != null)
                    previewQuad.SetActive(false);
                return;
            }

            if (previewQuad == null)
                previewQuad = CreatePreviewQuad();

            if (!previewQuad.activeSelf)
                previewQuad.SetActive(true);

            Vector3 pos = spawnHit.point + Vector3.up * 0.08f;

            float yaw = (signalOrigin != null) ? signalOrigin.eulerAngles.y : 0f;
            Quaternion rot = Quaternion.Euler(90f, yaw, 0f);

            previewQuad.transform.SetPositionAndRotation(pos, rot);

            Color c = isSpawnValid ? Color.green : Color.red;
            var r = previewQuad.GetComponent<Renderer>();
            if (r != null && r.material != null)
                r.material.color = c;
        }

        private void UpdateSpawnText()
        {
            if (display == null)
                return;

            display.SetContent(
                isSpawnValid ? "SPAWN BOAT" : "POSITION BOAT OVER WATER",
                FontStyles.UpperCase);

            display.SetAction(isSpawnValid ? "SPAWN" : "CANCEL");
        }

        private GameObject CreatePreviewQuad()
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);

            quad.transform.localScale = new Vector3(4f, 8f, 1f);

            var col = quad.GetComponent<Collider>();
            if (col != null)
                Destroy(col);

            var rend = quad.GetComponent<Renderer>();
            if (rend != null)
            {
                Material mat = new Material(Shader.Find("Unlit/Color"));
                rend.material = mat;
            }

            return quad;
        }


        private void DestroyPreview()
        {
            if (previewQuad != null)
            {
                Destroy(previewQuad);
                previewQuad = null;
            }
        }

        private void UpdateDisplay()
        {
            if (display == null)
                return;

            if (currentState == default)
            {
                currentState =
                    BoatSpawner.cachedBoatSources != null &&
                    BoatSpawner.cachedBoatSources.Count > 0
                        ? CommsState.Root_CachedBoats
                        : CommsState.Root_NoCachedBoats;
            }

            display.content.alignment = TextAlignmentOptions.Center;

            switch (currentState)
            {
                case CommsState.Root_NoCachedBoats:
                    if (currentState == CommsState.Root_NoCachedBoats &&
                        BoatSpawner.cachedBoatSources != null &&
                        BoatSpawner.cachedBoatSources.Count > 0)
                    {
                        currentState = CommsState.Root_CachedBoats;
                    }

                    display.SetDisplay(
                        "DV_BOATS",
                        "BOATS HAVE NOT YET BEEN DETECTED.\nCLICK TELEPORT TO VISIT A HARBOR (FREE).",
                        "TELEPORT");
                    break;

                case CommsState.PostArrival_WaitForScan:
                    PlayClick();
                    display.SetDisplay(
                        "DV_BOATS",
                        "PLEASE WAIT...\n\nSCANNING FOR BOATS",
                        "");
                    break;

                case CommsState.OfferReturn:
                    PlayClick();
                    display.SetDisplay(
                        "DV_BOATS",
                        "BOATS DETECTED\nRETURN TO PREVIOUS LOCATION?",
                        "RETURN");
                    break;

                case CommsState.Root_CachedBoats:
                    PlayClick();
                    display.SetDisplay(
                        "DV_BOATS",
                        "YOU MAY NOW SPAWN BOATS IN THE WATER ANYWHERE",
                        "PROCEED");
                    break;

                case CommsState.SelectBoatType:
                    PlayClick();
                    display.SetDisplay(
                        "DV_BOATS",
                        $"SELECT BOAT TYPE:\n\nBOAT TYPE {boatTypeOrder[selectedBoatIndex]}",
                        "CONFIRM");
                    break;

                case CommsState.SpawnAction:
                    PlayClick();
                    display.SetDisplay(
                        "DV_BOATS",
                        "POSITION BOAT",
                        "");
                    break;
            }
        }

        private void ResolveRefs()
        {
            if (Controller == null)
            {
                Debug.LogError("[BoatComms] Controller not assigned");
                return;
            }

            if (display == null)
            {
                display = Controller.GetComponentInChildren<CommsRadioDisplay>(true);
                if (display == null)
                    Debug.LogError("[BoatComms] Failed to resolve CommsRadioDisplay");
            }

            ResolveSignalOriginIfNeeded();
            ResolveAudioClips();
        }

        private void ResolveAudioClips()
        {
            if (clickClip == null)
                clickClip = FindAnyAudioClipOnController();

            if (scrollClip == null)
                scrollClip = clickClip;
        }

        private AudioClip FindAnyAudioClipOnController()
        {
            if (Controller == null)
                return null;

            if (Controller.selectionAction != null)
                return Controller.selectionAction;

            MonoBehaviour[] comps = Controller.GetComponentsInChildren<MonoBehaviour>(true);
            for (int i = 0; i < comps.Length; i++)
            {
                var mb = comps[i];
                if (mb == null)
                    continue;

                Type t = mb.GetType();
                FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int f = 0; f < fields.Length; f++)
                {
                    if (fields[f].FieldType != typeof(AudioClip))
                        continue;

                    AudioClip clip = fields[f].GetValue(mb) as AudioClip;
                    if (clip != null)
                        return clip;
                }
            }

            return null;
        }

        private void PlayClick()
        {
            if (clickClip == null)
                return;

            CommsRadioController.PlayAudioFromRadio(clickClip, transform);
        }

        private void PlayScroll()
        {
            if (scrollClip == null)
                return;

            CommsRadioController.PlayAudioFromRadio(scrollClip, transform);
        }

        private void PlayConfirm()
        {
            PlayClick();
        }

        private void OnReturnOfferReady()
        {
            if (currentState != CommsState.PostArrival_WaitForScan)
                return;

            pendingReturnValid = true;
            currentState = CommsState.OfferReturn;
            UpdateDisplay();
        }

        private static bool IsSpawnPosOverWater(Vector3 spawnPos)
        {
            Vector3 origin = spawnPos + Vector3.up * 20f;
            Ray ray = new Ray(origin, Vector3.down);

            RaycastHit[] hits = Physics.RaycastAll(ray, 200f);
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

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
    }
}


namespace DV_Boats
{
    [HarmonyPatch(typeof(CommsRadioController), "Awake")]
    public static class BoatCommsInjector
    {
        private static void Postfix(CommsRadioController __instance)
        {
            try
            {
                GameObject go = new GameObject("CommsRadioBoatComms");
                go.transform.SetParent(__instance.transform, false);
                go.SetActive(false);

                BoatComms boatComms = go.AddComponent<BoatComms>();
                boatComms.Controller = __instance;

                var allModesField = AccessTools.Field(typeof(CommsRadioController), "allModes");
                if (allModesField == null)
                {
                    Debug.LogError("[DV_Boats] Failed to find allModes field via reflection");
                    return;
                }

                var allModes = allModesField.GetValue(__instance) as List<ICommsRadioMode>;
                if (allModes == null)
                {
                    Debug.LogError("[DV_Boats] allModes list is null");
                    return;
                }

                for (int i = 0; i < allModes.Count; i++)
                {
                    if (allModes[i] is BoatComms)
                    {
                        Debug.Log("[DV_Boats] BoatComms already present, skipping");
                        return;
                    }
                }

                allModes.Add(boatComms);

                __instance.ReactivateModes();

                go.SetActive(true);

                Debug.Log("[DV_Boats] BoatComms injected successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError("[DV_Boats] BoatComms injection failed:\n" + ex);
            }
        }
    }
}
