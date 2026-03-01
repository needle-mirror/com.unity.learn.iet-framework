using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Tutorials.Editor
{
    /// <summary>
    /// PropertyDrawer for <see cref="CodeSampleBlock"/>.
    /// </summary>
    [CustomPropertyDrawer(typeof(CodeSampleBlock))]
    public class CodeSampleBlockDrawer : PropertyDrawer
    {
        /// <summary>
        /// Creates the VisualElement representing the code block, for usage in an Inspector.
        /// </summary>
        /// <param name="property">The SerializedProperty that will be drawn.</param>
        /// <returns>The VisualElement that represents the control.</returns>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            VisualElement textArea = UIUtils.CreateTextAreaElement(Localization.Tr(LocalizationKeys.k_TutorialPageCodeSample), property);
            ScrollView textAreaScrollView = textArea.Q<ScrollView>();
            textAreaScrollView.userData = false; // Represents the expanded/contracted state of the ScrollView (true => expanded);
            root.Add(textArea);

            Button autoFormatButton = new(() =>
            {
                property.stringValue = CodeSampleUtils.AsFormattedCode(property.stringValue);
                property.serializedObject.ApplyModifiedProperties();
            })
            {
                text = "Auto-Format",
                tooltip = "Automatically format the code sample.",
                style = { flexGrow = 1 }
            };

            Button expandContractButton = new()
            {
                tooltip = "Expands or contracts the editing area of the code sample.",
                style = { flexGrow = 1 }
            };
            expandContractButton.clicked += OnExpandContractBtn;
            OnExpandContractBtn();

            VisualElement buttonsRow = new()
            {
                style = { flexDirection = FlexDirection.Row }
            };

            buttonsRow.Add(autoFormatButton);
            buttonsRow.Add(expandContractButton);

            root.Add(buttonsRow);

            return root;

            void OnExpandContractBtn()
            {
                bool isExpanded = (bool)textAreaScrollView.userData;
                textAreaScrollView.style.maxHeight = isExpanded ? 500 : 110;
                expandContractButton.text = isExpanded ? "Contract" : "Expand";
                textAreaScrollView.userData = !isExpanded;
            }
        }
    }
}
