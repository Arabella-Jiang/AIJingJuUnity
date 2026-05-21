#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Attaches Character0 visuals + locomotion collider on the Player object (editor scene setup).
/// </summary>
public static class Character0PlayerSetup
{
    private const string LibraryPath = "Assets/Character0/Character0SpriteLibrary.asset";

    public static bool TryApply(GameObject player, Grid grid, Tilemap ground, Tilemap obstacle)
    {
        var library = AssetDatabase.LoadAssetAtPath<Character0SpriteLibrary>(LibraryPath);
        if (library == null || !library.IsReady)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Character0"))
                return false;

            Character0Importer.ImportAll();
            library = AssetDatabase.LoadAssetAtPath<Character0SpriteLibrary>(LibraryPath);
            if (library == null || !library.IsReady)
                return false;
        }

        var sr = player.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = player.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 200;

        var visual = player.GetComponent<Character0Visual>();
        if (visual == null)
            visual = player.AddComponent<Character0Visual>();
        visual.Library = library;

        var first = library.GetFrame(library.idle, 0, 0, library.idleColumns);
        if (first != null)
            sr.sprite = first;

        SetupCollider(player);
        SetupRigidbody(player);
        SetupYSort(player, grid);
        SetupController(player, ground, obstacle);
        return true;
    }

    private static void SetupCollider(GameObject player)
    {
        var circle = player.GetComponent<CircleCollider2D>();
        if (circle == null)
            circle = player.AddComponent<CircleCollider2D>();
        circle.radius = 0.22f;
        circle.offset = new Vector2(0f, -0.12f);
    }

    private static void SetupRigidbody(GameObject player)
    {
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    private static void SetupYSort(GameObject player, Grid grid)
    {
        var ySort = player.GetComponent<YSortByPosition>();
        if (ySort == null)
            ySort = player.AddComponent<YSortByPosition>();
        ySort.SetGrid(grid);
    }

    private static void SetupController(GameObject player, Tilemap ground, Tilemap obstacle)
    {
        var controller = player.GetComponent<IsometricPlayerController>();
        if (controller == null)
            controller = player.AddComponent<IsometricPlayerController>();
        controller.GroundTilemap = ground;
        controller.ObstacleTilemap = obstacle;
    }
}
#endif
