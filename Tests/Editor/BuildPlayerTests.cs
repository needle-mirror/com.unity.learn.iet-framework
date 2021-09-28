using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Tutorials.Core.Editor.Tests
{
    public class BuildPlayerTests : TestBase
    {
        const string k_TempScenePath = "Assets/UntitledScene.unity";

        static string buildPath
        {
            get
            {
                // NOTE Use "Builds" subfolder in order to make this test pass locally when
                // using Windows & Visual Studio
                const string buildName = "Builds/BuildPlayerTests_Build";
                if (Application.platform == RuntimePlatform.OSXEditor)
                    return buildName + ".app";
                return buildName;
            }
        }

        [SetUp]
        public void SetUp()
        {
            // 2021.2.0 and newer will fail if we don't save the current scene
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), k_TempScenePath);
            Assert.That(File.Exists(buildPath), Is.False, "Existing file at path " + buildPath);
            Assert.That(Directory.Exists(buildPath), Is.False, "Existing directory at path " + buildPath);
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(buildPath))
                File.Delete(buildPath);

            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, true);

            AssetDatabase.DeleteAsset(k_TempScenePath);
        }

        static BuildTarget buildTarget
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.OSXEditor:
                        return BuildTarget.StandaloneOSX;

                    case RuntimePlatform.WindowsEditor:
                        return BuildTarget.StandaloneWindows;

                    case RuntimePlatform.LinuxEditor:
                        // NOTE Universal & 32-bit Linux support dropped after 2018 LTS
                        return BuildTarget.StandaloneLinux64;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        [Test]
        public void BuildPlayer_Succeeds()
        {
            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = new[] { GetTestAssetPath("EmptyTestScene.unity") },
                locationPathName = buildPath,
                targetGroup = BuildTargetGroup.Standalone,
                target = buildTarget,
                options = BuildOptions.StrictMode,
            };

            Assume.That(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, buildTarget));
            BuildPipeline.BuildPlayer(buildPlayerOptions);
        }
    }
}
