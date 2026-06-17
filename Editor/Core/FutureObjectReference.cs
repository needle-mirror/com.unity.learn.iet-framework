using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Future Object Reference is a reference to a Unity Object that might not exist yet (prefab instance).
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class FutureObjectReference : ScriptableObject
    {
        [SerializeField] private SceneObjectReferenceHolder m_ReferenceHolder;

        [SerializeField] private Criterion m_Criterion;

        [SerializeField] private string m_ReferenceName;

        /// <summary>
        /// Returns the SceneObjectReferenceHolder for this FutureObjectReference.
        /// Creates the SceneObjectReferenceHolder instance if it does not exist.
        /// </summary>
        private SceneObjectReferenceHolder ReferenceHolder
        {
            get
            {
                if (m_ReferenceHolder == null)
                {
                    m_ReferenceHolder = CreateInstance<SceneObjectReferenceHolder>();
                    m_ReferenceHolder.hideFlags = HideFlags.HideAndDontSave;
                }

                return m_ReferenceHolder;
            }
        }

        /// <summary>
        /// The SceneObjectReference of this FutureObjectReference.
        /// </summary>
        public SceneObjectReference SceneObjectReference
        {
            get
            {
                if (ReferenceHolder.SceneObjectReference == null)
                    ReferenceHolder.SceneObjectReference = new SceneObjectReference();

                return ReferenceHolder.SceneObjectReference;
            }
            set => ReferenceHolder.SceneObjectReference = value;
        }

        /// <summary>
        /// The Criterion this FutureObjectReference belongs to.
        /// </summary>
        public Criterion Criterion { get => m_Criterion; set => m_Criterion = value; }

        /// <summary>
        /// The name used to refer the Unity Object.
        /// </summary>
        public string ReferenceName { get => m_ReferenceName; set => m_ReferenceName = value; }

        private void OnDestroy()
        {
            if (m_ReferenceHolder != null)
                DestroyImmediate(m_ReferenceHolder);
        }
    }

    /// <summary>
    /// SceneObjectReference holder.
    /// </summary>
    public class SceneObjectReferenceHolder : ScriptableObject
    {
        /// <summary>
        /// The ScenObjectReference.
        /// </summary>
        public SceneObjectReference SceneObjectReference { get => m_SceneObjectReference; set => m_SceneObjectReference = value; }
        [SerializeField] private SceneObjectReference m_SceneObjectReference;
    }
}
