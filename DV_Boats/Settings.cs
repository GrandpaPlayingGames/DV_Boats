using UnityEngine;
using UnityModManagerNet;
using System;

namespace DV_Boats
{
    public class Settings : UnityModManager.ModSettings, IDrawable
    {
        // =========================
        // DEBUG
        // =========================
        public bool debugLogging = false;
        public bool spawnOnly = false;
        public bool realisticSpeed = false;

        // =========================
        // UI SHOW / HIDE
        // =========================
        public KeyCode showHideUIKey = KeyCode.F7;
        public bool showHideUICtrl = false;
        public bool showHideUIAlt = false;
        public bool showHideUIShift = false;

        // =========================
        // DRIVING MODE
        // =========================
        public KeyCode exitDriveKey = KeyCode.X;
        public bool exitDriveCtrl = true;
        public bool exitDriveAlt = false;
        public bool exitDriveShift = false;

        public bool driveBoatCtrl = false;
        public bool driveBoatAlt = true;
        public bool driveBoatShift = false;
        public KeyCode driveBoatKey = KeyCode.D;

        public KeyCode ghostBoatToggleKey = KeyCode.G;
        public bool ghostBoatToggleCtrl = false;
        public bool ghostBoatToggleAlt = false;
        public bool ghostBoatToggleShift = false;

        // =========================
        // MOVEMENT KEYS
        // =========================
        public KeyCode forwardKey = KeyCode.UpArrow;
        public bool forwardCtrl = false;
        public bool forwardAlt = false;
        public bool forwardShift = false;
        public KeyCode backwardKey = KeyCode.DownArrow;
        public bool backwardCtrl = false;
        public bool backwardAlt = false;
        public bool backwardShift = false;
        public KeyCode leftKey = KeyCode.LeftArrow;
        public bool leftCtrl = false;
        public bool leftAlt = false;
        public bool leftShift = false;
        public KeyCode rightKey = KeyCode.RightArrow;
        public bool rightCtrl = false;
        public bool rightAlt = false;
        public bool rightShift = false;

        // =========================
        // HORNS
        // =========================
        public KeyCode hornKey = KeyCode.H;
        public bool hornCtrl = false;
        public bool hornAlt = false;
        public bool hornShift = false;

        public KeyCode foghornKey = KeyCode.J;
        public bool foghornCtrl = false;
        public bool foghornAlt = false;
        public bool foghornShift = false;


        // =========================
        // LIGHTS
        // =========================
        public KeyCode NavLightKey = KeyCode.K;
        public bool NavLightCtrl = false;
        public bool NavLightAlt = false;
        public bool NavLightShift = false;
        public KeyCode DeckLightKey = KeyCode.L;
        public bool DeckLightCtrl = false;
        public bool DeckLightAlt = false;
        public bool DeckLightShift = false;
        public KeyCode SpotLightKey = KeyCode.Semicolon;
        public bool SpotLightCtrl = false;
        public bool SpotLightAlt = false;
        public bool SpotLightShift = false;
        public KeyCode SpotLightSwivelLeftKey = KeyCode.Comma;
        public bool SpotLightSwivelLeftCtrl = false;
        public bool SpotLightSwivelLeftAlt = false;
        public bool SpotLightSwivelLeftShift = false;
        public KeyCode SpotLightSwivelRightKey = KeyCode.Period;
        public bool SpotLightSwivelRightCtrl = false;
        public bool SpotLightSwivelRightAlt = false;
        public bool SpotLightSwivelRightShift = false;
        public KeyCode SpotLightSwivelCenterKey = KeyCode.Slash;
        public bool SpotLightSwivelCenterCtrl = false;
        public bool SpotLightSwivelCenterAlt = false;
        public bool SpotLightSwivelCenterShift = false;

        // =========================
        // Cameras
        // =========================
        public KeyCode cameraNextKey = KeyCode.RightBracket;
        public bool cameraNextCtrl = false;
        public bool cameraNextAlt = false;
        public bool cameraNextShift = false;

        public KeyCode cameraPrevKey = KeyCode.LeftBracket;
        public bool cameraPrevCtrl = false;
        public bool cameraPrevAlt = false;
        public bool cameraPrevShift = false;

        // =========================
        // AUDIO (NOT EXPOSED)
        // =========================
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float engineVolume = 1.0f;
        [Range(0f, 1f)] public float effectsVolume = 1.0f;

        // =========================
        // MOVEMENT TUNING 
        // =========================
        [Range(0.25f, 1.0f)]
        public float turnStrengthMultiplier = 0.75f;
        public bool disableBoatProbes = false;

        // ======================================================
        // UI STATE
        // ======================================================
        private KeyCode keyWaiting = KeyCode.None;

        // ======================================================
        // UMM GUI
        // ======================================================
        public void OnGUI(UnityModManager.ModEntry modEntry)
        {
            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.gray }
            };

            GUILayout.Space(6);
            
            BeginGroup("Game Options", titleStyle);
            realisticSpeed = GUILayout.Toggle(realisticSpeed, "Realistic Speed (On) / Fast (Off)");
            EndGroup();

            BeginGroup("UI Controls", titleStyle);
            DrawModifierKey("Show / Hide UI", ref showHideUIKey, ref showHideUICtrl, ref showHideUIAlt, ref showHideUIShift);
            EndGroup();

