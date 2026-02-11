using System;
using UnityEngine;

namespace DV_Boats
{
    internal static class BoatCameraManager
    {
        private static GameObject boatCamRoot;

        private static Transform originalCameraParent;
        private static Vector3 originalCameraLocalPos;
        private static Quaternion originalCameraLocalRot;
        private static bool hasCachedDvCamPose;

        private static Quaternion currentViewRotation = Quaternion.identity;

        public static void PrimeCameraOwnership()
        {
            try
            {
                if (hasCachedDvCamPose)
                {
                    Main.Log("[BoatCameraManager] Camera ownership already primed.");
                    return;
                }

                var dvCam = PlayerManager.ActiveCamera;
                if (dvCam == null)
                {
                    Main.Log("[BoatCameraManager] ❌ PrimeCameraOwnership: ActiveCamera is null.");
                    return;
                }

                if (dvCam.transform.parent != dvCam.transform)
                {
                    originalCameraParent = dvCam.transform.parent;
                    originalCameraLocalPos = dvCam.transform.localPosition;
                    originalCameraLocalRot = dvCam.transform.localRotation;
                    hasCachedDvCamPose = true;
                }
                else
                {
                    originalCameraParent = null;
                    originalCameraLocalPos = Vector3.zero;
                    originalCameraLocalRot = Quaternion.identity;
                    hasCachedDvCamPose = true;

                    Main.Log("[BoatCameraManager] ⚠ ActiveCamera parent was self — cached with null parent fallback.");
                }
            }
            catch (Exception ex)
            {
                Main.Log("[BoatCameraManager] ❌ PrimeCameraOwnership ERROR: " + ex);
            }
        }

        public static void ApplyBoatCamera(CameraViewDefinition def, Transform boat)
        {
            if (def == null)
            {
                Main.Log("[BoatCameraManager] ❌ ApplyBoatCamera: def=null");
                return;
            }

            if (boat == null)
            {
                Main.Log("[BoatCameraManager] ❌ ApplyBoatCamera: boat=null");
                return;
            }

            var dvCam = PlayerManager.ActiveCamera;
            if (dvCam == null)
            {
                Main.Log("[BoatCameraManager] ❌ ApplyBoatCamera: ActiveCamera is null.");
                return;
            }

            if (!hasCachedDvCamPose)
                PrimeCameraOwnership();

            TeardownBoatCameraRoot();

            if (boatCamRoot == null)
                boatCamRoot = new GameObject("DV_Boats_BoatCamRoot");

            boatCamRoot.hideFlags = HideFlags.HideAndDontSave;

            boatCamRoot.transform.SetParent(boat, false);

            boatCamRoot.transform.localPosition = def.offset;

            currentViewRotation = ComputeInitialAnchorRotation(
                def,
                boat,
                boatCamRoot.transform.position
            );

            dvCam.transform.SetParent(boatCamRoot.transform, false);
            dvCam.transform.localPosition = Vector3.zero;
            dvCam.transform.localRotation = Quaternion.identity;
        }

        public static void ActivateFreeRoam()
        {
            TeardownBoatCameraRoot();

            var dvCam = PlayerManager.ActiveCamera;
            if (dvCam != null && hasCachedDvCamPose)
            {
                if (originalCameraParent == dvCam.transform)
                {                    
                    originalCameraParent = null;
                }

                if (originalCameraParent != null)
                    dvCam.transform.SetParent(originalCameraParent, false);
                else
                    dvCam.transform.SetParent(null, true);

                dvCam.transform.localPosition = originalCameraLocalPos;
                dvCam.transform.localRotation = originalCameraLocalRot;

                BoatController boat = BoatDriveManager.ActiveBoat; // or equivalent
                if (boat != null)
                {
                    PlacePlayerOnBoat(boat);
                }
            }
        }

        private static void TeardownBoatCameraRoot()
        {
            var dvCam = PlayerManager.ActiveCamera;
            if (dvCam != null && hasCachedDvCamPose)
            {
                if (boatCamRoot != null && dvCam.transform.IsChildOf(boatCamRoot.transform))
                {
                    if (originalCameraParent != null && originalCameraParent != dvCam.transform)
                        dvCam.transform.SetParent(originalCameraParent, false);
                    else
                        dvCam.transform.SetParent(null, true);

                    dvCam.transform.localPosition = originalCameraLocalPos;
                    dvCam.transform.localRotation = originalCameraLocalRot;
                }
            }

            if (boatCamRoot != null)
            {
                UnityEngine.Object.Destroy(boatCamRoot);
                boatCamRoot = null;
                Main.Log("[BoatCameraManager] Destroyed BoatCamRoot.");
            }
        }

        public static Quaternion ComputeInitialAnchorRotation(CameraViewDefinition def, Transform boat, Vector3 anchorWorldPos)
        {
            string mode = def != null && def.mode != null ? def.mode.ToLowerInvariant() : "lookforward";

            if (mode == "lookforward")
            {
                Vector3 flatFwd = new Vector3(boat.forward.x, 0f, boat.forward.z);
                if (flatFwd.sqrMagnitude < 0.0001f) flatFwd = boat.forward;
                return Quaternion.LookRotation(flatFwd.normalized, Vector3.up);
            }

            if (mode == "lookbackward")
            {
                Vector3 flatBack = new Vector3(-boat.forward.x, 0f, -boat.forward.z);
                if (flatBack.sqrMagnitude < 0.0001f) flatBack = -boat.forward;
                return Quaternion.LookRotation(flatBack.normalized, Vector3.up);
            }

            if (mode == "lookatoffset")
            {
                Vector3 lookWorld =
                    boat.position +
                    boat.right * def.lookAtOffset.x +
                    boat.up * def.lookAtOffset.y +
                    boat.forward * def.lookAtOffset.z;

                Vector3 dir = lookWorld - anchorWorldPos;
                if (dir.sqrMagnitude < 0.0001f) dir = boat.forward;
                return Quaternion.LookRotation(dir.normalized, Vector3.up);
            }

            {
                Vector3 dir = boat.position - anchorWorldPos;
                if (dir.sqrMagnitude < 0.0001f) dir = boat.forward;
                return Quaternion.LookRotation(dir.normalized, Vector3.up);
            }
        }

        public static void PlacePlayerOnBoat(BoatController boat)
        {
            Transform playerTf = PlayerManager.PlayerTransform;
            if (playerTf == null || boat == null)
                return;

            Vector3 placePos =
                boat.transform.position +
                boat.transform.up * 1.2f;

            playerTf.position = placePos;

            CharacterController cc = playerTf.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                cc.enabled = true;
            }

            cc.Move(Vector3.zero);

            if (PlayerManager.ActiveCamera != null)
            {
                Transform camTf = PlayerManager.ActiveCamera.transform;
                Vector3 euler = camTf.eulerAngles;
                camTf.rotation = Quaternion.Euler(euler.x, euler.y, 0f);
            }
        }

        public static void ResetCameraCache()
        {
            hasCachedDvCamPose = false;
            originalCameraParent = null;
            originalCameraLocalPos = Vector3.zero;
            originalCameraLocalRot = Quaternion.identity;
        }

        public static Quaternion GetCurrentViewRotation()
        {
            return currentViewRotation;
        }

    }
}
