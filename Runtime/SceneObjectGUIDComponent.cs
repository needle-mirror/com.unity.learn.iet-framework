using System;
using UnityEngine;
using System.Linq;

namespace Unity.Tutorials.Core
{
    /// <summary>
    /// Generates a globally unique identifier (System.Guid) for a GameObject that has this component.
    /// </summary>
    /// <remarks>
    /// This component is removed from the GameObject when were are not in Editor Mode.
    /// </remarks>
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    // TODO 2.0 should be renamed to SceneObjectGuidComponent 
    public class SceneObjectGUIDComponent : MonoBehaviour
    {
        [SerializeField]
        string m_Id;
        [NonSerialized]
        bool m_Registered;

        /// <summary>
        /// The unique identifier.
        /// </summary>
        public string Id => m_Id;

        void Awake()
        {
            if (!Application.isEditor)
                Destroy(this);

            if (string.IsNullOrEmpty(m_Id))
            {
                m_Id = Guid.NewGuid().ToString();
            }
            else
            {
                var components = FindObjectsOfType<SceneObjectGUIDComponent>();
                if (components.Any(c => c.m_Id == m_Id && c != this))
                {
                    m_Id = Guid.NewGuid().ToString();
                }
            }
            hideFlags |= HideFlags.HideInInspector;
            Register();
        }

        private void Register()
        {
            if (m_Registered || !Application.isEditor)
                return;
            SceneObjectGUIDManager.Instance.Register(this);
            m_Registered = true;
        }

        void OnValidate()
        {
            // Register in OnValidate becuase Awake in not called on domain reload in edit mode
            Register();
        }

        void OnDestroy()
        {
            SceneObjectGUIDManager.Instance?.Unregister(this);
        }
    }
}
