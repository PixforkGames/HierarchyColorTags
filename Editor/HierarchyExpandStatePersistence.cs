using System;
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
    /// Uses HierarchyView.GetState/SetState — Unity's own purpose-built API for this exact case
    /// (HierarchyViewState.Content even has dedicated EnterPlayMode/ExitPlayMode presets) — rather
    /// than walking the hierarchy and recording expanded objects ourselves. GetState returns the
    /// already-serialized per-node state as a single byte[] (HierarchyViewState.ViewModelState);
    /// save/restore is therefore two API calls per open Hierarchy window per transition, not a
    /// tree walk. The byte[] is stashed in SessionState (survives the domain reload Play Mode
    /// entry/exit normally triggers) as base64, keyed by window index — good enough for the
    /// overwhelmingly common single-Hierarchy-window case.
    /// </summary>
    [InitializeOnLoad]
    internal static class HierarchyExpandStatePersistence {
        private const string SessionKeyPrefix = "PixforkGames.HierarchyColorTags.ExpandState.";

        static HierarchyExpandStatePersistence() {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change) {
            switch (change) {
                case PlayModeStateChange.ExitingEditMode:
                    SaveState(HierarchyViewState.Content.EnterPlayMode);
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    RestoreState(HierarchyViewState.Content.EnterPlayMode);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    SaveState(HierarchyViewState.Content.ExitPlayMode);
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    RestoreState(HierarchyViewState.Content.ExitPlayMode);
                    break;
            }
        }

        private static void SaveState(HierarchyViewState.Content content) {
            HierarchyWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
            for (int i = 0; i < windows.Length; i++) {
                HierarchyView view = windows[i].View;
                if (view == null) continue;

                byte[] bytes = view.GetState(content).ViewModelState;
                string key = SessionKeyPrefix + i;
                if (bytes == null || bytes.Length == 0) {
                    SessionState.EraseString(key);
                } else {
                    SessionState.SetString(key, Convert.ToBase64String(bytes));
                }
            }
        }

        // Called after the scene swap has already happened (and already collapsed everything) —
        // re-applies the pre-transition per-node flags onto the now-rebuilt tree.
        private static void RestoreState(HierarchyViewState.Content content) {
            HierarchyWindow[] windows = Resources.FindObjectsOfTypeAll<HierarchyWindow>();
            for (int i = 0; i < windows.Length; i++) {
                string key = SessionKeyPrefix + i;
                string base64 = SessionState.GetString(key, string.Empty);
                if (string.IsNullOrEmpty(base64)) continue;
                SessionState.EraseString(key);

                HierarchyView view = windows[i].View;
                if (view == null) continue;

                // GetState here just gives us a correctly-shaped state object (ValidContent,
                // scroll position, etc.) for the current tree — only ViewModelState gets replaced
                // with the saved bytes before writing it back.
                HierarchyViewState state = view.GetState(content);
                state.ViewModelState = Convert.FromBase64String(base64);
                view.SetState(state);
            }
        }
    }
}
