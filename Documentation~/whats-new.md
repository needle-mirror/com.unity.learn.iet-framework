# What's new in version 2.0

Summary of changes in Tutorial Framework package version 2.0.

The main updates in this release include:

## Added
- Added support for multiple `TutorialContainer` assets within a project, allowing multiple set of tutorials ("tutorial projects") for the user to choose from.
- Added support for tutorial categories by making it possible for `TutorialContainer` assets to refer to each other.
- Checkbox to enable progress tracking in tutorials. Enabling progress tracking generates a GUID for the tutorial's **Lesson Id** automatically.
- Added **Use default editors for editing tutorial assets** preference, disabled by default.
- Added `CommonTutorialCallbacks` assets from the Tutorial Authoring Tools package.
- UI: Added **Select Container** button to the authoring toolbar. This button selects the currently effective tutorial container in the project.
- UI: Added **Tutorials** > **Welcome Dialog** menu item for accessing the possible welcome dialog of the project conveniently at all times.
- UI: Added **Show simplified type names** preference which affects the appearance of `SerializedType` fields.
This preference can be found under **Preferences** > **In-Editor Tutorials**.
- Rich text parser: Added word wrapping support for CJK characters.
- Rich text parser: Added support for `<wordwrap>` tag that can be used to force word wrapping even when Chinese, Japanese or Korean is detected.
- Rich text parser: Leading whitespace can be used as indentation.
- Documentation: Package documentation/manual added.
- Documentation: All public APIs documented.

### Changed
- **Order In View** values of tutorial container sections are no longer reassigned automatically, allowing users to mix sections and tutorial categories easily.
- UI: Renamed **All Tutorials** button to **Back to overview**.
- Removed tutorial page editor's **Force default Inspector** option, superseded by the **Use default editors for editing tutorial assets** preference.
- All tutorial assets can be edited without having Tutorial Authoring Tools present.
- `TutorialPage`: Deprecated `OnBeforePageShown`, `OnAfterPageShown`, `OnTutorialPageStay`, and `OnBeforeTutorialQuit` events.
These events are superseded by the new `Showing`, `Shown`, `Staying` and `Tutorial.Quit` (added in 2.0.0-pre.5) events.
- Tutorial logic: Tutorials are not shown as completed until the criteria of its last page are completed.
- UI: Tutorial cards do not have completion markers unless progress tracking is enabled.
- UI: Improved tutorial page's **Custom Callbacks** authoring view by making it more compact.
- UI: Cleaned up and restructured the **Tutorials** menu, authoring-related items can be now found under the **Tutorials** > **Authoring** submenu.
- UI: Changed authoring toolbar's buttons to use icons instead of text labels.
- Scripting API: Made `TutorialManager` part of the public API of the package.
- **Breaking change**: All public APIs reviewed; many APIs made internal and some new public APIs added.
- **Breaking change**: All public APIs are now PascalCase instead a mix of camelCase and PascalCase.
- **Breaking change**: `Unity.InteractiveTutorials` namespace rename to `Unity.Tutorials.Core(.Editor)`.
- **Breaking change**: `Unity.InteractiveTutorials.Core` assembly renamed to to `Unity.Tutorials.Core.Editor`.
- **Breaking change**: `Unity.InteractiveTutorials.Core.Scripts` assembly renamed to to `Unity.Tutorials.Core`.
- **Breaking change**: `TutorialContainer`'s `ProjectName` renamed to `Title`, old `Title` renamed to `Subtitle`.
- **Breaking change**: Renamed `SceneObjectGUIDComponent` to `SceneObjectGuid` and `SceneObjectGUIDManager` to `SceneObjectGuidManager`.
- **Breaking change**: Changed tutorial assets' events to use the `UnityEvent` class instead of the standard C# `event` implementation. 
- UX: Show a warning in the Console if the user is not signed in.
- UX: **Show Tutorials** menu item simply focuses **Tutorials** window in all cases, also when a tutorial is in progress.
- UX: If `TutorialContainer.ProjectLayout` has a layout without **Tutorials** window, the window is now shown as a free-floating window instead of not showing it at all.
- UI: `SerializedType` fields can now be edited using a searchable menu on Unity 2020 and newer.
- Moved UI image files from `Editor/Resources` to `Editor/UI/Images`.

### Removed
- Omitted tests from the package.
- **Breaking change**: Removed `Tutorial.TutorialPagesModified` event and `RaiseTutorialPagesModified` function, superseded by the `Modified` event.
- **Breaking change**: Removed `Tutorial.SkipTutorialBehavior`.
- **Breaking change**: Removed `TutorialProjectSettings.StartupTutorial`. This functionality can be now implemented by using `TutorialManager.StartTutorial()` if wanted.
- **Breaking change**: Removed `TriggerTaskCriterion`, `*CollisionBroadcaster*`, `IPlayerAvatar`, and `SelectionRoot` classes.
- Dependencies: Removed Physics and Physics2D dependencies from the package.

### Fixed
- Fixed null reference exception and **Tutorials** window being broken when updating the package.
- Fixed having **Auto Advance** option enabled on the last page of a tutorial making the first page of the tutorial to be skipped upon a rerun.
- Fixed **Scene(s) Have Been Modified** dialog being shown multiple times when **Cancel** or **Don't Save** was chosen.
- Fixed **Scene(s) Have Been Modified** dialog not being shown while having unsaved changes and quitting a tutorial.
- Fixed null reference exception when tutorial ended by auto-advancing while having unsaved changes.
- Fixed null reference exception when **Inspector** was docked as a child of another view and **Tutorials** window was shown using the auto-docking mechanism.
- Fixed "Editing of Tutorial Pages no longer works on pages that have a Criterion" (case [1332176](https://fogbugz.unity3d.com/f/cases/1332176/))
- Fixed `OnBeforePageShown` and `OnAfterPageShown` events not being raised for the first page of a tutorial when starting the tutorial.
- Authoring: Fixed window layouts not being preprocessed until the project is restarted.
- Authoring: Fixed **Tutorials** > **Genesis** > **Clear all statuses** to clear the tutorial cards' completion markers correctly.
- Authoring: Fixed "HTTP/1.1 401 Unauthorized" warning spam in the Console when the tutorial author was not signed in.
- Authoring: Fixed `TutorialCallbacks.asset` not being guaranteed to be created in the same folder as `TutorialCallbacks.cs` when using the **Create Callback Handler** button.
- UI: Fixed tutorial cards' completion markers not showing the correct state when the project was just opened while having **Tutorials** window visible.
- UI: Fixed tutorial card not being marked as completed when a completed tutorial was quit by clicking the **Close** (**X**) button.
- UI: Fixed the **Next** button's state (enabled/disabled) to match the completion criteria of a tutorial page in cases where the criteria are invalidated after the initial completion.
- UX: Fixed unnecessary window layout restoring when when quitting a tutorial which did not have a window layout set.
- UI: Fixed unwanted horizontal scroll bar appearing on tutorial pages on Unity 2021 by disabling the horizontal scroll bar altogether.

For a full list of changes and updates in this version, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@2.0/changelog/CHANGELOG.html
