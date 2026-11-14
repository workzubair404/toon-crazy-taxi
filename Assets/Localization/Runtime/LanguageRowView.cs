using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
    public class LanguageRowView : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] Image background;
        [SerializeField] Button button;
        [SerializeField] Color normalColor = new Color(1f, 1f, 1f, 0.08f);
        [SerializeField] Color selectedColor = new Color(0.95f, 0.62f, 0.78f, 0.95f);

        string code;

        public void Bind(LanguageDefinition definition, bool selected, Action<string> onSelected)
        {
            if (definition == null)
                return;
            code = definition.code;
            if (label != null)
            {
                bool arabic = definition.code == "ar";
                label.text = arabic ? ArabicShaper.Shape(definition.nativeName) : definition.nativeName;
                label.isRightToLeftText = definition.rightToLeft && !arabic;
                TMP_FontAsset font = LocalizationManager.GetFont(definition.fontSetId);
                if (font != null)
                    label.font = font;
                label.enableWordWrapping = true;
                label.overflowMode = TextOverflowModes.Overflow;
            }
            if (background != null)
                background.color = selected ? selectedColor : normalColor;
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (onSelected != null)
                        onSelected(code);
                });
            }
        }
    }
}
