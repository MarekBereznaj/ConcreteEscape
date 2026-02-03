#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class WarriorMaidenAnimatorFromAsset
{
    // Uprav, pokud máš jinou cestu
    private const string AnimFolder = "Assets/_Project/Prefabs/Warrior maiden/Animation";
    private const string OutputPath = "Assets/_Project/WarriorMaiden_NEW.controller";

    // Názvy klipů podle assetu (z obrázku)
    private const string Idle = "Idle1";
    private const string Walk = "walk1";
    private const string Run1 = "run1_fbx";
    private const string Run2 = "run2_fbx";

    private const string StrafeL1 = "Left_Strafe1";
    private const string StrafeL2 = "Left_Strafe2";
    private const string StrafeR1 = "Strafe_right1";
    private const string StrafeR2 = "Strafe_right2";

    private const string Jump1 = "Jumping1";
    private const string Jump2 = "Jumping2";

    private const string Attack1 = "attack1_mb";
    private const string Attack2 = "attack2_mb";
    private const string Block = "block";
    private const string Death1 = "death1";
    private const string Death2 = "death2";

    [MenuItem("Tools/Warrior Maiden/Generate Animator (Correct Locomotion)")]
    public static void Generate()
    {
        // --- load clips ---
        var idle = FindClip(Idle);
        var walk = FindClip(Walk);
        var run = FindClip(Run1) ?? FindClip(Run2);

        var strafeL = FindClip(StrafeL1) ?? FindClip(StrafeL2);
        var strafeR = FindClip(StrafeR1) ?? FindClip(StrafeR2);

        var jump = FindClip(Jump1) ?? FindClip(Jump2);

        var atk1 = FindClip(Attack1);
        var atk2 = FindClip(Attack2);
        var block = FindClip(Block);
        var death1 = FindClip(Death1);
        var death2 = FindClip(Death2);

        if (idle == null || walk == null || run == null)
        {
            Debug.LogError(
                "❌ Chybí Idle/Walk/Run klipy.\n" +
                $"Idle: {(idle ? idle.name : "NULL")}\n" +
                $"Walk: {(walk ? walk.name : "NULL")}\n" +
                $"Run : {(run ? run.name : "NULL")}\n"
            );
            return;
        }

        // --- důležité: Loop pro locomotion klipy (řeší 'pár kroků a freeze') ---
        ForceLoop(idle);
        ForceLoop(walk);
        ForceLoop(run);
        if (strafeL) ForceLoop(strafeL);
        if (strafeR) ForceLoop(strafeR);

        // --- delete old ---
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(OutputPath) != null)
            AssetDatabase.DeleteAsset(OutputPath);

        // --- create controller ---
        var controller = AnimatorController.CreateAnimatorControllerAtPath(OutputPath);
        var sm = controller.layers[0].stateMachine;

        // --- parameters ---
        AddParam(controller, "Speed", AnimatorControllerParameterType.Float);      // 0..1
        AddParam(controller, "MoveX", AnimatorControllerParameterType.Float);      // -1..1 (A/D)
        AddParam(controller, "MoveZ", AnimatorControllerParameterType.Float);      // -1..1 (W/S)
        AddParam(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        AddParam(controller, "Jump", AnimatorControllerParameterType.Trigger);

        AddParam(controller, "Attack", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "Block", AnimatorControllerParameterType.Trigger);
        AddParam(controller, "Die", AnimatorControllerParameterType.Trigger);

        // --- Locomotion state with 2D blend (strafe + forward) ---
        // Pokud strafe klipy nejsou, spadne to do 1D (Idle/Walk/Run)
        Motion locomotionMotion = CreateLocomotion(controller, idle, walk, run, strafeL, strafeR);

        var locomotion = sm.AddState("Locomotion");
        locomotion.motion = locomotionMotion;
        sm.defaultState = locomotion;

        // --- Jump state (optional) ---
        if (jump != null)
        {
            var jumpState = sm.AddState("Jump");
            jumpState.motion = jump;

            var toJump = locomotion.AddTransition(jumpState);
            SetupTransition(toJump, 0.05f);
            toJump.AddCondition(AnimatorConditionMode.If, 0f, "Jump");

            var toLoc = jumpState.AddTransition(locomotion);
            SetupTransition(toLoc, 0.08f);
            toLoc.AddCondition(AnimatorConditionMode.If, 0f, "IsGrounded");
        }
        else
        {
            Debug.LogWarning("⚠️ Jumping1/Jumping2 nenalezen – controller bude bez Jump.");
        }

        // --- Attacks / block / death from Any State (optional) ---
        // Všechny tyhle klipy jsou skoro vždy NON-LOOP, takže to necháme s ExitTime ON.
        if (atk1 != null)
        {
            var s = sm.AddState("Attack1");
            s.motion = atk1;

            var tr = sm.AddAnyStateTransition(s);
            tr.hasExitTime = false;
            tr.duration = 0.05f;
            tr.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            var back = s.AddTransition(locomotion);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.05f;
        }

        if (atk2 != null)
        {
            var s = sm.AddState("Attack2");
            s.motion = atk2;

            var tr = sm.AddAnyStateTransition(s);
            tr.hasExitTime = false;
            tr.duration = 0.05f;
            tr.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            var back = s.AddTransition(locomotion);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.05f;
        }

        if (block != null)
        {
            var s = sm.AddState("Block");
            s.motion = block;

            var tr = sm.AddAnyStateTransition(s);
            tr.hasExitTime = false;
            tr.duration = 0.05f;
            tr.AddCondition(AnimatorConditionMode.If, 0f, "Block");

            var back = s.AddTransition(locomotion);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.05f;
        }

        if (death1 != null)
        {
            var s = sm.AddState("Death1");
            s.motion = death1;

            var tr = sm.AddAnyStateTransition(s);
            tr.hasExitTime = false;
            tr.duration = 0.05f;
            tr.AddCondition(AnimatorConditionMode.If, 0f, "Die");

            // death usually stays, no transition back
        }
        if (death2 != null)
        {
            var s = sm.AddState("Death2");
            s.motion = death2;

            var tr = sm.AddAnyStateTransition(s);
            tr.hasExitTime = false;
            tr.duration = 0.05f;
            tr.AddCondition(AnimatorConditionMode.If, 0f, "Die");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = controller;
        Debug.Log($"✅ Hotovo: nový Animator Controller vytvořen: {OutputPath}");
    }

    // ---------- Locomotion creators ----------

    private static Motion CreateLocomotion(
        AnimatorController controller,
        AnimationClip idle,
        AnimationClip walk,
        AnimationClip run,
        AnimationClip strafeL,
        AnimationClip strafeR)
    {
        // Pokud strafe klipy chybí -> 1D BlendTree (Speed)
        if (strafeL == null || strafeR == null)
        {
            var tree1D = new BlendTree
            {
                name = "Locomotion_1D",
                blendType = BlendTreeType.Simple1D,
                blendParameter = "Speed",
                useAutomaticThresholds = false
            };
            tree1D.AddChild(idle, 0f);
            tree1D.AddChild(walk, 0.5f);
            tree1D.AddChild(run, 1f);

            AssetDatabase.AddObjectToAsset(tree1D, controller);
            return tree1D;
        }

        // 2D BlendTree: MoveX (A/D), MoveZ (W/S), scaled by Speed in code
        var tree2D = new BlendTree
        {
            name = "Locomotion_2D",
            blendType = BlendTreeType.FreeformDirectional2D,
            blendParameter = "MoveX",
            blendParameterY = "MoveZ",
            useAutomaticThresholds = false
        };

        // center idle
        tree2D.AddChild(idle, new Vector2(0f, 0f));

        // forward walk/run (same direction, different magnitude)
        tree2D.AddChild(walk, new Vector2(0f, 0.6f));
        tree2D.AddChild(run, new Vector2(0f, 1.2f));

        // strafe
        tree2D.AddChild(strafeL, new Vector2(-1f, 0f));
        tree2D.AddChild(strafeR, new Vector2(1f, 0f));

        AssetDatabase.AddObjectToAsset(tree2D, controller);
        return tree2D;
    }

    // ---------- helpers ----------

    private static void AddParam(AnimatorController c, string name, AnimatorControllerParameterType t)
    {
        foreach (var p in c.parameters)
            if (p.name == name) return;
        c.AddParameter(name, t);
    }

    private static void SetupTransition(AnimatorStateTransition t, float duration)
    {
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = duration;
        t.interruptionSource = TransitionInterruptionSource.Source; // kompatibilní
        t.orderedInterruption = true;
    }

    private static AnimationClip FindClip(string name)
    {
        string[] guids = AssetDatabase.FindAssets($"t:AnimationClip {name}", new[] { AnimFolder });
        if (guids == null || guids.Length == 0) return null;

        // Prefer exact match
        foreach (var g in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(g);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip != null && string.Equals(clip.name, name, StringComparison.OrdinalIgnoreCase))
                return clip;
        }

        // Fallback
        var p0 = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(p0);
    }

    // SAFE-ish loop fix for clip assets (doesn't touch rig import settings)
    private static void ForceLoop(AnimationClip clip)
    {
        if (clip == null) return;
        if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)) return;

        var so = new SerializedObject(clip);
        var loopTime = so.FindProperty("m_LoopTime");
        var loopPose = so.FindProperty("m_LoopBlend");

        bool changed = false;

        if (loopTime != null && !loopTime.boolValue)
        {
            loopTime.boolValue = true;
            changed = true;
        }
        if (loopPose != null && !loopPose.boolValue)
        {
            loopPose.boolValue = true;
            changed = true;
        }

        if (changed)
        {
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(clip);
        }
    }
}
#endif
