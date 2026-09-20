using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

public static class FixPlayer2DUIAnimation
{
    private const string PrefabPath = "Assets/_Game/Prefabs/Player2D.prefab";
    private const string SourceClipPath = "Assets/Tiny Swords/Pawn and Resources/Pawn/Blue Pawn/Pawn Blue Animations/Pawn_Idle_Blue.anim";
    private const string OutputFolder = "Assets/_Game/Animations";
    private const string OutputClipPath = OutputFolder + "/Player2D_UI_Idle.anim";
    private const string OutputControllerPath = OutputFolder + "/Player2D_UI.controller";

    [MenuItem("Tools/Player 2D/Fix UI Idle Animation")]
    public static void Fix()
    {
        EnsureFolder(OutputFolder);

        AnimationClip sourceClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(SourceClipPath);
        if (sourceClip == null)
            throw new FileNotFoundException("Cannot find source idle clip", SourceClipPath);

        DeleteAssetIfExists(OutputClipPath);
        DeleteAssetIfExists(OutputControllerPath);

        AnimationClip uiClip = CreateImageClip(sourceClip);
        AssetDatabase.CreateAsset(uiClip, OutputClipPath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(OutputControllerPath);
        AnimatorState state = controller.layers[0].stateMachine.AddState("Player2D_UI_Idle");
        state.motion = uiClip;
        controller.layers[0].stateMachine.defaultState = state;

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Image image = prefabRoot.GetComponentInChildren<Image>(true);
            if (image == null)
                throw new MissingComponentException("Player2D prefab does not contain a UI Image");

            Animator animator = image.GetComponent<Animator>();
            if (animator == null)
                animator = image.gameObject.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            image.preserveAspect = true;
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Player2D] Created Image-compatible idle animation and assigned it to Player2D.prefab.");
    }

    private static AnimationClip CreateImageClip(AnimationClip sourceClip)
    {
        AnimationClip clip = new AnimationClip
        {
            name = "Player2D_UI_Idle",
            frameRate = sourceClip.frameRate,
            wrapMode = WrapMode.Loop
        };

        foreach (EditorCurveBinding sourceBinding in AnimationUtility.GetObjectReferenceCurveBindings(sourceClip))
        {
            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(sourceClip, sourceBinding);
            EditorCurveBinding imageBinding = sourceBinding;
            imageBinding.type = typeof(Image);
            AnimationUtility.SetObjectReferenceCurve(clip, imageBinding, keys);
        }

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        return clip;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace("\\", "/");
        string folder = Path.GetFileName(path);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private static void DeleteAssetIfExists(string path)
    {
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
    }
}
