# What's new in version 2.0

Summary of changes in Tutorial Framework package version 2.0.

The main updates in this release include:

## Added
- UI: Added **Show simplified type names** preference which affects the appearance of `SerializedType` fields.
This preference can be found under **Preferences** > **In-Editor Tutorials**.
- Rich text parser: Added word wrapping support for CJK characters.
- Rich text parser: Added support for `<wordwrap>` tag that can be used to force word wrapping even when Chinese, Japanese or Korean is detected.
- Rich text parser: leading whitespace can be used as indentation.
- Documentation: package documentation/manual added.
- Documentation: All public APIs documented.

### Changed
- Breaking change: all public APIs reviewed; many APIs made internal and some new public APIs added.
- Breaking change: all public APIs are now PascalCase instead a mix of camelCase and PascalCase.
- Breaking change: `Unity.InteractiveTutorials` namespace rename to `Unity.Tutorials.Core(.Editor)`.
- Breaking change: `Unity.InteractiveTutorials.Core` assembly renamed to to `Unity.Tutorials.Core.Editor`.
- Breaking change: `Unity.InteractiveTutorials.Core.Scripts` assembly renamed to to `Unity.Tutorials.Core`.
- Breaking change: `TutorialContainer`'s `ProjectName` renamed to `Title`, old `Title` renamed to `Subtitle`.
- Breaking change: Renamed `SceneObjectGUIDComponent` to `SceneObjectGuid` and `SceneObjectGUIDManager` to `SceneObjectGuidManager`.
- UX: Show a warning in the Console if the user is not signed in.
- UX: **Show Tutorials** menu item simply focuses **Tutorials** window in all cases, also when a tutorial is in progress.
- UX: If `TutorialContainer.ProjectLayout` has a layout without **Tutorials** window, the window is now shown as a free-floating window instead of not showing it at all.
- UI: `SerializedType` fields can now be edited using a searchable menu.

### Removed
- Breaking change: Removed `TriggerTaskCriterion`, `*CollisionBroadcaster*`, `IPlayerAvatar`, and `SelectionRoot` classes.
- Dependencies: Removed Physics and Physics2D dependencies from the package.

### Fixed
- Fixed null reference exception and **Tutorials** window being broken when updating the package.
- Fixed having **Auto Advance** option enabled on the last page of a tutorial making the first page of the tutorial to be skipped upon a rerun.
- Fixed **Scene(s) Have Been Modified** dialog being shown multiple times when **Cancel** or **Don't Save** was chosen.
- Fixed **Scene(s) Have Been Modified** dialog not being shown while having unsaved changes and quitting a tutorial.
- Fixed null reference exception when tutorial ended by auto-advancing while having unsaved changes.
- Fixed null reference exception when **Inspector** was docked as a child of another view and **Tutorials** window was shown using the auto-docking mechanism.
- Authoring: Fixed window layouts not being preprocessed until the project is restarted.
- Authoring: Fixed **Tutorials** > **Genesis** > **Clear all statuses** to clear the tutorial cards' completion markers correctly.
- Authoring: Fixed "HTTP/1.1 401 Unauthorized" warning spam in the Console when the tutorial author was not signed in.
- UI: Fixed tutorial cards' completion markers not showing the correct state when the project was just opened while having **Tutorials** window visible.
- UI: Fixed tutorial card not being marked as completed when a completed tutorial was quit by clicking the **Close** (**X**) button.

For a full list of changes and updates in this version, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@2.0/changelog/CHANGELOG.html
