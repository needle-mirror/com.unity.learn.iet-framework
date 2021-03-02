using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Tutorials.Core.Editor.Tests
{
    public class TutorialManagerTests
    {
        class TestWindow1 : EditorWindow
        {
        }

        class TestWindow2 : EditorWindow
        {
        }

        string m_TempFolderPath;
        string m_TutorialLayoutPath;
        Tutorial m_Tutorial;
        string m_TutorialScenePath;

        [SetUp]
        public void SetUp()
        {
            if (EditorApplication.isPlaying)
                return;

            var tempFolderGUID = AssetDatabase.CreateFolder("Assets", "Temp");
            m_TempFolderPath = AssetDatabase.GUIDToAssetPath(tempFolderGUID);

            m_Tutorial = ScriptableObject.CreateInstance<Tutorial>();
            m_Tutorial.LessonId = "unittest"; // prevent warning spam regarding unset lesson ID
            AssetDatabase.CreateAsset(m_Tutorial, m_TempFolderPath + "/Tutorial.asset");
            var page = ScriptableObject.CreateInstance<TutorialPage>();
            AssetDatabase.CreateAsset(page, m_TempFolderPath + "/Page.asset");
            m_Tutorial.m_Pages = new Tutorial.TutorialPageCollection(new[] { page });
            AssetDatabase.Refresh();

            SetupLayout(m_Tutorial);
            SetupScene(m_Tutorial);
        }

        void SetupLayout(Tutorial tutorial)
        {
            // Ensure tutorial window is not open
            foreach (var window in Resources.FindObjectsOfTypeAll<TutorialWindow>())
            {
                window.Close();
            }

            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow1>(), Is.Empty, "TestWindow1 is present");

            // Save current layout and use it as the tutorial layout
            // TODO Cannot seem to save the layout when ran in Yamato. Let's use the default layout instead.
            //m_TutorialLayoutPath = m_TempFolderPath + "/TutorialLayout.dwlt";
            m_TutorialLayoutPath = TutorialContainer.k_DefaultLayoutPath;
            //WindowLayout.SaveWindowLayout(m_TutorialLayoutPath);
            var tutorialLayout = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(m_TutorialLayoutPath);
            tutorial.WindowLayout = tutorialLayout;
            TutorialManager.PrepareWindowLayout(tutorial.WindowLayoutPath);

            // Open TestWindow1
            EditorWindow.GetWindow<TestWindow1>().titleContent.text = "TestWindow1";

            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow1>(), Is.Not.Empty, "TestWindow1 is not present");
        }

        void SetupScene(Tutorial tutorial)
        {
            var tutorialScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            m_TutorialScenePath = m_TempFolderPath + "/TutorialScene.unity";
            EditorSceneManager.SaveScene(tutorialScene, m_TutorialScenePath);
            AssetDatabase.Refresh(); // TODO: Might not be needed!
            tutorial.m_Scene = Resources.Load<SceneAsset>(m_TutorialScenePath);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            // Ensure TestWindow1 is not open
            foreach (var window in Resources.FindObjectsOfTypeAll<TestWindow1>())
            {
                window.Close();
            }

            TutorialManager.instance.RestoreOriginalState();

            // TODO restoring of original scenes is now delayed so we need to wait for an extra bit
            // before we can delete Temp folder which has the temp scene.
            yield return new WaitForDelayCall();
            yield return new WaitForDelayCall();

            UnityEngine.Object.DestroyImmediate(TutorialManager.instance);

            // Deletion of TutorialManager will take care all of these:
            // Delete original layout to avoid triggering the layout restore again when TutorialWindow is closed
            // File.Delete(TutorialManager.k_OriginalLayoutPath);
            // Load tutorial layout reverting to layout before test run (with the exception of a potential closed TutorialWindow)
            // EditorUtility.LoadWindowLayout(m_TutorialLayoutPath);

            AssetDatabase.DeleteAsset(m_TempFolderPath);
        }

        [Ignore("TODO: disabled due to weird issues with layout loading")]
        [UnityTest]
        [TestCase(false, TestName = "StartTutorial_WhenTutorialWindowIsNotOpen_OriginalLayoutIsRestoredWhenTutorialIsCompleted", ExpectedResult = null)]
        [TestCase(true, TestName = "StartTutorial_WhenTutorialWindowIsOpen_OriginalLayoutIsRestoredWhenTutorialIsCompleted", ExpectedResult = null)]
        public IEnumerator StartTutorial_OriginalLayoutIsRestoredWhenTutorialIsCompleted(bool tutorialWindowOpen)
        {
            if (tutorialWindowOpen)
                EditorWindow.GetWindow<TutorialWindow>();

            TutorialManager.instance.StartTutorial(m_Tutorial);
            yield return new WaitForDelayCall();

            // Complete tutorial
            m_Tutorial.CurrentPage.ValidateCriteria();
            m_Tutorial.TryGoToNextPage();
            yield return new WaitForDelayCall();

            // Assert that original layout is restored (i.e. TestWindow1 should exist)
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow1>(), Is.Not.Empty, "TestWindow1 is not present");
        }

        [Ignore("TODO: disabled due to weird issues with layout loading")]
        [UnityTest]
        public IEnumerator RestartTutorial_RestoresTutorialLayout()
        {
            TutorialManager.instance.StartTutorial(m_Tutorial);

            // Open TestWindow2
            EditorWindow.GetWindow<TestWindow2>().titleContent.text = "TestWindow2";

            // Complete tutorial
            TutorialManager.instance.ResetTutorial();
            yield return new WaitForDelayCall();

            // Assert that tutorial layout is restored (i.e. TestWindow2 should not longer be present)
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow2>(), Is.Empty, "TestWindow2 is present");

            // Assert that original layout was not restored (i.e. TestWindow1 should not be present)
            Assert.That(Resources.FindObjectsOfTypeAll<TestWindow1>(), Is.Empty, "TestWindow1 is present");
        }

        [Ignore("TODO: disabled due to weird issues with layout loading")]
        [UnityTest]
        public IEnumerator StartTutorial_WhenInPlayMode_ExitsPlayMode()
        {
            yield return new EnterPlayMode();

            TutorialManager.instance.StartTutorial(m_Tutorial);

            yield return new WaitForDelayCall();

            Assert.That(EditorApplication.isPlaying, Is.False);
        }

        [Ignore("TODO: disabled due to weird issues with layout loading")]
        [UnityTest]
        public IEnumerator ResetTutorial_WhenInPlayMode_ExitsPlayMode()
        {
            TutorialManager.instance.StartTutorial(m_Tutorial);

            yield return new EnterPlayMode();

            TutorialManager.instance.ResetTutorial();

            yield return new WaitForDelayCall();

            Assert.That(EditorApplication.isPlaying, Is.False);
        }

        [Ignore("TODO: disabled due to weird issues with layout loading")]
        [UnityTest]
        public IEnumerator StartTutorial_OriginalSceneStateIsRestoredWhenTutorialIsCompleted()
        {
            // Open some new scenes
            var scene0Path = m_TempFolderPath + "/Scene0.unity";
            var scene0 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            EditorSceneManager.SaveScene(scene0, scene0Path);
            var scene1 = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            var scene1Path = m_TempFolderPath + "/Scene1.unity";
            EditorSceneManager.SaveScene(scene1, scene1Path);
            var scene2 = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            var scene2Path = m_TempFolderPath + "/Scene2.unity";
            EditorSceneManager.SaveScene(scene2, scene2Path);
            var scene3 = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            var scene3Path = m_TempFolderPath + "/Scene3.unity";
            EditorSceneManager.SaveScene(scene3, scene3Path);

            // Set the last scene to be active
            SceneManager.SetActiveScene(scene3);

            // Unload scene 2 and 3
            EditorSceneManager.CloseScene(scene1, false);
            EditorSceneManager.CloseScene(scene2, false);

            TutorialManager.instance.StartTutorial(m_Tutorial);

            // Complete tutorial
            m_Tutorial.CurrentPage.ValidateCriteria();
            m_Tutorial.TryGoToNextPage();
            yield return new WaitForDelayCall();
            // TODO restoring of original scenes is now delayed so we need to wait for an extra bit
            yield return new WaitForDelayCall();

            // Assert that we're back at original scene state
            Assert.That(SceneManager.sceneCount, Is.EqualTo(4));
            Assert.That(SceneManager.GetSceneAt(0).path, Is.EqualTo(scene0Path));
            Assert.That(SceneManager.GetSceneAt(1).path, Is.EqualTo(scene1Path));
            Assert.That(SceneManager.GetSceneAt(2).path, Is.EqualTo(scene2Path));
            Assert.That(SceneManager.GetSceneAt(3).path, Is.EqualTo(scene3Path));
            Assert.That(SceneManager.GetSceneAt(0).isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneAt(1).isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneAt(2).isLoaded, Is.False);
            Assert.That(SceneManager.GetSceneAt(3).isLoaded, Is.True);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(scene3Path));
        }
    }
}
