using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using UnityEngine.Windows;

namespace Unity.Tutorials.Core.Editor.Tests
{
    class BuildStartedCriterionTests : CriterionTestBase<BuildStartedCriterion>
    {
        [UnityTest]
        public IEnumerator CustomHandlerIsInvoked_IsCompleted()
        {
#if UNITY_EDITOR_WIN
            var target = BuildTarget.StandaloneWindows;
            var locationPathName = "Test/Test.exe";
#elif UNITY_EDITOR_OSX
            var target = BuildTarget.StandaloneOSX;
            var locationPathName = "Test/Test";
#elif UNITY_EDITOR_LINUX
            var target = BuildTarget.StandaloneLinux64;
            var locationPathName = "Test/Test";
#else
#error Unsupported platform
#endif

            m_Criterion.BuildPlayerCustomHandler(new BuildPlayerOptions
            {
                scenes = null,
                target = target,
                locationPathName = locationPathName,
                targetGroup = BuildTargetGroup.Unknown
            });
            yield return null;

            Assert.IsTrue(m_Criterion.IsCompleted);

            // Cleanup
            if (Directory.Exists("Test"))
            {
                Directory.Delete("Test");
            }
        }

        [UnityTest]
        public IEnumerator AutoComplete_IsCompleted()
        {
            yield return null;
            Assert.IsTrue(m_Criterion.AutoComplete());
        }
    }
}
