using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Contains the names for the menu items used by the In-Editor Tutorials packages.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public static class MenuItems
    {
        /// <summary>
        /// Name of the main menu used by the package.
        /// </summary>
        public const string Menu = "Tutorials";
        /// <summary>
        /// Path for menu. Append menu item names to this string.
        /// </summary>
        private const string MenuPath = Menu + "/";
        /// <summary>
        /// The default menu item for showing the tutorials in the project.
        /// </summary>
        public const string ShowTutorials = "Show Tutorials Window";
        /// <summary>
        /// Menu path for the authoring submenu.
        /// </summary>
        public const string AuthoringMenuPath = Menu + "/Authoring/";

        [MenuItem(MenuPath + "Welcome Dialog")]
        private static void OpenWelcomeDialog()
        {
            TutorialWelcomePage welcomePage = TutorialProjectSettings.Instance.WelcomePage;
            if (welcomePage != null)
                TutorialModalWindow.Show(welcomePage);
            else
                Debug.LogError("No Welcome Page set in Tutorial Project Settings.");
        }

        [MenuItem(MenuPath + ShowTutorials)]
        private static void OpenTutorialWindow()
        {
            if (TutorialWindow.ShowWindow(false) == null)
            {
                TutorialWindow.GetOrCreateWindowNextToInspector();
            }
        }
    }
}
