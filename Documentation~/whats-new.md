<!-- What's new page template for packages: https://confluence.unity3d.com/display/DOCS/What%27s+new+page+template+for+packages -->

# What's new in version 2.1

Summary of changes in Tutorial Framework package version 2.1.

The main updates in this release include:

### Changed
- When traversing back to an already completed tutorial page, the masking settings of the page are reapplied if the page doesn't have completion criteria set.
This new behavior improves the tutorial authoring experience and allows a tutorial user to reobserve pages in "a tour of the UI" type of tutorials.
- When applying masking, do not not throw an exception if unmasked view's **Editor Window Type** or **View Type** is explicitly set to **None**, meaning, the masking is likely not fully configured yet.

### Added
- Added support for UI Toolkit masking and highlighting.
- Added multiple scene support for tutorials: the first element of **Scenes** list is considered to be the main scene and the rest of scenes are loaded additively.
- Added tutorial start-up scene management options: as a new option, it's possible to reuse the currently active scene(s).
- Rich text parser: Added `<style>` tag support, making it possible to set a text block to any style class.
- Added sanitization for tutorial assets' text content so that unprintable control characters, for example, a carriage return, are removed automatically.
- Scripting API: Made `TutorialModalWindow` part of the public API of the package. This class can be used to implement welcome/closing dialogs for the tutorial project.

### Fixed
- Fixed unresolved `SerializedType` values being shown incorrectly as **None**. These values are now shown as **Not Found** with a red background in the Inspector.
- Fixed `SerializedType` values that were resolved but the resolved type had a different assembly-qualified type name than the original name being shown incorrectly as **None**. These values are now shown with a yellow background in the Inspector.
- Fixed misplaced unmasking of UI controls in floating editor windows on Unity 2021.2.0.
- Fixed memory leak errors ("A Native Collection has not been disposed...") that occurred on Unity 2021.
- UI: Disabled horizontal scrollbars in all windows.
- Fixed original scenes not being restored correctly when exiting a tutorial which contained multiple scenes.
- Fixed original scenes not being restored correctly when exiting a tutorial, when its original state contained multiple scenes.

For a full list of changes and updates in this version, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@latest?subfolder=/changelog/CHANGELOG.html
