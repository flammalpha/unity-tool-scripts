/**
* FlammAlpha 2024
* Replaces _ with / in animation object names
*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class RenameAnimations
{
    #region Normal

    [MenuItem("Tools/Animation Rename/Clip Names Normal")]
    private static void RevertAnimationClips()
    {
        // Get GUIDs for all .anim assets
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            RevertAnimation(clip);
        }

        Debug.Log("All animation objects have been renamed!");
    }

    [MenuItem("Assets/Animation Rename/Clip Name Normal")]
    private static void RevertAnimationClip()
    {
        Object clip = Selection.activeObject;

        RevertAnimation(clip);
    }

    [MenuItem("Assets/Animation Rename/Clip Name Normal", true)]
    private static bool ValidateRevertAnimationClip()
    {
        return Selection.activeObject is AnimationClip;
    }

    private static bool RevertAnimation(Object asset)
    {
        if (asset == null)
        {
            Debug.Log($"Animation object is null!");
            return false;
        }

        string assetPath = AssetDatabase.GetAssetPath(asset);
        string filename = Path.GetFileNameWithoutExtension(assetPath);

        // Create serialized object for updating properties
        SerializedObject serializedClip = new SerializedObject(asset);
        SerializedProperty clipNameProperty = serializedClip.FindProperty("m_Name");

        // update and apply
        clipNameProperty.stringValue = filename;
        serializedClip.ApplyModifiedProperties();
        Debug.Log("AnimationClip Name has been successfully reverted !");
        return true;
    }

    #endregion Normal

    #region SLASH

    [MenuItem("Tools/Animation Rename/Clip Names SLASH")]
    private static void RenameAnimationClips()
    {
        // Get GUIDs for all .anim assets
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            RenameAnimation(clip);
        }

        Debug.Log("All animation objects have been renamed!");
    }

    [MenuItem("Assets/Animation Rename/Clip Name SLASH")]
    private static void RenameAnimationClip()
    {
        AnimationClip clip = (AnimationClip)Selection.activeObject;

        RenameAnimation(clip);
    }

    [MenuItem("Assets/Animation Rename/Clip Name SLASH", true)]
    private static bool ValidateRenameAnimationClip()
    {
        return Selection.activeObject is AnimationClip;
    }

    private static bool RenameAnimation(AnimationClip clip)
    {
        if (clip == null)
        {
            Debug.Log($"Animation object is null!");
            return false;
        }

        string newName = clip.name.Replace(' ', '_');
        newName = newName.Replace('_', '/');

        // Create serialized object for updating properties
        SerializedObject serializedClip = new SerializedObject(clip);
        SerializedProperty clipNameProperty = serializedClip.FindProperty("m_Name");

        // update and apply
        clipNameProperty.stringValue = newName;
        serializedClip.ApplyModifiedProperties();

        Debug.Log($"Animation object has been renamed to {newName} !");
        return true;
    }

    #endregion SLASH

    #region NOSPACE

    [MenuItem("Tools/Animation Rename/File Names NOSPACE")]
    private static void RenameAnimationFiles()
    {
        // Get GUIDs for all .anim assets
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            RenameAssetFile(assetPath);
        }

        // Make sure the changes are saved
        AssetDatabase.SaveAssets();
        Debug.Log("Animation names have been successfully changed!");
    }

    [MenuItem("Assets/Animation Rename/File Name NOSPACE")]
    private static void RenameSelectedAssetFile()
    {
        Object asset = Selection.activeObject;
        if (asset != null)
        {
            string assetPath = AssetDatabase.GetAssetPath(asset);
            RenameAssetFile(assetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("Asset has been successfully renamed !");
        }
        else
        {
            Debug.Log("No asset is selected!");
        }
    }

    // We only want the menu to be enabled when an AnimationClip is selected
    [MenuItem("Assets/Animation Rename/File Name NOSPACE", true)]
    private static bool CanRenameFile()
    {
        return Selection.activeObject && Selection.activeObject is AnimationClip;
    }

    private static bool RenameAssetFile(string assetPath)
    {
        if (assetPath == null || assetPath == "") {
            return false;
        }
        string assetFileName = Path.GetFileNameWithoutExtension(assetPath);

        // Replace underscores with slashes in the asset name
        string newAssetName = assetFileName.Replace(' ', '_');

        // Rename the asset
        AssetDatabase.RenameAsset(assetPath, newAssetName);
        return true;
    }

    #endregion NOSPACE
}
#endif