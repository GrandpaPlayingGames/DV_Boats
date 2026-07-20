using DV_Boats;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;
public static class UIHelpers
{
    public static bool DialogOpen = false;

    private class DialogKeyboardController : MonoBehaviour
    {
        private Button[] buttons;
        private int selectedIndex = 0;

        private static readonly Color SelectedColor = new Color(0.30f, 0.75f, 0.30f, 1f);
        private static readonly Color UnselectedColor = new Color(0.15f, 0.35f, 0.15f, 1f);

        public void Init(Button[] btns, int defaultIndex = 0)
        {
            buttons = btns;
            selectedIndex = Mathf.Clamp(defaultIndex, 0, buttons.Length - 1);
            UpdateHighlight();
        }

        void Update()
        {
            if (!DialogOpen)
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                selectedIndex = (selectedIndex + 1) % buttons.Length;
                UpdateHighlight();
            }

            if (Input.GetKeyDown(KeyCode.Escape) && buttons.Length == 2)
            {
                DialogOpen = false;
                buttons[1].onClick.Invoke(); // No
            }

             if (Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                DialogOpen = false;
                buttons[selectedIndex].onClick.Invoke();
            }
        }


        void OnGUI()
        {
            if (!UIHelpers.DialogOpen)
                return;

            Event e = Event.current;
            if (e == null)
                return;

              if (e.type == EventType.KeyDown)
            {
                switch (e.keyCode)
                {
                    case KeyCode.Tab:
                    case KeyCode.Return:
                    case KeyCode.KeypadEnter:
                    case KeyCode.Escape:
                        e.Use();               
                        GUIUtility.hotControl = 0; 
                        GUIUtility.keyboardControl = 0;
                        return;
                }
            }
        }


