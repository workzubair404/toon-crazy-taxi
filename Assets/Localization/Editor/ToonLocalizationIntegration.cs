using System.Collections.Generic;
using System.IO;
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
        internal static string LastPlacement = "";

        public static void BindLegacyUiLabels(HashSet<int> dynamicIds, HashSet<int> nativeNameIds)
        {
            Text[] labels = Object.FindObjectsOfType<Text>(true);
            for (int i = 0; i < labels.Length; i++)
                BindLegacyLabel(labels[i], dynamicIds, nativeNameIds);
        }

        static void BindLegacyLabel(Text label, HashSet<int> dynamicIds, HashSet<int> nativeNameIds)
        {
            if (label == null)
                return;

            if (label.GetComponent<LegacyLocalizedText>() != null
                || label.GetComponent<NonLocalizedText>() != null
                || label.GetComponent<LocalizedDynamicText>() != null)
            {
                AlreadyCount++;
                LegacyLocalizedText existing = label.GetComponent<LegacyLocalizedText>();
                if (existing != null && !string.IsNullOrEmpty(existing.Key))
                {
                    MarkUsed(existing.Key);
                    EnsureKey(existing.Key, Normalize(label.text));
                }
                return;
            }

            if (nativeNameIds.Contains(label.GetInstanceID()) || dynamicIds.Contains(label.GetInstanceID()))
            {
                label.gameObject.AddComponent<LocalizedDynamicText>();
                ContentDirty = true;
                BoundCount++;
                return;
            }

            string value = Normalize(label.text);
            if (string.IsNullOrEmpty(value) || IsNumeric(value) || IsCounter(value))
            {
                label.gameObject.AddComponent<NonLocalizedText>();
                ContentDirty = true;
                BoundCount++;
                return;
            }

            LegacyLocalizedText localized = label.gameObject.AddComponent<LegacyLocalizedText>();
            SetLegacyKey(localized, ResolveOrCreate(value));
            ContentDirty = true;
            BoundCount++;
        }

        static void SetLegacyKey(LegacyLocalizedText localized, string key)
        {
            SerializedObject serialized = new SerializedObject(localized);
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            MarkUsed(key);
        }

        public static void PlaceLanguageUi()
        {
            LastPlacement = "";
            string[] scenes = GetGameScenePaths();
            if (scenes.Length == 0)
            {
                LastPlacement = "No scenes are enabled in Build Settings. The language panel was not placed.";
                Debug.LogError("Localization: " + LastPlacement);
                return;
            }

            string first = scenes[0];
            string menu = FindMainMenuScene(scenes);
            string menuName = Path.GetFileNameWithoutExtension(menu);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (prefab == null)
            {
                LastPlacement = "SelectLanguagePanel prefab is missing.";
                Debug.LogError("Localization: " + LastPlacement);
                return;
            }

            EditorSceneManager.OpenScene(first, OpenSceneMode.Single);
            GameLocalizationHost[] hosts = Object.FindObjectsOfType<GameLocalizationHost>(true);
            GameLocalizationHost host = hosts.Length > 0 ? hosts[0] : null;
            if (hosts.Length > 1)
            {
                for (int i = 1; i < hosts.Length; i++)
                    Object.DestroyImmediate(hosts[i].gameObject);
            }

            if (host == null)
            {
                GameObject root = new GameObject("Localization");
                host = root.AddComponent<GameLocalizationHost>();
                GameObject panelObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                panelObject.name = "SelectLanguagePanel";
                panelObject.transform.SetParent(root.transform, false);
                EnsureOverlayCanvas(panelObject);
                LanguageSelectionPanel panel = panelObject.GetComponent<LanguageSelectionPanel>();
                SerializedObject hostObject = new SerializedObject(host);
                hostObject.FindProperty("panel").objectReferenceValue = panel;
                hostObject.FindProperty("mainMenuSceneName").stringValue = menuName;
                hostObject.ApplyModifiedPropertiesWithoutUndo();
                panelObject.SetActive(false);
            }
            else
            {
                SerializedObject hostObject = new SerializedObject(host);
                hostObject.FindProperty("mainMenuSceneName").stringValue = menuName;
                hostObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            EditorSceneManager.OpenScene(menu, OpenSceneMode.Single);
            string buttonPath = EnsureLanguageButtonForToon();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            LastPlacement = "Language panel is in " + first + " on Localization. It opens when " + menuName + " loads. Language button: " + buttonPath;
            Debug.Log("Localization: " + LastPlacement);
        }

        static string FindMainMenuScene(string[] scenes)
        {
            string fallback = null;
            for (int i = 0; i < scenes.Length; i++)
            {
                string name = Path.GetFileNameWithoutExtension(scenes[i]).ToLowerInvariant();
                if (name.Contains("splash") || name.Contains("load") || name.Contains("boot") || name.Contains("intro") || name.Contains("logo"))
                    continue;
                if (fallback == null)
                    fallback = scenes[i];
                if (name.Contains("menu") || name.Contains("main") || name.Contains("home"))
                    return scenes[i];
            }
            return fallback != null ? fallback : scenes[0];
        }

        static string EnsureLanguageButtonForToon()
        {
            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name == "Language")
                    return GetPath(buttons[i].transform);
            }

            Transform parent = FindLanguageButtonParentForToon();
            if (parent == null)
                return "not added. No settings panel or main menu button was found.";

            Button created = CreateMenuLanguageButtonForToon(parent);
            return created != null ? GetPath(created.transform) : "not added.";
        }

        static Transform FindLanguageButtonParentForToon()
        {
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name.IndexOf("Setting", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return roots[i].transform;
            }

            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                string name = transforms[i].name;
                if (name.IndexOf("Setting", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && transforms[i].GetComponentInChildren<Button>(true) != null)
                    return transforms[i];
            }

            Button[] buttons = Object.FindObjectsOfType<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] != null && buttons[i].name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return buttons[i].transform.parent;
            }

            Canvas canvas = Object.FindObjectOfType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }

        static Button CreateMenuLanguageButtonForToon(Transform parent)
        {
            Button template = null;
            Button[] buttons = parent.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                string name = buttons[i].name;
                if (name.IndexOf("Play", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Resume", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    template = buttons[i];
                    break;
                }
            }
            if (template == null && buttons.Length > 0)
                template = buttons[0];
            if (template == null)
                return null;

            GameObject copy = Object.Instantiate(template.gameObject, template.transform.parent);
            copy.name = "Language";
            ContentDirty = true;
            RectTransform rect = copy.GetComponent<RectTransform>();
            RectTransform templateRect = template.GetComponent<RectTransform>();
            rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(templateRect.sizeDelta.x + 24f, 0f);

            Button button = copy.GetComponent<Button>();
            SerializedObject buttonObject = new SerializedObject(button);
            SerializedProperty calls = buttonObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null)
                calls.ClearArray();
            buttonObject.ApplyModifiedPropertiesWithoutUndo();

            Text legacy = copy.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                LegacyLocalizedText localized = legacy.GetComponent<LegacyLocalizedText>();
                if (localized == null)
                    localized = legacy.gameObject.AddComponent<LegacyLocalizedText>();
                SetLegacyKey(localized, "ui.button.language");
                legacy.text = "Language";
            }

            TMP_Text tmp = copy.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                LocalizedText localized = tmp.GetComponent<LocalizedText>();
                if (localized == null)
                    localized = tmp.gameObject.AddComponent<LocalizedText>();
                SetKey(localized, "ui.button.language");
                tmp.text = "Language";
            }

            return button;
        }

        static void EnsureOverlayCanvas(GameObject panelObject)
        {
            Canvas canvas = panelObject.GetComponent<Canvas>();
            if (canvas == null)
                canvas = panelObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            if (canvas.sortingOrder < 5000)
                canvas.sortingOrder = 5000;
            if (panelObject.GetComponent<GraphicRaycaster>() == null)
                panelObject.AddComponent<GraphicRaycaster>();
        }

    }
}
