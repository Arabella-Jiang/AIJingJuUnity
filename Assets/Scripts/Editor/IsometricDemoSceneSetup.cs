#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>
/// Menu: JingJu → Setup Isometric Demo Scene
/// floor_00/01/02/06/07/08 ground, floor_01 border, vendor prefabs on top (invisible footprint colliders).
/// </summary>
public static class IsometricDemoSceneSetup
{
    private const string TilesFolder = "Assets/Art/PlaceholderTiles";
    private const string GroundTilePath = TilesFolder + "/GroundTile.asset";
    private const string ObstacleTilePath = TilesFolder + "/ObstacleTile.asset";
    private const string PlayerSpritePath = TilesFolder + "/PlayerSprite.png";

    private const string FloorTilesFolder = "Assets/Art/地砖/Tiles";
    private const string BoundarySourceFloor = "Assets/Art/地砖/Tiles/floor_01.asset";
    private const string BoundaryTilePath = "Assets/Art/地砖/Tiles/boundary_stone.asset";
    private const string VendorPrefabsFolder = "Assets/Art/建筑小品/Prefabs";

    // One flat ground tile only — mixing 02/06/07/08 looks like stacked 3D blocks (brush uses one tile).
    private const string PrimaryGroundFloor = "Assets/Art/地砖/Tiles/floor_00.asset";

    private const int MapWidth = 36;
    private const int MapHeight = 28;
    private static readonly (string prefabName, int cellX, int cellY)[] VendorSpawns =
    {
        ("1", 10, 7),
        ("2", 25, 7),
        ("3", 16, 15),
        ("5", 10, 20),
    };

    [MenuItem("JingJu/Setup Isometric Demo Scene")]
    public static void SetupDemoScene()
    {
        EnsurePlaceholderAssets();

        foreach (var name in new[] { "IsometricWorld", "Grid", "Player", "MapBounds", "CameraBounds", "GameUI", "Props" })
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

        ConfigureProducerFloorImports();

        var primaryGround = LoadPrimaryGroundTile();
        var boundaryTile = EnsureBoundaryTile();
        var useProducerArt = primaryGround != null && boundaryTile != null;

        Tile placeholderGround = null;
        Tile placeholderObstacle = null;
        if (!useProducerArt)
        {
            placeholderGround = EnsureTileAsset(GroundTilePath, new Color(0.35f, 0.62f, 0.32f, 1f));
            placeholderObstacle = EnsureTileAsset(ObstacleTilePath, new Color(0.72f, 0.28f, 0.22f, 1f));
            if (placeholderGround == null || placeholderObstacle == null)
            {
                Debug.LogError("[JingJu] Failed to create placeholder tiles. Run Slice 地砖 first or check Console.");
                return;
            }
        }

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridRoot.transform, false);
        var groundTilemap = groundGo.AddComponent<Tilemap>();
        var groundRenderer = groundGo.AddComponent<TilemapRenderer>();
        groundRenderer.sortingOrder = 0;
        groundRenderer.mode = TilemapRenderer.Mode.Chunk;

        var decorGo = new GameObject("Decor");
        decorGo.transform.SetParent(gridRoot.transform, false);
        decorGo.AddComponent<Tilemap>();
        var decorRenderer = decorGo.AddComponent<TilemapRenderer>();
        decorRenderer.sortingOrder = 5;

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

        var propsRoot = new GameObject("Props");
        propsRoot.transform.SetParent(gridRoot.transform, false);

        if (useProducerArt)
            PaintProducerMap(groundTilemap, obstacleTilemap, primaryGround, boundaryTile);
        else
            PaintPlaceholderMap(groundTilemap, obstacleTilemap, placeholderGround, placeholderObstacle);

        int vendors = PlaceVendorPrefabs(grid, propsRoot);