            BeginGroup("Driving Mode", titleStyle);
            DrawModifierKey("Enter Drive Mode", ref driveBoatKey, ref driveBoatCtrl, ref driveBoatAlt, ref driveBoatShift);             
            DrawModifierKey("Exit Drive Mode", ref exitDriveKey, ref exitDriveCtrl,ref exitDriveAlt,ref exitDriveShift);
            DrawModifierKey("Ghost Boat Toggle", ref ghostBoatToggleKey, ref ghostBoatToggleCtrl,ref ghostBoatToggleAlt, ref ghostBoatToggleShift);
            EndGroup();

            BeginGroup("Movement Keys", titleStyle);
            DrawModifierKey("Forward", ref forwardKey, ref forwardCtrl, ref forwardAlt, ref forwardShift);
            DrawModifierKey("Backward", ref backwardKey, ref backwardCtrl, ref backwardAlt, ref backwardShift);
            DrawModifierKey("Turn Left", ref leftKey, ref leftCtrl, ref leftAlt, ref leftShift);
            DrawModifierKey("Turn Right", ref rightKey, ref rightCtrl, ref rightAlt, ref rightShift);
            EndGroup();

            BeginGroup("Camera Modes", titleStyle);
            DrawModifierKey("Next Camera", ref cameraNextKey, ref cameraNextCtrl, ref cameraNextAlt, ref cameraNextShift);
            DrawModifierKey("Previous Camera", ref cameraPrevKey, ref cameraPrevCtrl, ref cameraPrevAlt, ref cameraPrevShift);
            EndGroup();

            BeginGroup("Horns", titleStyle);
            DrawModifierKey("Horn", ref hornKey, ref hornCtrl, ref hornAlt, ref hornShift);
            DrawModifierKey("Foghorn", ref foghornKey, ref foghornCtrl, ref foghornAlt, ref foghornShift);
            EndGroup();

            BeginGroup("Lights & Spotlight", titleStyle);
            DrawModifierKey("Nav Lights", ref NavLightKey, ref NavLightCtrl, ref NavLightAlt, ref NavLightShift);
            DrawModifierKey("Deck Light", ref DeckLightKey, ref DeckLightCtrl, ref DeckLightAlt, ref DeckLightShift);
            DrawModifierKey("Spotlights", ref SpotLightKey, ref SpotLightCtrl, ref SpotLightAlt, ref SpotLightShift);
            DrawModifierKey("Spotlight left", ref SpotLightSwivelLeftKey, ref SpotLightSwivelLeftCtrl, ref SpotLightSwivelLeftAlt, ref SpotLightSwivelLeftShift);
            DrawModifierKey("Spotlight Right", ref SpotLightSwivelRightKey, ref SpotLightSwivelRightCtrl, ref SpotLightSwivelRightAlt, ref SpotLightSwivelRightShift);
            DrawModifierKey("Spotlight left", ref SpotLightSwivelCenterKey, ref SpotLightSwivelCenterCtrl, ref SpotLightSwivelCenterAlt, ref SpotLightSwivelCenterShift);
            EndGroup();
        }

        // ======================================================
        // UI HELPERS
        // ======================================================
        private void DrawRebindKey(string label, ref KeyCode key)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(220));

            string text = (keyWaiting == key) ? "Press key..." :
                          (key == KeyCode.None ? "(Unbound)" : key.ToString());

            if (GUILayout.Button(text, GUILayout.Width(120)))
                keyWaiting = (keyWaiting == key) ? KeyCode.None : key;

            if (keyWaiting == key && Event.current.type == EventType.KeyDown)
            {
                var newKey = Event.current.keyCode;

                if (newKey == KeyCode.Escape)
                {
                    keyWaiting = KeyCode.None;
                    GUI.FocusControl(null);
                    Event.current.Use();
                    GUILayout.EndHorizontal();
                    return;
                }

                if (newKey != KeyCode.None && !newKey.ToString().StartsWith("Mouse"))
                {
                    key = newKey;
                    keyWaiting = KeyCode.None;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }

            GUILayout.EndHorizontal();
        }

        private void DrawModifierKey(
            string label,
            ref KeyCode key,
            ref bool ctrl,
            ref bool alt,
            ref bool shift)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(180));

            ctrl = GUILayout.Toggle(ctrl, "Ctrl", GUILayout.Width(45));
            alt = GUILayout.Toggle(alt, "Alt", GUILayout.Width(40));
            shift = GUILayout.Toggle(shift, "Shift", GUILayout.Width(50));

            DrawKeyButton(ref key);

            GUILayout.EndHorizontal();
        }

        private void DrawKeyButton(ref KeyCode key)
        {
            string text = (keyWaiting == key) ? "Press key..." :
                          (key == KeyCode.None ? "(Unbound)" : key.ToString());

            if (GUILayout.Button(text, GUILayout.Width(120)))
                keyWaiting = (keyWaiting == key) ? KeyCode.None : key;

            if (keyWaiting == key && Event.current.type == EventType.KeyDown)
            {
                var newKey = Event.current.keyCode;

                if (newKey == KeyCode.Escape)
                {
                    keyWaiting = KeyCode.None;
                    GUI.FocusControl(null);
                    Event.current.Use();
                    return;
                }

                if (newKey != KeyCode.None && !newKey.ToString().StartsWith("Mouse"))
                {
                    key = newKey;
                    keyWaiting = KeyCode.None;
                    GUI.FocusControl(null);
                    Event.current.Use();
                }
            }
        }

        private void BeginGroup(string title, GUIStyle style)
        {
            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
            GUILayout.BeginVertical("box");
            GUI.backgroundColor = Color.white;
            GUILayout.Label(title, style);
            GUILayout.Space(6);
        }

        private void EndGroup()
        {
            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
            BoatUIController.ResetFooterStatus();
        }

        public void OnChange() {
            BoatUIController.ResetFooterStatus();
        }
    }
}


