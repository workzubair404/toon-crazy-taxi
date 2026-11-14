using System.Collections.Generic;
using System.IO;
using System.Text;
using Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Localization.EditorTools
{
    public static class LocalizationValidator
    {
        const string StringsFolder = "Assets/Localization/Strings";

        public static string Validate()
        {
            StringBuilder report = new StringBuilder();
            int issues = 0;
            issues += ValidateTables(report);
            issues += ValidateDeviceMap(report);
            issues += ValidatePersistence(report);
            issues += ValidateFonts(report);
            issues += ValidateScenes(report);
            issues += ValidateOverflow(report);
            report.Insert(0, issues == 0
                ? "Localization validation passed.\n"
                : "Localization validation found " + issues + " issue(s).\n");
            return report.ToString();
        }

        static int ValidateTables(StringBuilder report)
        {
            int issues = 0;
            string englishPath = StringsFolder + "/en.json";
            if (!File.Exists(englishPath))
            {
                report.AppendLine("Missing en.json");
                return 1;
            }

            Dictionary<string, string> english = LocalizationJson.Parse(File.ReadAllText(englishPath));
            List<LanguageDefinition> languages = LanguageDefaults.Create();
            for (int i = 0; i < languages.Count; i++)
            {
                string path = StringsFolder + "/" + languages[i].code + ".json";
                if (!File.Exists(path))
                {
                    issues++;
                    report.AppendLine("Missing table " + path);
                    continue;
                }

                Dictionary<string, string> table = LocalizationJson.Parse(File.ReadAllText(path));
                foreach (KeyValuePair<string, string> pair in english)
                {
                    string value;
                    if (!table.TryGetValue(pair.Key, out value) || string.IsNullOrEmpty(value))
                    {
                        issues++;
                        report.AppendLine(languages[i].code + " missing key " + pair.Key);
                    }
                }
                foreach (KeyValuePair<string, string> pair in table)
                {
                    if (!english.ContainsKey(pair.Key))
                    {
                        issues++;
                        report.AppendLine(languages[i].code + " has extra key " + pair.Key);
                    }
                }
            }

            LanguageCatalog catalog = Resources.Load<LanguageCatalog>("LanguageCatalog");
            if (catalog == null)
            {
                issues++;
                report.AppendLine("LanguageCatalog is not in a Resources folder.");
                return issues;
            }
            if (catalog.languages == null || catalog.languages.Count != languages.Count)
            {
                issues++;
                report.AppendLine("Catalog language count does not match the built-in list.");
            }
            for (int i = 0; i < languages.Count; i++)
            {
                if (catalog.FindTable(languages[i].code) == null)
                {
                    issues++;
                    report.AppendLine("Catalog is not referencing " + languages[i].code + ".json");
                }
            }
            return issues;
        }

        static int ValidateDeviceMap(StringBuilder report)
        {
            int issues = 0;
            issues += Expect("zh-TW", "zh-Hant", report);
            issues += Expect("zh-CN", "zh-Hans", report);
            issues += Expect("zh-Hant", "zh-Hant", report);
            issues += Expect("fil-PH", "tl", report);
            issues += Expect("kk-KZ", "kk", report);
            issues += Expect("kw", "kw", report);
            issues += Expect("pt-BR", "pt", report);
            issues += Expect("iw", "he", report);
            issues += Expect("ChineseTraditional", "zh-Hant", report);
            if (DeviceLanguage.MapSystemLanguage(SystemLanguage.Vietnamese) != "vi")
            {
                issues++;
                report.AppendLine("SystemLanguage.Vietnamese did not map to vi.");
            }
            if (DeviceLanguage.MapSystemLanguage(SystemLanguage.Unknown) != LocalizationManager.FallbackCode)
            {
                issues++;
                report.AppendLine("Unknown device language did not fall back to English.");
            }
            return issues;
        }

        static int Expect(string tag, string code, StringBuilder report)
        {
            string mapped = DeviceLanguage.Map(tag);
            if (mapped == code)
                return 0;
            report.AppendLine("Device tag " + tag + " mapped to " + mapped + " instead of " + code);
            return 1;
        }

        static int ValidatePersistence(StringBuilder report)
        {
            string previousCode = PlayerPrefs.GetString(LocalizationManager.PrefLanguageCode, string.Empty);
            int previousChosen = PlayerPrefs.GetInt(LocalizationManager.PrefHasChosen, 0);
            LocalizationManager.ResetForEditor();
            LocalizationManager.SetLanguage("fr", true);
            LocalizationManager.MarkLanguageChosen();
            LocalizationManager.ResetForEditor();
            LocalizationManager.Initialize();
            int issues = 0;
            if (LocalizationManager.CurrentCode != "fr")
            {
                issues++;
                report.AppendLine("Saved language was not restored. Current code is " + LocalizationManager.CurrentCode);
            }
            if (!LocalizationManager.HasChosenLanguage)
            {
                issues++;
                report.AppendLine("Chosen-language flag was not saved.");
            }

            if (string.IsNullOrEmpty(previousCode))
                PlayerPrefs.DeleteKey(LocalizationManager.PrefLanguageCode);
            else
                PlayerPrefs.SetString(LocalizationManager.PrefLanguageCode, previousCode);
            PlayerPrefs.SetInt(LocalizationManager.PrefHasChosen, previousChosen);
            PlayerPrefs.Save();
            LocalizationManager.ResetForEditor();
            return issues;
        }

        static int ValidateFonts(StringBuilder report)
        {
            LocalizationManager.ResetForEditor();
            LocalizationManager.Initialize();
            int issues = 0;
            List<LanguageDefinition> languages = LanguageDefaults.Create();
            for (int i = 0; i < languages.Count; i++)
            {
                LanguageDefinition definition = LocalizationManager.Catalog != null
                    ? LocalizationManager.Catalog.Find(languages[i].code)
                    : null;
                if (definition == null)
                {
                    issues++;
                    report.AppendLine("Catalog is missing " + languages[i].code);
                    continue;
                }
                if (!LocalizationManager.FontSupports(definition))
                {
                    issues++;
                    report.AppendLine("Font probe failed for " + definition.code);
                    continue;
                }

                string path = StringsFolder + "/" + definition.code + ".json";
                if (!File.Exists(path))
                    continue;
                Dictionary<string, string> table = LocalizationJson.Parse(File.ReadAllText(path));
                StringBuilder sample = new StringBuilder();
                foreach (KeyValuePair<string, string> pair in table)
                    sample.Append(pair.Value);
                TMP_FontAsset font = LocalizationManager.GetFont(definition.fontSetId);
                if (font == null || font.atlasTexture == null)
                {
                    issues++;
                    report.AppendLine("Font asset missing for " + definition.code);
                    continue;
                }
                uint[] missing;
                bool supported = true;
                try
                {
                    supported = font.HasCharacters(sample.ToString(), out missing, true, true);
                }
                catch (System.Exception exception)
                {
                    supported = false;
                    report.AppendLine("Font " + definition.fontSetId + " failed while adding glyphs for " + definition.code + ". " + exception.Message);
                }
                if (!supported)
                {
                    issues++;
                    report.AppendLine("Font " + definition.fontSetId + " is missing glyphs used by " + definition.code);
                }
            }
            return issues;
        }

        static int ValidateScenes(StringBuilder report)
        {
            string previous = SceneManager.GetActiveScene().path;
            int issues = 0;
            string[] scenes = BuildScenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorSceneManager.OpenScene(scenes[i], OpenSceneMode.Single);
                TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>(true);
                for (int n = 0; n < labels.Length; n++)
                {
                    TMP_Text label = labels[n];
                    if (label.GetComponent<LocalizedText>() != null || label.GetComponent<LocalizedFormatText>() != null || label.GetComponent<LocalizedDynamicText>() != null || label.GetComponent<NonLocalizedText>() != null)
                        continue;
                    issues++;
                    report.AppendLine(scenes[i] + " has unbound text '" + label.text + "' on " + label.name);
                }

                Text[] legacy = Object.FindObjectsOfType<Text>(true);
                for (int n = 0; n < legacy.Length; n++)
                {
                    Text label = legacy[n];
                    if (label.GetComponent<LegacyLocalizedText>() != null || label.GetComponent<LocalizedDynamicText>() != null || label.GetComponent<NonLocalizedText>() != null)
                        continue;
                    issues++;
                    report.AppendLine(scenes[i] + " has unbound legacy text '" + label.text + "' on " + label.name);
                }

                LocalizedText[] bound = Object.FindObjectsOfType<LocalizedText>(true);
                for (int n = 0; n < bound.Length; n++)
                {
                    string key = bound[n].Key;
                    string raw = LocalizationManager.GetRaw(key);
                    if (raw == key)
                    {
                        issues++;
                        report.AppendLine(scenes[i] + " uses missing key " + key);
                    }
                }
            }

            if (!string.IsNullOrEmpty(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            return issues;
        }

        static string[] BuildScenes => LocalizationProjectSetup.GetGameScenePaths();

        static int ValidateOverflow(StringBuilder report)
        {
            string previous = SceneManager.GetActiveScene().path;
            string[] codes = { "en", "de", "ru", "el", "ar", "he", "ja" };
            int issues = 0;
            for (int s = 0; s < BuildScenes.Length; s++)
            {
                if (!File.Exists(BuildScenes[s]))
                    continue;
                EditorSceneManager.OpenScene(BuildScenes[s], OpenSceneMode.Single);
                List<bool> previousActive = new List<bool>();
                List<GameObject> toggled = new List<GameObject>();
                Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
                for (int i = 0; i < canvases.Length; i++)
                    ActivateHierarchy(canvases[i].gameObject, toggled, previousActive);

                CanvasScaler[] scalers = Object.FindObjectsOfType<CanvasScaler>(true);
                List<Transform> scalerTransforms = new List<Transform>();
                List<Vector3> scalerScales = new List<Vector3>();
                var handle = typeof(CanvasScaler).GetMethod("Handle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                for (int i = 0; i < scalers.Length; i++)
                {
                    if (scalers[i] == null)
                        continue;
                    scalerTransforms.Add(scalers[i].transform);
                    scalerScales.Add(scalers[i].transform.localScale);
                    if (handle != null)
                        handle.Invoke(scalers[i], null);
                }

                Canvas.ForceUpdateCanvases();
                for (int c = 0; c < codes.Length; c++)
                {
                    LocalizationManager.ResetForEditor();
                    LocalizationManager.SetLanguage(codes[c], false);
                    LocalizedText[] labels = Object.FindObjectsOfType<LocalizedText>(true);
                    for (int n = 0; n < labels.Length; n++)
                    {
                        labels[n].Refresh();
                        TMP_Text text = labels[n].GetComponent<TMP_Text>();
                        if (text == null)
                            continue;
                        text.ForceMeshUpdate();
                        if (!text.isTextOverflowing)
                            continue;
                        issues++;
                        report.AppendLine("Overflow " + codes[c] + " " + BuildScenes[s] + " key " + labels[n].Key + " on " + text.name);
                    }
                }

                for (int i = 0; i < toggled.Count; i++)
                {
                    if (toggled[i] != null)
                        toggled[i].SetActive(previousActive[i]);
                }
                for (int i = 0; i < scalerTransforms.Count; i++)
                {
                    if (scalerTransforms[i] != null)
                        scalerTransforms[i].localScale = scalerScales[i];
                }
                DiscardSceneChanges(SceneManager.GetActiveScene());
            }
            LocalizationManager.ResetForEditor();
            if (!string.IsNullOrEmpty(previous) && File.Exists(previous))
                EditorSceneManager.OpenScene(previous, OpenSceneMode.Single);
            return issues;
        }

        static void DiscardSceneChanges(Scene scene)
        {
            try
            {
                var method = typeof(EditorSceneManager).GetMethod(
                    "ClearSceneDirtiness",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (method != null)
                    method.Invoke(null, new object[] { scene });
            }
            catch (System.Exception)
            {
            }
        }

        static void ActivateHierarchy(GameObject gameObject, List<GameObject> toggled, List<bool> previousActive)
        {
            if (!gameObject.activeSelf)
            {
                toggled.Add(gameObject);
                previousActive.Add(false);
                gameObject.SetActive(true);
            }
            for (int i = 0; i < gameObject.transform.childCount; i++)
                ActivateHierarchy(gameObject.transform.GetChild(i).gameObject, toggled, previousActive);
        }

    }
}
