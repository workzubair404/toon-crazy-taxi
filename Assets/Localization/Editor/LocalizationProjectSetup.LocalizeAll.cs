using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Localization.EditorTools
{
    public static partial class LocalizationProjectSetup
    {
        static Dictionary<string, string> ExactKeys = new Dictionary<string, string>();
        static Dictionary<string, string> FoldedKeys = new Dictionary<string, string>();
        static Dictionary<string, string> IgnoreKeys = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        static Dictionary<string, Dictionary<string, string>> Tables = new Dictionary<string, Dictionary<string, string>>();
        static Dictionary<string, HashSet<string>> OriginalKeys = new Dictionary<string, HashSet<string>>();
        static HashSet<string> UsedKeys = new HashSet<string>();
        static readonly List<string> CreatedKeys = new List<string>();

        static int BoundCount;
        static int AlreadyCount;
        static bool ContentDirty;

        static readonly HashSet<string> DynamicOwners = new HashSet<string>
        {
            "Tutorial",
            "OrderInfo",
            "ProgressDisplay",
            "UpgradeHandler",
            "StorePanelController",
            "IapResultPanelSupport",
            "TvRemoteHint",
            "DailyRewardPanel",
            "DailyRewardManager",
            "RewardPopup",
            "SettingsManager",
            "LevelBaseFillAchievementItem",
            "UIManager",
            "LevelBaseFillCoinHud",
            "GN_MainMenu"
        };

        [MenuItem("Localization/Localize All")]
        public static void LocalizeAll()
        {
            string previous = SceneManager.GetActiveScene().path;
            if (SceneManager.GetActiveScene().isDirty && !string.IsNullOrEmpty(previous))
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            try
            {
                EditorUtility.DisplayProgressBar("Localization", "Loading string tables", 0.02f);
                LoadTables();

                string[] prefabs = GetContentPrefabPaths();
                for (int i = 0; i < prefabs.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(
                        "Localization",
                        "Prefabs " + (i + 1) + "/" + prefabs.Length,
                        0.05f + 0.45f * (i / (float)Mathf.Max(1, prefabs.Length)));
                    BindPrefab(prefabs[i]);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string[] scenes = GetGameScenePaths();
                for (int i = 0; i < scenes.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("Localization", scenes[i], 0.55f + 0.3f * (i / (float)Mathf.Max(1, scenes.Length)));
                    BindScene(scenes[i]);
                }

                EditorUtility.DisplayProgressBar("Localization", "Writing string tables", 0.9f);
                CollectBoundKeysFromScenes();
                PruneAndWriteTables();
                ApplyKnownTranslations();
                WriteTranslationReport();
                AssetDatabase.Refresh();
                CreateCatalog();
                AssetDatabase.SaveAssets();

                PlaceLanguageUi();

                string report = LocalizationValidator.Validate();
                Debug.Log(report);

                string needsPath = Root + "/needs-translation.txt";
                string needsNote = File.Exists(needsPath)
                    ? "\n\nStill need real translations: Assets/Localization/needs-translation.txt"
                    : string.Empty;
                string placementNote = string.IsNullOrEmpty(LastPlacement) ? "" : "\n\n" + LastPlacement;
                string validationNote = report.StartsWith("Localization validation passed.")
                    ? FirstLine(report)
                    : ValidationSummary(report);
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "Localization",
                        "Bound " + BoundCount + " new labels. " + AlreadyCount + " were already localized.\nNew keys: " + CreatedKeys.Count + ".\n\n" + validationNote + needsNote + placementNote,
                        "OK");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                    EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            }
        }

        public static string[] GetGameScenePaths()
        {
            var paths = new List<string>();
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (!scenes[i].enabled || string.IsNullOrEmpty(scenes[i].path))
                    continue;
                string path = scenes[i].path.Replace('\\', '/');
                if (File.Exists(path) && !paths.Contains(path))
                    paths.Add(path);
            }
            return paths.ToArray();
        }

        public static string[] GetContentPrefabPaths()
        {
            HashSet<string> seen = new HashSet<string>();
            HashSet<string> prefabs = new HashSet<string>();
            Queue<string> pending = new Queue<string>();
            string[] scenes = GetGameScenePaths();
            for (int i = 0; i < scenes.Length; i++)
                pending.Enqueue(scenes[i]);

            while (pending.Count > 0)
            {
                string path = pending.Dequeue();
                if (string.IsNullOrEmpty(path) || !seen.Add(path) || !File.Exists(path))
                    continue;
                MatchCollection links = Regex.Matches(File.ReadAllText(path), "guid:\\s*([0-9a-f]{32})");
                for (int i = 0; i < links.Count; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(links[i].Groups[1].Value);
                    if (string.IsNullOrEmpty(assetPath))
                        continue;
                    assetPath = assetPath.Replace('\\', '/');
                    if (!assetPath.EndsWith(".prefab") || !IsGameContent(assetPath))
                        continue;
                    if (prefabs.Add(assetPath))
                        pending.Enqueue(assetPath);
                }
            }

            List<string> withText = new List<string>();
            foreach (string path in prefabs)
            {
                if (File.ReadAllText(path).IndexOf("m_text:", System.StringComparison.Ordinal) >= 0
                    || File.ReadAllText(path).IndexOf("m_Text:", System.StringComparison.Ordinal) >= 0)
                    withText.Add(path);
            }
            withText.Sort(System.StringComparer.Ordinal);
            return withText.ToArray();
        }

        static bool IsGameContent(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            path = path.Replace('\\', '/');
            if (!path.StartsWith("Assets/"))
                return false;
            if (path.StartsWith("Assets/Localization/"))
                return false;
            if (path.Contains("/Plugins/") || path.Contains("/TextMesh Pro/") || path.Contains("/Demigiant/") || path.Contains("/Examples/"))
                return false;
            return true;
        }

        static void LoadTables()
        {
            BoundCount = 0;
            AlreadyCount = 0;
            CreatedKeys.Clear();
            ExactKeys = new Dictionary<string, string>();
            FoldedKeys = new Dictionary<string, string>();
            IgnoreKeys = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
            UsedKeys.Clear();
            Tables = new Dictionary<string, Dictionary<string, string>>();
            OriginalKeys = new Dictionary<string, HashSet<string>>();

            List<LanguageDefinition> languages = LanguageDefaults.Create();
            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i].code;
                string path = StringsFolder + "/" + code + ".json";
                Dictionary<string, string> table = File.Exists(path)
                    ? LocalizationJson.Parse(File.ReadAllText(path))
                    : new Dictionary<string, string>();
                Tables[code] = table;
                OriginalKeys[code] = new HashSet<string>(table.Keys);
            }

            Dictionary<string, string> english = Tables["en"];
            foreach (KeyValuePair<string, string> pair in english)
                Remember(pair.Value, pair.Key, false);
            foreach (KeyValuePair<string, string> pair in TextKeys)
                Remember(pair.Key, pair.Value, true);
            Remember("No Ads", "ui.store.noAds", true);
        }

        static void Remember(string text, string key, bool overwrite)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
                return;
            string exact = Normalize(text);
            string folded = FoldSpace(exact);
            if (overwrite || !ExactKeys.ContainsKey(exact))
                ExactKeys[exact] = key;
            if (overwrite || !FoldedKeys.ContainsKey(folded))
                FoldedKeys[folded] = key;
            if (overwrite || !IgnoreKeys.ContainsKey(folded))
                IgnoreKeys[folded] = key;
        }

        static bool TryResolve(string value, out string key)
        {
            string exact = Normalize(value);
            if (ExactKeys.TryGetValue(exact, out key))
                return true;
            string folded = FoldSpace(exact);
            if (FoldedKeys.TryGetValue(folded, out key))
                return true;
            return IgnoreKeys.TryGetValue(folded, out key);
        }

        static string ResolveOrCreate(string value)
        {
            string key;
            if (TryResolve(value, out key))
                return key;

            string slug = Slug(value);
            key = "ui.auto." + slug;
            int suffix = 2;
            while (KeyExists(key))
            {
                key = "ui.auto." + slug + "-" + suffix;
                suffix++;
            }
            EnsureKey(key, value);
            Remember(value, key, true);
            return key;
        }

        static bool KeyExists(string key)
        {
            Dictionary<string, string> english;
            return Tables.TryGetValue("en", out english) && english.ContainsKey(key);
        }

        static void EnsureKey(string key, string english)
        {
            Dictionary<string, string> table = Tables["en"];
            string current;
            bool created = !table.TryGetValue(key, out current) || string.IsNullOrEmpty(current);
            MarkUsed(key);
            if (created)
            {
                table[key] = english;
                CreatedKeys.Add(key + " = " + english);
            }
            else
            {
                english = current;
            }

            List<LanguageDefinition> languages = LanguageDefaults.Create();
            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i].code;
                Dictionary<string, string> language;
                if (!Tables.TryGetValue(code, out language))
                {
                    language = new Dictionary<string, string>();
                    Tables[code] = language;
                }
                string value;
                if (language.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                    continue;
                language[key] = code == "en" ? english : Translate(code, key, english);
            }
        }

        static string FormatEnglish(string key)
        {
            if (key == "ui.store.getAmount")
                return "Get {0}";
            if (key == "ui.dailyReward.amount")
                return "GET {0} CASH";
            if (key == "ui.iap.purchaseCoins")
                return "Purchase {0} Coins";
            return key;
        }

        static string MatchFormat(string value, out int argument)
        {
            argument = 0;
            Match amount = Regex.Match(value, @"^Get\s+(\d+)$", RegexOptions.IgnoreCase);
            if (amount.Success && int.TryParse(amount.Groups[1].Value, out argument))
                return "ui.store.getAmount";
            Match cash = Regex.Match(value, @"^GET\s+(\d+)\s+CASH$", RegexOptions.IgnoreCase);
            if (cash.Success && int.TryParse(cash.Groups[1].Value, out argument))
                return "ui.dailyReward.amount";
            Match coins = Regex.Match(value, @"^Purchase\s+(\d+)\s+Coins$", RegexOptions.IgnoreCase);
            if (coins.Success && int.TryParse(coins.Groups[1].Value, out argument))
                return "ui.iap.purchaseCoins";
            return null;
        }

        static string Translate(string language, string key, string english)
        {
            if (language == "en")
                return english;
            return english;
        }

        static void CollectBoundKeysFromScenes()
        {
            if (Tables == null || Tables.Count == 0)
                LoadTables();

            string[] scenes = GetGameScenePaths();
            for (int s = 0; s < scenes.Length; s++)
                EditorSceneManager.OpenScene(scenes[s], OpenSceneMode.Single);

            LegacyLocalizedText[] legacy = Object.FindObjectsOfType<LegacyLocalizedText>(true);
            for (int i = 0; i < legacy.Length; i++)
            {
                if (legacy[i] == null || string.IsNullOrEmpty(legacy[i].Key))
                    continue;
                Text label = legacy[i].GetComponent<Text>();
                string english = label != null ? Normalize(label.text) : legacy[i].Key;
                MarkUsed(legacy[i].Key);
                EnsureKey(legacy[i].Key, english);
            }

            LocalizedText[] bound = Object.FindObjectsOfType<LocalizedText>(true);
            for (int i = 0; i < bound.Length; i++)
            {
                if (bound[i] == null || string.IsNullOrEmpty(bound[i].Key))
                    continue;
                TMP_Text label = bound[i].GetComponent<TMP_Text>();
                string english = label != null ? Normalize(label.text) : bound[i].Key;
                MarkUsed(bound[i].Key);
                EnsureKey(bound[i].Key, english);
            }
        }

        static void PruneAndWriteTables()
        {
            HashSet<string> keep = new HashSet<string>(UsedKeys);
            keep.Add("language.title");
            keep.Add("language.confirm");
            keep.Add("language.close");
            keep.Add("ui.button.language");
            CollectScriptKeys(keep);

            List<LanguageDefinition> languages = LanguageDefaults.Create();
            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i].code;
                Dictionary<string, string> table = Tables[code];
                List<string> remove = new List<string>();
                foreach (string key in table.Keys)
                {
                    if (!keep.Contains(key))
                        remove.Add(key);
                }
                for (int r = 0; r < remove.Count; r++)
                    table.Remove(remove[r]);
                WriteWholeTable(StringsFolder + "/" + code + ".json", table);
            }

            if (CreatedKeys.Count > 0)
                Debug.Log("Localization added " + CreatedKeys.Count + " key(s):\n" + string.Join("\n", CreatedKeys.ToArray()));
        }

        static void CollectScriptKeys(HashSet<string> keep)
        {
            string[] guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets" });
            Regex pattern = new Regex("LocalizationManager\\.(?:Get|GetRaw|Format)\\(\\s*\"([^\"]+)\"", RegexOptions.Compiled);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".cs"))
                    continue;
                string slash = path.Replace('\\', '/');
                if (slash.StartsWith("Assets/Localization/"))
                    continue;
                MatchCollection matches = pattern.Matches(File.ReadAllText(path));
                for (int m = 0; m < matches.Count; m++)
                    keep.Add(matches[m].Groups[1].Value);
            }
        }

        static void WriteWholeTable(string path, Dictionary<string, string> table)
        {
            List<string> keys = new List<string>(table.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            StringBuilder builder = new StringBuilder();
            builder.Append("{\n");
            for (int k = 0; k < keys.Count; k++)
            {
                builder.Append("    \"");
                builder.Append(EscapeJson(keys[k]));
                builder.Append("\": \"");
                builder.Append(EscapeJson(table[keys[k]]));
                builder.Append("\"");
                if (k < keys.Count - 1)
                    builder.Append(",");
                builder.Append("\n");
            }
            builder.Append("}\n");
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
        }

        static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            StringBuilder builder = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '\\': builder.Append("\\\\"); break;
                    case '"': builder.Append("\\\""); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default: builder.Append(c); break;
                }
            }
            return builder.ToString();
        }

        static void WriteTranslationReport()
        {
            Dictionary<string, string> english = Tables["en"];
            StringBuilder builder = new StringBuilder();
            List<string> keys = new List<string>(english.Keys);
            keys.Sort(System.StringComparer.Ordinal);
            List<LanguageDefinition> languages = LanguageDefaults.Create();
            for (int k = 0; k < keys.Count; k++)
            {
                string key = keys[k];
                for (int i = 0; i < languages.Count; i++)
                {
                    string code = languages[i].code;
                    if (code == "en")
                        continue;
                    string value;
                    if (!Tables[code].TryGetValue(key, out value) || value == english[key])
                        builder.AppendLine(code + "\t" + key + "\t" + english[key]);
                }
            }

            string path = Root + "/needs-translation.txt";
            if (builder.Length == 0)
            {
                if (File.Exists(path))
                    File.Delete(path);
                return;
            }
            File.WriteAllText(path, builder.ToString(), new UTF8Encoding(false));
            Debug.Log("Localization needs real translations. See " + path);
        }

        static void CollectOwnership(MonoBehaviour[] behaviours, out HashSet<int> dynamicIds, out HashSet<int> nativeNameIds)
        {
            dynamicIds = new HashSet<int>();
            nativeNameIds = new HashSet<int>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null)
                    continue;
                string typeName = behaviours[i].GetType().Name;
                bool nativeName = typeName == "LanguageRowView";
                bool dynamic = DynamicOwners.Contains(typeName);
                if (!nativeName && !dynamic)
                    continue;
                SerializedObject serialized = new SerializedObject(behaviours[i]);
                SerializedProperty property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference)
                        continue;
                    if (property.objectReferenceValue is TMP_Text text)
                    {
                        if (nativeName)
                            nativeNameIds.Add(text.GetInstanceID());
                        else
                            dynamicIds.Add(text.GetInstanceID());
                        continue;
                    }
                    if (property.objectReferenceValue is Text legacy)
                    {
                        if (nativeName)
                            nativeNameIds.Add(legacy.GetInstanceID());
                        else
                            dynamicIds.Add(legacy.GetInstanceID());
                    }
                }
            }
        }

        static void MarkUsed(string key)
        {
            if (!string.IsNullOrEmpty(key))
                UsedKeys.Add(key);
        }

        static void MarkExisting(GameObject gameObject)
        {
            LocalizedText text = gameObject.GetComponent<LocalizedText>();
            if (text != null)
                MarkUsed(text.Key);
            LocalizedFormatText format = gameObject.GetComponent<LocalizedFormatText>();
            if (format != null)
                MarkUsed(format.Key);
            LegacyLocalizedText legacy = gameObject.GetComponent<LegacyLocalizedText>();
            if (legacy != null)
                MarkUsed(legacy.Key);
        }

        static bool HasLocalization(GameObject gameObject)
        {
            return gameObject.GetComponent<LocalizedText>() != null
                || gameObject.GetComponent<LocalizedFormatText>() != null
                || gameObject.GetComponent<LocalizedDynamicText>() != null
                || gameObject.GetComponent<NonLocalizedText>() != null
                || gameObject.GetComponent<LegacyLocalizedText>() != null;
        }

        static void CollapseLocalization(GameObject gameObject)
        {
            LocalizedFormatText[] formats = gameObject.GetComponents<LocalizedFormatText>();
            LocalizedText[] texts = gameObject.GetComponents<LocalizedText>();
            LocalizedDynamicText[] dynamics = gameObject.GetComponents<LocalizedDynamicText>();
            NonLocalizedText[] plain = gameObject.GetComponents<NonLocalizedText>();
            int total = formats.Length + texts.Length + dynamics.Length + plain.Length;
            if (total <= 1)
                return;
            ContentDirty = true;
            Component keep = formats.Length > 0 ? formats[0] : texts.Length > 0 ? texts[0] : dynamics.Length > 0 ? (Component)dynamics[0] : plain[0];
            DestroyExtra(formats, keep);
            DestroyExtra(texts, keep);
            DestroyExtra(dynamics, keep);
            DestroyExtra(plain, keep);
        }

        static void DestroyExtra<T>(T[] components, Component keep) where T : Component
        {
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i] != keep)
                    Object.DestroyImmediate(components[i]);
            }
        }

        static void EnsureLayout(GameObject gameObject)
        {
            if (gameObject.GetComponent<LocalizedText>() == null && gameObject.GetComponent<LocalizedFormatText>() == null)
                return;
            if (gameObject.GetComponent<LocalizedLayout>() != null)
                return;
            LocalizedLayout layout = gameObject.AddComponent<LocalizedLayout>();
            TMP_Text label = gameObject.GetComponent<TMP_Text>();
            float width = label != null ? label.rectTransform.rect.width : 0f;
            if (width < 2f && label != null)
                width = label.rectTransform.sizeDelta.x;
            SetMinWidth(layout, Mathf.Max(120f, width));
            ContentDirty = true;
        }

        static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        }

        static string FoldSpace(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            StringBuilder builder = new StringBuilder(value.Length);
            bool pending = false;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsWhiteSpace(value[i]))
                {
                    pending = builder.Length > 0;
                    continue;
                }
                if (pending)
                {
                    builder.Append(' ');
                    pending = false;
                }
                builder.Append(value[i]);
            }
            return builder.ToString();
        }

        static string Slug(string value)
        {
            string folded = FoldSpace(value).ToLowerInvariant();
            StringBuilder builder = new StringBuilder();
            bool dash = false;
            for (int i = 0; i < folded.Length; i++)
            {
                char c = folded[i];
                if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    builder.Append(c);
                    dash = false;
                }
                else if (!dash && builder.Length > 0)
                {
                    builder.Append('-');
                    dash = true;
                }
            }
            string slug = builder.ToString().Trim('-');
            if (slug.Length > 40)
                slug = slug.Substring(0, 40).Trim('-');
            return slug.Length == 0 ? "text" : slug;
        }

        static bool IsCounter(string value)
        {
            return value == "∞" || Regex.IsMatch(value, @"^[xX×]\s*\d+$");
        }

        static string FirstLine(string report)
        {
            if (string.IsNullOrEmpty(report))
                return "Done.";
            int index = report.IndexOf('\n');
            return index < 0 ? report : report.Substring(0, index);
        }

        static string ValidationSummary(string report)
        {
            if (string.IsNullOrEmpty(report))
                return "Validation failed.";
            const int maxLines = 5;
            string[] lines = report.Split('\n');
            var builder = new StringBuilder();
            int added = 0;
            for (int i = 0; i < lines.Length && added < maxLines; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("Localization validation", System.StringComparison.Ordinal))
                    continue;
                if (builder.Length > 0)
                    builder.Append('\n');
                builder.Append(line);
                added++;
            }
            if (added == 0)
                return FirstLine(report);
            if (lines.Length > maxLines + 2)
                builder.Append("\n(See Console for full report.)");
            return builder.ToString();
        }

        static void ApplyKnownTranslations()
        {
            if (Tables == null || Tables.Count == 0)
                LoadTables();

            Dictionary<string, string> english;
            if (!Tables.TryGetValue("en", out english))
                return;

            string luckyKey = "ui.auto.lucky-wheel";
            string spinKey = "ui.auto.spin-the-wheel";
            string noAdsKey = "ui.store.noAds";

            List<LanguageDefinition> languages = LanguageDefaults.Create();
            bool dirty = false;
            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i].code;
                if (code == "en")
                    continue;

                Dictionary<string, string> table;
                if (!Tables.TryGetValue(code, out table))
                    continue;

                string luckyEn = english.ContainsKey(luckyKey) ? english[luckyKey] : "Lucky Wheel";
                string spinEn = english.ContainsKey(spinKey) ? english[spinKey] : "Spin the wheel";
                string noAdsEn = english.ContainsKey(noAdsKey) ? english[noAdsKey] : "NO ADS";

                string lucky = KnownText(code, luckyKey, luckyEn);
                string spin = KnownText(code, spinKey, spinEn);
                string noAds = KnownText(code, noAdsKey, noAdsEn);

                dirty |= SetIfEnglishPlaceholder(table, luckyKey, luckyEn, lucky);
                dirty |= SetIfEnglishPlaceholder(table, spinKey, spinEn, spin);
                dirty |= SetIfEnglishPlaceholder(table, noAdsKey, noAdsEn, noAds);
            }

            if (!dirty)
                return;

            for (int i = 0; i < languages.Count; i++)
            {
                string code = languages[i].code;
                Dictionary<string, string> table;
                if (Tables.TryGetValue(code, out table))
                    WriteWholeTable(StringsFolder + "/" + code + ".json", table);
            }
        }

        static bool SetIfEnglishPlaceholder(Dictionary<string, string> table, string key, string english, string translated)
        {
            if (string.IsNullOrEmpty(translated) || translated == english)
                return false;
            string current;
            if (!table.TryGetValue(key, out current) || current != english)
                return false;
            table[key] = translated;
            return true;
        }

        static string KnownText(string code, string key, string english)
        {
            if (key == "ui.auto.lucky-wheel")
            {
                switch (code)
                {
                    case "ar": return "عجلة الحظ";
                    case "zh-Hans": return "幸运转盘";
                    case "zh-Hant": return "幸運轉盤";
                    case "kw": return "Ol Lucky";
                    case "cs": return "Kolo štěstí";
                    case "nl": return "Geluksrad";
                    case "fr": return "Roue de la fortune";
                    case "de": return "Glücksrad";
                    case "el": return "Τροχός της τύχης";
                    case "he": return "גלגל המזל";
                    case "hi": return "लकी व्हील";
                    case "it": return "Ruota della fortuna";
                    case "ja": return "ラッキーホイール";
                    case "kk": return "Сәтті дөңгелек";
                    case "ko": return "럭키 휠";
                    case "pl": return "Koło fortuny";
                    case "pt": return "Roda da sorte";
                    case "ru": return "Колесо удачи";
                    case "es": return "Rueda de la suerte";
                    case "tl": return "Lucky Wheel";
                    case "vi": return "Vòng quay may mắn";
                }
            }
            if (key == "ui.auto.spin-the-wheel")
            {
                switch (code)
                {
                    case "ar": return "أدر العجلة";
                    case "zh-Hans": return "转动转盘";
                    case "zh-Hant": return "轉動轉盤";
                    case "kw": return "Trog an ol";
                    case "cs": return "Točte kolem";
                    case "nl": return "Draai aan het rad";
                    case "fr": return "Faites tourner la roue";
                    case "de": return "Drehe das Rad";
                    case "el": return "Γυρίστε τον τροχό";
                    case "he": return "סובבו את הגלגל";
                    case "hi": return "पहिया घुमाएँ";
                    case "it": return "Gira la ruota";
                    case "ja": return "ホイールを回す";
                    case "kk": return "Дөңгелекті айналдырыңыз";
                    case "ko": return "휠을 돌리세요";
                    case "pl": return "Kręć kołem";
                    case "pt": return "Gire a roleta";
                    case "ru": return "Крутите колесо";
                    case "es": return "Gira la rueda";
                    case "tl": return "Paikutin ang gulong";
                    case "vi": return "Quay vòng";
                }
            }
            if (key == "ui.store.noAds" && code == "it")
                return "SENZA PUBBLICITÀ";

            return english;
        }
    }
}
