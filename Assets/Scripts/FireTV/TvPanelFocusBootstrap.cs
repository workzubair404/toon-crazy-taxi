using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ensures popups that use SetActive (settings, exit, pause, etc.) register with OverlayFocus.
/// </summary>
static class TvPanelFocusBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Hook()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterFromScripts();
        RegisterBySceneScan();
    }

    static void RegisterFromScripts()
    {
        foreach (GN_MainMenu menu in UnityEngine.Object.FindObjectsOfType<GN_MainMenu>(true))
        {
            Attach(menu.settingPanel);
            Attach(menu.ExitDialogue);
            Attach(menu.storePanel);
            Attach(menu.rewardPanel);
            Attach(menu.UnlockPopUpPanel);
        }

        foreach (GN_LevelSelection level in UnityEngine.Object.FindObjectsOfType<GN_LevelSelection>(true))
        {
            Attach(level.UnlockPopUpPanel);
            Attach(level.LoadingScreen);
        }

        foreach (GN_PlayerSelection player in UnityEngine.Object.FindObjectsOfType<GN_PlayerSelection>(true))
        {
            Attach(player.UnlockPopUpPanel);
            if (player.Selection_UI != null)
                Attach(player.Selection_UI.LoadingScreen);
        }

        foreach (GN_GameController game in UnityEngine.Object.FindObjectsOfType<GN_GameController>(true))
        {
            if (game.Game_Elements == null)
                continue;
            Game_Dialogues d = game.Game_Elements;
            Attach(d.PauseMenu);
            Attach(d.GameExit);
            Attach(d.LevelComplete);
            Attach(d.LevelFailed);
            Attach(d.continueScreen);
            Attach(d.LoadingScreen);
            if (d.doubleCoinsButton != null)
                Attach(FindModalRoot(d.doubleCoinsButton.transform));

        }

        foreach (Localization.LanguageSelectionPanel panel in UnityEngine.Object.FindObjectsOfType<Localization.LanguageSelectionPanel>(true))
            Attach(panel.gameObject);
    }

    static void RegisterBySceneScan()
    {
        RectTransform[] rects = UnityEngine.Object.FindObjectsOfType<RectTransform>(true);
        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rt = rects[i];
            if (rt == null)
                continue;
            if (!ShouldAttachByName(rt.name, rt))
                continue;
            if (rt.GetComponentInChildren<Button>(true) == null && rt.name != "Loading")
                continue;
            Attach(rt.gameObject);
        }
    }

    static GameObject FindModalRoot(Transform t)
    {
        Transform best = t;
        while (t.parent != null)
        {
            string n = t.parent.name;
            if (n == "LevelComplete" || n == "LevelFailed" || n == "Pause" || n == "Settings" || n == "Exit")
            {
                best = t.parent;
                break;
            }
            t = t.parent;
        }
        return best.gameObject;
    }

    static bool ShouldAttachByName(string name, RectTransform rt)
    {
        if (string.IsNullOrEmpty(name))
            return false;
        if (name == "SelectLanguagePanel" || name == "PopUpPanel")
            return true;
        if (name == "Loading" || name == "LoadingAnim")
            return IsFullscreenOverlay(rt);
        if (name == "Settings" || name == "Pause" || name == "LevelComplete" || name == "LevelFailed"
            || name == "FailPanel" || name == "CompletePanel" || name == "Exit")
            return IsFullscreenOverlay(rt);
        if (name.IndexOf("Unlock", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        return false;
    }

    static bool IsFullscreenOverlay(RectTransform rt)
    {
        return rt.anchorMin.x <= 0.05f && rt.anchorMin.y <= 0.05f
            && rt.anchorMax.x >= 0.95f && rt.anchorMax.y >= 0.95f;
    }

    public static void Attach(GameObject panel)
    {
        if (panel == null)
            return;
        if (panel.GetComponent<TvOverlayFocus>() == null)
            panel.AddComponent<TvOverlayFocus>();
    }
}