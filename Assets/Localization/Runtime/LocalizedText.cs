using TMPro;
using UnityEngine;

namespace Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] bool adaptSize = true;
        [SerializeField] float minimumScale = 0.45f;

        TMP_Text text;
        LocalizedLayout layout;
        TextAlignmentOptions originalAlignment;
        float originalSize;
        bool captured;

        public string Key => key;

        void Awake()
        {
            Cache();
        }

        void OnEnable()
        {
            Cache();
            LocalizationManager.LanguageChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= Refresh;
        }

        public void SetKey(string value)
        {
            key = value;
            Refresh();
        }

        public void Refresh()
        {
            Cache();
            if (text == null || string.IsNullOrEmpty(key))
                return;
            Apply(text, LocalizationManager.Get(key), originalAlignment, adaptSize, originalSize, minimumScale);
            if (layout != null)
                layout.Apply();
        }

        /// <summary>
        /// Shrinks this label so a longer translation fits. Auto-size stays on.
        /// </summary>
        public void FitOverflow()
        {
            Cache();
            if (text == null)
                return;
            adaptSize = true;
            if (minimumScale > 0.3f)
                minimumScale = 0.3f;
            text.enableWordWrapping = true;
            Refresh();
            text.ForceMeshUpdate();
            if (!text.isTextOverflowing)
                return;
            float next = Mathf.Max(18f, (originalSize > 0f ? originalSize : text.fontSize) * 0.85f);
            if (originalSize - next < 0.5f)
                return;
            originalSize = next;
            text.fontSize = next;
            Refresh();
        }

        public static bool ContainsRightToLeft(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if ((c >= '\u0590' && c <= '\u08FF') || (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF'))
                    return true;
            }
            return false;
        }

        public static void Apply(TMP_Text label, string value, TextAlignmentOptions alignment, bool adapt, float size, float minScale)
        {
            if (label == null)
                return;

            TMP_FontAsset font = LocalizationManager.CurrentFont;
            if (font != null)
                label.font = font;

            bool rtl = LocalizationManager.IsRightToLeft && ContainsRightToLeft(value);
            label.isRightToLeftText = rtl && LocalizationManager.CurrentCode != "ar";
            label.alignment = alignment;
            if (adapt)
            {
                label.enableAutoSizing = true;
                label.fontSizeMax = size > 0f ? size : label.fontSize;
                label.fontSizeMin = Mathf.Max(10f, label.fontSizeMax * minScale);
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;
            }
            label.text = value ?? string.Empty;
        }

        void Cache()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
            if (layout == null)
                layout = GetComponent<LocalizedLayout>();
            if (captured || text == null)
                return;
            originalAlignment = text.alignment;
            originalSize = text.fontSize;
            captured = true;
        }
    }
}
