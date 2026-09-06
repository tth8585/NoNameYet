using System.IO;
using UnityEditor;
using UnityEngine;

public static class TinySwordsUITextureOptimizer
{
    private const string UiRoot = "Assets/Tiny Swords/UI Elements";

    [MenuItem("Tools/Tiny Swords/Optimize UI Textures")]
    public static void OptimizeAll()
    {
        string[] assetGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { UiRoot });
        int optimizedCount = 0;

        foreach (string assetGuid in assetGuids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                continue;

            ConfigureImporter(importer);
            importer.SaveAndReimport();
            optimizedCount++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TinySwordsUI] Optimized {optimizedCount} UI textures.");
    }

    [MenuItem("Tools/Tiny Swords/Optimize UI Textures", true)]
    private static bool ValidateOptimizeAll()
    {
        return Directory.Exists(UiRoot);
    }

    private static void ConfigureImporter(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.anisoLevel = 1;
        importer.maxTextureSize = 512;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.compressionQuality = 75;
        importer.crunchedCompression = false;

        SetPlatform(importer, "Android", 512);
        SetPlatform(importer, "iPhone", 512);
        SetPlatform(importer, "Standalone", 512);
    }

    private static void SetPlatform(TextureImporter importer, string platform, int maxSize)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
        settings.name = platform;
        settings.overridden = true;
        settings.maxTextureSize = maxSize;
        settings.textureCompression = TextureImporterCompression.Compressed;
        settings.compressionQuality = 75;
        settings.crunchedCompression = false;
        importer.SetPlatformTextureSettings(settings);
    }
}
