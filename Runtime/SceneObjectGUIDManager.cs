using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Tutorials.Core
{
    /// <summary>
    /// Manages SceneObjectGUIDComponents.
    /// </summary>
    // TODO 2.0 should be renamed to SceneObjectGuidManager
    public class SceneObjectGUIDManager
    {
        static SceneObjectGUIDManager m_Instance;
        Dictionary<string, SceneObjectGUIDComponent> m_Components = new Dictionary<string, SceneObjectGUIDComponent>();

        /// <summary>
        /// Returns the singleton instance.
        /// </summary>
        public static SceneObjectGUIDManager Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new SceneObjectGUIDManager();
                }
                return m_Instance;
            }
            private set { m_Instance = value; }
        }

        /// <summary>
        /// Registers a GUID component.
        /// </summary>
        /// <param name="component"></param>
        public void Register(SceneObjectGUIDComponent component)
        {
            Assert.IsFalse(string.IsNullOrEmpty(component.Id));
            //Add will trow an exception if the id is already registered
            m_Components.Add(component.Id, component);
        }

        /// <summary>
        /// Does the manager contain a Component for specific GUID.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool Contains(string id)
        {
            return m_Components.ContainsKey(id);
        }

        /// <summary>
        /// Unregisters a GUID Component.
        /// </summary>
        /// <param name="component"></param>
        /// <returns>True if the Component was found and unregistered, false otherwise.</returns>
        public bool Unregister(SceneObjectGUIDComponent component)
        {
            return m_Components.Remove(component.Id);
        }

        /// <summary>
        /// Returns the GUID Component for a specific GUID, if found.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public SceneObjectGUIDComponent GetComponent(string id)
        {
            if (m_Components.TryGetValue(id, out SceneObjectGUIDComponent value))
            {
                return value;
            }
            return null;
        }
    }
}
