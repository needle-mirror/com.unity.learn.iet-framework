# Upgrading to Tutorial Framework version 2.0.0

To upgrade to Tutorial Framework package version 2.0.0 from earlier versions, you need to do the following:

- [Check your use of renamed namespaces](#check-your-use-of-renamed-namespaces)
- [Check your use of renamed assemblies](#check-your-use-of-renamed-assemblies)
- [Make sure your API calls are converted from camelCase to PascalCase](#make-sure-your-api-calls-are-converted-from-camelcase-to-pascalcase)
- [Use AddListener and RemoveListener functions to subscribe to tutorial assets' events](#use-addlistener-and-removelistener-functions-to-subscribe-to-tutorial-assets-events)
- [Check your use of deprecated events](#check-your-use-of-deprecated-events)
- Recommended: [upgrade all tutorial assets in the project](#upgrade-all-tutorial-assets-in-the-project)

## Check your use of renamed namespaces
`Unity.InteractiveTutorials` namespace was renamed to `Unity.Tutorials.Core(.Editor)`.
Make sure the Console shows no compilation errors from your code accessing the package APIs and if it does, adjust the code accordingly.

## Check your use of renamed assemblies
`Unity.InteractiveTutorials.Core` is now `Unity.Tutorials.Core.Editor` and `Unity.InteractiveTutorials.Core.Scripts` is now  `Unity.Tutorials.Core`.
Make sure the Console shows no errors and if it does, adjust your assembly definitions accordingly.

## Make sure your API calls are converted from camelCase to PascalCase
Again, make sure the Console shows no errors. If you see camelCase in your code when accessing this package, change it to PascalCase and you should see the errors disappear.

## Use AddListener and RemoveListener functions to subscribe to tutorial assets' events
Tutorial assets' events were changed to use the [UnityEvent] class instead of the standard C# `event` implementation. Again, make sure the Console shows no errors.
If you see code using the `+=` and `-=` operators to subscribe to tutorial assets' events, use `AddListener` and `RemoveListener` functions instead.

## Check your use of deprecated events
Some tutorial assets' events were deprecated. Most of these events should be migrated automatically but make sure the Console shows no warnings regarding deprecated events and if it does, adjust the your event handlers.

## Upgrade all tutorial assets in the project
It's recommended to to reserialize and save all of your tutorial assets. To do this, select all of your tutorial assets, then right-click and select **Set Dirty**.
Tip: search, for example, for "t:TutorialPage" in order to find all tutorial page assets. After this, save your project. Your reserialized and updated tutorial assets
can now be committed to your source control. Make sure you test there are no issues with your tutorial assets before proceeding.

For a full list of changes and updates in this version, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@2.0/changelog/CHANGELOG.html
[UnityEvent]: https://docs.unity3d.com/Manual/UnityEvents.html