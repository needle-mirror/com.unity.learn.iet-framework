using System;
using UnityEngine;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Used to serialize System.Type.
    /// </summary>
    [Serializable]
    public class SerializedType : ISerializationCallbackReceiver
    {
        [SerializeField]
        string m_TypeName;

        /// <summary>
        /// The Type.
        /// </summary>
        public Type Type
        {
            get { return string.IsNullOrEmpty(m_TypeName) ? null : Type.GetType(m_TypeName); }
            set { m_TypeName = value == null ? "" : value.AssemblyQualifiedName; }
        }

        /// <summary>
        /// Constructs with a type.
        /// </summary>
        /// <param name="type"></param>
        public SerializedType(Type type)
        {
            this.Type = type;
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnBeforeSerialize()
        {
        }

        /// <summary>
        /// UnityEngine.ISerializationCallbackReceiver override, do not call.
        /// </summary>
        public void OnAfterDeserialize()
        {
            // TODO what's this? Is this needed?
            // Remove "-testable" suffix from assembly names
            if (!ProjectMode.IsAuthoringMode() && m_TypeName != null)
            {
                m_TypeName = m_TypeName.Replace("Assembly-CSharp-Editor-firstpass-testable", "Assembly-CSharp-Editor-firstpass");
                m_TypeName = m_TypeName.Replace("Assembly-CSharp-Editor-testable", "Assembly-CSharp-Editor");
                m_TypeName = m_TypeName.Replace("Assembly-CSharp-firstpass-testable", "Assembly-CSharp-firstpass");
                m_TypeName = m_TypeName.Replace("Assembly-CSharp-testable", "Assembly-CSharp");
            }

            // Backwards-compatibility for IET < 2.0 when the namespace and assemblies were Unity.InteractiveTutorials.*
            // instead of Unity.Tutorials.Core(.Editor).
            if (m_TypeName.IsNotNullOrEmpty())
            {
                m_TypeName = m_TypeName.Replace("Unity.InteractiveTutorials.Core", "Unity.Tutorials.Core.Editor");
                m_TypeName = m_TypeName.Replace("Unity.InteractiveTutorials", "Unity.Tutorials.Core.Editor");
            }
        }
    }
}
