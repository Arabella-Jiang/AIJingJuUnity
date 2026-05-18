using UnityEngine;

/// <summary>
/// Marks the main HUD root. Layout is built in editor via JingJu → Setup Main HUD / Demo Scene.
/// Grid reference: 64×64 px on 1920×1080 canvas.
/// </summary>
[DisallowMultipleComponent]
public sealed class MainHUDLayout : MonoBehaviour
{
    [Header("Layout reference (64px grid)")]
    [SerializeField] private int gridCellPixels = 64;

    public int GridCellPixels => gridCellPixels;
}
