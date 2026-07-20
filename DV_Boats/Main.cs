

using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;



namespace DV_Boats
{
    public static class Main
    {
        public static bool Enabled { get; private set; }
        public static Settings Settings;
        public static UnityModManager.ModEntry ModEntry;

        private static BoatController lastHoveredBoat;
        private static float lastHoverLogTime;

        private static Harmony harmony;

        public static bool initialBoatScanDone = false;
        public static bool inGameplay = false;

        private static bool _gameLoadedFired = false;
        public static bool hasRunOnceThisSession = false;
        private static CharacterControllerProvider _cachedProvider;


    public static void Load(UnityModManager.ModEntry modEntry)
    {
            ModEntry = modEntry;

            modEntry.OnToggle = OnToggle;
            modEntry.OnUnload = OnUnload;
            modEntry.OnUpdate = OnUpdate;

            Settings = UnityModManager.ModSettings.Load<Settings>(modEntry);
            modEntry.OnGUI = Settings.OnGUI;
            modEntry.OnSaveGUI = Settings.Save;

            harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll();

            AudioLoader.Init(modEntry.Path);

            new GameObject("BoatWorldOriginWatcher").AddComponent<BoatWorldOriginWatcher>();
        }

        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            Update();
        }

        public static void Update()
        {

            CheckGameLoadedOnce();
            UpdateBoatHover();          
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool enabled)
        {
            Enabled = enabled;

            if (enabled)
            {
                Log("DV_Boats enabled");
                var hoverGo = new GameObject("DV_Boats_HoverGUI");
                UnityEngine.Object.DontDestroyOnLoad(hoverGo);
                hoverGo.AddComponent<BoatHoverGuiRunner>();

                var uiGo = new GameObject("DV_Boats_UI");
                UnityEngine.Object.DontDestroyOnLoad(uiGo);
                uiGo.AddComponent<BoatUIController>();

                DVBoatsRunner.Ensure();
            }
            else
            {
                BoatSpawner.DestroyClone();
                Log("DV_Boats disabled");
            }

            return true;
        }

        private static bool OnUnload(UnityModManager.ModEntry modEntry)
        {
            Enabled = false;
            Log("DV_Boats unloaded");
            return true;
        }

