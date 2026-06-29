# What's New in Tutorial Framework 6.0

Tutorial Framework 6.0.0 (and the accompanying package, Tutorial Authoring Tools 2.0.0) introduces big improvements for the tutorial authoring experience, and almost no visible change for the tutorial users.

The main highlights of this release include:

- Added a Tutorials Overview window to see all the Tutorials present in the project at a glance. Tutorials can be reordered and reparented right within the window. The window also displays warnings, such as for tutorials that have been linked to two or more TutorialContainers. The window can be found under Tutorials > Authoring > Tutorials Overview.
- Custom icons for all asset types so they are easier to recognise in the Project window.
- Tutorial Pages are now sub-assets of Tutorials. This simplifies Page management, as the tutorial author only has to manage one asset.
- The Inspectors of many components (TutorialContainer, Tutorial, TutorialPage, WelcomePage...) have been reorganised to improve clarity and to make editing easier.
- New paragraph format for Tutorial Pages. This new format makes for a more flexible and faster editing experience.
- The Inspector of TutorialContainer now shows child containers. This makes it possible to navigate from a TutorialContainer to its children and back to the parent directly from the Inspector.
- It is now possible to save the Editor masking used by a page's paragraph as a preset, as a dedicated MaskingPreset ScriptableObject. This allows to reuse the masking in multiple pages.

For a full list of changes and updates, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@6.0?subfolder=/changelog/CHANGELOG.html