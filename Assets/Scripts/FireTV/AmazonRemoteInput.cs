using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// One APK, three behaviours:
/// • Fire TV / Android TV — remote UI always (green line, D-pad, Back, Play/Pause). Finger touch still works when the device has a screen.
/// • Phone or tablet — touch only; no focus line and no remote UI capture (touch + on-screen controls).
/// Editor: Tools → Debug Input mimics TV vs tablet. Gameplay: Tab toggles Drive / UI.
/// </summary>
[DefaultExecutionOrder(50)]
public class AmazonRemoteInput : MonoBehaviour
{
    const float NavInitial = 0.34f;
    const float NavRepeat = 0.14f;
    const float FocusLineHeight = 17f;

    bool television;
    bool userWantsDrive = true;
    GameObject focused;
    RectTransform focusRect;
    RectTransform focusLine;
    RectTransform focusBar;
    Canvas focusCanvas;
    bool warnedLine;

    int navHeldX, navHeldY;
    float navNextX, navNextY;
    float backLock, pauseLock, selectLock;
    int lastTargetSig;
    int lastTopCanvasOrder = int.MinValue;

    readonly List<GameObject> targets = new List<GameObject>();
    readonly HashSet<int> seen = new HashSet<int>();

    struct Pad
    {
        public int x, y;
        public bool selectDown;
        public bool backDown;
        public bool pauseDown;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        var host = new GameObject("AmazonRemoteInput");
        DontDestroyOnLoad(host);
        host.AddComponent<AmazonRemoteInput>();
    }

    void Awake()
    {
        television = DetectTelevision();
        ApplyEditorInputMode();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Localization.OverlayFocus.Clear();
        ClearUiCursor();
        userWantsDrive = IsGameplayActive();
    }

    void Update()
    {
        if (!RemoteUiOn())
        {
            EventSystem idle = EventSystem.current;
            if (idle != null)
                idle.sendNavigationEvents = true;
            HideLine();
            return;
        }

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Tab) && IsGameplayActive())
            userWantsDrive = !userWantsDrive;
#endif

        EventSystem events = EventSystem.current;
        if (events != null)
            events.sendNavigationEvents = false;

        Pad pad = ReadPad();
        bool swallowed = false;

        if (pad.backDown)
        {
            HandleBack();
            swallowed = true;
        }

        if (!swallowed && pad.pauseDown)
        {
            HandlePause();
            swallowed = true;
        }

        if (UseDriveMode())
        {
            HideLine();
            ClearUiCursor();
            return;
        }

        UpdateUi(pad, swallowed);
    }

    void LateUpdate()
    {
        if (RemoteUiOn() && UseDriveMode())
            ApplyRemoteDrive(ReadPad());
        UpdateFocusFrame();
    }

    bool UseDriveMode()
    {
        if (!IsGameplayActive())
            return false;
        if (LanguagePanelOpen())
            return false;
        if (OverlayTop() != null)
            return false;
        if (Time.timeScale < 0.01f)
            return false;
        return userWantsDrive;
    }

    static bool IsGameplayActive()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name.IndexOf("Gameplay", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return UnityEngine.Object.FindObjectOfType<GN_GameController>() != null;
    }

    void ApplyRemoteDrive(Pad pad)
    {
        RCC_MobileButtons mobile = RCC_MobileButtons.Instance;
        if (mobile == null)
            return;

        float remoteGas = pad.y > 0 ? 1f : 0f;
        float remoteBrake = pad.y < 0 ? 1f : 0f;
        float remoteSteer = (pad.x > 0 ? 1f : 0f) - (pad.x < 0 ? 1f : 0f);

        mobile.inputs.throttleInput = Mathf.Max(mobile.inputs.throttleInput, remoteGas);
        mobile.inputs.brakeInput = Mathf.Max(mobile.inputs.brakeInput, remoteBrake);
        if (Mathf.Abs(remoteSteer) > 0.01f)
            mobile.inputs.steerInput = Mathf.Clamp(mobile.inputs.steerInput + remoteSteer, -1f, 1f);
    }

    bool RemoteUiOn()
    {
#if UNITY_EDITOR
        return TvInputDebug.Current == TvInputDebug.Mode.Tv;
#else
        return television;
#endif
    }

    public void ApplyEditorInputMode()
    {
#if UNITY_EDITOR
        if (TvInputDebug.Current != TvInputDebug.Mode.Tv)
        {
            ClearUiCursor();
            HideLine();
            if (EventSystem.current != null)
                EventSystem.current.sendNavigationEvents = true;
        }
#endif
    }

    void ClearUiCursor()
    {
        focused = null;
        focusRect = null;
        lastTargetSig = 0;
        lastTopCanvasOrder = int.MinValue;
    }

    void HideLine()
    {
        if (focusCanvas != null)
            focusCanvas.enabled = false;
    }

    static bool DetectTelevision()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            string model = SystemInfo.deviceModel ?? string.Empty;
            string upper = model.ToUpperInvariant();
            bool fireTvModel = upper.StartsWith("AFT") || upper.Contains("FIRE TV");

            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var packageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                bool fireTv = packageManager.Call<bool>("hasSystemFeature", "amazon.hardware.fire_tv");
                bool androidTv = packageManager.Call<bool>("hasSystemFeature", "android.hardware.type.television");
                bool touchScreen = packageManager.Call<bool>("hasSystemFeature", "android.hardware.touchscreen");

                int uiMode = 0;
                using (var ui = activity.Call<AndroidJavaObject>("getSystemService", "uimode"))
                    uiMode = ui.Call<int>("getCurrentModeType");

                // TV first: remote UI even when this model also has a touchscreen.
                if (fireTvModel || fireTv || androidTv || uiMode == 4)
                    return true;

                // Tablet / phone: touchscreen device that is not a TV — touch only, no green line.
                if (touchScreen)
                    return false;
            }

            if (fireTvModel)
                return true;
        }
        catch { }
