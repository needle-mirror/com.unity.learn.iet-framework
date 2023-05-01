using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Core.Editor.Tests
{
    public class VideoPlaybackManagerTests : TestBase
    {
        VideoPlaybackManager m_VideoPlaybackManager;
        VideoClip m_VideoClip1;
        VideoClip m_VideoClip2;

        [SetUp]
        public void SetUp()
        {
            // NOTE If opening a new scene, temporary render textures won't get disposed properly.
            //EditorSceneManager.OpenScene(GetTestAssetPath("EmptyTestScene.unity"));

            m_VideoPlaybackManager = new VideoPlaybackManager();
            m_VideoClip1 = AssetDatabase.LoadAssetAtPath<VideoClip>(GetTestAssetPath("TestVideoClip1.webm"));
            m_VideoClip2 = AssetDatabase.LoadAssetAtPath<VideoClip>(GetTestAssetPath("TestVideoClip2.webm"));
        }

        [TearDown]
        public void TearDown()
        {
            m_VideoPlaybackManager.OnDisable();
        }

        [Test]
        public void GetTextureForVideoClip_BeforeOnEnableIsCalled_ThrowsNullReferenceException()
        {
            Assert.That(() => m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip1)), Throws.Exception);
        }

        [Ignore("TODO : this hang on some OS & version of Unity. Look like the player is NEVER prepared during testing?? Work perfectly fine in tutorials though, video play properly")]
        [UnityTest]
        public IEnumerator GetTextureForVideoClip_AfterOnEnableIsCalled_ReturnsValidTexture()
        {
            m_VideoPlaybackManager.OnEnable();

            var key = new VideoPlaybackManager.CacheKey(m_VideoClip1);
            m_VideoPlaybackManager.GetTextureForVideoClip(key);

            //we have to wait for the player to prepare. In the case something broke the preparation, we also add a
            //timeout of 5 second which will exit if it get stuck preparing
            var startTime = DateTime.Now;
            while (!m_VideoPlaybackManager.IsPrepared(key) && (DateTime.Now - startTime).Seconds < 5.0f)
            {
                yield return null;
            }

            var texture = m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip1));

            Assert.That(texture != null, Is.True);
        }

        [Test]
        public void GetTextureForVideoClip_AfterOnDisableIsCalled_ThrowsNullReferenceException()
        {
            m_VideoPlaybackManager.OnEnable();
            m_VideoPlaybackManager.OnDisable();

            Assert.That(() => m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip1)), Throws.Exception);
        }

        [UnityTest]
        public IEnumerator GetTextureForVideoClip_ReturnsSameTextureForSameVideoClip()
        {
            m_VideoPlaybackManager.OnEnable();

            var key = new VideoPlaybackManager.CacheKey(m_VideoClip1);
            m_VideoPlaybackManager.GetTextureForVideoClip(key);

            //we have to wait for the player to prepare. In the case something broke the preparation, we also add a
            //timeout of 5 second which will exit if it get stuck preparing
            var startTime = DateTime.Now;
            while (!m_VideoPlaybackManager.IsPrepared(key) && (DateTime.Now - startTime).Seconds < 5.0f)
            {
                yield return null;
            }

            var texture1 = m_VideoPlaybackManager.GetTextureForVideoClip(key);
            var texture2 = m_VideoPlaybackManager.GetTextureForVideoClip(key);

            Assert.That(texture1 == texture2, Is.True);
        }

        [Ignore("TODO : redo that, seems like it never prepare the player on some platform and Unity version. May be linked to editor coroutine problems.")]
        [UnityTest]
        public IEnumerator GetTextureForVideoClip_ReturnsDifferentTextureForDifferentVideoClip()
        {
            m_VideoPlaybackManager.OnEnable();

            var key1 = new VideoPlaybackManager.CacheKey(m_VideoClip1);
            var key2 = new VideoPlaybackManager.CacheKey(m_VideoClip2);

            m_VideoPlaybackManager.GetTextureForVideoClip(key1);
            m_VideoPlaybackManager.GetTextureForVideoClip(key2);

            var startTime = DateTime.Now;
            while (!m_VideoPlaybackManager.IsPrepared(key1) && !m_VideoPlaybackManager.IsPrepared(key2) && (DateTime.Now - startTime).Seconds < 5.0f)
            {
                yield return null;
            }


            var texture1 = m_VideoPlaybackManager.GetTextureForVideoClip(key1);
            var texture2 = m_VideoPlaybackManager.GetTextureForVideoClip(key2);

            Assert.That(texture1 == texture2, Is.False);
        }

        [Test]
        public void GetTextureForVideoClip_OnDisable_DestroysAllObjectsCreatedByManager()
        {
            var objectsBefore = Resources.FindObjectsOfTypeAll<Object>();
            m_VideoPlaybackManager.OnEnable();
            m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip1));
            m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip2));
            var newObjects = Resources.FindObjectsOfTypeAll<Object>().Except(objectsBefore);

            m_VideoPlaybackManager.OnDisable();

            foreach (var newObject in newObjects)
            {
                Assert.That(newObject == null, Is.True, "Object not destroyed: " + newObject);
            }
        }

        [Test]
        public void GetTextureForVideoClip_ClearCache_DestroysAllObjectsCreatedByCallsToGetTextureForVideoClip()
        {
            m_VideoPlaybackManager.OnEnable();
            var objectsBefore = Resources.FindObjectsOfTypeAll<Object>();
            m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip1));
            m_VideoPlaybackManager.GetTextureForVideoClip(new VideoPlaybackManager.CacheKey(m_VideoClip2));
            var newObjects = Resources.FindObjectsOfTypeAll<Object>().Except(objectsBefore);

            m_VideoPlaybackManager.ClearCache();

            foreach (var newObject in newObjects)
            {
                Assert.That(newObject == null, Is.True, "Object not destroyed: " + newObject);
            }
        }
    }
}
