# Changelog

All notable changes to this package are documented in this file.

## [1.0.0] - 2026-09-01

### Added
- Initial release, extracted from the *On Tape Rewind* project's in-project tool into a
  standalone, installable Unity package.
- Eight hierarchy color tags plus "None", applied via Unity's built-in per-object icon override.
- A semi-transparent full-row tint in the Hierarchy window showing each object's current tag.
- **GameObject → Color Tag** menu (also available from the Hierarchy's right-click menu) for
  applying a tag to the current selection.
- Shift+right-click a Hierarchy row to open a swatch picker at the cursor, with number-key
  shortcuts (1-8 to pick, 0 to clear, Esc to cancel) and a highlight on the row's current color.
