using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Isometric depth sorting: uses grid cell (x + y) so the player draws above floor tiles at the same cell.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class YSortByPosition : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [SerializeField] private int orderPerDepth = 10;
    [Tooltip("Extra order so characters render in front of ground on the same tile.")]
    [SerializeField] private int characterBias = 50;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (grid == null)
            grid = FindObjectOfType<Grid>();
    }

    private void LateUpdate()
    {
        if (grid == null)
        {
            // Fallback: keep above default tilemaps (order 0–5)
            spriteRenderer.sortingOrder = 100;
            return;
        }

        Vector3Int cell = grid.WorldToCell(transform.position);
        int depth = cell.x + cell.y;
        spriteRenderer.sortingOrder = depth * orderPerDepth + characterBias;
    }

    public void SetGrid(Grid value) => grid = value;
}
