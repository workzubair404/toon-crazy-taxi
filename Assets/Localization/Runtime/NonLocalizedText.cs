using TMPro;
using UnityEngine;

namespace Localization
{
    [RequireComponent(typeof(TMP_Text))]
    public class NonLocalizedText : MonoBehaviour
    {
        [SerializeField] string reason = "Numeric";

        TMP_Text text;

        void Awake()
        {
            text = GetComponent<TMP_Text>();
        }

        void OnEnable()
        {
            if (text == null)
                text = GetComponent<TMP_Text>();
            LocalizationManager.LanguageChanged += ApplyFont;
            ApplyFont();
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= ApplyFont;
        }

        void ApplyFont()
        {
            if (text == null)
                return;
            TMP_FontAsset font = LocalizationManager.CurrentFont;
            if (font != null)
                text.font = font;
        }
    }
}
