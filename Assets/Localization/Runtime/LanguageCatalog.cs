using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Localization
{
    [Serializable]
    public class LanguageDefinition
    {
        public string code;
        public string nativeName;
        public string englishName;
        public bool rightToLeft;
        public string fontSetId;
        public string probe;
    }

    [Serializable]
    public class FontSet
    {
        public string id;
        public TMP_FontAsset primary;
    }

    [CreateAssetMenu(fileName = "LanguageCatalog", menuName = "Localization/Language Catalog")]
    public class LanguageCatalog : ScriptableObject
    {
        public string fallbackCode = "en";
        public List<LanguageDefinition> languages = new List<LanguageDefinition>();
        public List<FontSet> fonts = new List<FontSet>();
        public List<TextAsset> tables = new List<TextAsset>();

        public LanguageDefinition Find(string code)
        {
            if (string.IsNullOrEmpty(code) || languages == null)
                return null;
            for (int i = 0; i < languages.Count; i++)
            {
                if (languages[i] != null && languages[i].code == code)
                    return languages[i];
            }
            return null;
        }

        public TextAsset FindTable(string code)
        {
            if (string.IsNullOrEmpty(code) || tables == null)
                return null;
            for (int i = 0; i < tables.Count; i++)
            {
                if (tables[i] != null && tables[i].name == code)
                    return tables[i];
            }
            return null;
        }

        public TMP_FontAsset FindFont(string fontSetId)
        {
            if (string.IsNullOrEmpty(fontSetId) || fonts == null)
                return null;
            for (int i = 0; i < fonts.Count; i++)
            {
                if (fonts[i] != null && fonts[i].id == fontSetId)
                    return fonts[i].primary;
            }
            return null;
        }
    }
}
