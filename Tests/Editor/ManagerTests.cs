using UnityEngine;
using UnityEditor;
using NUnit.Framework;

namespace Unity.Tutorials.Core.Editor.Tests
{
    // TODO badly named, give a more suitable name, SceneObjectGUIDTests or something like that
    public class AssetToSceneObjectReferenceManagerTests
    {
        private SceneObjectGUIDManager manager;

        [SetUp]
        public void InitManager()
        {
            manager = SceneObjectGUIDManager.Instance;
        }

        [Test]
        public void Manager_WithNoRegisteredComponents_WontReturnAnyComponents()
        {
            Assert.IsFalse(manager.Contains("non_existing_id"));
        }

        [Test]
        public void Manager_WithOneRegisteredComponent_ReturnTheComponent()
        {
            var c = CreateGameObjectWithReferenceComponent();

            Assert.IsTrue(manager.Contains(c.Id));
            Assert.IsNotNull(manager.GetComponent(c.Id));
        }

        [Test]
        public void Manager_WithManyComponentsRegistered_WillReturnTheCorrectOnes()
        {
            var c1 = CreateGameObjectWithReferenceComponent();
            var c2 = CreateGameObjectWithReferenceComponent();
            var c3 = CreateGameObjectWithReferenceComponent();

            Assert.IsTrue(manager.Contains(c2.Id));
            Assert.AreEqual(c2, manager.GetComponent(c2.Id));
            Assert.AreNotEqual(c1, manager.GetComponent(c2.Id));
            Assert.AreNotEqual(c3, manager.GetComponent(c2.Id));
        }

        [Test]
        public void Manager_WithManyComponentsRegistered_WillReturnNullForNotExisting()
        {
            CreateGameObjectWithReferenceComponent();
            CreateGameObjectWithReferenceComponent();
            CreateGameObjectWithReferenceComponent();

            Assert.IsNull(manager.GetComponent("Not_Existing_id"));
        }

        [Test]
        public void Manager_WithComponentAddedAndRemoved_WillReturnNull()
        {
            var c = CreateGameObjectWithReferenceComponent();
            var id = c.Id;
            Object.DestroyImmediate(c);

            Assert.IsNull(manager.GetComponent(id));
        }

        [Test]
        public void Manager_WithManyComponentsAddedAndOneRemoved_WillOnlyReturnTheExisting()
        {
            var c1 = CreateGameObjectWithReferenceComponent();
            var c2 = CreateGameObjectWithReferenceComponent();
            var c2Id = c2.Id;
            Object.DestroyImmediate(c2);
            var c3 = CreateGameObjectWithReferenceComponent();

            Assert.IsNotNull(manager.GetComponent(c1.Id));
            Assert.IsNotNull(manager.GetComponent(c3.Id));
            Assert.IsNull(manager.GetComponent(c2Id));
        }

        private static SceneObjectGUIDComponent CreateGameObjectWithReferenceComponent()
        {
            var go = new GameObject();
            Undo.RegisterCreatedObjectUndo(go, "Created test GO");
            return go.AddComponent<SceneObjectGUIDComponent>();
        }
    }
}
