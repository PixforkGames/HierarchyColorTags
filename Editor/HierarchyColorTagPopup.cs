using UnityEngine;
using UnityEngine.UIElements;

namespace PixforkGames.HierarchyColorTags.Editor {
    /// <summary>
    /// Small floating swatch grid opened by shift+right-clicking a Hierarchy row (see
    /// HierarchyColorTagView.OnPointerDown) — a faster path to the same tags as the
    /// "GameObject/Color Tag/..." menu, with the actual colors visible instead of names.
    /// Lives directly in the Hierarchy window's own UI Toolkit panel rather than as a separate
    /// EditorWindow, so it can be positioned exactly at the cursor with no screen-space/GUI
    /// coordinate conversion.
    /// </summary>
    internal static class HierarchyColorTagPopup {
        private const float SwatchSize = 22f;
        private const float SwatchSpacing = 4f;
        private const int Columns = 4;
        private static readonly Color CurrentTagHighlight = new Color(0.35f, 0.75f, 1f, 1f);

        private static VisualElement current;

        public static void Show(VisualElement panelRoot, Vector2 panelPosition, GameObject[] targets, int currentIndex) {
            Close();
            if (panelRoot == null || targets == null || targets.Length == 0) return;

            VisualElement root = new VisualElement {
                focusable = true,
                pickingMode = PickingMode.Position,
            };
            root.style.position = Position.Absolute;
            root.style.left = panelPosition.x;
            root.style.top = panelPosition.y;
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexWrap = Wrap.Wrap;
            root.style.width = Columns * (SwatchSize + SwatchSpacing) + SwatchSpacing;
            root.style.paddingLeft = root.style.paddingRight = root.style.paddingTop = root.style.paddingBottom = SwatchSpacing;
            root.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 0.98f);
            root.style.borderTopLeftRadius = root.style.borderTopRightRadius =
                root.style.borderBottomLeftRadius = root.style.borderBottomRightRadius = 4;
            SetBorder(root, new Color(0f, 0f, 0f, 0.6f), 1f);

            root.Add(CreateSwatch(-1, null, currentIndex, targets));
            for (int i = 0; i < HierarchyColorTag.LabelColors.Length; i++) {
                root.Add(CreateSwatch(i, HierarchyColorTag.LabelColors[i], currentIndex, targets));
            }

            root.RegisterCallback<KeyDownEvent>(evt => OnKeyDown(evt, targets));
            root.RegisterCallback<FocusOutEvent>(_ => root.schedule.Execute(CloseIfStillCurrent).ExecuteLater(0));

            // Trickle-down on the panel root: runs before the click reaches whatever it actually
            // landed on, so a click outside the popup closes it instead of also acting on the row
            // underneath.
            panelRoot.RegisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);

            panelRoot.Add(root);
            current = root;

            // Deferred one tick: the Hierarchy's own row-click handling (still processing the same
            // physical mouse-down that opened this popup) re-asserts focus onto the clicked row
            // immediately after this callback returns, which would instantly blur `root` again and
            // trigger the FocusOutEvent close below. Grabbing focus next frame instead lets that
            // settle first.
            root.schedule.Execute(() => {
                if (current == root) root.Focus();
            }).ExecuteLater(0);

