using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    internal static class AudioUtilProxy
    {
        internal static void PlayClip(AudioClip audioClip)
        {
#if UNITY_2020_2_OR_NEWER
            AudioUtil.PlayPreviewClip(audioClip);
#else
            AudioUtil.PlayClip(audioClip);
#endif
        }
    }
}