#endif
        return false;
    }

    Pad ReadPad()
    {
        Pad pad = new Pad();
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        if (h <= -0.5f || Input.GetKey(KeyCode.LeftArrow))
            pad.x = -1;
        else if (h >= 0.5f || Input.GetKey(KeyCode.RightArrow))
            pad.x = 1;

        if (v <= -0.5f || Input.GetKey(KeyCode.DownArrow))
            pad.y = -1;
        else if (v >= 0.5f || Input.GetKey(KeyCode.UpArrow))
            pad.y = 1;

        pad.selectDown = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetButtonDown("Submit");
        pad.backDown = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1);
        pad.pauseDown = Input.GetKeyDown(KeyCode.Pause) || Input.GetKeyDown(KeyCode.P);

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var activity = new AndroidJavaClass("com.alphastudio.firetv.FireTvRemoteActivity"))
            {
                if (activity.CallStatic<bool>("consumePlayPause"))
                    pad.pauseDown = true;
                if (activity.CallStatic<bool>("consumeBack"))
                    pad.backDown = true;
                if (activity.CallStatic<bool>("consumeSelect"))
                    pad.selectDown = true;
            }
        }
        catch { }
#endif
        return pad;
    }

    void HandleBack()
    {
        if (Time.unscaledTime < backLock)
            return;
        backLock = Time.unscaledTime + 0.35f;

        if (LanguagePanelOpen())
            return;

        CollectButtons();
        if (ClickNamed("close", "cancel", "back", "no"))
        {
            ClearUiCursor();
            return;
        }

        if (ClickNamed("resume"))
        {
            ClearUiCursor();
            return;
        }

        ClickNamed("pause");
        ClearUiCursor();
    }

    void HandlePause()
    {
        if (Time.unscaledTime < pauseLock)
            return;
        pauseLock = Time.unscaledTime + 0.35f;

        if (LanguagePanelOpen())
            return;

        CollectButtons();
        if (ClickNamed("resume"))
        {
            ClearUiCursor();
            return;
        }

        if (OverlayTop() != null)
            return;

        ClickNamed("pause");
        ClearUiCursor();
    }

    void UpdateUi(Pad pad, bool swallowed)
    {
        EnsureFocusLine();
        CollectButtons();

        if (targets.Count == 0)
        {
            ClearUiCursor();
            return;
        }

        int sig = ComputeTargetSignature();
        int topOrder = HighestTargetCanvasOrder();
        bool setChanged = sig != lastTargetSig || topOrder != lastTopCanvasOrder;
        lastTargetSig = sig;
        lastTopCanvasOrder = topOrder;

        bool focusInvalid = focused == null || !focused.activeInHierarchy || !targets.Contains(focused);
        if (focused != null)
        {
            RectTransform frt = focused.GetComponent<RectTransform>();
            if (frt != null && !IsOnScreen(RectToScreenCenter(frt)))
                focusInvalid = true;
        }
        if (setChanged || focusInvalid)
            focused = FirstVisibleButton();

        if (!swallowed && pad.x != 0)
            MoveFocus(pad.x, 0, ref navHeldX, ref navNextX);
        if (!swallowed && pad.y != 0)
            MoveFocus(0, pad.y, ref navHeldY, ref navNextY);

        if (pad.x == 0) navHeldX = 0;
        if (pad.y == 0) navHeldY = 0;

        if (!swallowed && pad.selectDown && Time.unscaledTime >= selectLock)
        {
            selectLock = Time.unscaledTime + 0.25f;
            ClickFocused();
        }

        focusRect = focused != null ? focused.GetComponent<RectTransform>() : null;
        if (EventSystem.current != null && focused != null)
            EventSystem.current.SetSelectedGameObject(focused);

        EnsureFocusedVisibleInScroll();
    }

    void MoveFocus(int dx, int dy, ref int held, ref float next)
    {
        int key = dx != 0 ? dx : dy;
        if (held != key)
        {
            held = key;
            next = Time.unscaledTime + NavInitial;
            StepFocus(dx, dy);
            return;
        }

        if (Time.unscaledTime >= next)
        {
            next = Time.unscaledTime + NavRepeat;
            StepFocus(dx, dy);
        }
    }

    void StepFocus(int dx, int dy)
    {
        if (focused == null)
            return;
        RectTransform from = focused.GetComponent<RectTransform>();
        if (from == null)
            return;

        Vector3 origin = from.TransformPoint(from.rect.center);
        GameObject best = null;
        float bestScore = float.MaxValue;
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject go = targets[i];
            if (go == focused)
                continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
                continue;
            Vector3 d = rt.TransformPoint(rt.rect.center) - origin;
            if (dx != 0 && Mathf.Sign(d.x) != dx)
                continue;
            if (dy != 0 && Mathf.Sign(d.y) != dy)
                continue;
            float along = dx != 0 ? Mathf.Abs(d.x) : Mathf.Abs(d.y);
            float side = dx != 0 ? Mathf.Abs(d.y) : Mathf.Abs(d.x);
            float score = along + side * 2.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = go;
            }
        }

        if (best != null)
            focused = best;
    }

    void ClickFocused()
    {
        if (focused == null)
            return;
        Button button = focused.GetComponent<Button>();
        if (button != null && button.interactable)
            button.onClick.Invoke();
        else if (EventSystem.current != null)
            ExecuteEvents.Execute(focused, new PointerEventData(EventSystem.current), ExecuteEvents.submitHandler);
        ClearUiCursor();
    }

    bool ClickNamed(params string[] words)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject go = targets[i];
            if (go == null || !NameIs(go, words))
                continue;
            focused = go;
            ClickFocused();
            return true;
        }
        return false;
    }

    static bool NameIs(GameObject go, params string[] words)
    {
        string n = go.name.Replace(" ", "").Replace("_", "").ToLowerInvariant();
        for (int i = 0; i < words.Length; i++)
        {
            string word = words[i];
            if (n == word || n.StartsWith(word) || n.EndsWith(word))
                return true;
        }
        return false;
    }

    GameObject FirstVisibleButton()
    {
        GameObject pauseHud = null;
        GameObject best = null;
        int bestOrder = int.MinValue;
        float bestScore = float.MaxValue;
        for (int i = 0; i < targets.Count; i++)
        {
            GameObject go = targets[i];
            if (go == null || !go.activeInHierarchy)
                continue;
            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null)
                continue;
            Vector3 screen = RectToScreenCenter(rt);
            if (!IsOnScreen(screen))
                continue;
            if (NameIs(go, "pause") && go.GetComponentInParent<RCC_MobileButtons>(true) == null)
                pauseHud = go;
            int order = GetCanvasSortOrder(go);
            float score = -screen.y * 1000f + screen.x;
            if (order > bestOrder || (order == bestOrder && score < bestScore))
            {
                bestOrder = order;
                bestScore = score;
                best = go;
            }
        }
        return pauseHud != null ? pauseHud : best;
    }

    static bool IsOnScreen(Vector3 screen)
    {
        float pad = 8f;
        return screen.x >= pad && screen.x <= Screen.width - pad
            && screen.y >= pad && screen.y <= Screen.height - pad;
    }

    static Vector3 RectToScreenCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        Vector3 mid = (corners[0] + corners[2]) * 0.5f;
        Canvas canvas = rt.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;
        return RectTransformUtility.WorldToScreenPoint(cam, mid);
    }

    static int GetCanvasSortOrder(GameObject go)
    {
        Canvas canvas = go.GetComponentInParent<Canvas>();
        if (canvas == null)
            return 0;
        return canvas.rootCanvas.sortingOrder;
    }

    int HighestTargetCanvasOrder()
    {
        int best = int.MinValue;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null)
                continue;
            int order = GetCanvasSortOrder(targets[i]);
            if (order > best)
                best = order;
        }
        return best;
    }

    int ComputeTargetSignature()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + targets.Count;
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                    hash = hash * 31 + targets[i].GetInstanceID();
            }
            return hash;
        }
    }

    void CollectButtons()
    {
        targets.Clear();
        seen.Clear();

        Button[] buttons = FindObjectsOfType<Button>();
        Transform overlayRoot = OverlayTop();

        int topOrder = int.MinValue;
        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (!CanFocus(b, overlayRoot))
                continue;
            int order = GetCanvasSortOrder(b.gameObject);
            if (order > topOrder)
                topOrder = order;
        }

        for (int i = 0; i < buttons.Length; i++)
        {
            Button b = buttons[i];
            if (!CanFocus(b, overlayRoot))
                continue;
            if (overlayRoot == null && topOrder != int.MinValue && GetCanvasSortOrder(b.gameObject) < topOrder)
            {
                RectTransform rt = b.GetComponent<RectTransform>();
                if (rt == null || !IsOnScreen(RectToScreenCenter(rt)))
                    continue;
            }
            int id = b.gameObject.GetInstanceID();
            if (!seen.Add(id))
                continue;
            targets.Add(b.gameObject);
        }
    }

    bool CanFocus(Button b, Transform overlayRoot)
    {
        if (b == null || !b.isActiveAndEnabled || !b.interactable)
            return false;
        if (!IsSelectableTarget(b))
            return false;
        if (IsDriveControlButton(b))
            return false;
        if (overlayRoot != null && !b.transform.IsChildOf(overlayRoot) && b.transform != overlayRoot)
            return false;
        return true;
    }

    static bool IsDriveControlButton(Button b)
    {
        if (b.GetComponentInParent<RCC_MobileButtons>(true) != null)
            return true;
        return b.GetComponentInParent<RCC_UIController>(true) != null;
    }

    static bool IsSelectableTarget(Button b)
    {
        CanvasGroup[] groups = b.GetComponentsInParent<CanvasGroup>(true);
        for (int i = 0; i < groups.Length; i++)
        {
            if (groups[i] != null && (!groups[i].interactable || groups[i].alpha < 0.05f || !groups[i].blocksRaycasts))
                return false;
        }
        return true;
    }

    static Transform OverlayTop()
    {
        Type type = null;
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (int i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType("Localization.OverlayFocus");
            if (type != null)
                break;
        }
        if (type == null)
            return null;

        MethodInfo method = type.GetMethod("Top", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            return null;
        return method.Invoke(null, null) as Transform;
    }

    static bool LanguagePanelOpen()
    {
        Transform top = OverlayTop();
        if (top == null)
            top = DetectActiveModalRoot();
        if (top == null)
            return false;
        if (top.GetComponent("LanguageSelectionPanel") != null)
            return true;
        return top.name.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>Fullscreen popups toggled with SetActive (settings, exit, pause, etc.).</summary>
    static Transform DetectActiveModalRoot()
    {
        Transform best = null;
        int bestDepth = -1;
        int bestSibling = -1;

        RectTransform[] rects = UnityEngine.Object.FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null || !rt.gameObject.activeInHierarchy)
                continue;
            if (rt.GetComponentInParent<Canvas>() == null)
                continue;
            if (!LooksLikeModalRoot(rt))
                continue;
            if (rt.GetComponentInChildren<Button>(true) == null
                && rt.name != "Loading" && rt.name != "LoadingAnim")
                continue;

            int depth = rt.GetSiblingIndex();
            int hierarchyDepth = 0;
            Transform walk = rt;
            while (walk.parent != null)
            {
                hierarchyDepth++;
                walk = walk.parent;
            }

            if (hierarchyDepth > bestDepth || (hierarchyDepth == bestDepth && depth > bestSibling))
            {
                bestDepth = hierarchyDepth;
                bestSibling = depth;
                best = rt;
            }
        }

        return best;
    }

    static bool LooksLikeModalRoot(RectTransform rt)
    {
        if (rt.GetComponent<TvOverlayFocus>() == null && rt.GetComponentInParent<TvOverlayFocus>(true) == null)
            return false;

        string n = rt.name;
        if (n == "Loading" || n == "LoadingAnim")
            return true;

        if (rt.anchorMin.x > 0.05f || rt.anchorMin.y > 0.05f)
            return false;
        if (rt.anchorMax.x < 0.95f || rt.anchorMax.y < 0.95f)
            return false;
        if (n.IndexOf("Language", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Setting", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Exit", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Pause", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Panel", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Popup", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Dialog", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Complete", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Failed", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Reward", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Store", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n.IndexOf("Unlock", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (n == "FailPanel" || n == "CompletePanel")
            return true;
        return false;
    }

    void EnsureFocusedVisibleInScroll()
    {
        if (focused == null)
            return;
        ScrollRect scroll = focused.GetComponentInParent<ScrollRect>();
        if (scroll == null || scroll.content == null || scroll.viewport == null)
            return;

        RectTransform item = focused.GetComponent<RectTransform>();
        if (item == null)
            return;

        Canvas.ForceUpdateCanvases();
        Vector3[] itemCorners = new Vector3[4];
        Vector3[] viewCorners = new Vector3[4];
        item.GetWorldCorners(itemCorners);
        scroll.viewport.GetWorldCorners(viewCorners);

        float itemTop = itemCorners[1].y;
        float itemBottom = itemCorners[0].y;
        float viewTop = viewCorners[1].y;
        float viewBottom = viewCorners[0].y;

        float delta = 0f;
        if (itemTop > viewTop)
            delta = itemTop - viewTop;
        else if (itemBottom < viewBottom)
            delta = itemBottom - viewBottom;

        if (Mathf.Abs(delta) < 0.5f)
            return;

        Vector2 pos = scroll.content.anchoredPosition;
        pos.y -= delta;
        scroll.content.anchoredPosition = pos;
    }

    void EnsureFocusLine()
    {
        if (focusLine != null)
            return;

        GameObject prefab = Resources.Load<GameObject>("Tv/TvFocusLine");
        if (prefab == null)
        {
            if (!warnedLine)
            {
                warnedLine = true;
                Debug.LogWarning("Tv/TvFocusLine prefab missing from Resources.");
            }
            return;
        }

        GameObject instance = Instantiate(prefab);
        DontDestroyOnLoad(instance);
        focusLine = instance.GetComponent<RectTransform>();
        focusCanvas = instance.GetComponent<Canvas>();
        focusBar = focusLine.Find("Line") as RectTransform;
        if (focusBar == null && focusLine.childCount > 0)
            focusBar = focusLine.GetChild(0) as RectTransform;
        if (focusCanvas == null)
        {
            focusCanvas = instance.AddComponent<Canvas>();
            focusCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        focusCanvas.overrideSorting = true;
        focusCanvas.sortingOrder = 32760;
        focusCanvas.enabled = false;

        Graphic[] graphics = instance.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
            graphics[i].raycastTarget = false;
    }

    void UpdateFocusFrame()
    {
        if (focusLine == null || focusCanvas == null)
            return;

        bool show = RemoteUiOn() && focusRect != null && focusRect.gameObject.activeInHierarchy;
        focusCanvas.enabled = show;
        if (!show)
            return;

        int order = GetCanvasSortOrder(focusRect.gameObject);
        focusCanvas.sortingOrder = Mathf.Max(32760, order + 10);

        Vector3[] corners = new Vector3[4];
        focusRect.GetWorldCorners(corners);
        Vector3 midBottom = (corners[0] + corners[3]) * 0.5f;
        float width = Vector3.Distance(corners[0], corners[3]);

        RectTransform bar = focusBar != null ? focusBar : focusLine;
        Transform host = focusCanvas.transform;
        if (bar.parent != host)
            bar.SetParent(host, false);

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, midBottom + Vector3.down * 8f);
        RectTransform canvasRt = focusCanvas.transform as RectTransform;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out local);

        bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0.5f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = local;
        bar.sizeDelta = new Vector2(Mathf.Max(40f, width), FocusLineHeight);
        bar.SetAsLastSibling();
    }
}
