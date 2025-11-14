#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BuildAnimationFromPngs_Normalized
{
    private const bool LOOP = true;
    private const bool RECURSIVE = false;

    [MenuItem("Tools/Sprites/Build 1s Normalized Animation From PNGs")]
    private static void BuildNormalizedClips()
    {
        var folders = Selection.GetFiltered<UnityEngine.Object>(SelectionMode.Assets)
            .Select(AssetDatabase.GetAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .ToArray();

        if (folders.Length == 0)
        {
            Debug.LogWarning("Select one or more folders in the Project view.");
            return;
        }

        int total = 0;
        foreach (var folder in folders)
        {
            var sprites = CollectSprites(folder, RECURSIVE);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"No PNG sprites found in {folder}");
                continue;
            }

            string animDir = EnsureAnimFolder(folder);
            string baseName = System.IO.Path.GetFileName(folder.TrimEnd('/'));
            string clipPath = AssetDatabase.GenerateUniqueAssetPath($"{animDir}/{baseName}_1s.anim");

            var clip = CreateNormalizedClip(sprites, LOOP);
            AssetDatabase.CreateAsset(clip, clipPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"Built 1s animation with {sprites.Count} frames → {clipPath}");
            total++;
        }

        Debug.Log($"Done. Created {total} animation(s).");
    }

    private static List<Sprite> CollectSprites(string folder, bool recursive)
    {
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
        var paths = guids.Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

#if UNITY_2021_2_OR_NEWER
        paths.Sort(EditorUtility.NaturalCompare);
#else
        paths.Sort(StringComparer.OrdinalIgnoreCase);
#endif

        var sprites = new List<Sprite>();
        foreach (var p in paths)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(p);
            if (s != null) sprites.Add(s);
        }
        return sprites;
    }

    private static string EnsureAnimFolder(string folder)
    {
        string animDir = $"{folder.TrimEnd('/')}/Animations";
        if (!AssetDatabase.IsValidFolder(animDir))
            AssetDatabase.CreateFolder(folder, "Animations");
        return animDir;
    }

    private static AnimationClip CreateNormalizedClip(IReadOnlyList<Sprite> frames, bool loop)
    {
        var clip = new AnimationClip();

        int frameCount = frames.Count;
        float totalTime = 1f;
        float dt = totalTime / frameCount;

        // fps = sprite count (so duration = 1s)
        clip.frameRate = frameCount;

        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = string.Empty,
            propertyName = "m_Sprite"
        };

        var keys = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i * dt,
                value = frames[i]
            };
        }

        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        // Loop setting
        var so = new SerializedObject(clip);
        var settings = so.FindProperty("m_AnimationClipSettings");
        if (settings != null)
            settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
        so.ApplyModifiedPropertiesWithoutUndo();

        return clip;
    }
}
#endif
