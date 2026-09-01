# Hierarchy Color Tags

Color-code GameObjects in the Hierarchy window so you can spot groups of related objects at a
glance — no components, no runtime cost. Tags are stored using Unity's own per-object icon
override, the same mechanism behind the classic Inspector icon picker, so they're purely an
editor convenience that never ships in a build.

## Requirements

- Unity 6000.0 or newer, with the new UI Toolkit-based Hierarchy window (default in current
  Unity 6 releases; enable it via **Preferences > Hierarchy** if you're on an early Unity 6
  version where it's still opt-in).

## Installation

Via the Unity Package Manager:

1. **Window → Package Manager → + → Add package from git URL…**
2. Enter the URL of this repository, optionally with a `#<tag>` suffix to pin a version.

Or, for local development, add it as a local path dependency in the consuming project's
`Packages/manifest.json`:

```json
"com.pixforkgames.hierarchy-colors": "file:../../HierarchyColorTags"
```

## Usage

https://github.com/user-attachments/assets/e140faf3-cf36-4fde-9214-f7a48571f3c3


- **Shift+right-click** any row in the Hierarchy to open a swatch picker at the cursor. Click a
  color to apply it, or press **1–8** to pick one, **0** to clear, **Esc** to cancel.
- If the clicked row is part of your current selection, the color applies to the whole
  selection; otherwise it applies to just that object.
- The swatch grid highlights the row's current color, if any.
- Alternatively, right-click a row (or use the **GameObject** menu) → **Color Tag** → pick a
  named color.

## License

See [LICENSE.md](LICENSE.md).
