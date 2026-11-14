using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
    [RequireComponent(typeof(Text))]
    public class LegacyLocalizedText : MonoBehaviour
    {
        [SerializeField] string key;
        [SerializeField] bool adaptSize = true;
        [SerializeField] float minimumScale = 0.45f;

        Text text;
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
            text.text = LocalizationManager.Get(key);
            if (adaptSize && captured && text.fontSize > 8)
            {
                float min = Mathf.Max(8f, originalSize * minimumScale);
                if (text.preferredWidth > text.rectTransform.rect.width && text.fontSize > min)
                    text.fontSize = Mathf.RoundToInt(Mathf.Max(min, text.fontSize - 1));
            }
        }

        void Cache()
        {
            if (text == null)
                text = GetComponent<Text>();
            if (!captured && text != null)
            {
                originalSize = text.fontSize;
                captured = true;
            }
        }
    }
}
