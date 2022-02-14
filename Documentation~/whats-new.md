<!-- What's new page template for packages: https://confluence.unity3d.com/display/DOCS/What%27s+new+page+template+for+packages -->

# What's new in version 2.2.0

Summary of changes in Tutorial Framework package version 2.2.0.

The main updates in this release include:

### Added
- Added `Package Installed` Criterion
- Added a possibility to specify which element(s) (the last, the first, all of them) is/are chosen for unmasking if multiple elements match the chosen selector.
- Added text wrapping for "narrative description" and "instruction description" fields of the Inspectors of tutorial pages

### Fixed
- Fixed masking and highlighting not refreshing when hierarchy or project window content changes
- Fixed "Cannot save invalid window <window> (Unity.Tutorials.Core.Editor.TutorialModalWindow) to layout." warning message appearing when a button of the welcome dialog was used to trigger a layout reload
- Fixed unmasking not working properly for the next 1st time when switching tutorials without reloading the Editor's layout

For a full list of changes and updates in this version, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@latest?subfolder=/changelog/CHANGELOG.html
