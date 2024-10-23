using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Experimental.Video;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace Unity.Tutorials.Core.Editor
{
    internal class VideoPlaybackManager
    {
        public struct CacheKey: IEquatable<CacheKey>
        {
            public string Url;
            public VideoClip Clip;

            public CacheKey(TutorialParagraph paragraph)
            {
                Url = string.IsNullOrEmpty(paragraph.VideoUrl) ? null : paragraph.VideoUrl;
                Clip = paragraph.Video;
            }

            public CacheKey(VideoClip clip)
            {
                Url = null;
                Clip = clip;
            }

            public CacheKey(string url)
            {
                Url = url;
                Clip = null;
            }

            public bool Equals(CacheKey other)
            {
                if (other.Url != null && Url != null)
                    return other.Url == Url;

                return other.Clip == Clip;
            }

            public override bool Equals(object obj)
            {
                return obj is CacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Url?.GetHashCode(), Clip?.GetHashCode());
            }
        }

        struct CacheEntry
        {
            public Texture2D Texture2D;
            public VideoPlayer VideoPlayer;
            public Action<string> OnErrorCallback;
            public bool SceneAudioWasMuted;
        }

        // NOTE Static reference fixes a peculiar NRE issue when a tutorial which has Window Layout set
        // is exited by Tutorials > Show Tutorials instead of exiting the tutorial regularly.
        static GameObject m_GameObject;

        // This is to fix a bug in the videoplayer in edit mode in Unity : audio isn't initialized properly until you
        // enter play mode. Audio Source seems to properly initialize the audio subsystem, so we play a dummy clip
        // which force the init of the audio system when starting playing a video
        private static AudioSource s_BugFixAudioSource;
        private static AudioClip s_BugFixClip;

        Dictionary<CacheKey, CacheEntry> m_Cache = new Dictionary<CacheKey, CacheEntry>();

        public void OnEnable()
        {
            if (!m_GameObject)
            {
                m_GameObject = new GameObject() { hideFlags = HideFlags.HideAndDontSave };
                EditorApplication.playModeStateChanged += PlayModeStateChange;
                EditorSceneManager.sceneOpened += SceneLoaded;

                s_BugFixClip = AudioClip.Create("testClip", 44000, 1, 44000, false);
                var sourceGo = new GameObject() { hideFlags = HideFlags.HideAndDontSave };
                s_BugFixAudioSource = sourceGo.AddComponent<AudioSource>();
                s_BugFixAudioSource.clip = s_BugFixClip;
            }
        }

        public void OnDisable()
        {
            ClearCache();
            Object.DestroyImmediate(s_BugFixClip);
            if(s_BugFixAudioSource != null) Object.DestroyImmediate(s_BugFixAudioSource.gameObject);
            Object.DestroyImmediate(m_GameObject);

            s_BugFixClip = null;
            s_BugFixAudioSource = null;
            m_GameObject = null;
            EditorApplication.playModeStateChanged -= PlayModeStateChange;
            EditorSceneManager.sceneOpened -= SceneLoaded;
        }

        void PlayModeStateChange(PlayModeStateChange stateChange)
        {
            //exiting playmode and entering edit mode will destroy the Texture2D, so we need to clear the cache so that
            //the dangling reference get cleared and a new one will be created by the GetTextureForVideoClip call
            if (stateChange == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                ClearCache();
            }
        }

        // Opening a scene destroy the objects like changing the play state, so we need to clear the cache so it get
        // recreated here too
        void SceneLoaded(Scene scene, OpenSceneMode loadMode)
        {
            ClearCache();
        }

        public bool IsPrepared(CacheKey cacheKey)
        {
            CacheEntry cacheEntry;
            //url player can fail to prepare
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry))
            {
                return cacheEntry.VideoPlayer.isPrepared;
            }

            return false;
        }

        // onError will be invoked if the player encounter an error playing
        public Texture2D GetTextureForVideoClip(CacheKey cacheKey, Action<string> onError = null)
        {
            CacheEntry cacheEntry;
            if (!m_Cache.TryGetValue(cacheKey, out cacheEntry))
            {
                var videoPlayer = m_GameObject.AddComponent<VideoPlayer>();

                if (cacheKey.Url != null)
                    videoPlayer.url = cacheKey.Url;
                else if (cacheKey.Clip)
                    videoPlayer.clip = cacheKey.Clip;

                videoPlayer.playOnAwake = false;
                videoPlayer.isLooping = false;
                videoPlayer.renderMode = VideoRenderMode.RenderTexture;
                videoPlayer.skipOnDrop = false;
                videoPlayer.SetDirectAudioVolume(0, 1.0f);
                videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
                videoPlayer.Pause();

                cacheEntry.VideoPlayer = videoPlayer;
                m_Cache.Add(cacheKey, cacheEntry);

                // We used Local Function so we can unregister the callback once its called.
                // In the case of a video clip, this will be called right away, but in the case of a url video, this may
                // take some delay.
                void PreparedFunc(VideoPlayer source)
                {
                    source.targetTexture = new RenderTexture((int)source.width, (int)source.height, 32);
                    source.prepareCompleted -= PreparedFunc;

                    var localCacheEntry = m_Cache[cacheKey];
                    localCacheEntry.Texture2D = new Texture2D((int)cacheEntry.VideoPlayer.width, (int)cacheEntry.VideoPlayer.height, TextureFormat.RGBA32, false);
                    m_Cache[cacheKey] = localCacheEntry;
                }

                videoPlayer.prepareCompleted += PreparedFunc;
                videoPlayer.Prepare();

                videoPlayer.errorReceived += (evt, msg) =>
                {
                    // video player will survive the video player UI Element, so to ensure we use the latest error
                    // function we read it from the cache as this will be updated but this callback is only created
                    // the first time the player is instantiated.
                    m_Cache[cacheKey].OnErrorCallback?.Invoke(msg);
                    //this video player is now broken, so we remove it from the cache
                    m_Cache.Remove(cacheKey);
                };
            }

            //In the case of a url clip, the video player need to prepare before creating the textures, so we could have
            //a null cached texture if this is called before the player is prepared
            if (cacheEntry.Texture2D != null)
            {
                TextureToTexture2D(cacheEntry.VideoPlayer.targetTexture, ref cacheEntry.Texture2D);
            }

            cacheEntry.OnErrorCallback = onError;
            m_Cache[cacheKey] = cacheEntry;

            return cacheEntry.Texture2D;
        }

        public void Play(CacheKey cacheKey)
        {
            CacheEntry cacheEntry;
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry) && cacheEntry.VideoPlayer.isPrepared)
            {
                s_BugFixAudioSource.Play();
                cacheEntry.VideoPlayer.Play();
                
                //A button in GameView can mute audio and direct audio respect that. So we need to save if it was disable
                //so we can disable it again when the video is stopped/destroyed
                cacheEntry.SceneAudioWasMuted = EditorUtility.audioMasterMute;

                //reassigned it as cacheEntry is a struct so we got a copy when getting it
                m_Cache[cacheKey] = cacheEntry;

                EditorUtility.audioMasterMute = false;
            }
        }

        public void Pause(CacheKey cacheKey)
        {
            CacheEntry cacheEntry;
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry) && cacheEntry.VideoPlayer.isPrepared)
            {
                cacheEntry.VideoPlayer.Pause();

                if(cacheEntry.SceneAudioWasMuted)
                    EditorUtility.audioMasterMute = true;
            }
        }

        public void SetVolume(CacheKey cacheKey, float newVolume)
        {
            CacheEntry cacheEntry;
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry) && cacheEntry.VideoPlayer.isPrepared)
            {
                cacheEntry.VideoPlayer.SetDirectAudioVolume(0, newVolume);
            }
        }

        public bool IsPlaying(CacheKey cacheKey)
        {
            CacheEntry cacheEntry;
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry))
            {
                return cacheEntry.VideoPlayer.isPlaying;
            }

            return false;
        }

        public float GetPlayPercent(CacheKey cacheKey)
        {
            CacheEntry cacheEntry;
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry))
            {
                var player = cacheEntry.VideoPlayer;
                return (float)(player.time/player.length);
            }

            return 0.0f;
        }

        public void SetPlayPercent(CacheKey cacheKey, float percent)
        {
            CacheEntry cacheEntry;
            if (m_Cache.TryGetValue(cacheKey, out cacheEntry))
            {
                var player = cacheEntry.VideoPlayer;
                player.time = percent * player.length;
            }
        }

        static void TextureToTexture2D(Texture texture, ref Texture2D texture2D)
        {
            RenderTexture currentRT = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 32);
            Graphics.Blit(texture, renderTexture);

            RenderTexture.active = renderTexture;
            texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture2D.Apply();

            RenderTexture.active = currentRT;
            RenderTexture.ReleaseTemporary(renderTexture);
        }

        public void ClearCache()
        {
            foreach (var cacheEntry in m_Cache.Values)
            {
                if(cacheEntry.SceneAudioWasMuted)
                    EditorUtility.audioMasterMute = true;

                Object.DestroyImmediate(cacheEntry.Texture2D);
                Object.DestroyImmediate(cacheEntry.VideoPlayer);
            }

            m_Cache.Clear();
        }
    }
}
