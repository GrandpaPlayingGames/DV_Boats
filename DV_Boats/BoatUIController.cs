using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DV_Boats
{
    internal class BoatUIController : MonoBehaviour
    {
        private GameObject canvasObj;
        private GameObject panelObj;

        private float currentY;

        private static readonly Vector2 UI_ANCHOR = new Vector2(0.5f, 0f);
        private static readonly Vector2 UI_PIVOT = new Vector2(0.5f, 0f);

        private Text statusText;
        private bool _statusOverrideActive;
        private Coroutine _statusFlashRoutine;
        private string _savedStatusText;
        private Color _savedStatusColor;
        private static Text footerStatusText;

        private static Sprite hornIconSmall;
        private static Sprite hornIconLarge;
        private static Sprite navlightsIcon;
        private static Sprite decklightIcon;
        private static Sprite spotlightIcon;
        private static Sprite ghostBoatIcon;

        private static bool visible;

        private static bool hornActive;
        private static bool foghornActive;

        private static float hornTimer;
        private static float foghornTimer;

        private const float HORN_ACTIVE_TIME = 2.5f;
        private const float FOGHORN_ACTIVE_TIME = 3.0f;


        private static UnityEngine.UI.Button hornButton;
        private static UnityEngine.UI.Button foghornButton;

        private static readonly Color ButtonIdleColor = new Color(1f, 1f, 1f, 0.2f);
        private static readonly Color ButtonActiveColor = new Color(0.2f, 0.9f, 0.2f, 0.85f);

        internal enum HornKind
        {
            Horn,
            FogHorn,
            ghostBoat,
            navlights,
            decklight,
            spotlight
        }

        private Button prevCamBtn;
        private Button nextCamBtn;

        private static Button btnForward;
        private static Button btnBackward;
        private static Button btnLeft;
        private static Button btnRight;

        private UnityEngine.UI.Button ghostBoatButton;
        public static bool ghostBoatOn;
        private UnityEngine.UI.Image ghostBoatButtonImage;
        public static bool GhostBoatOn => ghostBoatOn;

        private static bool moveForwardUIHeld;
        private static bool moveBackwardUIHeld;
        private static bool moveLeftUIHeld;
        private static bool moveRightUIHeld;

        private static bool moveForwardKeyHeld;
        private static bool moveBackwardKeyHeld;
        private static bool moveLeftKeyHeld;
        private static bool moveRightKeyHeld;


        private Image ghostScreenTint;
        private Coroutine ghostTintRoutine;
        private bool ghostOverlayInitialized = false;


        public static bool MoveForwardHeld =>
    moveForwardUIHeld || moveForwardKeyHeld;

        public static bool MoveBackwardHeld =>
            moveBackwardUIHeld || moveBackwardKeyHeld;

        public static bool MoveLeftHeld =>
            moveLeftUIHeld || moveLeftKeyHeld;

        public static bool MoveRightHeld =>
            moveRightUIHeld || moveRightKeyHeld;

        private UnityEngine.UI.Button deckLightButton;
        private UnityEngine.UI.Button navLightButton;
        private UnityEngine.UI.Button spotLightButton;      

        private UnityEngine.UI.Image deckLightButtonImage;
        private UnityEngine.UI.Image navLightButtonImage;
        private UnityEngine.UI.Image spotLightButtonImage;
        private UnityEngine.UI.Image spotLightSwivelCenterButtonImage;

        public static bool deckLightOn;
        public static bool navLightOn;
        public static bool spotLightOn;
        public static bool DeckLightOn => deckLightOn;
        public static bool NavLightOn => navLightOn;

        private static Button btnSpotLightSwivelLeft;
        private static Button btnSpotLightSwivelRight;
        private static Button btnSpotLightSwivelCenter;        
        public static bool SpotLightOn => spotLightOn;

        private static bool spotLightSwivelLeftUIHeld;
        private static bool spotLightSwivelRightUIHeld;

        private static bool spotLightSwivelLeftKeyHeld;
        private static bool spotLightSwivelRightKeyHeld;
        public static bool SpotLightSwivelLeftHeld =>
    spotLightSwivelLeftUIHeld || spotLightSwivelLeftKeyHeld;

        public static bool SpotLightSwivelRightHeld =>
            spotLightSwivelRightUIHeld || spotLightSwivelRightKeyHeld;


        [SerializeField]
        private Light deckLight;

        private Text _camModeLabel;
        private List<string> _camModes;
        private int _camIndex;
        private string currentCameraMode = "FreeRoam";

        public bool hasEnteredBoatCameraThisSession = false;
        public static BoatUIController Instance { get; private set; }

        private static void ___________SYSTEM___________()
        {
        }

        private void Awake()
        {
            Instance = this;
            CreateUI();
        }

        public static void Show()
        {
            EnsureExists();

            if (Instance == null || Instance.canvasObj == null)
                return;

            Instance.RebuildCameraModes();
            Instance.canvasObj.SetActive(true);
            visible = true;
        }


        public static void Hide()
        {
            if (Instance != null && Instance.canvasObj != null)
                Instance.canvasObj.SetActive(false);
            visible = false;
        }

        private void Update()
        {
            UpdateHornUI();
            UpdateMovementUI();
            UpdateLightButtonVisuals();
            UpdateStatusText();
        }
        public static void SetStatus(string text, Color? color = null)
        {
            if (Instance == null || Instance.statusText == null)
                return;

            Instance.statusText.text = text;

            if (color.HasValue)
                Instance.statusText.color = color.Value;
        }

        public static void Toggle()
        {
            if (visible)
                Hide();
            else
                Show();
        }


        private static void ___________CREATE_DRIVING_UI___________()
        {
        }

        private static void EnsureExists()
        {
            if (Instance != null)
                return;

            var go = new GameObject("BoatUIController");
            UnityEngine.Object.DontDestroyOnLoad(go);

            Instance = go.AddComponent<BoatUIController>();
            Instance.CreateUI();
        }        

        private void CreateUI()
        {
            LoadIcons();

            CreateGhostOverlayCanvas();

            Main.Log("Creating UI");
            // ---------------- Canvas ----------------
            canvasObj = new GameObject("BoatUICanvas");
            canvasObj.layer = LayerMask.NameToLayer("UI");

            var canvas = canvasObj.AddComponent<UnityEngine.Canvas>();
            canvas.renderMode = UnityEngine.RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            UnityEngine.Object.DontDestroyOnLoad(canvasObj);

            panelObj = CreateUIElement("BoatUIPanel", canvasObj.transform);

            var panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.85f);

            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = UI_ANCHOR;
            panelRect.anchorMax = UI_ANCHOR;
            panelRect.pivot = UI_PIVOT;
            panelRect.anchoredPosition = new Vector2(0f, 90f);
            panelRect.sizeDelta = new Vector2(900f, 430f);

            SetY(380);

            CreateText(
                "🚤 DV_Boats",
                panelObj.transform,
                24,
                UnityEngine.TextAnchor.UpperCenter,
                0f,
                Color.white,
                600f
            );

            SetY(355);

            CreateText(
                "© 2026 GrandpaPlayingGames",
                panelObj.transform,
                13,
                UnityEngine.TextAnchor.UpperCenter,
                0f,
                new Color(0.7f, 0.7f, 0.7f),
                600f
            );

            SetY(350);
            CreateDivider(panelObj.transform);

            SetY(317);
            statusText = CreateText(
                "Status: Driving mode active",
                panelObj.transform,
                14,
                UnityEngine.TextAnchor.UpperCenter,
                0f,
                Color.yellow,
                600f
            );

            SetY(310);
            CreateDivider(panelObj.transform);

            SetY(260);

            BoatCameraManager.PrimeCameraOwnership();

            CreateText(
                "Camera Mode:",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleLeft,
                -300,
                Color.white,
                200f
            );

            CreateText(
                "GhostBoat:",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleRight,
                205,
                Color.white,
                200f
            );
            
            ghostBoatButton = CreateLabeledButton("", panelObj.transform, 350, new Vector2(40, 28));
            ghostBoatButtonImage = ghostBoatButton.GetComponent<UnityEngine.UI.Image>();
            ghostBoatButton.onClick.AddListener(OnGhostBoatClicked);

            ConfigureButton(
                ghostBoatButton,
                "GhostBoat",
                () => Main.FormatKeybind(
                    Main.Settings.ghostBoatToggleKey,
                    Main.Settings.ghostBoatToggleCtrl,
                    Main.Settings.ghostBoatToggleAlt,
                    Main.Settings.ghostBoatToggleShift
                ),
                ghostBoatIcon,
                iconScale: 1.15f,
                iconPadding: new Vector2(4f, 4f)
            );

            _camModes = new List<string>();
            _camModes.Add("FreeRoam");

            // Load camera config
            BoatCameraConfigLoader.EnsureLoaded();
            Transform boatTf = GetActiveBoatTransform_SAFELY();
            BoatController bc = boatTf != null ? boatTf.GetComponent<BoatController>() : null;
            string boatId = bc != null ? bc.BoatTypeId : null;

            if (!string.IsNullOrEmpty(boatId) &&
    BoatCameraConfigLoader.camerasByBoat.TryGetValue(boatId, out var camDict))
            {
                foreach (var camName in camDict.Keys)
                    _camModes.Add(camName);
            }
            else
            {
                Main.Log("[CAM] No active boat — skipping camera list build");
            }

            _camIndex = 0;

            _camModeLabel = CreateText(
                _camModes[_camIndex],
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleCenter,
                0f,
                Color.white,
                200f
            );   

            prevCamBtn = CreateLabeledButton("<", panelObj.transform, -120, new Vector2(40, 28));
            nextCamBtn = CreateLabeledButton(">", panelObj.transform, 120, new Vector2(40, 28));

            prevCamBtn.onClick.AddListener(OnPrevCameraClicked);
            nextCamBtn.onClick.AddListener(OnNextCameraClicked);

            ConfigureButton(
                prevCamBtn,
                "Previous Camera",
                () => Main.FormatKeybind(
                    Main.Settings.cameraPrevKey,
                    Main.Settings.cameraPrevCtrl,
                    Main.Settings.cameraPrevAlt,
                    Main.Settings.cameraPrevShift    
                )
            );

            ConfigureButton(
                nextCamBtn,
                "Next Camera",
                () => Main.FormatKeybind(
                    Main.Settings.cameraNextKey,
                    Main.Settings.cameraNextCtrl,
                    Main.Settings.cameraNextAlt,
                    Main.Settings.cameraNextShift   
                )
            );

            SetY(240);
            CreateDivider(panelObj.transform);

            SetY(205);
            CreateText(
                "Steering & Rudder Controls",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleCenter
            );

            CreateText(
                "Alert Systems",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleLeft,
                -55,
                Color.white,
                600f
            );

            CreateText(
                "Lighting Systems",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleRight,
                20,
                Color.white,
                600f
            );

            SetY(160);
            btnForward = CreateLabeledButton("▲", panelObj.transform, 0, new Vector2(50, 32));
            AddPointerHandlers(btnForward, OnForwardButtonDown, OnForwardButtonUp);
            //no icon version
            ConfigureButton(
                btnForward,
                "Forward Thrust",
                () => Main.FormatKeybind(
                    Main.Settings.forwardKey,
                    Main.Settings.forwardCtrl,
                    Main.Settings.forwardAlt,
                    Main.Settings.forwardShift    
                )
            );

            SetY(150);
            CreateText(
                "Horn",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleLeft,
                -280,
                Color.white,
                175f
            );

            CreateText(
                "Foghorn",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleLeft,
                -177,
                Color.white,
                200f
            );

            CreateText(
                "Deck",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleRight,
                160,
                Color.white,
                200f
            );

            CreateText(
                "Nav",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleRight,
                82,
                Color.white,
                200f
            );

            CreateText(
                "Spot",
                panelObj.transform,
                15,
                UnityEngine.TextAnchor.MiddleRight,
                260,
                Color.white,
                200f
            );

            SetY(110);
            btnLeft = CreateLabeledButton("◄", panelObj.transform, -70, new Vector2(50, 32));
            btnRight = CreateLabeledButton("►", panelObj.transform, 70, new Vector2(50, 32));
            AddPointerHandlers(btnLeft, OnLeftButtonDown, OnLeftButtonUp);
            AddPointerHandlers(btnRight, OnRightButtonDown, OnRightButtonUp);

            ConfigureButton(
                           btnLeft,
                           "Steer Left",
                           () => Main.FormatKeybind(
                               Main.Settings.leftKey,
                               Main.Settings.leftCtrl,
                               Main.Settings.leftAlt,
                               Main.Settings.leftShift    
                           )
                       );

            ConfigureButton(
                           btnRight,
                           "Steer Right",
                           () => Main.FormatKeybind(
                               Main.Settings.rightKey,
                               Main.Settings.rightCtrl,
                               Main.Settings.rightAlt,
                               Main.Settings.rightShift     
                           )
                       );

            hornButton =
                CreateLabeledButton(
                    "",
                    panelObj.transform,
                    -350,
                    new Vector2(50, 32)
                );

            hornButton.onClick.AddListener(OnHornButtonClicked);

            ConfigureButton(
                hornButton,
                "Horn",
                () => Main.FormatKeybind(
                    Main.Settings.hornKey,
                    Main.Settings.hornCtrl,
                    Main.Settings.hornAlt,
                    Main.Settings.hornShift
                ),
                hornIconSmall,
                iconScale: 1.25f,
                iconPadding: new Vector2(2f, 2f)
            );            

            foghornButton =
                CreateLabeledButton(
                    "",
                    panelObj.transform,
                    -250,
                    new Vector2(50, 32)
                );

            foghornButton.onClick.AddListener(OnFoghornButtonClicked);

            ConfigureButton(
                foghornButton,
                "Horn",
                () => Main.FormatKeybind(
                    Main.Settings.foghornKey,
                    Main.Settings.foghornCtrl,
                    Main.Settings.foghornAlt,
                    Main.Settings.foghornShift
                ),
                hornIconLarge,
                iconScale: 1.25f,
                iconPadding: new Vector2(2f, 2f)
            );
           
            deckLightButton = CreateLabeledButton("", panelObj.transform, 245, new Vector2(50, 32));
            navLightButton = CreateLabeledButton("", panelObj.transform, 170, new Vector2(50, 32));
            spotLightButton = CreateLabeledButton("", panelObj.transform, 350, new Vector2(50, 32));
            deckLightButtonImage = deckLightButton.GetComponent<UnityEngine.UI.Image>();
            navLightButtonImage = navLightButton.GetComponent<UnityEngine.UI.Image>();
            spotLightButtonImage = spotLightButton.GetComponent<UnityEngine.UI.Image>();
            deckLightButton.onClick.AddListener(OnDeckLightClicked);
            navLightButton.onClick.AddListener(OnNavLightClicked);
            spotLightButton.onClick.AddListener(OnSpotLightClicked);

            ConfigureButton(
                deckLightButton,
                "Deck Light",
                () => Main.FormatKeybind(
                    Main.Settings.DeckLightKey,
                    Main.Settings.DeckLightCtrl,
                    Main.Settings.DeckLightAlt,
                    Main.Settings.DeckLightShift
                ),
                decklightIcon,
                iconScale: 1.25f,
                iconPadding: new Vector2(2f, 2f)
            );

            ConfigureButton(
                navLightButton,
                "Nav Lights",
                () => Main.FormatKeybind(
                    Main.Settings.NavLightKey,
                    Main.Settings.NavLightCtrl,
                    Main.Settings.NavLightAlt,
                    Main.Settings.NavLightShift
                ),
                navlightsIcon,
                iconScale: 1.15f,
                iconPadding: new Vector2(4f, 4f)
            );

            ConfigureButton(
                spotLightButton,
                "Spotlight",
                () => Main.FormatKeybind(
                    Main.Settings.SpotLightKey,
                    Main.Settings.SpotLightCtrl,
                    Main.Settings.SpotLightAlt,
                    Main.Settings.SpotLightShift
                ),
                spotlightIcon,
                iconScale: 1.15f,
                iconPadding: new Vector2(4f, 4f)
            );


            SetY(60);
            btnBackward = CreateLabeledButton("▼", panelObj.transform, 0, new Vector2(50, 32));
            AddPointerHandlers(btnBackward, OnBackwardButtonDown, OnBackwardButtonUp);
            ConfigureButton(
                           btnBackward,
                           "Reverse Thrust",
                           () => Main.FormatKeybind(
                               Main.Settings.backwardKey,
                               Main.Settings.backwardCtrl,
                               Main.Settings.backwardAlt,
                               Main.Settings.backwardShift    
                           )
                       );

            btnSpotLightSwivelLeft = CreateLabeledButton("←", panelObj.transform, 305, new Vector2(32, 32));
            AddPointerHandlers(btnSpotLightSwivelLeft, OnSpotLightSwivelLeftButtonDown, OnSpotLightSwivelLeftButtonUp);
            btnSpotLightSwivelRight = CreateLabeledButton("→", panelObj.transform, 395, new Vector2(32, 32));
            AddPointerHandlers(btnSpotLightSwivelRight, OnSpotLightSwivelRightButtonDown, OnSpotLightSwivelRightButtonUp);
            btnSpotLightSwivelCenter = CreateLabeledButton("││", panelObj.transform, 350, new Vector2(32, 32));
            btnSpotLightSwivelCenter.onClick.AddListener(OnSpotLightSwivelCenterClicked);

            ConfigureButton(
                           btnSpotLightSwivelLeft,
                           "Swivel Left",
                           () => Main.FormatKeybind(
                               Main.Settings.SpotLightSwivelLeftKey,
                               Main.Settings.SpotLightSwivelLeftCtrl,
                               Main.Settings.SpotLightSwivelLeftAlt,
                               Main.Settings.SpotLightSwivelLeftShift   
                           )
                       );
            ConfigureButton(
                           btnSpotLightSwivelRight,
                           "Swivel Right",
                           () => Main.FormatKeybind(
                               Main.Settings.SpotLightSwivelRightKey,
                               Main.Settings.SpotLightSwivelRightCtrl,
                               Main.Settings.SpotLightSwivelRightAlt,
                               Main.Settings.SpotLightSwivelRightShift     
                           )
                       );
            ConfigureButton(
                           btnSpotLightSwivelCenter,
                           "Center Spotlight",
                           () => Main.FormatKeybind(
                               Main.Settings.SpotLightSwivelCenterKey,
                               Main.Settings.SpotLightSwivelCenterCtrl,
                               Main.Settings.SpotLightSwivelCenterAlt,
                               Main.Settings.SpotLightSwivelCenterShift    
                           )
                       );



            SetY(45);
            CreateDivider(panelObj.transform);

            SetY(15);

            string hideShowText =
                "Press " +
                Main.FormatKeybind(
                    Main.Settings.showHideUIKey,
                    Main.Settings.showHideUICtrl,
                    Main.Settings.showHideUIAlt,
                    Main.Settings.showHideUIShift
                ) +
                " to Hide/Show Controls - Press " +
                Main.FormatKeybind(
                    Main.Settings.exitDriveKey,
                    Main.Settings.exitDriveCtrl,
                    Main.Settings.exitDriveAlt,
                    Main.Settings.exitDriveShift
                ) +
                " to Exit Driving Mode";

            footerStatusText =
                CreateText(
                    hideShowText,
                    panelObj.transform,
                    14,
                    TextAnchor.MiddleCenter,
                    0f,
                    Color.yellow
                );

            canvasObj.SetActive(false);            
        }

        private static void LoadIcons()
        {
            string basePath =
                Path.Combine(
                    Main.ModEntry.Path,
                    "Assets",
                    "Icons"
                );

            hornIconSmall =
                IconLoader.LoadSprite(
                    Path.Combine(basePath, "hornIconSmall.png")
                );

            hornIconLarge =
                IconLoader.LoadSprite(
                    Path.Combine(basePath, "hornIconLarge.png")
                );
            navlightsIcon =
                IconLoader.LoadSprite(
                    Path.Combine(basePath, "navlightsIcon.png")
                );
            decklightIcon =
                IconLoader.LoadSprite(
                    Path.Combine(basePath, "decklightIcon.png")
                );
            spotlightIcon =
                IconLoader.LoadSprite(
                    Path.Combine(basePath, "spotlightIcon.png")
                );
            ghostBoatIcon =
                IconLoader.LoadSprite(
                    Path.Combine(basePath, "ghostboatIcon.png")
                );
        }


        private static void ___________UI_HELPERS___________()
        {
        }

        private void SetY(float y)
        {
            currentY = y;
        }

        private Vector2 GetUIPos(float x = 0f)
        {
            return new Vector2(x, currentY);
        }

        private GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = UI_ANCHOR;
            rt.anchorMax = UI_ANCHOR;
            rt.pivot = UI_PIVOT;
            rt.anchoredPosition = GetUIPos();

            return go;
        }

        private UnityEngine.UI.Text CreateText(
            string text,
            Transform parent,
            int fontSize,
            UnityEngine.TextAnchor anchor,
            float xOffset = 0f,
            Color? color = null,
            float width = 800f)
        {
            var go = CreateUIElement("Text", parent);
            var txt = go.AddComponent<UnityEngine.UI.Text>();

            txt.text = text;
            txt.font = Resources.GetBuiltinResource<UnityEngine.Font>("Arial.ttf");
            txt.fontSize = fontSize;
            txt.alignment = anchor;
            txt.color = color ?? Color.white;

            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            txt.verticalOverflow = VerticalWrapMode.Overflow;

            var rt = txt.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, fontSize + 10);
            rt.anchoredPosition = GetUIPos(xOffset);

            return txt;
        }

        private void CreateDivider(Transform parent)
        {
            var go = CreateUIElement("Divider", parent);
            var img = go.AddComponent<UnityEngine.UI.Image>();
            img.color = new Color(1f, 1f, 1f, 0.25f);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(820f, 2f);
        }

        private Button CreateLabeledButton(
            string label,
            Transform parent,
            float xOffset,
            Vector2? size = null,
            int fontSize = 16,
            Color? backgroundColor = null,
            Color? textColor = null)
        {
            Vector2 finalSize = size ?? new Vector2(60, 30);
            Color bgColor = backgroundColor ?? new Color(1f, 1f, 1f, 0.2f);
            Color fgColor = textColor ?? Color.white;

            var go = CreateUIElement("Button_" + label, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = finalSize;

            bool isDropdownChild = (parent != null && parent.name.Contains("CamModePanel"));

            if (!isDropdownChild)
            {
                rect.anchorMin = UI_ANCHOR;
                rect.anchorMax = UI_ANCHOR;
                rect.pivot = UI_PIVOT;
                rect.anchoredPosition = GetUIPos(xOffset);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = Vector2.zero;
            }

            var image = go.AddComponent<Image>();
            image.color = bgColor;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;

            var textGo = CreateUIElement("Label", go.transform);
            var txt = textGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize = fontSize;
            txt.color = fgColor;

            var txtRect = textGo.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            go.transform.SetAsLastSibling();
            return btn;
        }

        private static void ConfigureButton(
            Button button,
            string actionLabel,
            Func<string> keybindTextProvider,
            Sprite iconSprite = null,
            float iconScale = 1f,
            Vector2? iconPadding = null
        )
        {
            if (button == null)
                return;

            var info = button.gameObject.AddComponent<UIButtonActionInfo>();
            info.ActionLabel = actionLabel;
            info.GetKeybindText = keybindTextProvider;

            button.gameObject.AddComponent<UIButtonHoverStatus>();

            if (iconSprite == null)
                return;

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
                label.gameObject.SetActive(false);

            var iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(button.transform, false);

            var iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.color = new Color(1f, 1f, 1f, 0.65f);
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            var rect = iconGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;

            Vector2 pad = iconPadding ?? new Vector2(2f, 2f);
            rect.offsetMin = pad;
            rect.offsetMax = -pad;

            iconGO.transform.localScale = Vector3.one * iconScale;
        }


        private static void ___________HORNS___________()
        {
        }
        private static void OnHornButtonClicked()
        {
            if (hornActive)
                return;

            hornActive = true;
            hornTimer = HORN_ACTIVE_TIME;
            SetButtonVisual(hornButton, true);

            BoatDriveManager.ActiveBoat?.PlayHorn();
        }

        private static void UpdateHornUI()
        {
            if (hornActive)
            {
                hornTimer -= Time.deltaTime;
                if (hornTimer <= 0f)
                {
                    hornActive = false;
                    SetButtonVisual(hornButton, false);
                }
            }

            if (foghornActive)
            {
                foghornTimer -= Time.deltaTime;
                if (foghornTimer <= 0f)
                {
                    foghornActive = false;
                    SetButtonVisual(foghornButton, false);
                }
            }
        }

        private static void OnFoghornButtonClicked()
        {
            if (foghornActive)
                return;

            foghornActive = true;
            foghornTimer = FOGHORN_ACTIVE_TIME;
            SetButtonVisual(foghornButton, true);

            BoatDriveManager.ActiveBoat?.PlayFoghorn();
        }

        private static void SetButtonVisual(
            UnityEngine.UI.Button btn,
            bool active)
        {
            if (btn == null)
                return;

            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = active ? ButtonActiveColor : ButtonIdleColor;
        }

        public void TriggerHornFromInput()
        {
            OnHornButtonClicked();
        }

        public void TriggerFoghornFromInput()
        {
            OnFoghornButtonClicked();
        }

        private static void ___________STEERAGE__________()
        {
        }
        private static void UpdateMovementUI()
        {
            SetButtonVisual(btnForward, moveForwardUIHeld || moveForwardKeyHeld);
            SetButtonVisual(btnBackward, moveBackwardUIHeld || moveBackwardKeyHeld);
            SetButtonVisual(btnLeft, moveLeftUIHeld || moveLeftKeyHeld);
            SetButtonVisual(btnRight, moveRightUIHeld || moveRightKeyHeld);
            SetButtonVisual(btnSpotLightSwivelLeft, spotLightSwivelLeftUIHeld || spotLightSwivelLeftKeyHeld);
            SetButtonVisual(btnSpotLightSwivelRight, spotLightSwivelRightUIHeld || spotLightSwivelRightKeyHeld);
        }


        private static void OnForwardButtonDown()
        {
            moveForwardUIHeld = true;
            SetButtonVisual(btnForward, true);
        }

        private static void OnForwardButtonUp()
        {
            moveForwardUIHeld = false;
            SetButtonVisual(btnForward, false);
        }

        private static void OnBackwardButtonDown()
        {
            moveBackwardUIHeld = true;
            SetButtonVisual(btnBackward, true);
        }

        private static void OnBackwardButtonUp()
        {
            moveBackwardUIHeld = false;
            SetButtonVisual(btnBackward, false);
        }

        private static void OnLeftButtonDown()
        {
            moveLeftUIHeld = true;
            SetButtonVisual(btnLeft, true);
        }

        private static void OnLeftButtonUp()
        {
            moveLeftUIHeld = false;
            SetButtonVisual(btnLeft, false);
        }

        private static void OnRightButtonDown()
        {
            moveRightUIHeld = true;
            SetButtonVisual(btnRight, true);
        }

        private static void OnRightButtonUp()
        {
            moveRightUIHeld = false;
            SetButtonVisual(btnRight, false);
        }

        private static void AddPointerHandlers(
            Button btn,
            System.Action onDown,
            System.Action onUp)
        {
            if (btn == null)
                return;
            string myName = btn.gameObject.name;

            var trigger = btn.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = btn.gameObject.AddComponent<EventTrigger>();
         
            if (trigger.triggers == null)
                trigger.triggers = new System.Collections.Generic.List<UnityEngine.EventSystems.EventTrigger.Entry>();

            var down = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            down.callback.AddListener(_ => onDown());
            trigger.triggers.Add(down);

            var up = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            up.callback.AddListener(_ => onUp());
            trigger.triggers.Add(up);

            var exit = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exit.callback.AddListener(_ => onUp());
            trigger.triggers.Add(exit);
        }


        public static void SetMoveForwardFromKey(bool held)
        {
            moveForwardKeyHeld = held;
        }

        public static void SetMoveBackwardFromKey(bool held)
        {
            moveBackwardKeyHeld = held;
        }

        public static void SetMoveLeftFromKey(bool held)
        {
            moveLeftKeyHeld = held;
        }

        public static void SetMoveRightFromKey(bool held)
        {
            moveRightKeyHeld = held;
        }

        private static void ___________GHOSTBOAT__________()
        {
        }

        private void OnGhostBoatClicked()
        {
            TriggerGhostBoatFromInput();
        }

        internal void TriggerGhostBoatFromInput()
        {
            ghostBoatOn = !ghostBoatOn;
            UpdateGhostBoatVisuals();

            if (ghostBoatOn)
            {
                FlashStatus("Ghost Boat Enabled", Color.red, 2f);
                EnableGhostScreenTint();
            }
            else
            {
                FlashStatus("Ghost Boat Disabled", Color.red, 2f);
                DisableGhostScreenTint();
            }
        }

        private void UpdateGhostBoatVisuals()
        {
            if (ghostBoatButtonImage != null)
                ghostBoatButtonImage.color = ghostBoatOn
                    ? ButtonActiveColor
                    : ButtonIdleColor;

        }

        private void EnableGhostScreenTint()
        {
            if (ghostScreenTint == null)
                return;

            ghostScreenTint.enabled = true;

            if (ghostTintRoutine != null)
                StopCoroutine(ghostTintRoutine);

            ghostTintRoutine = StartCoroutine(GhostScreenTintPulse());
        }

        private void CreateGhostOverlayCanvas()
        {
            if (ghostOverlayInitialized)
                return;

            ghostOverlayInitialized = true;

            GameObject canvasGO = new GameObject("GhostOverlayCanvas");
            DontDestroyOnLoad(canvasGO);

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvas.sortingOrder = -100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            canvasGO.AddComponent<GraphicRaycaster>();

            GameObject tintGO = new GameObject("GhostScreenTint");
            tintGO.transform.SetParent(canvasGO.transform, false);

            RectTransform rt = tintGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            ghostScreenTint = tintGO.AddComponent<Image>();
            ghostScreenTint.color = new Color(0.05f, 0.05f, 0.05f, 0f);
            ghostScreenTint.raycastTarget = false;
            ghostScreenTint.enabled = false;
        }

        private void DisableGhostScreenTint()
        {
            if (ghostTintRoutine != null)
            {
                StopCoroutine(ghostTintRoutine);
                ghostTintRoutine = null;
            }

            if (ghostScreenTint != null)
            {
                Color c = ghostScreenTint.color;
                c.a = 0f;
                ghostScreenTint.color = c;
                ghostScreenTint.enabled = false;
            }
        }

        private IEnumerator GhostScreenTintPulse()
        {
            const float maxAlpha = 0.85f; 
            const float minAlpha = 0.25f; 
            const float period = 3.0f;    
            const float rampTime = 0.74f; 

            float t = 0f;
            float ramp = 0f;

            while (true)
            {
                float dt = Time.unscaledDeltaTime;
                t += dt * (2f * Mathf.PI / period);

                ramp = Mathf.Min(1f, ramp + dt / rampTime);
 
                float s = (Mathf.Sin(t) + 1f) * 0.5f;
                s = s * s * (3f - 2f * s);

                float aMin = Mathf.Lerp(0f, minAlpha, ramp);
                float aMax = Mathf.Lerp(0f, maxAlpha, ramp);

                float alpha = Mathf.Lerp(aMin, aMax, s);

                Color c = ghostScreenTint.color;
                c.a = alpha;
                ghostScreenTint.color = c;

                yield return null;
            }
        }

        private static void ___________LIGHTS__________()
        {
        }

        private void OnDeckLightClicked()
        {
            TriggerDeckLightFromInput();
        }
        private void OnNavLightClicked()
        {
            TriggerNavLightFromInput();
        }
        private void OnSpotLightClicked()
        {
            TriggerSpotLightFromInput();
        }
        private void OnSpotLightSwivelCenterClicked()
        {
            TriggerSpotLightSwivelCenterFromInput();
        }

        private void UpdateLightButtonVisuals()
        {
            if (deckLightButtonImage != null)
                deckLightButtonImage.color = deckLightOn
                    ? ButtonActiveColor
                    : ButtonIdleColor;

            if (navLightButtonImage != null)
                navLightButtonImage.color = navLightOn
                    ? ButtonActiveColor
                    : ButtonIdleColor;

            if (spotLightButtonImage != null)
                spotLightButtonImage.color = spotLightOn
                    ? ButtonActiveColor
                    : ButtonIdleColor;

            if (spotLightSwivelCenterButtonImage != null)
                spotLightSwivelCenterButtonImage.color = spotLightOn
                    ? ButtonActiveColor
                    : ButtonIdleColor;
        }

        internal void TriggerDeckLightFromInput()
        {
            deckLightOn = !deckLightOn;
            UpdateLightButtonVisuals();
        }

        internal void TriggerNavLightFromInput()
        {
            navLightOn = !navLightOn;
            UpdateLightButtonVisuals();
        }

        private static void _______SPOTLIGHT___________()
        {
        }

        internal void TriggerSpotLightFromInput()
        {
            spotLightOn = !spotLightOn;
            UpdateLightButtonVisuals();
        }

        private static void OnSpotLightSwivelLeftButtonDown()
        {
            spotLightSwivelLeftUIHeld = true;
            SetButtonVisual(btnSpotLightSwivelLeft, true);
        }

        private static void OnSpotLightSwivelRightButtonDown()
        {
            spotLightSwivelRightUIHeld = true;
            SetButtonVisual(btnSpotLightSwivelRight, true);
        }

        private static void OnSpotLightSwivelLeftButtonUp()
        {
            spotLightSwivelLeftUIHeld = false;
            SetButtonVisual(btnSpotLightSwivelLeft, false);
        }

        private static void OnSpotLightSwivelRightButtonUp()
        {
            spotLightSwivelRightUIHeld = false;
            SetButtonVisual(btnSpotLightSwivelRight, false);
        }

        private Coroutine spotLightCenterFlash;

        internal void TriggerSpotLightSwivelCenterFromInput()
        {
            if (spotLightCenterFlash != null)
                StopCoroutine(spotLightCenterFlash);

            spotLightCenterFlash = StartCoroutine(
                UIHelpers.FlashButtonColor(
                    btnSpotLightSwivelCenter,
                    Color.green,
                    0.125f,
                    () => spotLightCenterFlash = null
                )
            );

            BoatDriveManager.ActiveBoat?.ResetSpotLightPose();
        }


        public static void SetSpotLightSwivelLeftFromKey(bool held)
        {
            spotLightSwivelLeftKeyHeld = held;
        }

        public static void SetSpotLightSwivelRightFromKey(bool held)
        {
            spotLightSwivelRightKeyHeld = held;
        }


        public static void ResetlightState()
        {
            spotLightOn = false;
            deckLightOn = false;    
            navLightOn = false;
            if (Instance != null)
                Instance.UpdateLightButtonVisuals();
        }

        private static void ___________STATUS_TEXT___________()
        {
        }
        private void UpdateStatusText()
        {
            if (_statusOverrideActive)
                return;

            var activeBoat = BoatDriveManager.ActiveBoat;

            if (activeBoat != null)
            {
                int heading = Mathf.RoundToInt(activeBoat.CurrentHeading);
                int speed = Mathf.RoundToInt(activeBoat.CurrentSpeedKmh);

                statusText.text =
                    $"Heading: {heading:000}°   Speed: {speed} km/h";
            }
            else
            {
                statusText.text = ""; 
            }
        }

        public void FlashStatus(string message, Color color, float seconds = 2f)
        {
            if (statusText == null)
                return;

            if (!_statusOverrideActive)
            {
                _savedStatusText = statusText.text;
                _savedStatusColor = statusText.color;
            }

            _statusOverrideActive = true;

            statusText.text = message;
            statusText.color = color;

            UIHelpers.SetFlashing(statusText, true, color, flashHz: 2.5f);

            if (_statusFlashRoutine != null)
                StopCoroutine(_statusFlashRoutine);

            _statusFlashRoutine = StartCoroutine(FlashStatusRoutine(seconds));
        }

        private IEnumerator FlashStatusRoutine(float seconds)
        {
            float t = seconds;

            while (t > 0f)
            {
                t -= Time.unscaledDeltaTime;
                yield return null;
            }

            UIHelpers.SetFlashing(statusText, false, _savedStatusColor);

            statusText.text = _savedStatusText;
            statusText.color = _savedStatusColor;

            _statusOverrideActive = false;
            _statusFlashRoutine = null;
        }


        public static void SetFooterStatus(string text)
        {
            if (Instance == null || footerStatusText == null)
                return;

            footerStatusText.text = text;
        }

        public static void ResetFooterStatus()
        {
            SetFooterStatus(BuildDefaultFooterStatusText());
        }
        public static void ShowDefaultFooterStatus()
        {
            SetStatus(BuildDefaultFooterStatusText());
        }

        public static void ShowFooterHoverStatus(string hoverText)
        {
            SetStatus(hoverText);
        }

        private static string BuildDefaultFooterStatusText()
        {
            return
                "Press " +
                Main.FormatKeybind(
                    Main.Settings.showHideUIKey,
                    Main.Settings.showHideUICtrl,
                    Main.Settings.showHideUIAlt,
                    Main.Settings.showHideUIShift
                ) +
                " to Hide/Show Controls - Press " +
                Main.FormatKeybind(
                    Main.Settings.exitDriveKey,
                    Main.Settings.exitDriveCtrl,
                    Main.Settings.exitDriveAlt,
                    Main.Settings.exitDriveShift
                ) +
                " to Exit Driving Mode";
        }

        private static void ___________CAMERAS___________()
        {
        }
        
        public void OnPrevCameraClicked()
        {
            if (_camModes == null || _camModes.Count == 0)
                return;

            int nextIndex = _camIndex - 1;
            if (nextIndex < 0)
                nextIndex = _camModes.Count - 1;

            if (nextIndex > 0 && !CanEnterBoatCameraMode())
            {
                Main.Log("[BoatUI] Blocked BoatCam (Prev) — player not grounded");
                StartCoroutine(FlashMustBeGroundedForBoatCam());
                return; 
            }

            string nextMode = _camModes[nextIndex];

            if (!ApplyCameraMode(nextMode))
            {
                _camIndex = 0;
                if (_camModeLabel != null)
                    _camModeLabel.text = _camModes[0];
                return;
            }

            _camIndex = nextIndex;
            if (_camModeLabel != null)
                _camModeLabel.text = nextMode;

            FlashPrevCameraButton();
        }

        public void OnNextCameraClicked()
        {
            if (_camModes == null || _camModes.Count == 0)
                return;

            int nextIndex = _camIndex + 1;
            if (nextIndex >= _camModes.Count)
                nextIndex = 0;
 
            if (nextIndex > 0 && !CanEnterBoatCameraMode())
            {
                Main.Log("[BoatUI] Blocked BoatCam (Next) — player not grounded");
                StartCoroutine(FlashMustBeGroundedForBoatCam());
                return;
            }

            string nextMode = _camModes[nextIndex];

            if (!ApplyCameraMode(nextMode))
            {
                _camIndex = 0;
                if (_camModeLabel != null)
                    _camModeLabel.text = _camModes[0];
                return;
            }

            _camIndex = nextIndex;
            if (_camModeLabel != null)
                _camModeLabel.text = nextMode;

            FlashNextCameraButton();
        }

        private Coroutine prevCamFlash;

        private void FlashPrevCameraButton()
        {
            if (prevCamBtn == null || !prevCamBtn.gameObject.activeInHierarchy)
                return;

            if (prevCamFlash != null)
                StopCoroutine(prevCamFlash);

            prevCamFlash = StartCoroutine(
                UIHelpers.FlashButtonColor(
                    prevCamBtn,
                    Color.green,
                    0.125f,
                    () => prevCamFlash = null
                )
            );
        }

        private Coroutine nextCamFlash;

        private void FlashNextCameraButton()
        {
            if (nextCamBtn == null || !nextCamBtn.gameObject.activeInHierarchy)
                return;

            if (nextCamFlash != null)
                StopCoroutine(nextCamFlash);

            nextCamFlash = StartCoroutine(
                UIHelpers.FlashButtonColor(
                    nextCamBtn,
                    Color.green,
                    0.125f,
                    () => nextCamFlash = null
                )
            );
        }
 
        private Transform _originalCameraParent;
        private Vector3 _originalCameraLocalPos;
        private Quaternion _originalCameraLocalRot;
        private bool _hasCachedCameraPose;

        private bool ApplyCameraMode(string mode)
        {
            string requestedMode = mode;

            Transform boatTf = GetActiveBoatTransform_SAFELY();
            Camera cam = PlayerManager.ActiveCamera;

            if (requestedMode == "FreeRoam")
            {
                BoatFollowAnchor.Instance.StopFollowing();

                BoatCameraManager.ActivateFreeRoam();

                if (hasEnteredBoatCameraThisSession && boatTf != null)
                {
                    BoatController bcm = boatTf.GetComponent<BoatController>();

                    if (bcm != null)
                    {
                        BoatCameraManager.PlacePlayerOnBoat(bcm);
                    }
                }
                else
                {
                    Main.Log("[DV_Boats] FreeRoam (RC-style) → no teleport.");
                }

                cam = PlayerManager.ActiveCamera;
                if (cam != null && _hasCachedCameraPose)
                {
                    cam.transform.SetParent(_originalCameraParent, false);
                    cam.transform.localPosition = _originalCameraLocalPos;
                    cam.transform.localRotation = _originalCameraLocalRot;
                }

                BoatFollowAnchor.Instance.StopFollowing();

                currentCameraMode = "FreeRoam";
                return true;
            }

            boatTf = GetActiveBoatTransform_SAFELY();
            BoatController bc = boatTf != null ? boatTf.GetComponent<BoatController>() : null;
            string boatId = bc != null ? bc.BoatTypeId : null;


            if (string.IsNullOrEmpty(boatId) ||
                !BoatCameraConfigLoader.camerasByBoat.TryGetValue(boatId, out var camSet) ||
                !camSet.TryGetValue(requestedMode, out CameraViewDefinition def))
            {
                return ForceFreeRoamFallback();
            }


            if (boatTf == null)
            {
                Main.Log("[DV_Boats] ❌ Cannot apply boat camera: boat transform null.");
                return ForceFreeRoamFallback();
            }

            hasEnteredBoatCameraThisSession = true;

            Quaternion initialRot =
                BoatCameraManager.ComputeInitialAnchorRotation(
                    def,
                    boatTf,
                    boatTf.TransformPoint(def.offset)
                );

            BoatFollowAnchor.Instance.BeginFollowing(boatTf, def.offset, initialRot);

            cam = PlayerManager.ActiveCamera;
            if (cam == null)
            {
                Main.Log("[DV_Boats] ❌ ActiveCamera null — cannot apply boat camera.");
                return false;
            }

             if (!_hasCachedCameraPose)
            {
                _originalCameraParent = cam.transform.parent;
                _originalCameraLocalPos = cam.transform.localPosition;
                _originalCameraLocalRot = cam.transform.localRotation;
                _hasCachedCameraPose = true;
            }

            cam.transform.SetParent(BoatFollowAnchor.Instance.transform, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            currentCameraMode = requestedMode;
            return true;
        }

        private bool ForceFreeRoamFallback()
        {
            currentCameraMode = "FreeRoam";
            BoatFollowAnchor.Instance.StopFollowing();
            BoatCameraManager.ActivateFreeRoam();
            return true;
        }

        private void RebuildCameraModes()
        {
            Main.Log("[CAM] Rebuilding camera list");

            string previousMode = null;
            if (_camModes != null &&
                _camIndex >= 0 &&
                _camIndex < _camModes.Count)
            {
                previousMode = _camModes[_camIndex];
            }

            _camModes.Clear();
            _camModes.Add("FreeRoam");

            BoatCameraConfigLoader.EnsureLoaded();

            Transform boatTf = GetActiveBoatTransform_SAFELY();
            if (boatTf == null)
            {
                Main.Log("[CAM] No active boat transform");
                _camIndex = 0;
                if (_camModeLabel != null)
                    _camModeLabel.text = _camModes[0];
                return;
            }

            BoatController bc = boatTf.GetComponentInChildren<BoatController>();
            if (bc == null || string.IsNullOrEmpty(bc.BoatTypeId))
            {
                Main.Log("[CAM] No BoatController or BoatTypeId");
                _camIndex = 0;
                if (_camModeLabel != null)
                    _camModeLabel.text = _camModes[0];
                return;
            }

            if (BoatCameraConfigLoader.camerasByBoat.TryGetValue(
                bc.BoatTypeId, out var camDict))
            {
                foreach (var camName in camDict.Keys)
                    _camModes.Add(camName);

                Main.Log($"[CAM] Added {camDict.Count} camera modes for {bc.BoatTypeId}");
            }
            else
            {
                Main.Log($"[CAM] No camera profile found for boatId={bc.BoatTypeId}");
            }

            if (!string.IsNullOrEmpty(previousMode))
            {
                int restoredIndex = _camModes.IndexOf(previousMode);
                _camIndex = (restoredIndex >= 0) ? restoredIndex : 0;
            }
            else
            {
                _camIndex = 0;
            }

            if (_camModeLabel != null && _camModes.Count > 0)
            {
                _camModeLabel.text = _camModes[_camIndex];
            }
        }

        private Transform GetActiveBoatTransform_SAFELY()
        {
            return BoatDriveManager.ActiveBoat != null
                ? BoatDriveManager.ActiveBoat.transform
                : null;
        }

        public void ResetCameraSelectionToFreeRoam()
        {
            _camIndex = 0;

            _camModeLabel.text = _camModes[0];
 
            Main.Log("[BoatUI] Camera selection reset to FreeRoam");
        }

        private static void ___________PLAYER_MOVEMENT___________()
        {
        }

        public void ForceExitToFreeRoamAndPlacePlayerOnBoat(BoatController boat)
        {
            BoatFollowAnchor.Instance?.StopFollowing();

            if (_hasCachedCameraPose && PlayerManager.ActiveCamera != null)
            {
                Transform camTf = PlayerManager.ActiveCamera.transform;

                camTf.SetParent(_originalCameraParent, false);
                camTf.localPosition = _originalCameraLocalPos;
                camTf.localRotation = _originalCameraLocalRot;
            }
            else
            {
                Main.Log("[BoatExit] WARNING: No cached camera pose to restore");
            }

            Transform playerTf = PlayerManager.PlayerTransform;

            if (playerTf != null && boat != null)
            {
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

                if (PlayerManager.ActiveCamera != null)
                {
                    Transform camTf = PlayerManager.ActiveCamera.transform;
                    Vector3 euler = camTf.eulerAngles;
                    camTf.rotation = Quaternion.Euler(euler.x, euler.y, 0f);
                }
            }
            else
            {
                Main.Log("[BoatExit] WARNING: Player or boat missing");
            }

            _hasCachedCameraPose = false;
        }

        private bool IsF3CameraActive()
        {
            try
            {
                Camera cam = PlayerManager.ActiveCamera;
                if (cam == null)
                    return false;

                 string camName = cam.name.ToLowerInvariant();

                return camName.Contains("external");
            }
            catch
            {
                return false;
            }
        }

        private Coroutine warningRoutine;

        private IEnumerator FlashMustBeGroundedForBoatCam()
        {
            if (warningRoutine != null)
                StopCoroutine(warningRoutine);

            warningRoutine = StartCoroutine(_FlashMustBeGroundedForBoatCam());
            yield break;
        }

        private IEnumerator _FlashMustBeGroundedForBoatCam()
        {
            if (statusText == null)
                yield break;

            _statusOverrideActive = true;

            string oldText = statusText.text;
            Color oldColor = statusText.color;


            statusText.text = "Must be on ground to enter Boat Camera";
            statusText.color = Color.yellow;

            UIHelpers.SetFlashing(statusText, true, Color.yellow, 1.5f);

            Main.Log("[BoatUI] ⚠ Player must be grounded to enter Boat Camera");

            yield return new WaitForSeconds(3.5f);

            // Restore
            UIHelpers.SetFlashing(statusText, false, oldColor, 1.5f);
            statusText.text = oldText;
            statusText.color = oldColor;

            _statusOverrideActive = false;
            warningRoutine = null;
        }

        private bool CanEnterBoatCameraMode()
        {
            if (IsF3CameraActive())
                return false;

            var playerTf = PlayerManager.PlayerTransform;
            if (playerTf == null)
                return false;

            var cc = playerTf.GetComponent<CharacterController>();
            if (cc == null)
                return false;

            if (!cc.isGrounded)
                return false;

            Vector3 origin = playerTf.position + Vector3.up * 0.1f;
            if (!Physics.Raycast(origin, Vector3.down, 1.3f, ~0, QueryTriggerInteraction.Ignore))
                return false;

            return true;
        }


        private static void ___________LEGACY_OR_REDUNDANT___________()
        {
        }
    }        
}

namespace DV_Boats
{    
    public class UIButtonActionInfo : MonoBehaviour
    {
        public string ActionLabel;
        public Func<string> GetKeybindText;
    }

    public class UIButtonHoverStatus :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private UIButtonActionInfo info;

        private void Awake()
        {
            info = GetComponent<UIButtonActionInfo>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {

            if (info == null || info.GetKeybindText == null)
                return;

            string text =
                "Keybinds: " +
                info.ActionLabel + ": " +
                info.GetKeybindText() +
                ",     Hide/Show UI: " +
                Main.FormatKeybind(
                    Main.Settings.showHideUIKey,
                    Main.Settings.showHideUICtrl,
                    Main.Settings.showHideUIAlt,
                    Main.Settings.showHideUIShift
                ) +
                ",     Exit Driving: " +
                Main.FormatKeybind(
                    Main.Settings.exitDriveKey,
                    Main.Settings.exitDriveCtrl,
                    Main.Settings.exitDriveAlt,
                    Main.Settings.exitDriveShift
                );

            BoatUIController.SetFooterStatus(text);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            BoatUIController.ResetFooterStatus();
        }        
    }
}
