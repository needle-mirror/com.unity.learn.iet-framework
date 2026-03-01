using UnityEngine;

namespace Unity.Tutorials
{
    internal static class GameObjectProxy
    {
        public static Bounds CalculateBounds(GameObject gameObject)
        {
            return gameObject.CalculateBounds();
        }
    }
}
