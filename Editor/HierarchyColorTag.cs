using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PixforkGames.HierarchyColorTags.Editor {
    /// <summary>
    /// Adds "GameObject/Color Tag/..." menu entries — Unity merges the GameObject menu into the
    /// Hierarchy window's right-click context menu, so this shows up there directly. Tints the
    /// selected GameObjects' Hierarchy icon using Unity's built-in "sv_label_0".."sv_label_7"
    /// textures, the same 8 colors as the classic Icon picker in the Inspector header — reusing
    /// that existing mechanism means the tag is just the normal per-object icon override and
    /// renders in the Hierarchy exactly like it always has, old or new hierarchy window alike.
    /// </summary>
    internal static class HierarchyColorTag {
        private const string MenuRoot = "GameObject/Color Tag/";
        private const int BasePriority = 49;

        /// <summary>The actual RGB of Unity's built-in sv_label_0..sv_label_7 textures (sampled
        /// via GetPixel through a temporary readable copy, since the source textures themselves
        /// aren't CPU-readable) — used by HierarchyColorTagView to paint a swatch in the Hierarchy
        /// row, since the new Hierarchy window doesn't render SetIconForObject overrides itself.</summary>
        public static readonly Color[] LabelColors = {
            new Color(0.565f, 0.565f, 0.565f), // sv_label_0 — gray
            new Color(0.290f, 0.502f, 0.831f), // sv_label_1 — blue
            new Color(0.278f, 0.737f, 0.647f), // sv_label_2 — teal
            new Color(0.188f, 0.737f, 0.184f), // sv_label_3 — green
            new Color(0.918f, 0.808f, 0.169f), // sv_label_4 — yellow
            new Color(0.918f, 0.561f, 0.169f), // sv_label_5 — orange
            new Color(0.808f, 0.184f, 0.184f), // sv_label_6 — red
            new Color(0.745f, 0.310f, 0.678f), // sv_label_7 — purple
        };

        /// <summary>Display names for LabelColors, in the same order — used by the menu items and
        /// by HierarchyColorTagPopup.</summary>
        public static readonly string[] LabelNames = {
            "Gray", "Blue", "Teal", "Green", "Yellow", "Orange", "Red", "Purple",
        };

        /// <summary>Resolves a GameObject's icon override (as set by this tool) back to its label
        /// index, or -1 if it isn't one of ours. Matches by icon name rather than reference so it
        /// survives domain reloads.</summary>
        public static int GetLabelIndex(GameObject go) {
            Texture2D icon = go != null ? EditorGUIUtility.GetIconForObject(go) : null;
            if (icon != null && icon.name != null && icon.name.StartsWith("sv_label_")
                && int.TryParse(icon.name.Substring("sv_label_".Length), out int index)
                && index >= 0 && index < LabelColors.Length) {
                return index;
            }
            return -1;
        }

        /// <summary>Resolves a per-object icon override (as set by this tool) back to its label
        /// color, if it is one of ours.</summary>
        public static bool TryGetColor(Texture2D icon, out Color color) {
            if (icon != null && icon.name != null && icon.name.StartsWith("sv_label_")
                && int.TryParse(icon.name.Substring("sv_label_".Length), out int index)
                && index >= 0 && index < LabelColors.Length) {
                color = LabelColors[index];
                return true;
            }
            color = default;
            return false;
        }

        [MenuItem(MenuRoot + "None", false, BasePriority)]
        private static void ClearColor() => Apply(-1, Selection.gameObjects);

        [MenuItem(MenuRoot + "Gray", false, BasePriority + 1)]
        private static void Color1() => Apply(0, Selection.gameObjects);

        [MenuItem(MenuRoot + "Blue", false, BasePriority + 2)]
        private static void Color2() => Apply(1, Selection.gameObjects);

        [MenuItem(MenuRoot + "Teal", false, BasePriority + 3)]
        private static void Color3() => Apply(2, Selection.gameObjects);

        [MenuItem(MenuRoot + "Green", false, BasePriority + 4)]
        private static void Color4() => Apply(3, Selection.gameObjects);

        [MenuItem(MenuRoot + "Yellow", false, BasePriority + 5)]
        private static void Color5() => Apply(4, Selection.gameObjects);

        [MenuItem(MenuRoot + "Orange", false, BasePriority + 6)]
        private static void Color6() => Apply(5, Selection.gameObjects);

        [MenuItem(MenuRoot + "Red", false, BasePriority + 7)]
        private static void Color7() => Apply(6, Selection.gameObjects);

        [MenuItem(MenuRoot + "Purple", false, BasePriority + 8)]
        private static void Color8() => Apply(7, Selection.gameObjects);

        /// <summary>Sets (or clears, for labelIndex -1) the color tag on every target. Shared by
        /// the GameObject menu (targets = Selection.gameObjects) and HierarchyColorTagPopup
        /// (targets = the clicked row's object, or the selection if the row is part of it).</summary>
        internal static void Apply(int labelIndex, GameObject[] targets) {
            if (targets == null || targets.Length == 0) return;

            Texture2D icon = labelIndex >= 0
                ? EditorGUIUtility.IconContent("sv_label_" + labelIndex).image as Texture2D
                : null;

            foreach (GameObject go in targets) {
                EditorGUIUtility.SetIconForObject(go, icon);
                EditorUtility.SetDirty(go);
                EditorSceneManager.MarkSceneDirty(go.scene);
            }
        }
    }
}
