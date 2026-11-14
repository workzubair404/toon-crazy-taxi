using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Localization;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Localization.EditorTools
{
    public static partial class LocalizationProjectSetup
    {
        const string Root = "Assets/Localization";
        const string FontSource = Root + "/Fonts/Source";
        const string FontFolder = Root + "/Fonts";
        const string StringsFolder = Root + "/Strings";
        const string CatalogPath = Root + "/Resources/LanguageCatalog.asset";
        const string PanelPath = Root + "/Prefab/SelectLanguagePanel.prefab";
        const string RowPath = Root + "/Prefab/LanguageRow.prefab";
        const string ScenePath = Root + "/Scenes/Localization.unity";

        static readonly Dictionary<string, string> TextKeys = new Dictionary<string, string>
        {
            { "BUY COIN", "ui.button.buyCoin" },
            { "DAILY REWARD", "ui.dailyReward.title" },
            { "UNLOCK EVERYTHIG", "ui.iap.unlockEverything" },
            { "UNLOCK EVERYTHING", "ui.iap.unlockEverything" },
            { "Rewards", "ui.achievements.rewards" },
            { "Play", "ui.button.play" },
            { "Achievement", "ui.achievements.title" },
            { "Reward", "ui.reward.title" },
            { "Alert", "ui.alert.title" },
            { "GET FREE ITEM", "ui.button.getFreeItem" },
            { "Vibration", "ui.settings.vibration" },
            { "YES", "ui.button.yes" },
            { "RESUME", "ui.button.resume" },
            { "CLOSE", "ui.button.close" },
            { "GET COIN", "ui.button.getCoin" },
            { "Next", "ui.button.next" },
            { "Better Luck Next Time", "ui.lose.betterLuck" },
            { "WIN", "ui.win.title" },
            { "Game Is Now Paused!", "ui.pause.subtitle" },
            { "Settings", "ui.settings.title" },
            { "CONGRATULATIONS", "ui.win.congratulations" },
            { "Oooppsss! Purchase Failed", "ui.iap.purchaseFailed" },
            { "Home", "ui.button.home" },
            { "HOME", "ui.button.home" },
            { "No", "ui.button.no" },
            { "GET REWARD", "ui.dailyReward.getReward" },
            { "FAILED", "ui.iap.failed" },
            { "QUIT", "ui.button.quit" },
            { "RETRY", "ui.button.retry" },
            { "LOOSE", "ui.lose.title" },
            { "LOSE", "ui.lose.title" },
            { "MUSIC", "ui.settings.music" },
            { "SUCCESS", "ui.iap.success" },
            { "Are you sure want to quit game?", "ui.quit.message" },
            { "Are you sure you want to quit?", "ui.quit.message" },
            { "Congrats! Purchase Successful", "ui.iap.purchaseSuccessful" },
            { "REMOVE ADS", "ui.iap.removeAds" },
            { "SFX", "ui.settings.sfx" },
            { "PAUSE", "ui.pause.title" },
            { "Loading...", "ui.loading" },
            { "Unlock", "ui.button.unlock" },
            { "UNLOCK", "ui.button.unlock" },
            { "Purchase", "ui.button.unlock" },
            { "Language", "ui.button.language" },
            { "LANGUAGE", "ui.button.language" },
            { "Select Language", "language.title" },
            { "Confirm", "language.confirm" },
            { "BACK", "ui.button.back" },
            { "Back", "ui.button.back" },
            { "EXIT?", "ui.quit.title" },
            { "GAME PAUSED", "ui.pause.title" },
            { "LEVEL COMPLETE", "ui.win.title" },
            { "YOU HAVE EARNED", "ui.win.youHaveEarned" },
            { "LEVEL BONUS:", "ui.win.levelBonus" },
            { "TIME BONUS:", "ui.win.timeBonus" },
            { "LOADING...", "ui.loading" },
            { "START", "ui.button.start" },
            { "Instruction", "ui.instruction.title" },
            { "Gear", "ui.hud.gear" },
            { "SPEED", "ui.hud.speed" },
            { "CONTROL", "ui.hud.control" },
            { "BRAKE", "ui.hud.brake" },
            { "LEVEL", "ui.level.title" },
            { "SOUNDS", "ui.settings.sfx" },
            { "ON", "ui.settings.on" },
            { "OFF", "ui.settings.off" },
            { "00:00", "ui.hud.timer" }
        };

        // Text localization only — use menu: Localization > Localize All
        public static void Setup()
        {
            Directory.CreateDirectory(Root + "/Resources");
            Directory.CreateDirectory(Root + "/UI");
            Directory.CreateDirectory(Root + "/Scenes");
            Directory.CreateDirectory(FontFolder);

            TMP_FontAsset latin = CreateFontAssets();
            CreateCatalog();
            LanguageRowView row = CreateRowPrefab(latin);
            LanguageSelectionPanel panel = CreatePanelPrefab(latin, row);
            CreatePreviewScene(panel);
            BindScene("Assets/2. Scenes/Splash.unity");
            BindScene("Assets/2. Scenes/Loading.unity");
            BindScene("Assets/2. Scenes/Gameplay.unity");
            IntegrateGameplay(panel);
            BindPrefab("Assets/AchievementItemPrefab.prefab");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Localization setup complete.");
        }

        public static TMP_FontAsset CreateFontAssets()
        {
            TMP_FontAsset latin = CreateFont("latin", "RobotoCondensed-Bold");
            TMP_FontAsset arabic = CreateFont("arabic", "Cairo-Black");
            TMP_FontAsset hebrew = CreateFont("hebrew", "NotoSansHebrew-Black");
            TMP_FontAsset devanagari = CreateFont("devanagari", "Teko-Bold");
            TMP_FontAsset sc = CreateFont("sc", "NotoSansSC-Black");
            TMP_FontAsset tc = CreateFont("tc", "NotoSansTC-Black");
            TMP_FontAsset jp = CreateFont("jp", "NotoSansJP-Black");
            TMP_FontAsset kr = CreateFont("kr", "NotoSansKR-Black");

            SetFallback(arabic, latin);
            SetFallback(hebrew, latin);
            SetFallback(devanagari, latin);
            SetFallback(sc, latin);
            SetFallback(tc, latin);
            SetFallback(jp, latin);
            SetFallback(kr, latin);
            if (latin != null)
            {
                latin.fallbackFontAssetTable = new List<TMP_FontAsset>();
                EditorUtility.SetDirty(latin);
            }
            return latin;
        }

        static TMP_FontAsset CreateFont(string id, string fileName)
        {
            string assetPath = FontFolder + "/Noto-" + id + " SDF.asset";
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
                return existing;

            string source = FindFontFile(fileName);
            if (string.IsNullOrEmpty(source))
            {
                Debug.LogError("Missing source font " + fileName);
                return null;
            }

            TrueTypeFontImporter importer = AssetImporter.GetAtPath(source) as TrueTypeFontImporter;
            if (importer != null && !importer.includeFontData)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(source);
            if (font == null)
            {
                Debug.LogError("Unity did not import " + source);
                return null;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
            {
                Debug.LogError("TextMeshPro could not create a font asset for " + source);
                return null;
            }

            asset.name = "Noto-" + id + " SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            if (asset.material != null)
            {
                asset.material.name = asset.name + " Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            if (asset.atlasTextures != null)
            {
                for (int i = 0; i < asset.atlasTextures.Length; i++)
                {
                    if (asset.atlasTextures[i] == null)
                        continue;
                    asset.atlasTextures[i].name = asset.name + " Atlas";
                    AssetDatabase.AddObjectToAsset(asset.atlasTextures[i], asset);
                }
            }
            EditorUtility.SetDirty(asset);
            return asset;
        }

        static string FindFontFile(string fileName)
        {
            string[] extensions = { ".ttf", ".otf" };
            for (int i = 0; i < extensions.Length; i++)
            {
                string path = FontSource + "/" + fileName + extensions[i];
                if (File.Exists(path))
                    return path;
            }
            return null;
        }

        static void SetFallback(TMP_FontAsset font, TMP_FontAsset fallback)
        {
            if (font == null || fallback == null)
                return;
            font.fallbackFontAssetTable = new List<TMP_FontAsset> { fallback };
            EditorUtility.SetDirty(font);
        }

        public static void CreateCatalog()
        {
            LanguageCatalog catalog = AssetDatabase.LoadAssetAtPath<LanguageCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<LanguageCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.fallbackCode = "en";
            catalog.languages = LanguageDefaults.Create();
            catalog.fonts = new List<FontSet>
            {
                Font("latin"), Font("arabic"), Font("hebrew"), Font("devanagari"),
                Font("sc"), Font("tc"), Font("jp"), Font("kr")
            };
            catalog.tables = new List<TextAsset>();
            string[] files = Directory.GetFiles(StringsFolder, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = files[i].Replace('\\', '/');
                TextAsset table = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
                if (table != null)
                    catalog.tables.Add(table);
            }
            EditorUtility.SetDirty(catalog);
        }

        static FontSet Font(string id)
        {
            return new FontSet
            {
                id = id,
                primary = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontFolder + "/Noto-" + id + " SDF.asset")
            };
        }

        static LanguageRowView CreateRowPrefab(TMP_FontAsset font)
        {
            GameObject row = new GameObject("LanguageRow", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(LanguageRowView));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(760f, 96f);
            Image image = row.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
            Button button = row.GetComponent<Button>();
            button.targetGraphic = image;
            LayoutElement element = row.GetComponent<LayoutElement>();
            element.minHeight = 96f;
            element.preferredHeight = 110f;
            element.flexibleWidth = 1f;

            GameObject labelObject = CreateTextObject("Label", row.transform, font, "English", 36, TextAlignmentOptions.MidlineLeft);
            Stretch(labelObject.GetComponent<RectTransform>(), 24f, 24f, 8f, 8f);
            TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
            label.enableWordWrapping = true;
            label.overflowMode = TextOverflowModes.Overflow;

            LanguageRowView view = row.GetComponent<LanguageRowView>();
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("background").objectReferenceValue = image;
            serialized.FindProperty("button").objectReferenceValue = button;
            serialized.FindProperty("normalColor").colorValue = new Color(1f, 1f, 1f, 0.08f);
            serialized.FindProperty("selectedColor").colorValue = new Color(0.24f, 0.52f, 0.95f, 0.95f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(row, RowPath);
            Object.DestroyImmediate(row);
            return AssetDatabase.LoadAssetAtPath<GameObject>(RowPath).GetComponent<LanguageRowView>();
        }

        static LanguageSelectionPanel CreatePanelPrefab(TMP_FontAsset font, LanguageRowView rowPrefab)
        {
            GameObject root = new GameObject("SelectLanguagePanel", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(LanguageSelectionPanel));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5000;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            Stretch(root.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            GameObject dimmer = CreateImage("Dimmer", root.transform, new Color(0f, 0f, 0f, 0.72f));
            Stretch(dimmer.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            GameObject window = CreateImage("Window", root.transform, new Color(0.12f, 0.12f, 0.16f, 0.98f));
            RectTransform windowRect = window.GetComponent<RectTransform>();
            windowRect.anchorMin = new Vector2(0.5f, 0.5f);
            windowRect.anchorMax = new Vector2(0.5f, 0.5f);
            windowRect.pivot = new Vector2(0.5f, 0.5f);
            windowRect.sizeDelta = new Vector2(920f, 1500f);

            GameObject titleObject = CreateTextObject("Title", window.transform, font, "Select Language", 64, TextAlignmentOptions.Center);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-48f, 140f);
            titleRect.anchoredPosition = new Vector2(0f, -16f);
            LocalizedText title = titleObject.AddComponent<LocalizedText>();
            SetKey(title, "language.title");
            LocalizedLayout titleLayout = titleObject.AddComponent<LocalizedLayout>();
            SetMinWidth(titleLayout, 400f);

            GameObject scrollObject = new GameObject("Scroll", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(RectMask2D));
            scrollObject.transform.SetParent(window.transform, false);
            scrollObject.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
            RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(28f, 180f);
            scrollRect.offsetMax = new Vector2(-28f, -170f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(scrollObject.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup group = content.GetComponent<VerticalLayoutGroup>();
            group.spacing = 12f;
            group.padding = new RectOffset(8, 8, 8, 8);
            group.childAlignment = TextAnchor.UpperCenter;
            group.childControlWidth = true;
            group.childControlHeight = true;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            GameObject actions = new GameObject("Actions", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            actions.transform.SetParent(window.transform, false);
            RectTransform actionsRect = actions.GetComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(0f, 0f);
            actionsRect.anchorMax = new Vector2(1f, 0f);
            actionsRect.pivot = new Vector2(0.5f, 0f);
            actionsRect.sizeDelta = new Vector2(-48f, 140f);
            actionsRect.anchoredPosition = new Vector2(0f, 24f);
            HorizontalLayoutGroup actionsLayout = actions.GetComponent<HorizontalLayoutGroup>();
            actionsLayout.spacing = 20f;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = true;

            Button confirm = CreateButton("Confirm", actions.transform, font, "Confirm", "language.confirm");
            Button close = CreateButton("Close", actions.transform, font, "CLOSE", "ui.button.close");

            LanguageSelectionPanel panel = root.GetComponent<LanguageSelectionPanel>();
            SerializedObject serialized = new SerializedObject(panel);
            serialized.FindProperty("title").objectReferenceValue = title;
            serialized.FindProperty("content").objectReferenceValue = contentRect;
            serialized.FindProperty("rowPrefab").objectReferenceValue = rowPrefab;
            serialized.FindProperty("confirmButton").objectReferenceValue = confirm;
            serialized.FindProperty("closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PanelPath);
            Object.DestroyImmediate(root);
            string resourcesPath = Root + "/Resources/SelectLanguagePanel.prefab";
            AssetDatabase.DeleteAsset(resourcesPath);
            AssetDatabase.CopyAsset(PanelPath, resourcesPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath).GetComponent<LanguageSelectionPanel>();
        }

        static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label, string key)
        {
            GameObject buttonObject = CreateImage(name, parent, new Color(0.24f, 0.52f, 0.95f, 1f));
            LayoutElement element = buttonObject.AddComponent<LayoutElement>();
            element.minHeight = 110f;
            element.minWidth = 240f;
            element.preferredHeight = 110f;
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            GameObject textObject = CreateTextObject("Label", buttonObject.transform, font, label, 36, TextAlignmentOptions.Center);
            Stretch(textObject.GetComponent<RectTransform>(), 12f, 12f, 8f, 8f);
            LocalizedText localized = textObject.AddComponent<LocalizedText>();
            SetKey(localized, key);
            textObject.AddComponent<LocalizedLayout>();
            return button;
        }

        static void CreatePreviewScene(LanguageSelectionPanel panel)
        {
            Scene previous = SceneManager.GetActiveScene();
            bool previousDirty = previous.isDirty;
            string previousPath = previous.path;
            if (previousDirty && !string.IsNullOrEmpty(previousPath))
                EditorSceneManager.SaveScene(previous);

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PanelPath);
            if (prefabAsset == null && panel != null)
                prefabAsset = panel.gameObject;
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
            instance.SetActive(false);
            GameObject preview = new GameObject("LanguagePreview");
            LanguageSelectionPreview component = preview.AddComponent<LanguageSelectionPreview>();
            SerializedObject serialized = new SerializedObject(component);
            serialized.FindProperty("panel").objectReferenceValue = instance.GetComponent<LanguageSelectionPanel>();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
                events.transform.SetAsLastSibling();
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!string.IsNullOrEmpty(previousPath))
                EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
        }

        static void BindScene(string path)
        {
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            BindLoadedScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static void BindLoadedScene()
        {
            if (Tables == null || Tables.Count == 0)
                LoadTables();
            StripMissingScripts();
            TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>(true);
            HashSet<int> dynamicIds;
            HashSet<int> nativeNameIds;
            CollectOwnership(Object.FindObjectsOfType<MonoBehaviour>(true), out dynamicIds, out nativeNameIds);
            for (int i = 0; i < labels.Length; i++)
                BindLabel(labels[i], dynamicIds, nativeNameIds);
            BindLegacyUiLabels(dynamicIds, nativeNameIds);
        }

        static void StripMissingScripts()
        {
            Transform[] transforms = Object.FindObjectsOfType<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null)
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
            }
        }

        static void BindLabel(TMP_Text label, HashSet<int> dynamicIds, HashSet<int> nativeNameIds)
        {
            if (label == null)
                return;
            CollapseLocalization(label.gameObject);
            if (HasLocalization(label.gameObject))
            {
                AlreadyCount++;
                MarkExisting(label.gameObject);
                EnsureLayout(label.gameObject);
                return;
            }

            TMP_InputField input = label.GetComponentInParent<TMP_InputField>();
            if (input != null && input.textComponent == label)
            {
                label.gameObject.AddComponent<NonLocalizedText>();
                ContentDirty = true;
                BoundCount++;
                return;
            }

            if (nativeNameIds.Contains(label.GetInstanceID()))
            {
                label.gameObject.AddComponent<NonLocalizedText>();
                ContentDirty = true;
                BoundCount++;
                return;
            }

            if (dynamicIds.Contains(label.GetInstanceID()))
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

            int argument;
            string formatKey = MatchFormat(value, out argument);
            if (!string.IsNullOrEmpty(formatKey))
            {
                EnsureKey(formatKey, FormatEnglish(formatKey));
                LocalizedFormatText format = label.gameObject.AddComponent<LocalizedFormatText>();
                SerializedObject serialized = new SerializedObject(format);
                serialized.FindProperty("key").stringValue = formatKey;
                serialized.FindProperty("argument").intValue = argument;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EnsureLayout(label.gameObject);
                ContentDirty = true;
                BoundCount++;
                return;
            }

            LocalizedText localized = label.gameObject.AddComponent<LocalizedText>();
            SetKey(localized, ResolveOrCreate(value));
            EnsureLayout(label.gameObject);
            ContentDirty = true;
            BoundCount++;
        }

        static void BindPrefab(string path)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transforms[i].gameObject);
            if (Tables == null || Tables.Count == 0)
                LoadTables();
            TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
            HashSet<int> dynamicIds;
            HashSet<int> nativeNameIds;
            CollectOwnership(root.GetComponentsInChildren<MonoBehaviour>(true), out dynamicIds, out nativeNameIds);
            for (int i = 0; i < labels.Length; i++)
                BindLabel(labels[i], dynamicIds, nativeNameIds);
            Text[] legacyLabels = root.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < legacyLabels.Length; i++)
                BindLegacyLabel(legacyLabels[i], dynamicIds, nativeNameIds);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void IntegrateGameplay(LanguageSelectionPanel panelPrefab)
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/2. Scenes/Gameplay.unity", OpenSceneMode.Single);
            Component ui = FindBehaviour("UIManager");
            if (ui == null)
            {
                Debug.LogError("Gameplay scene has no UIManager.");
                return;
            }

            Component settings = FindBehaviour("SettingsManager");
            if (settings != null)
                AssignAlertKeys(settings);

            SerializedObject uiObject = new SerializedObject(ui);
            GameObject pausePanel = uiObject.FindProperty("pausePanel").objectReferenceValue as GameObject;
            Button languageButton = CreateLanguageButton(pausePanel);

            System.Type hostType = System.Type.GetType("GameLocalizationHost, Assembly-CSharp");
            Component host = hostType == null ? null : ui.GetComponent(hostType);
            if (host == null && hostType != null)
                host = ui.gameObject.AddComponent(hostType);
            if (host == null)
            {
                Debug.LogError("Could not add GameLocalizationHost.");
                return;
            }
            SerializedObject hostObject = new SerializedObject(host);
            hostObject.FindProperty("panelPrefab").objectReferenceValue = panelPrefab;
            hostObject.FindProperty("pauseLanguageButton").objectReferenceValue = languageButton;
            hostObject.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        static Component FindBehaviour(string typeName)
        {
            List<Component> found = FindBehaviours(typeName);
            return found.Count > 0 ? found[0] : null;
        }

        static List<Component> FindBehaviours(string typeName)
        {
            List<Component> found = new List<Component>();
            MonoBehaviour[] behaviours = Object.FindObjectsOfType<MonoBehaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null && behaviours[i].GetType().Name == typeName)
                    found.Add(behaviours[i]);
            }
            return found;
        }

        static Button CreateLanguageButton(GameObject pausePanel)
        {
            if (pausePanel == null)
                return null;
            Transform existing = pausePanel.transform.Find("Language");
            if (existing != null)
                return existing.GetComponent<Button>();

            Button template = null;
            Button[] buttons = pausePanel.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == "Resume")
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
            RectTransform rect = copy.GetComponent<RectTransform>();
            RectTransform templateRect = template.GetComponent<RectTransform>();
            rect.anchoredPosition = templateRect.anchoredPosition + new Vector2(0f, -Mathf.Max(90f, templateRect.sizeDelta.y + 16f));

            Button button = copy.GetComponent<Button>();
            SerializedObject buttonObject = new SerializedObject(button);
            SerializedProperty calls = buttonObject.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (calls != null)
                calls.ClearArray();
            buttonObject.ApplyModifiedPropertiesWithoutUndo();

            TMP_Text label = copy.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                LocalizedText localized = label.GetComponent<LocalizedText>();
                if (localized == null)
                    localized = label.gameObject.AddComponent<LocalizedText>();
                SetKey(localized, "ui.button.language");
                label.text = "Language";
                if (label.GetComponent<LocalizedLayout>() == null)
                    label.gameObject.AddComponent<LocalizedLayout>();
            }
            return button;
        }

        static void AssignAlertKeys(Component settings)
        {
            SerializedObject serialized = new SerializedObject(settings);
            AssignPair(serialized.FindProperty("music"), "ui.settings.musicOn", "ui.settings.musicOff");
            AssignPair(serialized.FindProperty("sound"), "ui.settings.soundOn", "ui.settings.soundOff");
            AssignPair(serialized.FindProperty("vibration"), "ui.settings.vibrationOn", "ui.settings.vibrationOff");
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AssignPair(SerializedProperty property, string onKey, string offKey)
        {
            if (property == null)
                return;
            property.FindPropertyRelative("alertOnKey").stringValue = onKey;
            property.FindPropertyRelative("alertOffKey").stringValue = offKey;
        }

        static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            imageObject.GetComponent<Image>().color = color;
            return imageObject;
        }

        static GameObject CreateTextObject(string name, Transform parent, TMP_FontAsset font, string value, float size, TextAlignmentOptions alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
                text.fontSharedMaterial = font.material;
            }
            text.text = value;
            text.fontSize = size;
            text.alignment = alignment;
            text.color = Color.white;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            return textObject;
        }

        static void Stretch(RectTransform rect, float left, float right, float bottom, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        static void SetKey(LocalizedText text, string key)
        {
            SerializedObject serialized = new SerializedObject(text);
            serialized.FindProperty("key").stringValue = key;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            MarkUsed(key);
        }

        static void SetMinWidth(LocalizedLayout layout, float width)
        {
            SerializedObject serialized = new SerializedObject(layout);
            serialized.FindProperty("minWidth").floatValue = width;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static bool IsNumeric(string value)
        {
            bool hasDigit = false;
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsDigit(c))
                {
                    hasDigit = true;
                    continue;
                }
                if (c == ' ' || c == '+' || c == '%' || c == ':' || c == '.' || c == ',' || c == '-' || c == '/')
                    continue;
                return false;
            }
            return hasDigit;
        }

        static string GetPath(Transform transform)
        {
            string path = transform.name;
            while (transform.parent != null)
            {
                transform = transform.parent;
                path = transform.name + "/" + path;
            }
            return path;
        }
    }
}
