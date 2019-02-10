using UnityEditor;

namespace Unity.InteractiveTutorials
{
    public static class TutorialWindowMenuItem
    {
        public const string Menu = "Tutorials/";

        [MenuItem(Menu + "Open Walkthroughs")]
        static void OpenTutorialWindow()
        {
            TutorialWindow.CreateWindow();
        }
    }
}
