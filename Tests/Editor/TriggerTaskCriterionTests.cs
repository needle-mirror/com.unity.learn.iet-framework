using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Unity.Tutorials.Core;

using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Core.Editor.Tests
{
    public class TriggerTaskCriterionTests : CriterionTestBase<TriggerTaskCriterion>, IPrebuildSetup
    {
        const string k_TestSceneName = "TriggerTaskCriterionTestScene.unity";

        public void Setup()
        {
            // Add scene to editor build settings
            var testScenePath = GetTestAssetPath(k_TestSceneName).Replace("/", Path.DirectorySeparatorChar.ToString());
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(testScenePath, true) };
        }

        [SetUp]
        public void LoadScene()
        {
            var testScenePath = GetTestAssetPath(k_TestSceneName);
            SceneManager.LoadScene(testScenePath, LoadSceneMode.Additive);
        }

        [Ignore("This test needs to be in the editor assembly and run in play mode which is currently not supported")]
        [UnityTest]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionEnter, "Player2D", "Collider2D", true,  true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionEnter, "Player3D", "Collider3D", true,  true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionExit,  "Player2D", "Collider2D", false, true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionExit,  "Player3D", "Collider3D", false, true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerEnter,   "Player2D", "Trigger2D",  true,  true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerEnter,   "Player3D", "Trigger3D",  true,  true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerExit,    "Player2D", "Trigger2D",  false, true, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerExit,    "Player3D", "Trigger3D",  false, true, ExpectedResult = null)]
        public IEnumerator IsCompletedWhenExpected(TriggerTaskCriterion.TriggerTaskTestMode testMode, string playerName,
            string targetName, bool expectedCompletionWhenIntersecting, bool expectedCompletionWhenNotIntersectingAnymore)
        {
            var player = GameObject.Find(playerName);
            var target = GameObject.Find(targetName);
            Assert.IsNotNull(player);
            Assert.IsNotNull(target);

            player.AddComponent<PlayerAvatarTestComponent>();

            m_Criterion.objectReference.SceneObjectReference.Update(target);
            m_Criterion.TestMode = testMode;
            Assert.IsFalse(m_Criterion.Completed);

            player.transform.position = target.transform.position;
            yield return null;
            yield return null;
            Assert.AreEqual(expectedCompletionWhenIntersecting, m_Criterion.Completed);

            player.transform.position = Vector3.zero;
            yield return null;
            yield return null;
            Assert.AreEqual(expectedCompletionWhenNotIntersectingAnymore, m_Criterion.Completed);
        }

        [Ignore("This test needs to be in the editor assembly and run in play mode which is currently not supported")]
        [UnityTest]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionEnter, "Player2D", "Collider2D", true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionEnter, "Player2D", "Collider2D", false, true,  ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionEnter, "Player3D", "Collider3D", true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionEnter, "Player3D", "Collider3D", false, true,  ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionExit,  "Player2D", "Collider2D", true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionExit,  "Player2D", "Collider2D", false, false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionExit,  "Player3D", "Collider3D", true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.CollisionExit,  "Player3D", "Collider3D", false, false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerEnter,   "Player2D", "Trigger2D",  true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerEnter,   "Player2D", "Trigger2D",  false, true,  ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerEnter,   "Player3D", "Trigger3D",  true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerEnter,   "Player3D", "Trigger3D",  false, true,  ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerExit,    "Player2D", "Trigger2D",  true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerExit,    "Player2D", "Trigger2D",  false, false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerExit,    "Player3D", "Trigger3D",  true,  false, ExpectedResult = null)]
        [TestCase(TriggerTaskCriterion.TriggerTaskTestMode.TriggerExit,    "Player3D", "Trigger3D",  false, false, ExpectedResult = null)]
        public IEnumerator AutoComplete_ReturnsExpectedValueAndIsCompleteWhenExpected(TriggerTaskCriterion.TriggerTaskTestMode testMode,
            string playerName, string targetName, bool playerStartsInsideTarget, bool expectedReturnValueAndCompletion)
        {
            var player = GameObject.Find(playerName);
            var target = GameObject.Find(targetName);
            player.AddComponent<PlayerAvatarTestComponent>();

            if (playerStartsInsideTarget)
                player.transform.position = target.transform.position;

            m_Criterion.objectReference.SceneObjectReference.Update(target);
            m_Criterion.TestMode = testMode;

            Assert.IsFalse(m_Criterion.Completed);

            Assert.AreEqual(expectedReturnValueAndCompletion, m_Criterion.AutoComplete());
            yield return null;

            Assert.AreEqual(expectedReturnValueAndCompletion, m_Criterion.Completed);
        }
    }

    public class PlayerAvatarTestComponent : MonoBehaviour, IPlayerAvatar
    {
    }
}
