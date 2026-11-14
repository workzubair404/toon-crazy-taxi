using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Localization
{
    public static class LocalizationManager
    {
        public const string FallbackCode = "en";
        public const string PrefLanguageCode = "Localization.LanguageCode";
        public const string PrefHasChosen = "Localization.HasChosenLanguage";

        public static event Action LanguageChanged;

        public static LanguageCatalog Catalog { get; private set; }
        public static string CurrentCode { get; private set; } = FallbackCode;
        public static bool IsRightToLeft { get; private set; }
        public static TMP_FontAsset CurrentFont { get; private set; }
        public static bool HasChosenLanguage => PlayerPrefs.GetInt(PrefHasChosen, 0) == 1;

        static readonly Dictionary<string, string> Empty = new Dictionary<string, string>();
        static Dictionary<string, string> currentTable = Empty;
        static Dictionary<string, string> englishTable = Empty;
        static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            Catalog = Resources.Load<LanguageCatalog>("LanguageCatalog");
            englishTable = LoadTable(FallbackCode);
            string saved = PlayerPrefs.GetString(PrefLanguageCode, string.Empty);
            string initial = Catalog != null && Catalog.Find(saved) != null
                ? saved
                : DeviceLanguage.Detect(Catalog);
            Apply(initial, false, false);
            initialized = true;
        }

        public static void SetLanguage(string code, bool persist)
        {
            Initialize();
            Apply(code, persist, true);
        }

        public static void MarkLanguageChosen()
        {
            PlayerPrefs.SetInt(PrefHasChosen, 1);
            if (!string.IsNullOrEmpty(CurrentCode))
                PlayerPrefs.SetString(PrefLanguageCode, CurrentCode);
            PlayerPrefs.Save();
        }

        public static string Get(string key)
        {
            return Shape(GetRaw(key));
        }

        public static string GetRaw(string key)
        {
            Initialize();
            if (string.IsNullOrEmpty(key))
                return string.Empty;
            if (TryGet(currentTable, key, out string value))
                return value;
            if (TryGet(englishTable, key, out string english))
                return english;
            return key;
        }

        public static string Format(string key, params object[] args)
        {
            string template = GetRaw(key);
            if (args == null || args.Length == 0)
                return Shape(template);
            try
            {
                return Shape(string.Format(template, args));
            }
            catch (FormatException)
            {
                return Shape(template);
            }
        }

        public static TMP_FontAsset GetFont(string fontSetId)
        {
            Initialize();
            if (Catalog == null)
                return null;
            return Catalog.FindFont(fontSetId);
        }

        public static bool FontSupports(LanguageDefinition definition)
        {
            if (definition == null)
                return false;
            if (string.IsNullOrEmpty(definition.probe))
                return true;
            TMP_FontAsset font = Catalog != null ? Catalog.FindFont(definition.fontSetId) : null;
            if (font == null)
                return false;
            if (font.atlasTexture == null)
                return false;
            try
            {
                uint[] missing;
                return font.HasCharacters(definition.probe, out missing, true, true);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning("Localization font probe failed for " + definition.code + ". " + exception.Message);
                return false;
            }
        }

        public static void ResetForEditor()
        {
            initialized = false;
            currentTable = Empty;
            englishTable = Empty;
            CurrentFont = null;
            CurrentCode = FallbackCode;
            IsRightToLeft = false;
        }

        static void Apply(string code, bool persist, bool notify)
        {
            LanguageDefinition definition = Catalog != null ? Catalog.Find(code) : null;
            if (definition == null && Catalog != null)
                definition = Catalog.Find(Catalog.fallbackCode);
            if (definition == null)
                definition = Catalog != null ? Catalog.Find(FallbackCode) : null;

            if (definition != null && definition.code != FallbackCode && !FontSupports(definition))
            {
                Debug.LogWarning("Localization font for '" + definition.code + "' is unavailable. Using English.");
                LanguageDefinition fallback = Catalog.Find(FallbackCode);
                if (fallback != null)
                    definition = fallback;
            }

            CurrentCode = definition != null ? definition.code : FallbackCode;
            IsRightToLeft = definition != null && definition.rightToLeft;
            CurrentFont = definition != null && Catalog != null ? Catalog.FindFont(definition.fontSetId) : null;
            if (CurrentFont == null && Catalog != null)
                CurrentFont = Catalog.FindFont("latin");

            currentTable = LoadTable(CurrentCode);
            if (CurrentCode == FallbackCode)
                englishTable = currentTable;

            if (persist)
            {
                PlayerPrefs.SetString(PrefLanguageCode, CurrentCode);
                PlayerPrefs.Save();
            }

            if (notify && LanguageChanged != null)
                LanguageChanged.Invoke();
        }

        static Dictionary<string, string> LoadTable(string code)
        {
            if (Catalog == null)
                return Empty;
            TextAsset asset = Catalog.FindTable(code);
            if (asset == null)
                return Empty;
            return LocalizationJson.Parse(asset.text);
        }

        static bool TryGet(Dictionary<string, string> table, string key, out string value)
        {
            value = null;
            if (table == null)
                return false;
            if (!table.TryGetValue(key, out value))
                return false;
            return !string.IsNullOrEmpty(value);
        }

        static string Shape(string value)
        {
            if (!IsRightToLeft || string.IsNullOrEmpty(value))
                return value;
            if (CurrentCode == "ar")
                return ArabicShaper.Shape(value);
            return value;
        }
    }
}
