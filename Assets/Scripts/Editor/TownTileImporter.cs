#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Imports producer tiles from Assets/Art/城镇 and builds an isometric test map.
/// </summary>
public static class TownTileImporter
{
    private const string TownArtFolder = "Assets/Art/城镇";
    private const string TownTilesFolder = "Assets/Art/城镇/Tiles";

    private const int MapWidth = 16;
    private const int MapHeight = 12;

    // 64px = footprint width inside the exported PNG (Aseprite grid).
    // PPU = full PNG width so one image occupies exactly one Tilemap cell (no neighbor overlap).
    private const int TileGridPixels = 64;

    [MenuItem("JingJu/Import Town Tiles (Producer Art)")]
    public static void ImportTownTilesOnly()
    {
        var tiles = BuildTownTileAssets();
        if (tiles.Count == 0)
        {
            Debug.LogError("[JingJu] No town tiles found under Assets/Art/城镇");
            return;
        }

        Debug.Log($"[JingJu] Imported {tiles.Count} town tile(s) to {TownTilesFolder}");
    }

    /// <summary>
    /// Re-applies PPU (=texture width) and auto pivot for selected town PNG(s).
    /// </summary>
    [MenuItem("JingJu/Fix Town Sprite Pivot (Selected PNGs)")]
    public static void FixSelectedTownSpritePivots()
    {
        int count = 0;
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (!path.StartsWith(TownArtFolder, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (ApplyTownSpriteImport(path))
                count++;
        }

        if (count == 0)
            Debug.LogWarning("[JingJu] Select PNG(s) under Assets/Art/城镇, then run this menu again.");
        else
            Debug.Log($"[JingJu] Fixed pivot/PPU on {count} sprite(s). Erase Tilemap and paint again.");
    }

    [MenuItem("JingJu/Fix All Town Sprite Pivots (64px Grid)")]
    public static void FixAllTownSpritePivots()
    {
        ConfigureTownTextures();
        AssetDatabase.Refresh();
        Debug.Log("[JingJu] All town sprites updated (PPU=texture width, auto pivot). Erase Tilemap and paint again.");
    }

    [MenuItem("JingJu/Setup Town Map (Producer Tiles)")]
    public static void SetupTownMapScene()
    {
        var tilesByName = BuildTownTileAssets();
        if (tilesByName.Count == 0)
        {
            Debug.LogError("[JingJu] No town tiles. Put PNGs in Assets/Art/城镇 first.");
            return;
        }

        foreach (var name in new[] { "IsometricWorld", "Grid", "Player", "MapBounds", "CameraBounds" })
        {
            var old = GameObject.Find(name);
            if (old != null)
                Object.DestroyImmediate(old);
        }

        var gridRoot = new GameObject("IsometricWorld");
        var grid = gridRoot.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridRoot.transform, false);
        var groundTilemap = groundGo.AddComponent<Tilemap>();
        var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = 0;
        groundRenderer.mode = TilemapRenderer.Mode.Chunk;

        var decorGo = new GameObject("Decor");
        decorGo.transform.SetParent(gridRoot.transform, false);
        var decorTilemap = decorGo.AddComponent<Tilemap>();
        var decorRenderer = decorGo.AddComponent<TilemapRenderer>();
        decorRenderer.sortingOrder = 5;
        decorRenderer.mode = TilemapRenderer.Mode.Individual;

        var obstacleGo = new GameObject("Obstacle");
        obstacleGo.transform.SetParent(gridRoot.transform, false);
        var obstacleTilemap = obstacleGo.AddComponent<Tilemap>();
        var obstacleRenderer = obstacleGo.AddComponent<TilemapRenderer>();
        obstacleRenderer.sortingOrder = 10;
        obstacleRenderer.mode = TilemapRenderer.Mode.Chunk;

        var obstacleRb = obstacleGo.AddComponent<Rigidbody2D>();
        obstacleRb.bodyType = RigidbodyType2D.Static;
        var tilemapCollider = obstacleGo.AddComponent<TilemapCollider2D>();
        tilemapCollider.usedByComposite = true;
        obstacleGo.AddComponent<CompositeCollider2D>();

        PaintTownMap(groundTilemap, decorTilemap, obstacleTilemap, tilesByName);

        var mapBounds = CreateMapBounds(groundTilemap);
        var player = CreatePlayer(grid, groundTilemap, obstacleTilemap);
        var cameraFollow = IsometricDemoSceneSetup.SetupMainCameraPublic(player.transform, mapBounds);

        var gameUI = MainHUDUISetup.EnsureGameUICanvas();
        MainHUDUISetup.BuildMainHUD(gameUI.transform);
        IsometricDemoSceneSetup.SetupCameraZoomUIPublic(gameUI.transform, cameraFollow);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        Debug.Log("[JingJu] Town map built with producer tiles. Save scene (Ctrl+S).");
    }

