using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Tutorials.Editor
{
    [Serializable]
    internal class MaskingSettings
    {
        public bool Enabled { get => m_MaskingEnabled; set => m_MaskingEnabled = value; }

        [SerializeField, FormerlySerializedAs("m_Enabled")]
        private bool m_MaskingEnabled;

        internal List<UnmaskedView> UnmaskedViews
        {
            get => m_UnmaskedViews;
            set => m_UnmaskedViews = value;
        }

        [SerializeField]
        private List<UnmaskedView> m_UnmaskedViews = new();

        [SerializeField] internal MaskingPreset MaskPreset;

        /// <summary>
        /// Copies a MaskingSetting into the other, but copying its m_MaskingEnabled property,
        /// and duplicating the UnmaskedView List.
        /// </summary>
        internal void CopySettingsFrom(MaskingSettings sourceSettings)
        {
            m_MaskingEnabled = sourceSettings.Enabled;

            m_UnmaskedViews = new List<UnmaskedView>();
            foreach (UnmaskedView unmaskedView in sourceSettings.UnmaskedViews)
            {
                m_UnmaskedViews.Add(unmaskedView);
            }
        }
    }
}
