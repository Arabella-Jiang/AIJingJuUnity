using UnityEngine;

/// <summary>
/// Fullscreen modal that pauses player input only (not game time).
/// Subclass for backpack, map, tasks, character attributes, etc.
/// </summary>
public abstract class FullscreenGameplayPanel : MonoBehaviour
{
    [SerializeField] protected GameObject panelRoot;

    public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

    protected virtual void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    protected virtual void OnDisable()
    {
        GameplayInputBlocker.SetBlocked(this, false);
    }

    public virtual void Open()
    {
        if (panelRoot == null || IsOpen)
            return;

        panelRoot.SetActive(true);
        GameplayInputBlocker.SetBlocked(this, true);
        NotifyPlayerIdle();
    }

    public virtual void Close()
    {
        if (panelRoot == null || !IsOpen)
            return;

        panelRoot.SetActive(false);
        GameplayInputBlocker.SetBlocked(this, false);
        NotifyPlayerIdle();
    }

    public void Toggle()
    {
        if (IsOpen)
            Close();
        else
            Open();
    }

    protected static void NotifyPlayerIdle()
    {
        var player = Object.FindObjectOfType<IsometricPlayerController>();
        player?.EnterIdle();
    }
}
