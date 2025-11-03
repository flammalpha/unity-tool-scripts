using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FlammAlpha.UnityTools.Common;

namespace FlammAlpha.UnityTools.Animation
{
    /// <summary>
    /// Editor window that inspects blendshapes in a selected avatar GameObject and
    /// shows which animation clips from a provided Animator Controller reference them.
    /// - Avatar GameObject input
    /// - Animation Controller input (auto-filled from Avatar's Animator if present)
    /// - Grouped list (per GameObject / SkinnedMeshRenderer) with foldouts
    /// - Each blendshape shows referencing animation count and expandable list of clips
    /// </summary>
    public class BlendshapeInspector : EditorWindow
    {
        [MenuItem("Tools/FlammAlpha/Blendshape Inspector")]
        private static void ShowWindow() => GetWindow<BlendshapeInspector>("Blendshape Inspector");

        private GameObject avatarObject;
        private Animator animator;
        private RuntimeAnimatorController controller;

        // Model: map a renderer GameObject to its blendshapes and referencing clips
        private readonly Dictionary<SkinnedMeshRenderer, List<BlendshapeEntry>> model = new();

        // Foldout managers reusing existing utility
        private EditorListUtility.FoldoutManager<GameObject> rendererFoldouts = new();
        private EditorListUtility.FoldoutManager<string> blendshapeFoldouts = new();

        // Scroll position
        private Vector2 scrollPos;

        // Cache animation clips from controller
        private AnimationClip[] controllerClips = Array.Empty<AnimationClip>();

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= FillModel;
            Undo.undoRedoPerformed += FillModel;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= FillModel;
        }

