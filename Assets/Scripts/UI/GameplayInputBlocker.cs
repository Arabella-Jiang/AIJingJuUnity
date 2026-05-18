using System.Collections.Generic;

/// <summary>
/// Blocks player movement / click-to-move while fullscreen gameplay UI is open.
/// Does not change Time.timeScale — game time keeps running.
/// </summary>
public static class GameplayInputBlocker
{
    private static readonly HashSet<object> Sources = new();

    public static bool IsBlocked => Sources.Count > 0;

    public static void SetBlocked(object source, bool blocked)
    {
        if (source == null)
            return;

        if (blocked)
            Sources.Add(source);
        else
            Sources.Remove(source);
    }

    public static void ClearAll() => Sources.Clear();
}
