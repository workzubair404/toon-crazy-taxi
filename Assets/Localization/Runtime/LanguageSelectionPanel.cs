using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
    public class LanguageSelectionPanel : MonoBehaviour
    {
        [SerializeField] LocalizedText title;
        [SerializeField] RectTransform content;
        [SerializeField] LanguageRowView rowPrefab;
        [SerializeField] Button confirmButton;
        [SerializeField] Button closeButton;

        bool persistOnSelect;
        Action onClosed;
        bool built;

        void Awake()
        {
            if (confirmButton != null)
                confirmButton.onClick.AddListener(Confirm);
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        void OnEnable()
        {
            LocalizationManager.LanguageChanged += RefreshSelection;
        }

        void OnDisable()
        {
            LocalizationManager.LanguageChanged -= RefreshSelection;
        }

        public void Show(bool persistOnSelect, bool requireConfirm, Action onClosed)
        {
            this.persistOnSelect = persistOnSelect;
            this.onClosed = onClosed;
            if (confirmButton != null)
                confirmButton.gameObject.SetActive(requireConfirm);
            if (closeButton != null)
                closeButton.gameObject.SetActive(!requireConfirm);
            gameObject.SetActive(true);
            OverlayFocus.BringToFront(gameObject);
            Rebuild();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void Confirm()
        {
            LocalizationManager.SetLanguage(LocalizationManager.CurrentCode, true);
            LocalizationManager.MarkLanguageChosen();
            Action closed = onClosed;
            Hide();
            if (closed != null)
                closed();
        }

        public void Close()
        {
            Action closed = onClosed;
            Hide();
            if (closed != null)
                closed();
        }

        void RefreshSelection()
        {
            if (isActiveAndEnabled)
                Rebuild();
        }

        void Rebuild()
        {
            if (content == null || rowPrefab == null || LocalizationManager.Catalog == null)
                return;

            if (!built)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                    Destroy(content.GetChild(i).gameObject);
                LanguageCatalog catalog = LocalizationManager.Catalog;
                for (int i = 0; i < catalog.languages.Count; i++)
                {
                    LanguageRowView row = Instantiate(rowPrefab, content);
                    row.gameObject.SetActive(true);
                    row.name = catalog.languages[i].code;
                }
                built = true;
            }

            string current = LocalizationManager.CurrentCode;
            int rowIndex = 0;
            LanguageCatalog languages = LocalizationManager.Catalog;
            for (int i = 0; i < content.childCount && rowIndex < languages.languages.Count; i++)
            {
                LanguageRowView row = content.GetChild(i).GetComponent<LanguageRowView>();
                if (row == null)
                    continue;
                LanguageDefinition definition = languages.languages[rowIndex];
                row.Bind(definition, definition.code == current, Select);
                rowIndex++;
            }

            if (title != null)
                title.Refresh();
        }

        void Select(string code)
        {
            LocalizationManager.SetLanguage(code, persistOnSelect);
            Rebuild();
        }
    }
}
