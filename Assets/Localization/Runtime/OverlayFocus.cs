using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
    /// <summary>
    /// Puts the opened popup on the highest canvas order and remembers it
    /// so remote highlight stays on the panel the player is looking at.
    /// </summary>
    public static class OverlayFocus
    {
        static readonly List<Transform> stack = new List<Transform>();
        static int nextOrder = 400;

        public static void BringToFront(GameObject panel)
        {
            if (panel == null)
                return;

            Canvas canvas = panel.GetComponent<Canvas>();
            if (canvas == null)
                canvas = panel.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = nextOrder++;
            if (panel.GetComponent<GraphicRaycaster>() == null)
                panel.AddComponent<GraphicRaycaster>();

            panel.transform.SetAsLastSibling();
            stack.Remove(panel.transform);
            stack.Add(panel.transform);
        }

        public static Transform Top()
        {
            for (int i = stack.Count - 1; i >= 0; i--)
            {
                Transform t = stack[i];
                if (t != null && t.gameObject.activeInHierarchy)
                    return t;
                stack.RemoveAt(i);
            }
            return null;
        }

        public static void Clear()
        {
            stack.Clear();
            nextOrder = 400;
        }
    }
}
