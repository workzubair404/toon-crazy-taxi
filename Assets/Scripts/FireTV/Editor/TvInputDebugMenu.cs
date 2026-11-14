#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class TvInputDebugMenu
{
    const string Key = "TvInputDebugMode";
    const string TouchPath = "Tools/Debug Input/Touch (Tablet / Phone)";
    const string TvPath = "Tools/Debug Input/TV (Remote, optional touch)";

    static TvInputDebugMenu()
    {
        Apply(EditorPrefs.GetInt(Key, (int)TvInputDebug.Mode.Touch));
    }

    [MenuItem(TouchPath)]
    static void SetTouch()
    {
        EditorPrefs.SetInt(Key, (int)TvInputDebug.Mode.Touch);
        Apply((int)TvInputDebug.Mode.Touch);
    }

    [MenuItem(TouchPath, true)]
    static bool ValidateTouch()
    {
        Menu.SetChecked(TouchPath, EditorPrefs.GetInt(Key, (int)TvInputDebug.Mode.Touch) == (int)TvInputDebug.Mode.Touch);
        return true;
    }

    [MenuItem(TvPath)]
    static void SetTv()
    {
        EditorPrefs.SetInt(Key, (int)TvInputDebug.Mode.Tv);
        Apply((int)TvInputDebug.Mode.Tv);
    }

    [MenuItem(TvPath, true)]
    static bool ValidateTv()
    {
        Menu.SetChecked(TvPath, EditorPrefs.GetInt(Key, (int)TvInputDebug.Mode.Tv) == (int)TvInputDebug.Mode.Tv);
        return true;
    }

    static void Apply(int mode)
    {
        TvInputDebug.Current = (TvInputDebug.Mode)mode;
        AmazonRemoteInput input = UnityEngine.Object.FindObjectOfType<AmazonRemoteInput>();
        if (input != null)
            input.ApplyEditorInputMode();
    }
}
#endif
