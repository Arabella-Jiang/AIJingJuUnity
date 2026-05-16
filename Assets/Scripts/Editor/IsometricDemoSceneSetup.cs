#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Menu: JingJu → Setup Isometric Demo Scene
/// Builds Phase 0 test map (placeholder tiles), player, camera, and bounds.
/// </summary>
public static class IsometricDemoSceneSetup
{
    private const string TilesFolder = "Assets/Art/PlaceholderTiles";
    private const string GroundTilePath = TilesFolder + "/GroundTile.asset";
    private const string ObstacleTilePath = TilesFolder + "/ObstacleTile.asset";
    private const string PlayerSpritePath = TilesFolder + "/PlayerSprite.png";

    private const int MapWidth = 18;
    private const int MapHeight = 14;

    [MenuItem("JingJu/Setup Isometric Demo Scene")]
    public static void SetupDemoScene()
    {
        EnsurePlaceholderAssets();

        foreach (var name in new[] { "IsometricWorld", "Grid", "Player", "MapBounds", "CameraBounds", "GameUI" })
        {
            var old = GameObject.Find(name);
            if (old != null)
                Object.DestroyImmediate(old);
        }

        var gridRoot = GameObject.Find("IsometricWorld");
        if (gridRoot != null)
            Object.DestroyImmediate(gridRoot);

        gridRoot = new GameObject("IsometricWorld");
        var grid = gridRoot.AddComponent<Grid>();
        grid.cellLayout = GridLayout.CellLayout.Isometric;
        grid.cellSize = new Vector3(1f, 0.5f, 1f);

        var groundTile = EnsureTileAsset(GroundTilePath, new Color(0.35f, 0.62f, 0.32f, 1f));
        var obstacleTile = EnsureTileAsset(ObstacleTilePath, new Color(0.72f, 0.28f, 0.22f, 1f));
        if (groundTile == null || obstacleTile == null)
        {
            Debug.LogError("[JingJu] Failed to create placeholder tiles. See Console for import errors.");
            return;
        }

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridRoot.transform, false);
        var groundTilemap = groundGo.AddComponent<Tilemap>();
        var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = 0;
        groundRenderer.mode = TilemapRenderer.Mode.Chunk;

        var obstacleGo = new GameObject("Obstacle");
        obstacleGo.transform.SetParent(gridRoot.transform, false);
        var obstacleTilemap = obstacleGo.AddComponent<Tilemap>();
        var obstacleRenderer = obstacleGo.AddComponent<TilemapRenderer>();
        obstacleRenderer.sortingOrder = 10;
        obstacleRenderer.mode = TilemapRenderer.Mode.Chunk;

        // Order matters: Static Rigidbody2D → TilemapCollider2D → CompositeCollider2D
        // (CompositeCollider2D may auto-add Rigidbody2D — do not AddComponent twice)
        var obstacleRb = obstacleGo.AddComponent<Rigidbody2D>();
        obstacleRb.bodyType = RigidbodyType2D.Static;
        var tilemapCollider = obstacleGo.AddComponent<TilemapCollider2D>();
        tilemapCollider.usedByComposite = true;
        var obstacleComposite = obstacleGo.AddComponent<CompositeCollider2D>();
        obstacleComposite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        PaintTestMap(groundTilemap, obstacleTilemap, groundTile, obstacleTile);

