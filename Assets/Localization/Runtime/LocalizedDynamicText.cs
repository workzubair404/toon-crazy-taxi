using TMPro;
using UnityEngine;

namespace Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedDynamicText : MonoBehaviour
    {
        TMP_Text text;
        TextAlignmentOptions originalAlignment;
        float originalSize;
        bool captured;
        bool dirty;

        void Awake()
        {
            Cache();
        }

        void OnEnable()
        {
            Cache();
            LocalizationManager.LanguageChanged += MarkDirty;
            MarkDirty();
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= MarkDirty;
        }

        void LateUpdate()
        {
            if (!dirty)
                return;
            dirty = false;
            ApplyStyle();
        }

        public void MarkDirty()
        {
            dirty = true;
        }

        public void ApplyStyle()
        {
            Cache();
            if (text == null)
                return;
            LocalizedText.Apply(text, text.text, originalAlignment, true, originalSize, 0.45f);
        }

        void Cache()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
            if (captured || text == null)
                return;
            originalAlignment = text.alignment;
            originalSize = text.fontSize;
            captured = true;
        }
    }
}
