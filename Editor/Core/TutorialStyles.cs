using UnityEditor;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Controls masking and highlighting styles, and style sheets for the tutorials.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "Unity.Tutorials.Core.Editor", sourceAssembly: "Unity.Tutorials.Core.Editor")]
    public class TutorialStyles : ScriptableObject
    {
        /// <summary>
        /// Color of the masking overlay.
        /// </summary>
        public Color MaskingColor => m_MaskingColor;
        [Header("Masking and Highlighting")]
        [SerializeField]
        private Color m_MaskingColor = new Color32(0, 40, 53, 204);

        /// <summary>
        /// Color of the highlight border.
        /// </summary>
        public Color HighlightColor => m_HighlightColor;
        [SerializeField] private Color m_HighlightColor = new Color32(0, 198, 223, 255);

        /// <summary>
        /// Color of the blocked interaction overlay.
        /// </summary>
        public Color BlockedInteractionColor => m_BlockedInteractionColor;
        [SerializeField] private Color m_BlockedInteractionColor = new(1, 1, 1, 0.5f);

        /// <summary>
        /// Thickness of the highlight border in pixels.
        /// </summary>
        public float HighlightThickness => m_HighlightThickness;
        [SerializeField, Range(0f, 10f)] private float m_HighlightThickness = 3f;

        [SerializeField, Range(0f, 10f)] private float m_HighlightAnimationSpeed = 1.5f;

        [SerializeField, Range(0f, 10f)] private float m_HighlightAnimationDelay = 5f;

        /// <summary>
        /// Used when the Personal Editor Theme is chosen.
        /// </summary>
        [Header("Style Sheets")]
        [Tooltip("Used when the Personal Editor Theme is chosen.")]
        public StyleSheet LightThemeStyleSheet;

        /// <summary>
        /// Used when the Professional Editor Theme is chosen.
        /// </summary>
        [Tooltip("Used when the Professional Editor Theme is chosen.")]
        public StyleSheet DarkThemeStyleSheet;

        private StyleSheet m_LastCommonStyleSheet;
        private StyleSheet m_LastCustomStyleSheet;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            Apply();
        }

        private void Apply()
        {
            MaskingManager.HighlightAnimationSpeed = m_HighlightAnimationSpeed;
            MaskingManager.HighlightAnimationDelay = m_HighlightAnimationDelay;
        }

        /// <summary>
        /// Add the DarkThemeStyleSheet or LightThemeStyleSheet (based on editor theme) to the given VisualElement.
        /// </summary>
        /// <param name="target">The VisualElement on which to set the right Stylesheet</param>
        public void AddCustomStyleSheet(VisualElement target)
        {
            if (m_LastCustomStyleSheet != null)
                target.styleSheets.Remove(m_LastCustomStyleSheet);

            m_LastCustomStyleSheet = EditorGUIUtility.isProSkin ? DarkThemeStyleSheet : LightThemeStyleSheet;
            if (m_LastCustomStyleSheet != null)
            {
                target.styleSheets.Add(m_LastCustomStyleSheet);
            }
        }
    }
}
