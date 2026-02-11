using UnityEngine;

namespace DV_Boats
{
    internal static class BoatHoverUI
    {
        private static bool visible;
        private static string text;
        
        public static void Show(string t)
        {
            visible = true;
            text = t;
        }
        


        public static void Hide()
        {

            if (visible)

            visible = false;
            text = null;
        }     

        public static void Draw()
        {
            if (!visible || string.IsNullOrEmpty(text))
                return;

            if (Event.current.type != EventType.Repaint)
                return;

            GUI.depth = -1000;

            var style = new GUIStyle(GUI.skin.label);
            style.fontSize = 18;
            style.normal.textColor = Color.cyan;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = false;

            Vector2 size = style.CalcSize(new GUIContent(text));

            float paddingX = 16f;
            float paddingY = 10f;

            float w = size.x + paddingX * 2f;
            float h = size.y + paddingY * 2f;

            float x = (Screen.width - w) * 0.5f;
            float y = (Screen.height * 0.5f) + 60f;

            Rect rect = new Rect(x, y, w, h);

            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.Box(rect, GUIContent.none);
            GUI.color = prev;

            GUI.Label(rect, text, style);
        }
    }
}


namespace DV_Boats
{
    internal class BoatHoverGuiRunner : MonoBehaviour
    {
        private void OnGUI()
        {
            BoatHoverUI.Draw();
        }
    }
}