        var mapBounds = CreateMapBounds(grid, groundTilemap);
        var player = CreatePlayer(grid, groundTilemap, obstacleTilemap);
        var cameraFollow = SetupMainCamera(player.transform, mapBounds);
        var gameUI = MainHUDUISetup.EnsureGameUICanvas();
        MainHUDUISetup.BuildMainHUD(gameUI.transform);
        SetupCameraZoomUI(gameUI.transform, cameraFollow);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = player;

        Debug.Log(
            $"[JingJu] Demo ready ({(useProducerArt ? $"{MapWidth}x{MapHeight} floor_00 + border floor_01" : "placeholder tiles")}, " +
            $"{vendors} vendor(s)). WASD / click move. Ctrl+scroll zoom.");
    }

    private static Tile LoadPrimaryGroundTile()
    {
        return AssetDatabase.LoadAssetAtPath<Tile>(PrimaryGroundFloor);
    }

    private static Tile EnsureBoundaryTile()
    {
        var source = AssetDatabase.LoadAssetAtPath<Tile>(BoundarySourceFloor);
        if (source == null || source.sprite == null)
            return null;

        var existing = AssetDatabase.LoadAssetAtPath<Tile>(BoundaryTilePath);
        if (existing != null)
        {
            existing.sprite = source.sprite;
            existing.colliderType = Tile.ColliderType.Sprite;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = source.sprite;
        tile.colliderType = Tile.ColliderType.Sprite;
        AssetDatabase.CreateAsset(tile, BoundaryTilePath);
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static void PaintProducerMap(Tilemap ground, Tilemap obstacle, Tile groundTile, Tile boundaryTile)
    {
        ground.ClearAllTiles();
        obstacle.ClearAllTiles();

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool isBorder = x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1;

                if (isBorder)
                {
                    // Border only on Obstacle — same as painting one layer with the brush.
                    obstacle.SetTile(cell, boundaryTile);
                    continue;
                }

                ground.SetTile(cell, groundTile);
            }
        }
    }

    private static void PaintPlaceholderMap(Tilemap ground, Tilemap obstacle, Tile groundTile, Tile obstacleTile)
    {
        ground.ClearAllTiles();
        obstacle.ClearAllTiles();

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                bool isBorder = x == 0 || y == 0 || x == MapWidth - 1 || y == MapHeight - 1;

                if (isBorder)
                    obstacle.SetTile(cell, obstacleTile);
                else
                    ground.SetTile(cell, groundTile);
            }
        }
    }

    private static void ConfigureProducerFloorImports()
    {
        const string slicedFolder = "Assets/Art/地砖/Sliced";
        if (!AssetDatabase.IsValidFolder(slicedFolder))
            return;

        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { slicedFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                ApplyFloorSpriteImport(path);
        }
    }

    private static void ApplyFloorSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        if (!TryGetPngSize(assetPath, out int texWidth, out _))
            texWidth = 184;

        float ppu = Mathf.Max(texWidth, 1);
        Vector2 pivot = ComputeIsometricPivotFromPng(assetPath, 64);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteBorder = Vector4.zero;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
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
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
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

    private static int PlaceVendorPrefabs(Grid grid, GameObject propsRoot)
    {
        if (!AssetDatabase.IsValidFolder(VendorPrefabsFolder))
            return 0;

        int placed = 0;
        foreach (var spawn in VendorSpawns)
        {
            string prefabPath = $"{VendorPrefabsFolder}/{spawn.prefabName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                continue;

            var cell = new Vector3Int(spawn.cellX, spawn.cellY, 0);
            if (cell.x <= 0 || cell.y <= 0 || cell.x >= MapWidth - 1 || cell.y >= MapHeight - 1)
                continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, propsRoot.transform);
            instance.transform.position = grid.GetCellCenterWorld(cell);

            var ySort = instance.GetComponent<YSortByPosition>();
            if (ySort != null)
                ySort.SetGrid(grid);

            placed++;
        }

        return placed;
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
        tile.colliderType = Tile.ColliderType.Sprite;
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

        var spawnCell = new Vector3Int(MapWidth / 2, MapHeight / 2 - 2, 0);
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
        cam.orthographicSize = 11f;
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
        follow.AllowCtrlScrollZoom = false;

        camGo.transform.position = target.position + new Vector3(0f, 0f, -10f);
        return follow;
    }

    private static void SetupCameraZoomUI(Transform canvasRoot, StardewStyleCamera2D cameraFollow)
    {
        var oldZoom = canvasRoot.Find("CameraZoomPanel");
        if (oldZoom != null)
            Object.DestroyImmediate(oldZoom.gameObject);

        var panel = new GameObject("CameraZoomPanel");
        panel.transform.SetParent(canvasRoot, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(1f, 0f);
        panelRect.anchoredPosition = new Vector2(-20f, 20f);
        panelRect.sizeDelta = new Vector2(300f, 52f);

        var row = panel.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 10f;
        row.padding = new RectOffset(6, 6, 6, 6);
        row.childAlignment = TextAnchor.MiddleCenter;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;

        var zoomUi = panel.AddComponent<CameraZoomUI>();

        var zoomOutBtn = CreateZoomButton(panel.transform, "-", 44f);
        var slider = CreateZoomSlider(panel.transform);
        var zoomInBtn = CreateZoomButton(panel.transform, "+", 44f);

        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            zoomOutBtn.onClick, zoomUi.OnZoomOutClicked);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            zoomInBtn.onClick, zoomUi.OnZoomInClicked);

        var layoutElement = slider.gameObject.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1f;
        layoutElement.minWidth = 160f;
        layoutElement.preferredHeight = 28f;

        var zoomUiSo = new SerializedObject(zoomUi);
        zoomUiSo.FindProperty("cameraController").objectReferenceValue = cameraFollow;
        zoomUiSo.FindProperty("zoomSlider").objectReferenceValue = slider;
        zoomUiSo.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button CreateZoomButton(Transform parent, string label, float size)
    {
        var go = new GameObject($"Btn_{label}");
        go.transform.SetParent(parent, false);

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(size, size);

        var le = go.AddComponent<LayoutElement>();
        le.preferredWidth = size;
        le.preferredHeight = size;

        var image = go.AddComponent<Image>();
        image.color = new Color(0.12f, 0.14f, 0.18f, 0.88f);

        var btn = go.AddComponent<Button>();
        AddButtonLabel(go.transform, label, 26);

        return btn;
    }

    private static Slider CreateZoomSlider(Transform parent)
    {
        var root = new GameObject("ZoomSlider");
        root.transform.SetParent(parent, false);

        var rootRect = root.AddComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(180f, 28f);

        var bgGo = new GameObject("Background");
        bgGo.transform.SetParent(root.transform, false);
        var bgRect = bgGo.AddComponent<RectTransform>();
        StretchRect(bgRect);
        var bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.1f, 0.11f, 0.14f, 0.9f);

        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRect.offsetMin = new Vector2(8f, 0f);
        fillAreaRect.offsetMax = new Vector2(-8f, 0f);

        var fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        var fillRect = fill.AddComponent<RectTransform>();
        StretchRect(fillRect);
        var fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.75f, 0.68f, 0.45f, 0.95f);

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(root.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        StretchRect(handleAreaRect);

        var handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        var handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 28f);
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.95f, 0.93f, 0.88f, 1f);

        var slider = root.AddComponent<Slider>();
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.value = 0.5f;

        return slider;
    }

    private static void StretchRect(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static StardewStyleCamera2D SetupMainCameraPublic(Transform target, GameObject mapBounds)
    {
        return SetupMainCamera(target, mapBounds);
    }

    public static void SetupCameraZoomUIPublic(Transform canvasRoot, StardewStyleCamera2D cameraFollow)
    {
        SetupCameraZoomUI(canvasRoot, cameraFollow);
    }

    private static void AddButtonLabel(Transform parent, string label, int fontSize)
    {
        var textGo = new GameObject("Text");
        textGo.transform.SetParent(parent, false);
        var textRect = textGo.AddComponent<RectTransform>();
        StretchRect(textRect);

        var text = textGo.AddComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
#endif