        private static void UpdateBoatHover()
        {
            if (BoatDriveManager.IsDriving && ExitDriveKeyPressed())
            {
                BoatDriveManager.ExitDriving();
                BoatHoverUI.Hide();
                return;
            }

            if (BoatDriveManager.IsDriving && ToggleUIKeyPressed())
            {
                BoatUIController.Toggle();
                return;
            }

            var cam = Camera.main;
            if (cam == null)
                return;

            if (Time.time - lastHoverLogTime > 1f)
                lastHoverLogTime = Time.time;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out RaycastHit hit, 60f, ~0, QueryTriggerInteraction.Collide))
            {
                BoatHoverUI.Hide();
                return;
            }

            var hovered = hit.collider.GetComponentInParent<BoatController>();

            if (hovered != lastHoveredBoat)
            {
                if (lastHoveredBoat != null)
                    lastHoveredBoat.SetHovered(false);

                if (hovered != null)
                    hovered.SetHovered(true);

                lastHoveredBoat = hovered;
            }

            if (BoatDriveManager.IsDriving)
            {
                BoatHoverUI.Hide();
                return;
            }

            if (hovered != null &&
                hovered.Mode == BoatMode.Passive &&
                DriveKeyPressed())
            {
                BoatDriveManager.EnterDriving(hovered);
                BoatHoverUI.Hide();
                return;
            }

            if (hovered != null && hovered.Mode == BoatMode.Passive)
            {
                BoatHoverUI.Show(
                    "Press " +
                    FormatKeybind(
                        Settings.driveBoatKey,
                        Settings.driveBoatCtrl,
                        Settings.driveBoatAlt,
                        Settings.driveBoatShift) +
                    " to Drive");
            }
            else
            {
                BoatHoverUI.Hide();
            }
        }

        public static void Log(string message, bool force = false)
        {          
            if (!force && Settings != null && !Settings.debugLogging)
                return;

            ModEntry?.Logger.Log($"[DV_Boats] {message}");
        }

        private static bool KeyPressed(KeyCode key, bool ctrl, bool alt, bool shift)
        {
            if (!Input.GetKeyDown(key)) return false;
            if (ctrl && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return false;
            if (alt && !Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt)) return false;
            if (shift && !Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift)) return false;
            return true;
        }

        private static bool DriveKeyPressed()
        {
            return BoatController.IsKeyWithModifiersPressed(
                Main.Settings.driveBoatKey,
                Settings.driveBoatCtrl,
                Settings.driveBoatAlt,
                Settings.driveBoatShift,
                held: false
            );
        }

        private static bool ExitDriveKeyPressed()
        {
            return BoatController.IsKeyWithModifiersPressed(
                Settings.exitDriveKey,
                Settings.exitDriveCtrl,
                Settings.exitDriveAlt,
                Settings.exitDriveShift,
                held: false
            );
        }

        private static bool ToggleUIKeyPressed()
        {
            return KeyPressed(
                Settings.showHideUIKey,
                Settings.showHideUICtrl,
                Settings.showHideUIAlt,
                Settings.showHideUIShift);
        }

        public static string FormatKeybind(KeyCode key, bool ctrl, bool alt, bool shift)
        {
            var sb = new System.Text.StringBuilder();
            if (ctrl) sb.Append("Ctrl + ");
            if (alt) sb.Append("Alt + ");
            if (shift) sb.Append("Shift + ");
            sb.Append(key.ToString().ToUpperInvariant());
            return sb.ToString();
        }

        private static void CheckGameLoadedOnce()
        {
            if (_cachedProvider == null)
            {
                _cachedProvider =
                    UnityEngine.Object.FindObjectOfType<CharacterControllerProvider>();

                if (_cachedProvider == null)
                    return;
            }

            if (!_cachedProvider.IsGameLoaded)
            {
                _gameLoadedFired = false;
                return;
            }

            if (_gameLoadedFired)
                return;

            _gameLoadedFired = true;
            OnGameLoaded();
        }


        private static void OnGameLoaded()
        {
            if (!hasRunOnceThisSession)
            {
                BoatSpawnerProbes.CreateDebugProbeMarkers();
                BoatSpawner.OnGameLoaded();
                hasRunOnceThisSession = true;
            }
        }
    }


[HarmonyPatch(typeof(PauseMenu))]
    internal static class PauseMenu_Patches
    {
        [HarmonyPatch("OnExitLevelRequested")]
        [HarmonyPrefix]
        private static void OnExitLevelRequested_Prefix()
        {
            BoatFinder.clearCaches();
            BoatSpawner.cachedBoatSources.Clear();
            BoatDriveManager.ExitDriving();
            DuplicateBoatProbe.BoatDuplicateProbeManager.clearProbes();
            Main.hasRunOnceThisSession = false;
        }

        [HarmonyPatch("OnQuitRequested")]
        [HarmonyPrefix]
        private static void OnQuitRequested_Prefix()
        {
            BoatFinder.clearCaches();
            BoatSpawner.cachedBoatSources.Clear();
            BoatDriveManager.ExitDriving();
            DuplicateBoatProbe.BoatDuplicateProbeManager.clearProbes();
            Main.hasRunOnceThisSession = false;
        }
    }
}

namespace DV_Boats
{
    internal sealed class DVBoatsRunner : MonoBehaviour
    {
        internal static DVBoatsRunner Instance;

        internal static DVBoatsRunner Ensure()
        {
            if (Instance != null)
                return Instance;

            GameObject go = GameObject.Find("DVBoatsRunner");
            if (go == null)
            {
                go = new GameObject("DVBoatsRunner");
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            Instance = go.GetComponent<DVBoatsRunner>();
            if (Instance == null)
                Instance = go.AddComponent<DVBoatsRunner>();

            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }
    }
}