        private void OnSelectionChange()
        {
            // If user selects a GameObject in the editor, automatically assign it as avatarObject
            if (Selection.activeGameObject != null)
            {
                avatarObject = Selection.activeGameObject;
                // If it has an Animator, auto-assign animator and controller
                animator = avatarObject.GetComponent<Animator>();
                if (animator != null && animator.runtimeAnimatorController != null && controller == null)
                    controller = animator.runtimeAnimatorController;
                FillModel();
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(6);

            using (new GUILayout.VerticalScope("helpbox"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    avatarObject = (GameObject)EditorGUILayout.ObjectField("Avatar GameObject:", avatarObject, typeof(GameObject), true);
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                        Selection.activeGameObject = avatarObject;
                    if (EditorGUI.EndChangeCheck())
                    {
                        animator = avatarObject ? avatarObject.GetComponent<Animator>() : null;
                        // Auto fill controller if animator references one and user hasn't manually set another
                        if (animator != null && animator.runtimeAnimatorController != null && (controller == null || controller == animator.runtimeAnimatorController))
                        {
                            controller = animator.runtimeAnimatorController;
                        }
                        FillModel();
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUI.BeginChangeCheck();
                    controller = (RuntimeAnimatorController)EditorGUILayout.ObjectField("Animator Controller:", controller, typeof(RuntimeAnimatorController), false);
                    if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                        FillModel();
                    if (EditorGUI.EndChangeCheck())
                        FillModel();
                }
            }

            GUILayout.Space(8);

            if (avatarObject == null)
            {
                DrawTitle("Please assign an Avatar GameObject");
                return;
            }

            if (controller == null)
            {
                DrawTitle("Please assign an Animator Controller (or set on the Avatar's Animator)");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            // Expand/Collapse All buttons using existing utility
            EditorListUtility.DrawExpandCollapseButtons(
                () => rendererFoldouts.SetAll(model.Keys.Select(k => k.gameObject).ToArray(), true),
                () => rendererFoldouts.SetAll(model.Keys.Select(k => k.gameObject).ToArray(), false)
            );

            GUILayout.Space(8);

            int rendererIndex = 0;
            var renderers = model.Keys.ToArray();
            for (int i = 0; i < renderers.Length; ++i)
            {
                var renderer = renderers[i];
                if (renderer == null) continue;

                // Ensure foldout state exists
                rendererFoldouts.EnsureExists(renderer.gameObject);

                EditorListUtility.DrawListItem(rendererIndex++, () =>
                {
                    rendererFoldouts[renderer.gameObject] = EditorListUtility.DrawCollapsibleHeader(rendererFoldouts[renderer.gameObject], $"{renderer.gameObject.name} ({model[renderer].Count})");

                    if (rendererFoldouts[renderer.gameObject])
                    {
                        using (EditorListUtility.CreateIndentScope())
                        {
                            DisplayBlendshapesForRenderer(renderer, model[renderer]);
                        }
                    }
                });

                EditorListUtility.DrawSectionSpacing(i, renderers.Length);
            }

            GUILayout.EndScrollView();
        }

        private void DisplayBlendshapesForRenderer(SkinnedMeshRenderer renderer, List<BlendshapeEntry> list)
        {
            for (int i = 0; i < list.Count; ++i)
            {
                var entry = list[i];
                string foldKey = renderer.gameObject.GetInstanceID() + ":" + entry.name;
                rendererFoldouts.EnsureExists(renderer.gameObject); // safe-ensure
                blendshapeFoldouts.EnsureExists(foldKey);

                using (new EditorGUILayout.HorizontalScope())
                {
                    // Foldout toggle rendered as header via utility
                    blendshapeFoldouts[foldKey] = EditorGUILayout.Foldout(blendshapeFoldouts[foldKey], entry.name, true);
                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"({entry.referencingClips.Count})", GUILayout.Width(60));
                }

                if (blendshapeFoldouts[foldKey])
                {
                    using (EditorListUtility.CreateIndentScope())
                    {
                        for (int j = 0; j < entry.referencingClips.Count; ++j)
                        {
                            var clip = entry.referencingClips[j];
                            EditorGUILayout.ObjectField(clip, typeof(AnimationClip), false);
                        }
                    }
                }

                if (i < list.Count - 1) GUILayout.Space(2);
            }
        }

        private void FillModel()
        {
            model.Clear();

            if (avatarObject == null || controller == null)
            {
                controllerClips = Array.Empty<AnimationClip>();
                return;
            }

            controllerClips = controller.animationClips.ToArray();

            // Get all skinned mesh renderers in avatar hierarchy
            var renderers = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            // Precompute clips' bindings for quick lookup
            var clipBindings = new Dictionary<AnimationClip, EditorCurveBinding[]>();
            foreach (var clip in controllerClips)
            {
                try
                {
                    var floatBindings = AnimationUtility.GetCurveBindings(clip);
                    clipBindings[clip] = floatBindings;
                }
                catch (Exception)
                {
                    clipBindings[clip] = Array.Empty<EditorCurveBinding>();
                }
            }

            foreach (var smr in renderers)
            {
                var mesh = smr.sharedMesh;
                if (mesh == null || mesh.blendShapeCount == 0) continue;

                var entries = new List<BlendshapeEntry>(mesh.blendShapeCount);

                // create entries with empty clip lists
                for (int si = 0; si < mesh.blendShapeCount; ++si)
                {
                    string bsName = mesh.GetBlendShapeName(si);
                    entries.Add(new BlendshapeEntry { name = bsName, referencingClips = new List<AnimationClip>() });
                }

                // compute path of this renderer relative to avatarObject (used in clips)
                string rendererPath = AnimationUtility.CalculateTransformPath(smr.transform, avatarObject.transform);

                // For each clip, inspect bindings for blendShape.* on this path
                foreach (var kv in clipBindings)
                {
                    var clip = kv.Key;
                    var bindings = kv.Value;
                    foreach (var b in bindings)
                    {
                        if (!string.Equals(b.path, rendererPath, StringComparison.Ordinal))
                            continue;

                        if (!b.propertyName.StartsWith("blendShape.", StringComparison.Ordinal))
                            continue;

                        // propertyName format: "blendShape.<name>"
                        var referencedName = b.propertyName.Length > 11 ? b.propertyName.Substring(11) : string.Empty;
                        if (string.IsNullOrEmpty(referencedName)) continue;

                        // find entry by name (exact match)
                        var entry = entries.FirstOrDefault(e => e.name == referencedName);
                        if (entry != null)
                        {
                            if (!entry.referencingClips.Contains(clip))
                                entry.referencingClips.Add(clip);
                        }
                        else
                        {
                            // if not found by name, try loose matching (fallback)
                            var fuzzy = entries.FirstOrDefault(e => referencedName.IndexOf(e.name, StringComparison.OrdinalIgnoreCase) >= 0 || e.name.IndexOf(referencedName, StringComparison.OrdinalIgnoreCase) >= 0);
                            if (fuzzy != null && !fuzzy.referencingClips.Contains(clip))
                                fuzzy.referencingClips.Add(clip);
                        }
                    }
                }

                // Optionally, remove blendshapes with zero references? Keep them as user requested a list of all blendshapes
                model[smr] = entries;
            }
        }

        private static void DrawTitle(string title)
        {
            using (new GUILayout.HorizontalScope("in bigtitle"))
                GUILayout.Label(title, EditorStyles.boldLabel);
        }

        // Small model class
        private class BlendshapeEntry
        {
            public string name;
            public List<AnimationClip> referencingClips = new();
        }
    }
}
