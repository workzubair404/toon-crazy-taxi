using TMPro;
using UnityEngine;

namespace Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedFormatText : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] int argument;
        [SerializeField] float minimumScale = 0.45f;

        TMP_Text text;
        TextAlignmentOptions originalAlignment;
        float originalSize;
        bool captured;

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

        public string Key => key;

        public void Set(string newKey, int newArgument)
        {
            key = newKey;
            argument = newArgument;
            Refresh();
        }

        public void Refresh()
        {
            Cache();
            if (text == null || string.IsNullOrEmpty(key))
                return;
            LocalizedText.Apply(text, LocalizationManager.Format(key, argument), originalAlignment, true, originalSize, minimumScale);
        }

        /// <summary>
        /// Shrinks this label so a longer translation fits. Auto-size stays on.
        /// </summary>
        public void FitOverflow()
        {
            Cache();
            if (text == null)
                return;
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
