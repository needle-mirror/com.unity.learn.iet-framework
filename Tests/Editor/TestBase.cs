using System.IO;

namespace Unity.Tutorials.Core.Editor.Tests
{
    public class TestBase
    {
        protected static string GetTestAssetPath(string relativeAssetPath)
        {
            return Path.Combine("Packages/com.unity.learn.iet-framework/Tests/Editor", relativeAssetPath);
        }
    }
}