        var mapBounds = CreateMapBounds(grid, groundTilemap);
        var player = CreatePlayer(grid, groundTilemap, obstacleTilemap);
        var cameraFollow = SetupMainCamera(player.transform, mapBounds);
        SetupCameraZoomUI(cameraFollow);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        Debug.Log("[JingJu] Demo ready. Move: WASD / click ground. Zoom: Ctrl+scroll or UI +/-.");
    }

    private static void EnsurePlaceholderAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(TilesFolder))
            AssetDatabase.CreateFolder("Assets/Art", "PlaceholderTiles");

        if (!File.Exists(PlayerSpritePath))
        {
            var tex = CreateCircleTexture(64, 64, new Color(0.95f, 0.75f, 0.2f, 1f));
            File.WriteAllBytes(PlayerSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(PlayerSpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PlayerSpritePath);
            importer.spritePixelsPerUnit = 64;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        EnsureTileAsset(GroundTilePath, new Color(0.35f, 0.62f, 0.32f, 1f));
        EnsureTileAsset(ObstacleTilePath, new Color(0.72f, 0.28f, 0.22f, 1f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Creates or repairs a Tile when .asset / .png is missing or the sprite reference is broken.
    /// </summary>
    private static Tile EnsureTileAsset(string assetPath, Color color)
    {
        string pngPath = assetPath.Replace(".asset", ".png");
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        var pngSprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);

        bool valid = tile != null && tile.sprite != null && pngSprite != null;
        if (valid)
            return tile;

        if (AssetDatabase.LoadAssetAtPath<Object>(assetPath) != null)
            AssetDatabase.DeleteAsset(assetPath);
        if (AssetDatabase.LoadAssetAtPath<Object>(pngPath) != null)
            AssetDatabase.DeleteAsset(pngPath);

        CreateTileAsset(assetPath, color, 128, 64);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (tile == null || tile.sprite == null)
            Debug.LogError($"[JingJu] Tile rebuild failed: {assetPath}");

        return tile;
    }

    private static void CreateTileAsset(string path, Color color, int width, int height)
    {
        var tex = CreateDiamondTexture(width, height, color);
        var pngPath = path.Replace(".asset", ".png");
        File.WriteAllBytes(pngPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(pngPath);

        var importer = (TextureImporter)AssetImporter.GetAtPath(pngPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
        if (sprite == null)
        {
            Debug.LogError($"[JingJu] Sprite import failed: {pngPath}");
            return;
        }

        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null)
        {
            existing.sprite = sprite;
            EditorUtility.SetDirty(existing);
            return;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        AssetDatabase.CreateAsset(tile, path);
    }

    private static Texture2D CreateDiamondTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var clear = new Color(0, 0, 0, 0);
        float cx = width * 0.5f;
        float cy = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = Mathf.Abs(x - cx + 0.5f) / (width * 0.5f);
                float dy = Mathf.Abs(y - cy + 0.5f) / (height * 0.5f);
                tex.SetPixel(x, y, dx + dy <= 1f ? color : clear);
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    private static Texture2D CreateCircleTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        float cx = width * 0.5f;
        float cy = height * 0.5f;
        float r = width * 0.45f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                tex.SetPixel(x, y, d <= r ? color : new Color(0, 0, 0, 0));
            }
        }

        tex.Apply();
        tex.filterMode = FilterMode.Point;
        return tex;
    }

    private static void PaintTestMap(Tilemap ground, Tilemap obstacle, Tile groundTile, Tile obstacleTile)
    {
        ground.ClearAllTiles();
        obstacle.ClearAllTiles();

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool isBorder = x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1;

                // Phase 0 demo: only visible outer walls (no hidden inner blockers).
                if (isBorder)
                {
                    obstacle.SetTile(cell, obstacleTile);
                    continue;
                }

                ground.SetTile(cell, groundTile);
            }
        }
    }

    private static GameObject CreateMapBounds(Grid grid, Tilemap groundTilemap)
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

    private static GameObject CreatePlayer(Grid grid, Tilemap ground, Tilemap obstacle)
    {
        var existing = GameObject.Find("Player");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var player = new GameObject("Player");

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        var sr = player.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 200;

        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.2f;

        var ySort = player.AddComponent<YSortByPosition>();
        ySort.SetGrid(grid);

        var controller = player.AddComponent<IsometricPlayerController>();
        controller.GroundTilemap = ground;
        controller.ObstacleTilemap = obstacle;

        var spawnCell = new Vector3Int(MapWidth / 2, MapHeight / 2, 0);
        player.transform.position = ground.GetCellCenterWorld(spawnCell);

        return player;
    }

    private static StardewStyleCamera2D SetupMainCamera(Transform target, GameObject mapBounds)
    {
        var camGo = Camera.main != null ? Camera.main.gameObject : GameObject.Find("Main Camera");
        if (camGo == null)
            camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";

        var cam = camGo.GetComponent<Camera>();
        if (cam == null)
            cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.backgroundColor = new Color(0.15f, 0.18f, 0.22f);
        cam.transparencySortMode = TransparencySortMode.CustomAxis;
        cam.transparencySortAxis = new Vector3(0f, 1f, 0f);

        if (camGo.GetComponent<AudioListener>() == null)
            camGo.AddComponent<AudioListener>();

        var stale = camGo.GetComponents<StardewStyleCamera2D>();
        for (int i = 1; i < stale.Length; i++)
            Object.DestroyImmediate(stale[i]);

        var follow = camGo.GetComponent<StardewStyleCamera2D>();
        if (follow == null)
            follow = camGo.AddComponent<StardewStyleCamera2D>();

        follow.Target = target;
        follow.MapBoundsCollider = mapBounds.GetComponent<Collider2D>();

        camGo.transform.position = target.position + new Vector3(0f, 0f, -10f);
        return follow;
    }

    private static void SetupCameraZoomUI(StardewStyleCamera2D cameraFollow)
    {
        var uiRoot = new GameObject("GameUI");
        var canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        uiRoot.AddComponent<GraphicRaycaster>();

        if (Object.FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        var panel = new GameObject("CameraZoomPanel");
        panel.transform.SetParent(uiRoot.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-24f, 24f);
        panelRect.sizeDelta = new Vector2(120f, 112f);

        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        var zoomUi = panel.AddComponent<CameraZoomUI>();

        var zoomInBtn = CreateZoomButton(panel.transform, "+");
        var zoomOutBtn = CreateZoomButton(panel.transform, "-");

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            zoomInBtn.onClick, zoomUi.OnZoomInClicked);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            zoomOutBtn.onClick, zoomUi.OnZoomOutClicked);

        var zoomUiSo = new SerializedObject(zoomUi);
        zoomUiSo.FindProperty("cameraController").objectReferenceValue = cameraFollow;
        zoomUiSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button CreateZoomButton(Transform parent, string label)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(96f, 48f);

        var image = go.AddComponent<Image>();
        image.color = new Color(0.12f, 0.14f, 0.18f, 0.85f);

        var btn = go.AddComponent<Button>();

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 28;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        return btn;
    }
}
#endif
