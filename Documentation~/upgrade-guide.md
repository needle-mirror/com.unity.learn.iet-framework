
# Upgrading to Tutorial Framework Version 6.x

Tutorial Framework 6.0.0 introduced a big change in how tutorial assets are serialised. As such, when importing any 6.x version of the package for the first time, existing tutorial assets (Tutorial Container, Tutorial Page, Tutorial, etc.) will be automatically migrated to the new version.

If you want to trigger a manual migration, go to **Tutorials > Authoring > Upgrade Tutorial Data to v6** (requires the [Tutorial Authoring Tools] package to be installed).

> [!WARNING]
> Before upgrading to this version, please upgrade to the closest major version to the one you're currently using. This ensures that existing tutorials are properly converted and updated without any data loss. For instance, if your project uses Tutorial Framework 4.x, you might want to upgrade to version 5.x before upgrading to 6.x.

[Tutorial Authoring Tools]: https://docs.unity3d.com/Packages/com.unity.learn.iet-framework.authoring@2.1