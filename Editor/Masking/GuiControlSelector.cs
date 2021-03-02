using System;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Used the select which GUI controls are unmasked.
    /// </summary>
    [Serializable]
    public class GuiControlSelector
    {
        /// <summary>
        /// Supported selector modes.
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Select by GUIContent.
            /// </summary>
            GuiContent,
            /// <summary>
            /// Select by Named Control's name (the name used for GUI.SetNextControlName()).
            /// </summary>
            NamedControl,
            /// <summary>
            /// Select by property' path name.
            /// </summary>
            Property,
            /// <summary>
            /// Select by GUIStyle's name.
            /// </summary>
            GuiStyleName,
            /// <summary>
            /// Match by the referenced Unity Object.
            /// </summary>
            ObjectReference,
        }

        /// <summary>
        /// The used selector mode.
        /// </summary>
        public Mode SelectorMode { get => m_SelectorMode; set => m_SelectorMode = value; }
        [SerializeField]
        Mode m_SelectorMode;

        /// <summary>
        /// Applicable if Mode.GuiContent used.
        /// </summary>
        public GUIContent GuiContent { get => new GUIContent(m_GUIContent); set => m_GUIContent = new GUIContent(value); }
        [SerializeField]
        GUIContent m_GUIContent = new GUIContent();

        /// <summary>
        /// Applicable if Mode.NamedControl used.
        /// </summary>
        public string ControlName { get => m_ControlName; set => m_ControlName = value ?? ""; }
        [SerializeField]
        string m_ControlName = "";

        /// <summary>
        /// Applicable if Mode.Property used.
        /// </summary>
        public string PropertyPath { get => m_PropertyPath; set => m_PropertyPath = value ?? ""; }
        [SerializeField]
        string m_PropertyPath = "";

        /// <summary>
        /// Applicable if Mode.Property used.
        /// </summary>
        public Type TargetType { get => m_TargetType.Type; set => m_TargetType.Type = value; }
        [SerializeField, SerializedTypeFilter(typeof(UnityObject), false)]
        SerializedType m_TargetType = new SerializedType(null);

        /// <summary>
        /// Applicable if Mode.GuiStyleName used.
        /// </summary>
        public string GuiStyleName { get => m_GUIStyleName; set => m_GUIStyleName = value; }
        [SerializeField]
        string m_GUIStyleName;

        /// <summary>
        /// A reference to a Unity Object of which name will be matched against the text in UI elements.
        /// Applicable if Mode.ObjectReference used.
        /// </summary>
        /// <remarks>
        /// In order for this to work for assets, the asset must have a short name, i.e.,
        /// the name cannot be visible in the UI in shortened form, e.g. "A longer...".
        /// </remarks>
        public ObjectReference ObjectReference { get => m_ObjectReference; set => m_ObjectReference = value; }
        [SerializeField]
        ObjectReference m_ObjectReference;
    }
}
