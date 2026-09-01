using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PixforkGames.HierarchyColorTags.Editor {
    /// <summary>
    /// Paints the color set by HierarchyColorTag directly into Unity 6's new (UI Toolkit-based)
    /// Hierarchy window. That window builds its own rows and does not consult
    /// EditorGUIUtility.GetIconForObject the way the old IMGUI hierarchy did, so the tag would
    /// otherwise only be visible in the Inspector header / Scene View gizmo icon and never in the
    /// Hierarchy itself — this hooks the new window's row bind/unbind events (both static, cover
    /// every open Hierarchy tab) to add a small color swatch per row instead.
    /// Also hooks shift+right-click on a row to open HierarchyColorTagPopup instead of the normal
    /// context menu, as a faster path to the same tags.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyColorTagView {
        private const string SwatchName = "hierarchy-color-tag-swatch";
        private const float SwatchAlpha = 0.35f;

        // Rows are pooled and reused by the Hierarchy window, so bind/unbind is the only reliable
        // place to know which HierarchyViewItems are currently live — used by RefreshAll to retint
        // every visible row immediately after the popup changes a tag, rather than waiting for the
        // next rebind.
        private static readonly HashSet<HierarchyViewItem> LiveItems = new HashSet<HierarchyViewItem>();

        static HierarchyColorTagView() {
            HierarchyWindow.BindViewItem += OnBindViewItem;
            HierarchyWindow.UnbindViewItem += OnUnbindViewItem;
        }

        private static void OnBindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item) {
            LiveItems.Add(item);
            ApplyTint(item);
            item.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        private static void OnUnbindViewItem(HierarchyWindow window, HierarchyView view, HierarchyViewItem item) {
            LiveItems.Remove(item);
            item.UnregisterCallback<PointerDownEvent>(OnPointerDown);
            item.Q<VisualElement>(SwatchName)?.RemoveFromHierarchy();
        }

        /// <summary>Retints every currently bound row. Called by HierarchyColorTagPopup after it
        /// changes a tag, so the affected row(s) update immediately instead of on the next
        /// scroll/rebind.</summary>
        internal static void RefreshAll() {
            foreach (HierarchyViewItem item in LiveItems) ApplyTint(item);
        }

        private static void ApplyTint(HierarchyViewItem item) {
            VisualElement swatch = GetOrCreateSwatch(item);

            GameObject go = GetGameObject(item);
            Texture2D icon = go != null ? EditorGUIUtility.GetIconForObject(go) : null;

            if (go != null && HierarchyColorTag.TryGetColor(icon, out Color color)) {
                color.a = SwatchAlpha;
                swatch.style.backgroundColor = color;
                swatch.style.display = DisplayStyle.Flex;
            } else {
                swatch.style.display = DisplayStyle.None;
            }
        }

        private static void OnPointerDown(PointerDownEvent evt) {
            // Right button + shift only — everything else (plain right-click, left-click
            // selection, etc.) must fall through to the Hierarchy's normal handling untouched.
            if (evt.button != 1 || !evt.shiftKey) return;
            if (evt.currentTarget is not HierarchyViewItem item) return;

            GameObject go = GetGameObject(item);
            if (go == null) return;

            GameObject[] selection = Selection.gameObjects;
            GameObject[] targets = System.Array.IndexOf(selection, go) >= 0 ? selection : new[] { go };

            HierarchyColorTagPopup.Show(item.panel.visualTree, evt.position, targets, HierarchyColorTag.GetLabelIndex(go));

            // Registered on the item itself, which is a descendant of the HierarchyView that owns
            // the ContextualMenuManipulator driving the normal right-click menu — stopping the
            // event here (it bubbles up from us to that manipulator) suppresses that menu instead
            // of it opening on top of our popup.
            evt.StopPropagation();
            evt.StopImmediatePropagation();
        }

        // Inserted as the item's first child, i.e. behind the "hierarchy-item__container" child
        // that holds the icon and label — UI Toolkit draws children in order, so this renders
        // as a full-row tint sitting behind the text rather than a swatch next to it.
        private static VisualElement GetOrCreateSwatch(HierarchyViewItem item) {
            VisualElement swatch = item.Q<VisualElement>(SwatchName);
            if (swatch == null) {
                swatch = new VisualElement { name = SwatchName, pickingMode = PickingMode.Ignore };
                swatch.style.position = Position.Absolute;
                swatch.style.left = 0;
                swatch.style.right = 0;
                swatch.style.top = 0;
                swatch.style.bottom = 0;
                item.Insert(0, swatch);
            }
            return swatch;
        }

        private static GameObject GetGameObject(HierarchyViewItem item) {
            if (item.Handler is not HierarchyGameObjectHandler handler) return null;
            HierarchyNode node = item.Node;
            return handler.GetGameObject(node);
        }
    }
}
