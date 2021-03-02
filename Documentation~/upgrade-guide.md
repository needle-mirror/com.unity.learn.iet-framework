# Upgrading to Tutorial Framework version 2.0.0

To upgrade to Tutorial Framework package version 2.0.0 from earlier versions, you need to do the following:

- [Check your use of renamed namespaces](#check-your-use-of-renamed-namespaces)
- [Check your use of renamed assemblies](#check-your-use-of-renamed-assemblies)
- [Make sure your API calls are converted from camelCase to PascalCase](#make-sure-your-api-calls-are-converted-from-camelcase-to-pascalcase)
- Recommended: [upgrade all tutorial assets in the project](#upgrade-all-tutorial-assets-in-the-project)

## Check your use of renamed namespaces
`Unity.InteractiveTutorials` namespace was renamed to `Unity.Tutorials.Core(.Editor)`.
Make sure your console shows no compilation errors from your code accessing the package APIs and if it does, adjust the code accordingly.

## Check your use of renamed assemblies
`Unity.InteractiveTutorials.Core` is now `Unity.Tutorials.Core.Editor` and `Unity.InteractiveTutorials.Core.Scripts` is now  `Unity.Tutorials.Core`.
Make sure your console shows no errors and if it does, adjust your assembly definitions accordingly.

## Make sure your API calls are converted from camelCase to PascalCase
Again, make sure your console shows no errors. If you see camelCase in your code accessing this package, change them to PascalCase and you should see the errors disappear.

## Upgrade all tutorial assets in the project
It's recommended to to reserialize and save all of your tutorial assets. To do this, select all of your tutorial assets, right-click and select **Set Dirty**.
Tip: search, for example, for "t:TutorialPage" in order to find all tutorial page assets. After this, save your project. Your reserialized and updated tutorial assets
can now be committed to your source control. Make sure you test there are no issues with your tutorial assets before proceeding.

For a full list of changes and updates in this version, see the [Changelog].

[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@2.0/changelog/CHANGELOG.html
