using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Import settings limited to the curated Lab 1 sprites.</summary>
[InitializeOnLoad]
public sealed class Lab1AssetImporter : AssetPostprocessor
{
    private const string SpriteRoot = "Assets/_Project/Sprites/Kenney/";

    static Lab1AssetImporter()
    {
        EditorApplication.delayCall += ConfigureExistingSprites;
    }

    private void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(SpriteRoot) && assetPath.EndsWith(".png"))
            Configure((TextureImporter)assetImporter);
    }

    private static void Configure(TextureImporter importer)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = importer.assetPath.Contains("/Backgrounds/")
            ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
    }

    [MenuItem("Tools/Space Explorer/Configure Lab 1 Sprites")]
    public static void ConfigureExistingSprites()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += ConfigureExistingSprites;
            return;
        }

        int count = 0;
        foreach (string file in Directory.GetFiles(SpriteRoot, "*.png", SearchOption.AllDirectories))
        {
            string path = file.Replace('\\', '/');
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            TextureWrapMode wrap = path.Contains("/Backgrounds/")
                ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            bool changed = importer.textureType != TextureImporterType.Sprite
                || importer.spriteImportMode != SpriteImportMode.Single
                || importer.spritePixelsPerUnit != 100 || importer.mipmapEnabled
                || !importer.alphaIsTransparency || importer.filterMode != FilterMode.Bilinear
                || importer.wrapMode != wrap
                || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (changed)
            {
                Configure(importer);
                importer.SaveAndReimport();
            }
            count++;
        }
        string report = "Lab 1: configured " + count + " sprites (Single, 100 PPU, no mipmaps).";
        File.WriteAllText("Library/Lab1AssetImportReport.txt", report);
        Debug.Log(report);
    }
}
