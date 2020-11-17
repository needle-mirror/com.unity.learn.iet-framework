using UnityEditor;

namespace Unity.Tutorials.Core.Editor
{
    internal static class WindowLayoutProxy
    {
        public static void SaveWindowLayout(string path)
        {
            WindowLayout.SaveWindowLayout(path);
        }
    }
}
