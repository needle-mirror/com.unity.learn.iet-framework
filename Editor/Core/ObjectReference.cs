using System;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Used to refer Unity Objects in different Criterion implementations.
    /// </summary>
    [Serializable]
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class ObjectReference
    {
        [SerializeField] private SceneObjectReference m_SceneObjectReference;

        [SerializeField] private FutureObjectReference m_FutureObjectReference;

        /// <summary>
        /// Is this ObjectReference a FutureObjectReference instead of a SceneObjectReference;.
        /// </summary>
        public bool IsFutureReference => m_FutureObjectReference != null;

        /// <summary>
        /// The SceneObjectReference.
        /// </summary>
        public SceneObjectReference SceneObjectReference
        {
            get
            {
                if (IsFutureReference)
                    return m_FutureObjectReference.SceneObjectReference;
                return m_SceneObjectReference ?? (m_SceneObjectReference = new SceneObjectReference());
            }
            set
            {
                if (!IsFutureReference)
                    m_SceneObjectReference = value;
            }
        }
    }
}
