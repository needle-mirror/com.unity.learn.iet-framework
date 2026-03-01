using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomEditor(typeof(TutorialStyles))]
    internal class TutorialStylesEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        private readonly string[] k_PropertiesToHide = { "m_Script" };

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement rootElement = new();
            rootElement.styleSheets.Add(m_Stylesheet);

            // TODO : Update if related setting changes while the inspector is open.
            TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(rootElement);

            VisualElement spacer = new() {style = { height = 8}};
            rootElement.Add(spacer);

            UIUtils.DrawInspectorExcluding(rootElement, serializedObject, this, k_PropertiesToHide);

            return rootElement;
        }
    }
}
