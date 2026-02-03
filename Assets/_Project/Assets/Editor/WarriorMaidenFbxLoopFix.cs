#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class WarriorMaidenClipLoopFixSafe
{
    [MenuItem("Tools/Warrior Maiden/Fix LOOP on Selected Clips (SAFE)")]
    public static void FixLoopOnSelectedClips()
    {
        var selection = Selection.objects;
        if (selection == null || selection.Length == 0)
        {
            Debug.LogWarning("Vyber v Projectu AnimationClipy (Idle/Walk/Run/Strafe) a spusť znovu.");
            return;
        }

        int changed = 0;

        foreach (var obj in selection)
        {
            if (obj is not AnimationClip clip) continue;

            // Nešahat na preview klipy
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

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"✅ SAFE Loop fix hotovo. Upraveno klipů: {changed}");
    }
}
#endif
