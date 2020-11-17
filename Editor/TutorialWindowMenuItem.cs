using UnityEditor;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Contains the names for the default In-Editor Tutorials menu items.
    /// </summary>
    // TODO 2.0 Rename into something more concise
    public static class TutorialWindowMenuItem
    {
        /// <summary>
        /// Name of the menu.
        /// </summary>
        public const string Menu = "Tutorials"; // TODO Tr()
        /// <summary>
        /// Path for menu. Append menu item names to this string.
        /// </summary>
        public const string MenuPath = Menu + "/";
        /// <summary>
        /// The default menu items for showing the Tutorials.
        /// </summary>
        public const string Item = "Show Tutorials"; // TODO Tr()

        [MenuItem(MenuPath + Item)]
        static void OpenTutorialWindow()
        {
            UserStartupCode.ShowTutorialWindow();
        }
    }
}