            // Clamp on layout so the popup never opens partially off the Hierarchy panel — sizes
            // aren't known until after the first layout pass.
            root.RegisterCallback<GeometryChangedEvent>(_ => ClampToParent(root, panelRoot));
        }

        private static void OnPanelPointerDown(PointerDownEvent evt) {
            if (current == null) return;
            if (evt.target is VisualElement ve && (ve == current || current.Contains(ve))) return;
            Close();
        }

        private static void OnKeyDown(KeyDownEvent evt, GameObject[] targets) {
            int index = evt.keyCode switch {
                KeyCode.Alpha0 or KeyCode.Keypad0 => -1,
                KeyCode.Alpha1 or KeyCode.Keypad1 => 0,
                KeyCode.Alpha2 or KeyCode.Keypad2 => 1,
                KeyCode.Alpha3 or KeyCode.Keypad3 => 2,
                KeyCode.Alpha4 or KeyCode.Keypad4 => 3,
                KeyCode.Alpha5 or KeyCode.Keypad5 => 4,
                KeyCode.Alpha6 or KeyCode.Keypad6 => 5,
                KeyCode.Alpha7 or KeyCode.Keypad7 => 6,
                KeyCode.Alpha8 or KeyCode.Keypad8 => 7,
                _ => int.MinValue,
            };

            if (index != int.MinValue) {
                Apply(index, targets);
                evt.StopPropagation();
                return;
            }

            if (evt.keyCode == KeyCode.Escape) {
                Close();
                evt.StopPropagation();
            }
        }

        private static VisualElement CreateSwatch(int index, Color? color, int currentIndex, GameObject[] targets) {
            VisualElement swatch = new VisualElement { tooltip = index < 0 ? "None" : HierarchyColorTag.LabelNames[index] };
            swatch.style.width = SwatchSize;
            swatch.style.height = SwatchSize;
            swatch.style.marginRight = swatch.style.marginBottom = SwatchSpacing;
            swatch.style.borderTopLeftRadius = swatch.style.borderTopRightRadius =
                swatch.style.borderBottomLeftRadius = swatch.style.borderBottomRightRadius = 3;
            swatch.style.backgroundColor = color ?? new Color(0f, 0f, 0f, 0f);
            if (color == null) {
                // "None" swatch: a diagonal slash through an otherwise empty square, drawn as a
                // thin rotated line so it doesn't need its own texture.
                VisualElement slash = new VisualElement { pickingMode = PickingMode.Ignore };
                slash.style.position = Position.Absolute;
                slash.style.left = 2;
                slash.style.right = 2;
                slash.style.top = SwatchSize / 2f - 1;
                slash.style.height = 2;
                slash.style.backgroundColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                slash.style.rotate = new Rotate(new Angle(-45));
                swatch.Add(slash);
            }

            bool isCurrent = index == currentIndex;
            SetBorder(swatch, isCurrent ? CurrentTagHighlight : (color == null ? new Color(0.7f, 0.7f, 0.7f, 1f) : new Color(0f, 0f, 0f, 0.4f)), isCurrent ? 2f : 1f);

            swatch.RegisterCallback<ClickEvent>(_ => Apply(index, targets));
            return swatch;
        }

        private static void Apply(int index, GameObject[] targets) {
            HierarchyColorTag.Apply(index, targets);
            HierarchyColorTagView.RefreshAll();
            Close();
        }

        private static void ClampToParent(VisualElement root, VisualElement panelRoot) {
            if (root.parent == null) return;
            Rect parentRect = panelRoot.layout;
            float maxLeft = Mathf.Max(0, parentRect.width - root.layout.width);
            float maxTop = Mathf.Max(0, parentRect.height - root.layout.height);
            root.style.left = Mathf.Clamp(root.layout.x, 0, maxLeft);
            root.style.top = Mathf.Clamp(root.layout.y, 0, maxTop);
        }

        private static void SetBorder(VisualElement element, Color color, float width) {
            element.style.borderLeftColor = element.style.borderRightColor =
                element.style.borderTopColor = element.style.borderBottomColor = color;
            element.style.borderLeftWidth = element.style.borderRightWidth =
                element.style.borderTopWidth = element.style.borderBottomWidth = width;
        }

        private static void CloseIfStillCurrent() {
            // FocusOutEvent fires when focus moves to a swatch's own click handling too; only
            // close if focus has actually left the whole popup.
            if (current != null && current.panel != null && current.focusController.focusedElement is VisualElement focused
                && (focused == current || current.Contains(focused))) {
                return;
            }
            Close();
        }

        private static void Close() {
            if (current == null) return;
            VisualElement panelRoot = current.panel?.visualTree;
            if (panelRoot != null) panelRoot.UnregisterCallback<PointerDownEvent>(OnPanelPointerDown, TrickleDown.TrickleDown);
            current.RemoveFromHierarchy();
            current = null;
        }
    }
}
