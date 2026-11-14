/// <summary>Editor-only stand-in for device class (builds use Android TV detection).</summary>
public static class TvInputDebug
{
    public enum Mode
    {
        /// <summary>Phone / tablet — touch only.</summary>
        Touch = 0,
        /// <summary>Fire TV — remote + optional touch.</summary>
        Tv = 1
    }

    public static Mode Current = Mode.Touch;
}
