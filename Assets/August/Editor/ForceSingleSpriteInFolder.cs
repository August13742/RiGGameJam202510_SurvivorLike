#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class ForceSingleSpriteInFolder
{
    [MenuItem("Tools/Sprites/Force SINGLE (FullRect) for PNGs in Selected Folders")]
    private static void ForceSingle()
    {
        var selectedFolders = Selection.GetFiltered<Object>(SelectionMode.Assets)
            .Select(AssetDatabase.GetAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .ToArray();

        if (selectedFolders.Length == 0)
        {
            Debug.LogWarning("Select one or more folders in the Project view.");
            return;
        }

        int changed = 0;
        foreach (var folder in selectedFolders)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;

                bool dirty = false;

                // Core: make it a single sprite
                if (ti.textureType != TextureImporterType.Sprite) { ti.textureType = TextureImporterType.Sprite; dirty = true; }
                if (ti.spriteImportMode != SpriteImportMode.Single) { ti.spriteImportMode = SpriteImportMode.Single; dirty = true; }

                // Pixel art defaults
                if (ti.filterMode != FilterMode.Point) { ti.filterMode = FilterMode.Point; dirty = true; }
                if (ti.mipmapEnabled) { ti.mipmapEnabled = false; dirty = true; }
                if (ti.textureCompression != TextureImporterCompression.Uncompressed) { ti.textureCompression = TextureImporterCompression.Uncompressed; dirty = true; }
                if (ti.npotScale != TextureImporterNPOTScale.None) { ti.npotScale = TextureImporterNPOTScale.None; dirty = true; }

                // Avoid “tight” triangulation and phantom trims
                var settings = new TextureImporterSettings();
                ti.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect) { settings.spriteMeshType = SpriteMeshType.FullRect; dirty = true; }
                if (settings.spriteGenerateFallbackPhysicsShape) { settings.spriteGenerateFallbackPhysicsShape = false; dirty = true; }
                if (!settings.alphaIsTransparency) { settings.alphaIsTransparency = true; dirty = true; }
                ti.SetTextureSettings(settings);

                // Optional: keep palette exactness
                if (ti.wrapMode != TextureWrapMode.Clamp) { ti.wrapMode = TextureWrapMode.Clamp; dirty = true; }

                if (dirty)
                {
                    ti.SaveAndReimport();
                    changed++;
                }
            }
        }

        Debug.Log($"ForceSingleSpriteInFolder: updated {changed} PNG(s).");
    }
}
#endif