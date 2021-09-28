<!-- Upgrade guide page template for packages: https://confluence.unity3d.com/display/DOCS/Upgrade+guide+page+template+for+packages -->

# Upgrading to Tutorial Framework version 2.1.0

If you are upgrading from a version earlier than 2.0.0, please see the [Tutorial Framework 2.0.0 upgrade guide] first.

To upgrade to Tutorial Framework package version 2.1.0 from earlier versions, you need to do the following:

- Recommended: [upgrade Tutorial assets in the project](#upgrade-tutorial-assets-in-the-project)

## Upgrade Tutorial assets in the project
It's recommended to to reserialize and save all of your `Tutorial` assets. To do this, search for "t:Tutorial" in Project window, select all of the found assets, 
then right-click and select **Set Dirty**. Finally, save your project. Your reserialized and updated tutorial assets can now be committed to your source control.

For a full list of changes and updates in this version, see the [Changelog].

[Tutorial Framework 2.0.0 upgrade guide]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@2.0/manual/upgrade-guide.html
[Changelog]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework@latest?subfolder=/changelog/CHANGELOG.html
