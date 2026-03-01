using UnityEditor;
using UnityEngine;

namespace Unity.Tutorials.Editor
{
    internal static class EditorGUIProxy
    {
        public static string ScrollableTextAreaInternal(Rect position, string text, ref Vector2 scrollPosition, GUIStyle style) =>
            EditorGUI.ScrollableTextAreaInternal(position, text, ref scrollPosition, style);

        public static float contextWidth => EditorGUIUtility.contextWidth;
    }
}