    private static Dictionary<string, Tile> BuildTownTileAssets()
    {
        EnsureFolder("Assets/Art", "城镇");
        EnsureFolder(TownArtFolder, "Tiles");

        ConfigureTownTextures();
        AssetDatabase.Refresh();

        var result = new Dictionary<string, Tile>();
        var pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { TownArtFolder });
        foreach (var guid in pngGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (path.Contains("/Tiles/"))
                continue;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogWarning($"[JingJu] Skip (not a sprite): {path}");
                continue;
            }

            string baseName = Path.GetFileNameWithoutExtension(path);
            string tilePath = $"{TownTilesFolder}/{baseName}.asset";

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Sprite;
                AssetDatabase.CreateAsset(tile, tilePath);
            }
            else
            {
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
            }

            result[baseName] = tile;
        }

        AssetDatabase.SaveAssets();
        return result;
    }

    private static void ConfigureTownTextures()
    {
        if (!Directory.Exists(TownArtFolder))
            return;

        foreach (var file in Directory.GetFiles(TownArtFolder, "*.png"))
        {
            string assetPath = file.Replace('\\', '/');
            if (assetPath.Contains("/Tiles/"))
                continue;

            ApplyTownSpriteImport(assetPath);
        }
    }

    private static bool ApplyTownSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;

        if (!TryGetPngSize(assetPath, out int texWidth, out _))
            texWidth = 184;

        // One exported PNG = one Grid cell → PPU equals texture width in pixels.
        float ppu = Mathf.Max(texWidth, 1);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        Vector2 pivot = ComputeIsometricPivotFromPng(assetPath, TileGridPixels);
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = Vector4.zero;
        importer.SetTextureSettings(settings);

        importer.SaveAndReimport();
        Debug.Log($"[JingJu] {Path.GetFileName(assetPath)} pivot={pivot} PPU={ppu} (64px footprint inside image)");
        return true;
    }

    private static bool TryGetPngSize(string assetPath, out int width, out int height)
    {
        width = height = 0;
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
            return false;

        byte[] data = File.ReadAllBytes(fullPath);
        if (data.Length < 24 || data[0] != 137)
            return false;

        width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
        height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
        return width > 0 && height > 0;
    }

    private static void PaintTownMap(
        Tilemap ground,
        Tilemap decor,
        Tilemap obstacle,
        Dictionary<string, Tile> tiles)
    {
        ground.ClearAllTiles();
        decor.ClearAllTiles();
        obstacle.ClearAllTiles();

        Tile floorA = GetTile(tiles, "城镇-_0003_1");
        Tile floorB = GetTile(tiles, "城镇-_0004_2");
        Tile floorC = GetTile(tiles, "城镇-_0005_3");
        Tile stairA = GetTile(tiles, "城镇-_0000_6");
        Tile stairB = GetTile(tiles, "城镇-_0001_5");
        Tile blockB = GetTile(tiles, "城镇-_0002_4");

        Tile fallback = floorA ?? floorB ?? floorC ?? stairA;
        if (fallback == null)
        {
            Debug.LogError("[JingJu] No usable floor tile.");
            return;
        }

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool isBorder = x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1;

                if (isBorder)
                {
                    obstacle.SetTile(cell, blockB ?? fallback);
                    continue;
                }

                Tile variant = ((x + y) % 3) switch
                {
                    0 => floorA ?? fallback,
                    1 => floorB ?? fallback,
                    _ => floorC ?? fallback
                };
                ground.SetTile(cell, variant);
            }
        }

        // Center feature patch
        if (stairA != null)
            decor.SetTile(new Vector3Int(MapWidth / 2, MapHeight / 2, 0), stairA);
    }

    private static Tile GetTile(Dictionary<string, Tile> tiles, string key)
    {
        return tiles.TryGetValue(key, out var tile) ? tile : null;
    }

    private static GameObject CreatePlayer(Grid grid, Tilemap ground, Tilemap obstacle)
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var player = new GameObject("Player");
        if (!Character0PlayerSetup.TryApply(player, grid, ground, obstacle))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/PlaceholderTiles/PlayerSprite.png");
            if (sprite != null)
            {
                var sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 200;
            }

            var rb = player.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            player.AddComponent<CircleCollider2D>().radius = 0.2f;
            player.AddComponent<YSortByPosition>().SetGrid(grid);

            var controller = player.AddComponent<IsometricPlayerController>();
            controller.GroundTilemap = ground;
            controller.ObstacleTilemap = obstacle;
        }

        var spawn = new Vector3Int(MapWidth / 2, MapHeight / 2 - 1, 0);
        player.transform.position = ground.GetCellCenterWorld(spawn);
        return player;
    }

    private static GameObject CreateMapBounds(Tilemap groundTilemap)
    {
        var existing = GameObject.Find("MapBounds");
        if (existing != null)
            Object.DestroyImmediate(existing);

        groundTilemap.CompressBounds();
        var bounds = groundTilemap.localBounds;
        var worldMin = groundTilemap.transform.TransformPoint(bounds.min);
        var worldMax = groundTilemap.transform.TransformPoint(bounds.max);

        var go = new GameObject("MapBounds");
        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        var center = (worldMin + worldMax) * 0.5f;
        var size = worldMax - worldMin;
        go.transform.position = new Vector3(center.x, center.y, 0f);
        box.size = new Vector2(size.x, size.y);
        return go;
    }

    /// <summary>
    /// Finds the ~64px-wide footprint row (lower half) and returns a normalized sprite pivot.
    /// </summary>
    private static Vector2 ComputeIsometricPivotFromPng(string assetPath, int gridPixels)
    {
        string fullPath = Path.GetFullPath(assetPath);
        if (!File.Exists(fullPath))
            return new Vector2(0.5f, 0.85f);

        byte[] bytes = File.ReadAllBytes(fullPath);
        var temp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!temp.LoadImage(bytes))
        {
            Object.DestroyImmediate(temp);
            return new Vector2(0.5f, 0.85f);
        }

        int w = temp.width;
        int h = temp.height;
        int yStart = h / 2;
        int bestDist = int.MaxValue;
        int bestY = h - 1;
        float bestCx = w * 0.5f;

        for (int y = yStart; y < h; y++)
        {
            int minX = w;
            int maxX = -1;
            for (int x = 0; x < w; x++)
            {
                if (temp.GetPixel(x, y).a <= 0.08f)
                    continue;
                if (x < minX)
                    minX = x;
                if (x > maxX)
                    maxX = x;
            }

            if (maxX < minX)
                continue;

            int rowWidth = maxX - minX + 1;
            int dist = Mathf.Abs(rowWidth - gridPixels);
            if (dist < bestDist || (dist == bestDist && y > bestY))
            {
                bestDist = dist;
                bestY = y;
                bestCx = (minX + maxX + 1) * 0.5f;
            }
        }

        Object.DestroyImmediate(temp);
        return new Vector2(bestCx / w, (bestY + 0.5f) / h);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
