using System.Collections.Generic;

namespace Localization
{
    public static class LanguageDefaults
    {
        public static List<LanguageDefinition> Create()
        {
            return new List<LanguageDefinition>
            {
                Def("ar", "العربية", "Arabic", true, "arabic", "العربية"),
                Def("zh-Hans", "中文（简体）", "Chinese Simplified", false, "sc", "简体中文"),
                Def("zh-Hant", "中文（繁體）", "Chinese Traditional", false, "tc", "繁體中文"),
                Def("kw", "Kernewek", "Cornish", false, "latin", "Kernewek"),
                Def("cs", "Čeština", "Czech", false, "latin", "Čeština"),
                Def("nl", "Nederlands", "Dutch", false, "latin", "Nederlands"),
                Def("en", "English", "English", false, "latin", "English"),
                Def("fr", "Français", "French", false, "latin", "Français"),
                Def("de", "Deutsch", "German", false, "latin", "Deutsch"),
                Def("el", "Ελληνικά", "Greek", false, "latin", "Ελληνικά"),
                Def("he", "עברית", "Hebrew", true, "hebrew", "עברית"),
                Def("hi", "हिन्दी", "Hindi", false, "devanagari", "हिन्दी"),
                Def("it", "Italiano", "Italian", false, "latin", "Italiano"),
                Def("ja", "日本語", "Japanese", false, "jp", "日本語"),
                Def("kk", "Қазақша", "Kazakh", false, "latin", "Қазақша"),
                Def("ko", "한국어", "Korean", false, "kr", "한국어"),
                Def("pl", "Polski", "Polish", false, "latin", "Polski"),
                Def("pt", "Português", "Portuguese", false, "latin", "Português"),
                Def("ru", "Русский", "Russian", false, "latin", "Русский"),
                Def("es", "Español", "Spanish", false, "latin", "Español"),
                Def("tl", "Tagalog", "Tagalog", false, "latin", "Tagalog"),
                Def("vi", "Tiếng Việt", "Vietnamese", false, "latin", "Tiếng Việt")
            };
        }

        static LanguageDefinition Def(string code, string nativeName, string englishName, bool rtl, string fontSetId, string probe)
        {
            return new LanguageDefinition
            {
                code = code,
                nativeName = nativeName,
                englishName = englishName,
                rightToLeft = rtl,
                fontSetId = fontSetId,
                probe = probe
            };
        }
    }
}
