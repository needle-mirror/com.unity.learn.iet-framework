using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// Custom Editor for MaskingPreset ScriptableObjects.
    /// </summary>
    [CustomEditor(typeof(MaskingPreset)), CanEditMultipleObjects]
    public class MaskingPresetEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        private TutorialPage[] m_referencingPages;
        private MaskingPreset m_MaskingPreset;

        private const string k_UnmaskedViewsPropertyPath = "m_unmaskedViews";

        /// <summary>
        /// Creates the Inspector for this MaskingPreset.
        /// </summary>
        /// <returns>The VisualElement that represents the Inspector.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            m_MaskingPreset = (MaskingPreset)target;

            VisualElement inspector = new();
            inspector.styleSheets.Add(m_Stylesheet);

            // TODO: Figure out how to preview a specific mask, which is not hardcoded to the current tutorial
            // Button previewMaskingButton = new(OnPreviewMaskingButton)
            // {
            //     text = "Preview Masking",
            // };
            // inspector.Add(previewMaskingButton);
            //
            // void OnPreviewMaskingButton()
            // {
            // }

            SerializedProperty serializedProperty = serializedObject.FindProperty(k_UnmaskedViewsPropertyPath);
            UnmaskedViewsListView unmaskedViews = new(serializedProperty);
            inspector.Add(unmaskedViews);

            FindReferencingPages();
            ListView referencingPagesListView = new(m_referencingPages)
            {
                headerTitle = "Pages Referencing This",
                reorderable = false,
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                showFoldoutHeader = true,
                makeItem = MakeItem,
                bindItem = BindItem,
                showAlternatingRowBackgrounds = AlternatingRowBackground.None,
                dataSourceType = typeof(TutorialPage),
                fixedItemHeight = 22,
            };
            referencingPagesListView.AddToClassList("inspector-list");
            referencingPagesListView.Q<TextField>("unity-list-view__size-field").SetEnabled(false);
            inspector.Add(referencingPagesListView);

            return inspector;

            VisualElement MakeItem()
            {
                ObjectField objectField = new();
                objectField.AddToClassList("unity-base-field__aligned");
                return objectField;
            }

            void BindItem(VisualElement field, int index)
            {
                ObjectField objectField = (ObjectField)field;
                objectField.value = m_referencingPages[index];
                objectField.SetEnabled(false);
            }
        }

        private void FindReferencingPages()
        {
            m_referencingPages = AssetDatabase.FindAssets($"t:{nameof(TutorialPage)}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<TutorialPage>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(page => page.Paragraphs.Any(paragraph => paragraph.MaskingSettings?.MaskPreset == m_MaskingPreset))
                .ToArray();
        }
    }
}
