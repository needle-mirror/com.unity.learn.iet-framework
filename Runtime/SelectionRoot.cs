using UnityEngine;

namespace Unity.Tutorials.Core
{
    /// <summary>
    /// Add this component to a GameObject to make it as a the selection base object for Scene View picking.
    /// </summary>
    // TODO 2.0 unused, delete?
    [SelectionBase, ExecuteInEditMode]
    public class SelectionRoot : MonoBehaviour
    {
        void Update()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }
    }
}
