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
        private TutorialPage[] m_referencingPages;
        private MaskingPreset m_MaskingPreset;

        /// <summary>
        /// Creates the Inspector for this MaskingPreset.
        /// </summary>
        /// <returns>The VisualElement that represents the Inspector.</returns>
        public override VisualElement CreateInspectorGUI()
        {
            m_MaskingPreset = (MaskingPreset)target;

            VisualElement inspector = new();

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

            InspectorElement.FillDefaultInspector(inspector, serializedObject, this);

            FindReferencingPages();
            ListView referencingPages = new(m_referencingPages)
            {
                reorderable = false,
                reorderMode = ListViewReorderMode.Animated,
                showBorder = true,
                showFoldoutHeader = true,
                makeHeader = MakeHeader,
                makeItem = MakeItem,
                bindItem = BindItem,
                showAlternatingRowBackgrounds = AlternatingRowBackground.All,
                dataSourceType = typeof(TutorialPage),
                fixedItemHeight = 22,
                style = { marginTop = 5 }
            };

            inspector.Add(referencingPages);
            return inspector;

            VisualElement MakeHeader()
            {
                return new Label("Pages Referencing This")
                    { style = { unityFontStyleAndWeight = FontStyle.Bold, height = 22 } };
            }

            VisualElement MakeItem()
            {
                ObjectField objectField = new()
                {
                    style =
                    {
                        paddingLeft = 9,
                        paddingRight = 3,
                        paddingTop = 2,
                        paddingBottom = 2,
                        marginRight = 2,
                    }
                };
                objectField.AddToClassList("unity-base-field__aligned");
                return objectField;
            }

            void BindItem(VisualElement field, int index)
            {
                ObjectField objectField = (ObjectField)field;
                objectField.value = m_referencingPages[index];
                objectField.label = m_referencingPages[index].name;
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
