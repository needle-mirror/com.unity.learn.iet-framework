using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    internal static class EditorGUIUtilityProxy
    {
        public static Texture2D FindTexture(System.Type type) => EditorGUIUtility.FindTexture(type);
    }
}
