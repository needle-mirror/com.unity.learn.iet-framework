using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Core.Editor
{
    /// <summary>
    /// Controls masking and highlighting styles, and style sheets for the tutorials.
    /// </summary>
    public class TutorialStyles : ScriptableObject
    {
        /// <summary>
        /// Color of the masking overlay.
        /// </summary>
        public Color MaskingColor => m_MaskingColor;
        [Header("Masking and Highlighting")]
        [SerializeField]
        Color m_MaskingColor = new Color32(0, 40, 53, 204);

        /// <summary>
        /// Color of the highlight border.
        /// </summary>
        public Color HighlightColor => m_HighlightColor;
        [SerializeField]
        Color m_HighlightColor = new Color32(0, 198, 223, 255);

        /// <summary>
        /// Color of the blocked interaction overlay.
        /// </summary>
        public Color BlockedInteractionColor => m_BlockedInteractionColor;
        [SerializeField]
        Color m_BlockedInteractionColor = new Color(1, 1, 1, 0.5f);

        /// <summary>
        /// Thickness of the highlight border in pixels.
        /// </summary>
        public float HighlightThickness => m_HighlightThickness;
        [SerializeField, Range(0f, 10f)]
        float m_HighlightThickness = 3f;

        [SerializeField, Range(0f, 10f)]
        float m_HighlightAnimationSpeed = 1.5f;

        [SerializeField, Range(0f, 10f)]
        float m_HighlightAnimationDelay = 5f;

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

        StyleSheet m_LastCommonStyleSheet;

        /// <summary>
        /// The default style sheet file used when the Personal Editor Theme is chosen.
        /// </summary>
        public static readonly string DefaultLightStyleFile = $"{TutorialWindow.k_UIAssetPath}/Main_Light.uss";

        /// <summary>
        /// The default style sheet file used when the Professional Editor Theme is chosen.
        /// </summary>
        public static readonly string DefaultDarkStyleFile = $"{TutorialWindow.k_UIAssetPath}/Main_Dark.uss";

        #region TODO Will be deprecated and deleted
        /// <summary>
        /// Deprecated.
        /// </summary>
        public string OrderedListDelimiter => m_OrderedListDelimiter;
        [SerializeField, HideInInspector]
        string m_OrderedListDelimiter = ".";
        /// <summary>
        /// Deprecated.
        /// </summary>
        public string UnorderedListBullet => m_UnorderedListBullet;
        [SerializeField, HideInInspector]
        string m_UnorderedListBullet = "\u2022";
        #endregion

        void OnEnable()
        {
            Apply();
        }

        void OnValidate()
        {
            Apply();
        }

        void Apply()
        {
            MaskingManager.HighlightAnimationSpeed = m_HighlightAnimationSpeed;
            MaskingManager.HighlightAnimationDelay = m_HighlightAnimationDelay;
            if (LightThemeStyleSheet == null)
            {
                LightThemeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(DefaultLightStyleFile);
            }
            if (DarkThemeStyleSheet == null)
            {
                DarkThemeStyleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(DefaultDarkStyleFile);
            }
        }

        /// <summary>
        /// Applies a Theme-specific style to a VisualElement, removing all other styles except the base one
        /// </summary>
        /// <param name="target">VisualElement to which the style should apply (usually, you want to do this to the root)</param>
        public void ApplyThemeStyleSheetTo(VisualElement target)
        {
            //preserve the base style, remove all styles defined in UXML and apply new skin
            StyleSheet baseStyle = target.styleSheets[0];
            target.styleSheets.Clear();
            target.styleSheets.Add(baseStyle);
            AddThemeStyleTo(target);
        }

        /// <summary>
        /// Adds a Theme-specific style to a VisualElement
        /// </summary>
        /// <param name="target">VisualElement to which the style should be added (usually, you want to do this to the root)</param>
        void AddThemeStyleTo(VisualElement target)
        {
            m_LastCommonStyleSheet = EditorGUIUtility.isProSkin ? DarkThemeStyleSheet : LightThemeStyleSheet;
            if (!m_LastCommonStyleSheet) { return; }
            target.styleSheets.Add(m_LastCommonStyleSheet);
        }
    }
}