        private void UpdateHighlight()
        {
            if (buttons == null) return;

            for (int i = 0; i < buttons.Length; i++)
            {
                Image img = buttons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.color = (i == selectedIndex)
                        ? SelectedColor
                        : UnselectedColor;
                }
            }
        }
    }


    public static void ShowDialog(string title, string message, Action onYes, Action onNo, float scale = 1f)
    {
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            UnityEngine.Object.DontDestroyOnLoad(es);
        }

        GameObject canvasGO = new GameObject("ReplayDialogCanvas");
        UnityEngine.Object.DontDestroyOnLoad(canvasGO);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGO.AddComponent<GraphicRaycaster>();

        GameObject panelGO = new GameObject("DialogPanel");
        panelGO.transform.SetParent(canvasGO.transform, false);

        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.sizeDelta = new Vector2(300f * scale, 150f * scale);
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;

        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.85f);

        GameObject titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        Text titleText = titleGO.AddComponent<Text>();
        RectTransform titleRT = titleGO.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 1f);
        titleRT.anchorMax = new Vector2(0.5f, 1f);
        titleRT.pivot = new Vector2(0.5f, 1f);
        titleRT.anchoredPosition = new Vector2(0, -0);
        titleRT.sizeDelta = new Vector2(560 * scale, 40 * scale);
        titleText.text = title;
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = Mathf.RoundToInt(12 * scale); // optional
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        GameObject msgGO = new GameObject("Message");
        msgGO.transform.SetParent(panelGO.transform, false);
        Text msgText = msgGO.AddComponent<Text>();
        RectTransform msgRT = msgGO.GetComponent<RectTransform>();
        msgRT.anchorMin = new Vector2(0.5f, 0.5f);
        msgRT.anchorMax = new Vector2(0.5f, 0.5f);
        msgRT.pivot = new Vector2(0.5f, 0.5f);
        msgRT.anchoredPosition = new Vector2(0, 0);
        msgRT.sizeDelta = new Vector2(560 * scale, 100 * scale);
        msgText.text = message;
        msgText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        msgText.fontSize = Mathf.RoundToInt(9 * scale); // optional
        msgText.color = Color.white;
        msgText.alignment = TextAnchor.MiddleCenter;

        DialogOpen = true;

        if (onNo == null)
        {
            Button okBtn = CreateButton("OK", "OK", new Vector2(0, -50), panelGO.transform, () =>
            {
                DialogOpen = false;
                GameObject.Destroy(canvasGO);
                onYes?.Invoke();
            }, scale);

            var controller = canvasGO.AddComponent<DialogKeyboardController>();
            controller.Init(new Button[] { okBtn }, 0);

            canvasGO.AddComponent<AutoDestroyAfterSeconds>().Init(5f, onYes);
        }
        else
        {
            Button yesBtn = CreateButton("Yes", "Yes", new Vector2(-50, -50), panelGO.transform, () =>
            {
                DialogOpen = false;
                GameObject.Destroy(canvasGO);
                onYes?.Invoke();
            }, scale);

            Button noBtn = CreateButton("No", "No", new Vector2(50, -50), panelGO.transform, () =>
            {
                DialogOpen = false;
                GameObject.Destroy(canvasGO);
                onNo?.Invoke();
            }, scale);

            var controller = canvasGO.AddComponent<DialogKeyboardController>();
            controller.Init(new Button[] { yesBtn, noBtn }, 0);
        }

    }

    private static Button CreateButton(string name, string label, Vector2 pos, Transform parent, Action onClick, float scale)
    {
        GameObject buttonGO = new GameObject(name + "Button");
        buttonGO.transform.SetParent(parent, false);

        RectTransform btnRT = buttonGO.AddComponent<RectTransform>();
        btnRT.sizeDelta = new Vector2(60f * scale, 20f * scale);  
        btnRT.anchoredPosition = pos * scale;                    

        Button button = buttonGO.AddComponent<Button>();
        Image img = buttonGO.AddComponent<Image>();

        img.color = new Color(0.15f, 0.35f, 0.15f, 1f);

        button.onClick.AddListener(() => onClick());

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(buttonGO.transform, false);

        Text btnText = txtGO.AddComponent<Text>();
        RectTransform txtRT = txtGO.GetComponent<RectTransform>();

        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero;
        txtRT.offsetMax = Vector2.zero;

        btnText.text = label;
        btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.white;

        // scale font
        btnText.fontSize = Mathf.RoundToInt(12 * scale);

        return button;
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }

    public class AutoDestroyAfterSeconds : MonoBehaviour
    {
        private float delay;
        private Action onDone;

        public void Init(float seconds, Action callback)
        {
            delay = seconds;
            onDone = callback;
        }


        private void Update()
        {
            delay -= Time.unscaledDeltaTime;
            if (delay <= 0f)
            {
                onDone?.Invoke();
                Destroy(this.gameObject);
            }
        }
    }

    public static void SetFlashing(Graphic g, bool enable, Color? onColor = null, float flashHz = 1.5f)
    {
        if (g == null) return;

        var fl = g.GetComponent<DVReplay.UIFlasher>();
        if (enable)
        {
            if (fl == null) fl = g.gameObject.AddComponent<DVReplay.UIFlasher>();
            var c = onColor ?? g.color;
            fl.onColor = c;
            fl.offColor = new Color(c.r, c.g, c.b, 0.25f);
            fl.speedHz = flashHz;
        }
        else
        {
            if (fl != null) UnityEngine.Object.Destroy(fl);
            
            if (onColor.HasValue) g.color = onColor.Value;
        }
    }

    
    public static void SetFlashing(Text t, bool enable, Color? onColor = null, float flashHz = 1.5f)
        => SetFlashing((Graphic)t, enable, onColor, flashHz);

    public static IEnumerator FlashButtonColor(
     Button btn,
     Color flashColor,
     float duration,
     System.Action onFinished = null
 )
    {
        if (btn == null)
            yield break;

        Image img = btn.GetComponent<Image>();
        if (img == null)
            yield break;

        Color original = img.color;

        img.color = flashColor;
        yield return new WaitForSeconds(duration);
        img.color = original;

        if (EventSystem.current != null &&
            EventSystem.current.currentSelectedGameObject == btn.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        onFinished?.Invoke();
    }


}


namespace DVReplay
{
    [RequireComponent(typeof(Graphic))]

    public class UIFlasher : MonoBehaviour
    {
        public Color onColor = Color.yellow;
        public Color offColor = new Color(1f, 1f, 0f, 0.25f);
        public float speedHz = 1.5f;

        Graphic g;
        float t;

        void Awake() { g = GetComponent<Graphic>(); }

        void OnEnable() { t = 0f; }


        void Update()
        {
            t += Time.unscaledDeltaTime * speedHz * Mathf.PI * 2f;
            float s = (Mathf.Sin(t) + 1f) * 0.5f;
            g.color = Color.Lerp(offColor, onColor, s);
        }
    }
}



public static class IconLoader
{
    public static Sprite LoadSprite(string fullPath)
    {
        if (!File.Exists(fullPath))
        {
            Main.Log($"[IconLoader] File not found: {fullPath}");
            return null;
        }

        byte[] data = File.ReadAllBytes(fullPath);

        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(data);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        return Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
    }
}
