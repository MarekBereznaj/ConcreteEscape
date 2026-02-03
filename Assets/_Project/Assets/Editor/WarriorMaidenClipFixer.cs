#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WarriorMaidenClipFixer
{
    [MenuItem("Tools/Warrior Maiden/Fix Clips (Loop + Pose)")]
    public static void FixSelectedClips()
    {
        var objs = Selection.objects;
        if (objs == null || objs.Length == 0)
        {
            Debug.LogWarning("Vyber v Project okně AnimationClipy nebo FBX s animacemi.");
            return;
        }

        int changed = 0;

        foreach (var o in objs)
        {
            var path = AssetDatabase.GetAssetPath(o);
            if (string.IsNullOrEmpty(path)) continue;

            // Z FBX vytáhne všechny AnimationClip sub-assets
            var clips = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in clips)
            {
                if (a is AnimationClip clip)
                {
                    if (clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    var so = new SerializedObject(clip);
                    var loopTime = so.FindProperty("m_LoopTime");
                    var loopPose = so.FindProperty("m_LoopBlend");

                    bool did = false;

                    if (loopTime != null && !loopTime.boolValue)
                    {
                        loopTime.boolValue = true;
                        did = true;
                    }

                    if (loopPose != null && !loopPose.boolValue)
                    {
                        loopPose.boolValue = true;
                        did = true;
                    }

                    if (did)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                        EditorUtility.SetDirty(clip);
                        changed++;
                    }
                }
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ Hotovo. Upraveno klipů: {changed}");
    }
}
#endif
