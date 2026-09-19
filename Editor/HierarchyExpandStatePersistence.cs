using System.Collections.Generic;
using Unity.Hierarchy;
using Unity.Hierarchy.Editor;
using UnityEditor;
using UnityEngine;

namespace PixforkGames.HierarchyColorTags.Editor {
    /// <summary>
    /// Preserves each open Hierarchy window's expanded/collapsed node state across Play Mode
    /// transitions. Unity 6's Hierarchy window rebuilds against a different scene representation
    /// when entering/exiting Play Mode, which collapses every expanded group — this is stock Unity
    /// behavior (confirmed by disabling every other hook in this package and reproducing it), not
    /// anything this package's color tags cause.
    ///
    /// Deliberately does NOT walk the hierarchy: HierarchyViewModel.GetNodesWithFlags(Expanded) is
    /// an indexed query Unity already maintains, returning only the currently-expanded nodes
    /// (typically a handful), so both save and restore cost is proportional to the number of
    /// expanded nodes, not the size of the tree. (An earlier version tried HierarchyView.GetState/
    /// SetState — a byte[] blob Unity exposes with EnterPlayMode/ExitPlayMode presets seemingly
    /// built for exactly this — but empirically that blob does not round-trip expanded state, even
    /// with zero domain reload in between; verified live before abandoning it.)
    ///
    /// Node identity (HierarchyNode/instance ID) doesn't survive the scene swap, so each expanded
    /// node is remembered by GameObject hierarchy path (e.g. "Parent/Child") and re-resolved via
    /// GameObject.Find on restore — stable across the swap since the scene structure itself doesn't
    /// change, only the instances.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyExpandStatePersistence {
        private const string SessionKeyPrefix = "PixforkGames.HierarchyColorTags.ExpandState.";

        // The Hierarchy window rebuilds its tree for the new scene asynchronously
        // (HierarchyViewModel.UpdateIncrementalTimed spreads it across several editor ticks to
        // avoid a frame spike on large hierarchies) — restoring once, synchronously, right when
        // EnteredPlayMode/EnteredEditMode fires works for an instant but then gets silently
        // stomped once that rebuild catches up and finishes. Re-applying for a short bounded
        // window of ticks afterward is what actually sticks; verified live (a single immediate
        // restore reads back as IsExpanded==true right after calling Expand, but is false again
        // within a second).
        private const int RestoreRetryTicks = 60;
        private static int remainingRestoreTicks;
        // Parsed once in BeginRestore (SessionState is read/erased a single time there) and
        // reapplied every tick from this cache — re-reading SessionState per tick would be
        // pointless since the first read already erases it.
        private static string[][] pathsPerWindow;

        static HierarchyExpandStatePersistence() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change) {
            switch (change) {
                case PlayModeStateChange.ExitingEditMode:
                case PlayModeStateChange.ExitingPlayMode:
                    SaveExpandedPaths();
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.EnteredEditMode:
                    BeginRestore();
                    break;
            }
        }

        private static void BeginRestore() {
            HierarchyWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
            pathsPerWindow = new string[windows.Length][];
            bool anyPaths = false;
            for (int i = 0; i < windows.Length; i++) {
                string key = SessionKeyPrefix + i;
                string joined = SessionState.GetString(key, string.Empty);
                SessionState.EraseString(key);
                if (string.IsNullOrEmpty(joined)) continue;

                pathsPerWindow[i] = joined.Split('\n');
                anyPaths = true;
            }

            if (!anyPaths) return;

            remainingRestoreTicks = RestoreRetryTicks;
            EditorApplication.update -= RestoreTick;
            EditorApplication.update += RestoreTick;
        }

        private static void RestoreTick() {
            remainingRestoreTicks--;
            if (remainingRestoreTicks <= 0) EditorApplication.update -= RestoreTick;
            RestoreExpandedPaths();
        }

        private static void SaveExpandedPaths() {
            HierarchyWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
            for (int i = 0; i < windows.Length; i++) {
                HierarchyView view = windows[i].View;
                if (view == null) continue;

                HierarchyGameObjectHandler handler =
                    view.ViewModel.Hierarchy.GetOrCreateNodeTypeHandler<HierarchyGameObjectHandler>();
                HierarchyNode[] expanded = view.ViewModel.GetNodesWithFlags(HierarchyNodeFlags.Expanded);

                List<string> paths = new List<string>(expanded.Length);
                for (int n = 0; n < expanded.Length; n++) {
                    // Non-GameObject nodes (e.g. the scene's own root node, always expanded so its
                    // children show at all) return null here and are skipped — nothing to restore.
                    GameObject go = handler.GetGameObject(expanded[n]);
                    if (go != null) paths.Add(GetPath(go));
                }

                string key = SessionKeyPrefix + i;
                if (paths.Count == 0) {
                    SessionState.EraseString(key);
                } else {
                    SessionState.SetString(key, string.Join("\n", paths));
                }
                Debug.Log($"[HierarchyExpandStatePersistence] SaveExpandedPaths window {i}: {paths.Count} path(s): {string.Join(", ", paths)}");
            }
        }

        // Re-expands each remembered path on the current tree — called every tick for a short
        // window after the transition (see RestoreRetryTicks) since the Hierarchy window's own
        // rebuild-for-the-new-scene is still in progress for a few ticks after
        // EnteredPlayMode/EnteredEditMode fires and will otherwise silently undo a one-shot call.
        private static void RestoreExpandedPaths() {
            HierarchyWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
            for (int i = 0; i < windows.Length && i < pathsPerWindow.Length; i++) {
                string[] paths = pathsPerWindow[i];
                if (paths == null) continue;

                HierarchyView view = windows[i].View;
                if (view == null) continue;

                HierarchyGameObjectHandler handler =
                    view.ViewModel.Hierarchy.GetOrCreateNodeTypeHandler<HierarchyGameObjectHandler>();

                for (int p = 0; p < paths.Length; p++) {
                    GameObject go = GameObject.Find(paths[p]);
                    if (go == null) continue; // renamed/removed/inactive across the transition

                    HierarchyNode node = handler.GetOrCreateNode(go);
                    view.Expand(node);
                }
            }
        }

        private static string GetPath(GameObject go) {
            Transform t = go.transform;
            string path = t.name;
            while (t.parent != null) {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
