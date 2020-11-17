using UnityEditor;

namespace Unity.Tutorials.Core.Editor
{
    internal class GameViewProxy : EditorWindow
    {
        public static bool maximizeOnPlay
        {
            get { return GetWindow<GameView>().maximizeOnPlay; }
            set { GetWindow<GameView>().maximizeOnPlay = value; }
        }
    }
}
