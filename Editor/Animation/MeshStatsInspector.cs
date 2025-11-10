using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using FlammAlpha.UnityTools.Common;

namespace FlammAlpha.UnityTools.Animation
{
    /// <summary>
    /// Editor window that displays stats about all MeshRenderer and SkinnedMeshRenderer components
    /// in the selected avatar GameObject hierarchy. Shows triangle count and material count per GameObject.
    /// </summary>
    public class MeshStatsInspector : EditorWindow
    {
        [MenuItem("Tools/FlammAlpha/Mesh Stats Inspector")]
        private static void ShowWindow() => GetWindow<MeshStatsInspector>("Mesh Stats Inspector");

        private GameObject avatarObject;
        private Vector2 scrollPos;
        private List<MeshStatsEntry> meshStats = new();
        private EditorListUtility.FoldoutManager<GameObject> foldouts = new();

        private void OnSelectionChange()
        {
            if (Selection.activeGameObject != null)
            {
                avatarObject = Selection.activeGameObject;
                FillStats();
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Space(6);
            using (new GUILayout.VerticalScope("helpbox"))
            {
                EditorGUI.BeginChangeCheck();
                avatarObject = (GameObject)EditorGUILayout.ObjectField("Avatar GameObject:", avatarObject, typeof(GameObject), true);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                    Selection.activeGameObject = avatarObject;
                if (EditorGUI.EndChangeCheck())
                {
                    FillStats();
                }
            }

            GUILayout.Space(8);

            if (avatarObject == null)
            {
                DrawTitle("Please assign an Avatar GameObject");
                return;
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos);

            EditorListUtility.DrawExpandCollapseButtons(
                () => foldouts.SetAll(meshStats.Select(e => e.gameObject).ToArray(), true),
                () => foldouts.SetAll(meshStats.Select(e => e.gameObject).ToArray(), false)
            );

            GUILayout.Space(8);

            // Sort by triangle count descending
            var sortedStats = meshStats.OrderByDescending(e => e.triangleCount).ToList();
            int index = 0;
            for (int i = 0; i < sortedStats.Count; ++i)
            {
                var entry = sortedStats[i];
                if (entry.gameObject == null) continue;
                foldouts.EnsureExists(entry.gameObject);
                EditorListUtility.DrawListItem(index++, () =>
                {
                    foldouts[entry.gameObject] = EditorListUtility.DrawCollapsibleHeader(foldouts[entry.gameObject], $"{entry.gameObject.name} (Tris: {entry.triangleCount}, Mats: {entry.materials?.Length ?? 0})");
                    if (foldouts[entry.gameObject])
                    {
                        using (EditorListUtility.CreateIndentScope())
                        {
                            EditorGUILayout.LabelField("Path:", GetTransformPath(entry.gameObject.transform, avatarObject.transform));
                            EditorGUILayout.LabelField("Triangles:", entry.triangleCount.ToString());
                            EditorGUILayout.LabelField("Materials:", (entry.materials?.Length ?? 0).ToString());
                            if (entry.materials != null)
                            {
                                for (int m = 0; m < entry.materials.Length; ++m)
                                {
                                    using (new EditorGUILayout.HorizontalScope())
                                    {
                                        EditorGUILayout.LabelField($"Material {m}", GUILayout.Width(80));
                                        EditorGUI.BeginDisabledGroup(true);
                                        EditorGUILayout.ObjectField(entry.materials[m], typeof(Material), false);
                                        EditorGUI.EndDisabledGroup();
                                    }
                                }
                            }
                        }
                    }
                });
                EditorListUtility.DrawSectionSpacing(i, sortedStats.Count);
            }

            GUILayout.EndScrollView();
        }

        private void FillStats()
        {
            meshStats.Clear();
            if (avatarObject == null) return;
            // SkinnedMeshRenderers
            var skinned = avatarObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinned)
            {
                var mesh = smr.sharedMesh;
                int tris = mesh != null ? mesh.triangles.Length / 3 : 0;
                meshStats.Add(new MeshStatsEntry
                {
                    gameObject = smr.gameObject,
                    triangleCount = tris,
                    materials = smr.sharedMaterials
                });
            }
            // MeshRenderers
            var meshRenderers = avatarObject.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                var mf = mr.GetComponent<MeshFilter>();
                var mesh = mf != null ? mf.sharedMesh : null;
                int tris = mesh != null ? mesh.triangles.Length / 3 : 0;
                meshStats.Add(new MeshStatsEntry
                {
                    gameObject = mr.gameObject,
                    triangleCount = tris,
                    materials = mr.sharedMaterials
                });
            }
        }

        private static void DrawTitle(string title)
        {
            using (new GUILayout.HorizontalScope("in bigtitle"))
                GUILayout.Label(title, EditorStyles.boldLabel);
        }

        private static string GetTransformPath(Transform target, Transform root)
        {
            if (target == root) return target.name;
            var path = target.name;
            var current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }
            return path;
        }

        private class MeshStatsEntry
        {
            public GameObject gameObject;
            public int triangleCount;
            public Material[] materials;
        }
    }
}
