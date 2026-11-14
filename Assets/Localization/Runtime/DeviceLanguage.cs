using System;
using UnityEngine;

namespace Localization
{
    public static class DeviceLanguage
    {
        public static string Detect(LanguageCatalog catalog)
        {
            string fromPlatform = Map(ReadPlatformTag());
            if (IsSupported(catalog, fromPlatform))
                return fromPlatform;

            string fromUnity = MapSystemLanguage(Application.systemLanguage);
            if (IsSupported(catalog, fromUnity))
                return fromUnity;

            if (catalog != null && !string.IsNullOrEmpty(catalog.fallbackCode))
                return catalog.fallbackCode;
            return LocalizationManager.FallbackCode;
        }

        public static string Map(string tag)
        {
            if (string.IsNullOrEmpty(tag))
                return LocalizationManager.FallbackCode;

            string normalized = tag.Replace('_', '-').Trim();
            string lower = normalized.ToLowerInvariant();

            if (lower.StartsWith("zh") || lower == "chinese" || lower == "chinesesimplified" || lower == "chinesetraditional")
            {
                if (lower.Contains("hant") || lower.Contains("tw") || lower.Contains("hk") || lower.Contains("mo") || lower == "chinesetraditional")
                    return "zh-Hant";
                return "zh-Hans";
            }

            string primary = lower.Split('-')[0];
            switch (primary)
            {
                case "ar":
                case "arabic": return "ar";
                case "kw":
                case "cornish": return "kw";
                case "cs":
                case "czech": return "cs";
                case "nl":
                case "dutch": return "nl";
                case "en":
                case "english": return "en";
                case "fr":
                case "french": return "fr";
                case "de":
                case "german": return "de";
                case "el":
                case "greek": return "el";
                case "he":
                case "iw":
                case "hebrew": return "he";
                case "hi":
                case "hindi": return "hi";
                case "it":
                case "italian": return "it";
                case "ja":
                case "japanese": return "ja";
                case "kk":
                case "kazakh": return "kk";
                case "ko":
                case "korean": return "ko";
                case "pl":
                case "polish": return "pl";
                case "pt":
                case "portuguese": return "pt";
                case "ru":
                case "russian": return "ru";
                case "es":
                case "spanish": return "es";
                case "tl":
                case "fil":
                case "tagalog": return "tl";
                case "vi":
                case "vietnamese": return "vi";
                default: return primary;
            }
        }

        public static string MapSystemLanguage(SystemLanguage language)
        {
            switch (language)
            {
                case SystemLanguage.Arabic: return "ar";
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified: return "zh-Hans";
                case SystemLanguage.ChineseTraditional: return "zh-Hant";
                case SystemLanguage.Czech: return "cs";
                case SystemLanguage.Dutch: return "nl";
                case SystemLanguage.English: return "en";
                case SystemLanguage.French: return "fr";
                case SystemLanguage.German: return "de";
                case SystemLanguage.Greek: return "el";
                case SystemLanguage.Hebrew: return "he";
                case SystemLanguage.Hindi: return "hi";
                case SystemLanguage.Italian: return "it";
                case SystemLanguage.Japanese: return "ja";
                case SystemLanguage.Korean: return "ko";
                case SystemLanguage.Polish: return "pl";
                case SystemLanguage.Portuguese: return "pt";
                case SystemLanguage.Russian: return "ru";
                case SystemLanguage.Spanish: return "es";
                case SystemLanguage.Vietnamese: return "vi";
                default: return LocalizationManager.FallbackCode;
            }
        }

        static bool IsSupported(LanguageCatalog catalog, string code)
        {
            return catalog != null && catalog.Find(code) != null;
        }

        static string ReadPlatformTag()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (AndroidJavaClass localeClass = new AndroidJavaClass("java.util.Locale"))
                using (AndroidJavaObject locale = localeClass.CallStatic<AndroidJavaObject>("getDefault"))
                {
                    if (locale != null)
                    {
                        string tag = locale.Call<string>("toLanguageTag");
                        if (!string.IsNullOrEmpty(tag))
                            return tag;
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Localization could not read the Android locale. " + exception.Message);
            }
#endif
            return Application.systemLanguage.ToString();
        }
    }
}
