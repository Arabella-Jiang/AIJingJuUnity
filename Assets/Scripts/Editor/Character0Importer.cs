#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Slices producer Character0 sheets (460×460 cells, 5 direction rows) and builds a sprite library for the player.
/// </summary>
public static class Character0Importer
{
    private const string CharacterFolder = "Assets/Character0";
    private const string LibraryPath = CharacterFolder + "/Character0SpriteLibrary.asset";

    private const int CellPixels = 460;
    private const int PixelsPerUnit = 460;

    private static readonly string[] CoreSheetNames =
    {
        "Character0_Idle",
        "Character0_Walk",
        "Character0_Run",
    };

    [MenuItem("JingJu/Import Character0 Sprites")]
    public static void ImportAll()
    {
        if (!AssetDatabase.IsValidFolder(CharacterFolder))
        {
            Debug.LogError("[JingJu] Missing folder Assets/Character0");
            return;
        }

        int sliced = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CharacterFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (ApplySpriteSheetImport(path))
                sliced++;
        }

        AssetDatabase.Refresh();
        var library = BuildOrUpdateLibrary();
        AssetDatabase.SaveAssets();

        Debug.Log($"[JingJu] Character0: sliced {sliced} PNG(s). Library ready={library != null && library.IsReady}. " +
                  "Run Setup Isometric Demo Scene to spawn the character.");
    }

    public static Character0SpriteLibrary BuildOrUpdateLibrary()
    {
        var library = AssetDatabase.LoadAssetAtPath<Character0SpriteLibrary>(LibraryPath);
        if (library == null)
        {
            EnsureFolder("Assets", "Character0");
            library = ScriptableObject.CreateInstance<Character0SpriteLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        library.idle = LoadSheetSprites("Character0_Idle", out int idleCols);
        library.walk = LoadSheetSprites("Character0_Walk", out int walkCols);
        library.run = LoadSheetSprites("Character0_Run", out int runCols);

        library.idleColumns = Mathf.Max(idleCols, 1);
        library.walkColumns = Mathf.Max(walkCols, 1);
        library.runColumns = Mathf.Max(runCols, 1);

        EditorUtility.SetDirty(library);
        return library;
    }

    public static Character0SpriteLibrary LoadLibrary()
    {
        return AssetDatabase.LoadAssetAtPath<Character0SpriteLibrary>(LibraryPath);
    }

    private static Sprite[] LoadSheetSprites(string textureName, out int columnsPerRow)
    {
        columnsPerRow = 1;
        string path = $"{CharacterFolder}/{textureName}.png";
        if (!File.Exists(path))
            return System.Array.Empty<Sprite>();

        if (!TryGetPngSize(path, out int width, out int height))
            return System.Array.Empty<Sprite>();

        columnsPerRow = Mathf.Max(width / CellPixels, 1);
        int rows = Mathf.Max(height / CellPixels, 1);

        var sprites = new List<Sprite>(rows * columnsPerRow);
        var subAssets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
        foreach (var obj in subAssets)
        {
            if (obj is Sprite sprite)
                sprites.Add(sprite);
        }

        sprites.Sort((a, b) => ExtractTrailingIndex(a.name).CompareTo(ExtractTrailingIndex(b.name)));
        return sprites.ToArray();
    }

    private static int ExtractTrailingIndex(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return 0;

        int end = spriteName.Length - 1;
        while (end >= 0 && char.IsDigit(spriteName[end]))
            end--;

        if (end == spriteName.Length - 1)
            return 0;

        return int.TryParse(spriteName.Substring(end + 1), out int index) ? index : 0;
    }

    private static bool ApplySpriteSheetImport(string assetPath)
    {
        if (!TryGetPngSize(assetPath, out int width, out int height))
            return false;

        int cols = Mathf.Max(width / CellPixels, 1);
        int rows = Mathf.Max(height / CellPixels, 1);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;

        string baseName = Path.GetFileNameWithoutExtension(assetPath);
        var sheet = new SpriteMetaData[cols * rows];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                sheet[index] = new SpriteMetaData
                {
                    name = $"{baseName}_{index}",
                    rect = new Rect(col * CellPixels, height - (row + 1) * CellPixels, CellPixels, CellPixels),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.14f),
                };
                index++;
            }
        }

        importer.spritesheet = sheet;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
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

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }
}
#endif
