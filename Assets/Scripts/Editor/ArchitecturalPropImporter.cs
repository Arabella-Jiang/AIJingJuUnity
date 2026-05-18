#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports individual prop PNGs from Assets/Art/建筑小品 (no slicing).
/// Each file → Sprite + prefab with SpriteRenderer and YSortByPosition.
/// </summary>
public static class ArchitecturalPropImporter
{
    private const string PropArtFolder = "Assets/Art/建筑小品";
    private const string PrefabsFolder = PropArtFolder + "/Prefabs";

    private const int ProducerCellPixels = 184;
    private const int FootprintPixels = 64;

    [MenuItem("JingJu/Import 建筑小品 Props (No Slice)")]
    public static void ImportAllProps()
    {
        int count = ImportPropsInternal();
        if (count == 0)
            Debug.LogError("[JingJu] No PNGs in Assets/Art/建筑小品");
        else
            Debug.Log($"[JingJu] Imported {count} prop(s). Prefabs: {PrefabsFolder}. Drag into scene under IsometricWorld.");
    }

    [MenuItem("JingJu/Fix 建筑小品 Sprite Import (All PNGs)")]
    public static void FixAllPropSprites()
    {
        int count = ConfigurePropTextures();
        AssetDatabase.Refresh();
        Debug.Log($"[JingJu] Fixed PPU/pivot on {count} prop sprite(s).");
    }

    [MenuItem("JingJu/Fix 建筑小品 Sprite Import (Selected PNGs)")]
    public static void FixSelectedPropSprites()
    {
        int count = 0;
        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (!path.StartsWith(PropArtFolder, System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (path.Contains("/Prefabs/"))
                continue;
            if (ApplyPropSpriteImport(path))
                count++;
        }

        if (count == 0)
            Debug.LogWarning("[JingJu] Select PNG(s) under Assets/Art/建筑小品.");
        else
            Debug.Log($"[JingJu] Fixed {count} prop sprite(s).");
    }

    private static int ImportPropsInternal()
    {
        EnsureFolder("Assets/Art", "建筑小品");
        EnsureFolder(PropArtFolder, "Prefabs");

        int configured = ConfigurePropTextures();
        AssetDatabase.Refresh();

        int prefabs = 0;
        foreach (string path in FindPropTexturePaths())
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
                continue;

            string baseName = Path.GetFileNameWithoutExtension(path);
            string prefabPath = $"{PrefabsFolder}/{baseName}.prefab";
            CreateOrUpdatePrefab(prefabPath, sprite, baseName);
            prefabs++;
        }

        if (prefabs > 0)
        {
            var first = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsFolder}/1.prefab");
            if (first != null)
                Selection.activeObject = first;
        }

        return prefabs > 0 ? prefabs : configured;
    }

    private static int ConfigurePropTextures()
    {
        if (!Directory.Exists(PropArtFolder))
            return 0;

        int count = 0;
        foreach (string path in FindPropTexturePaths())
        {
            if (ApplyPropSpriteImport(path))
                count++;
        }

        return count;
    }

    private static IEnumerable<string> FindPropTexturePaths()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { PropArtFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;
            if (path.Contains("/Prefabs/"))
                continue;
            yield return path;
        }
    }

    private static bool ApplyPropSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;

        if (!TryGetPngSize(assetPath, out int texWidth, out _))
            texWidth = 184;

        int estCells = Mathf.Max(1, Mathf.RoundToInt(texWidth / (float)ProducerCellPixels));
        float ppu = texWidth / (float)estCells;
        Vector2 pivot = ComputeIsometricPivotFromPng(assetPath, FootprintPixels);

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
        return true;
    }

    private static void CreateOrUpdatePrefab(string prefabPath, Sprite sprite, string objectName)
    {
        bool editingExisting = File.Exists(prefabPath);
        GameObject root = editingExisting
            ? PrefabUtility.LoadPrefabContents(prefabPath)
            : new GameObject(objectName);

        root.name = objectName;
        var sr = root.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = root.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 50;

        var ySort = root.GetComponent<YSortByPosition>();
        if (ySort == null)
            ySort = root.AddComponent<YSortByPosition>();
        ySort.SetCharacterBias(120);

        EnsureFootprintCollider(root, sprite);

        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        if (editingExisting)
            PrefabUtility.UnloadPrefabContents(root);
        else
            Object.DestroyImmediate(root);
    }

    private static void EnsureFootprintCollider(GameObject root, Sprite sprite)
    {
        Transform footTransform = root.transform.Find("Footprint");
        GameObject footGo = footTransform != null ? footTransform.gameObject : new GameObject("Footprint");
        if (footTransform == null)
            footGo.transform.SetParent(root.transform, false);

        var box = footGo.GetComponent<BoxCollider2D>();
        if (box == null)
            box = footGo.AddComponent<BoxCollider2D>();

        float width = Mathf.Clamp(sprite.bounds.size.x * 0.85f, 0.45f, 6.5f);
        box.size = new Vector2(width, 0.28f);
        box.offset = Vector2.zero;

        if (footGo.GetComponent<PropFootprintCollider>() == null)
            footGo.AddComponent<PropFootprintCollider>();
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
            return new Vector2(0.5f, 0.12f);

        byte[] bytes = File.ReadAllBytes(fullPath);
        var temp = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!temp.LoadImage(bytes))
        {
            Object.DestroyImmediate(temp);
            return new Vector2(0.5f, 0.12f);
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

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
