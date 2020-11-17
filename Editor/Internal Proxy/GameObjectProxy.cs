using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    internal static class GameObjectProxy
    {
        public static Bounds CalculateBounds(GameObject gameObject)
        {
            return gameObject.CalculateBounds();
        }
    }
}
