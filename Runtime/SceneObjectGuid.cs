using System;
using UnityEngine;

namespace Unity.Tutorials
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
    public class SceneObjectGuid : MonoBehaviour
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
                SceneObjectGuid[] components = RuntimeFindObjectUtils.FindObjectsByType<SceneObjectGuid>();
                foreach (SceneObjectGuid component in components)
                {
                    if (component.m_Id != m_Id || component == this)
                        continue;

                    m_Id = Guid.NewGuid().ToString();
                    break;
                }
            }
            hideFlags |= HideFlags.HideInInspector;
            Register();
        }

        private void Register()
        {
            if (m_Registered || !Application.isEditor)
                return;
            SceneObjectGuidManager.Instance.Register(this);
            m_Registered = true;
        }

        void OnValidate()
        {
            // Register in OnValidate because Awake in not called on domain reload in edit mode
            Register();
        }

        void OnDestroy()
        {
            SceneObjectGuidManager.Instance?.Unregister(this);
        }
    }
}
