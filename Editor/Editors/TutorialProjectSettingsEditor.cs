using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    [CustomEditor(typeof(TutorialProjectSettings))]
    internal class TutorialProjectSettingsEditor : UnityEditor.Editor
    {
        [SerializeField] private StyleSheet m_Stylesheet;

        private readonly string[] k_PropertiesToHide = { "m_Script" };

        private TutorialProjectSettings Target => (TutorialProjectSettings)target;

        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            root.styleSheets.Add(m_Stylesheet);

            // TODO : Update if related setting changes while the inspector is open.
            TutorialProjectSettings.DrawDefaultAssetRestoreWarningElement(root);

            Button runStartupCodeBtn = new(OnRunStartupCodeBtnClicked)
            {
                text = Localization.Tr(LocalizationKeys.k_ButtonRunStartupCode)
            };
            runStartupCodeBtn.AddToClassList("button-md");
            root.Add(runStartupCodeBtn);

            Button showTutorialsBtn = new(OnShowTutorialsBtnClicked)
            {
                text = Localization.Tr(MenuItems.ShowTutorials)
            };
            showTutorialsBtn.AddToClassList("button-md");
            root.Add(showTutorialsBtn);

            VisualElement spacer = new() {style = { height = 8}};
            root.Add(spacer);

            UIUtils.DrawInspectorExcluding(root, serializedObject, this, k_PropertiesToHide);

            return root;

            void OnShowTutorialsBtnClicked()
            {
                TutorialWindow.GetOrCreateWindowNextToInspector();
            }

            void OnRunStartupCodeBtnClicked()
            {
                TutorialWindow.Instance.Broadcast(new TutorialQuitEvent());
                UserStartupCode.RunStartupCode(Target);
            }
        }
    }
}
